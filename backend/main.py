from fastapi import FastAPI, HTTPException, Query, Header
from fastapi.middleware.cors import CORSMiddleware

import os
import base64
import hashlib
import hmac
import secrets
import json

from contextlib import asynccontextmanager
from datetime import datetime, timezone
from typing import Optional

import psycopg
from psycopg_pool import ConnectionPool
from dotenv import load_dotenv

from google.oauth2 import id_token as google_id_token
from google.auth.transport import requests as google_requests

from models.auth import RegisterRequest, LoginRequest, GoogleLoginRequest
from models.transaction import TransactionCreate


# =====================================================
# ENVIRONMENT
# =====================================================

load_dotenv()

DATABASE_URL = os.getenv("DATABASE_URL")

# Used to create a simple signed login token.
# Add this to .env later for production.
AUTH_SECRET = os.getenv(
    "AUTH_SECRET",
    "finance-dashboard-secret-change-this"
)

# The OAuth Client ID from Google Cloud Console (Credentials page).
# "Sign in with Google" is disabled until this is set.
GOOGLE_CLIENT_ID = os.getenv("GOOGLE_CLIENT_ID")


# =====================================================
# DATABASE CONNECTION POOL
# =====================================================
#
# A fresh psycopg.connect() per request pays a full TCP+TLS
# handshake to the remote database on every single API call
# (multiple seconds against a remote host like Neon). Keeping
# a small pool of already-open connections avoids that cost.

connection_pool: Optional[ConnectionPool] = None


def get_connection():

    if not DATABASE_URL:
        raise HTTPException(
            status_code=503,
            detail="DATABASE_URL is not configured."
        )

    if connection_pool is None:
        raise HTTPException(
            status_code=503,
            detail="Database is not ready yet."
        )

    return connection_pool.connection()


# =====================================================
# DATABASE INITIALIZATION
# =====================================================

def initialize_database() -> None:

    if not DATABASE_URL:
        print("WARNING: DATABASE_URL is not configured.")
        return

    try:

        with psycopg.connect(
            DATABASE_URL,
            connect_timeout=5
        ) as connection:

            with connection.cursor() as cursor:

                # =================================================
                # USERS
                # =================================================

                cursor.execute(
                    """
                    CREATE TABLE IF NOT EXISTS users (
                        id BIGSERIAL PRIMARY KEY,

                        username TEXT NOT NULL,

                        email TEXT NOT NULL UNIQUE,

                        password_hash TEXT NOT NULL,

                        created_at TIMESTAMPTZ NOT NULL
                            DEFAULT NOW(),

                        updated_at TIMESTAMPTZ NOT NULL
                            DEFAULT NOW()
                    )
                    """
                )

                # =================================================
                # TRANSACTIONS
                # =================================================

                cursor.execute(
                    """
                    CREATE TABLE IF NOT EXISTS transactions (
                        id BIGSERIAL PRIMARY KEY,

                        user_id BIGINT,

                        type TEXT NOT NULL
                            CHECK (type IN ('income', 'expense')),

                        category TEXT NOT NULL,

                        amount NUMERIC(12, 2) NOT NULL
                            CHECK (amount > 0),

                        transaction_date DATE NOT NULL,

                        created_at TIMESTAMPTZ NOT NULL
                            DEFAULT NOW(),

                        CONSTRAINT fk_transactions_user
                            FOREIGN KEY (user_id)
                            REFERENCES users(id)
                            ON DELETE CASCADE
                    )
                    """
                )

                # =================================================
                # ADD USER_ID TO OLD TRANSACTIONS TABLE
                # =================================================

                cursor.execute(
                    """
                    ALTER TABLE transactions
                    ADD COLUMN IF NOT EXISTS user_id BIGINT
                    """
                )

                # Add foreign key only if it does not already exist.
                cursor.execute(
                    """
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname =
                                'fk_transactions_user'
                        ) THEN

                            ALTER TABLE transactions
                            ADD CONSTRAINT
                                fk_transactions_user
                            FOREIGN KEY (user_id)
                            REFERENCES users(id)
                            ON DELETE CASCADE;

                        END IF;
                    END
                    $$;
                    """
                )

                # =================================================
                # USERNAME UNIQUE
                # =================================================

                cursor.execute(
                    """
                    CREATE UNIQUE INDEX IF NOT EXISTS
                    users_username_unique
                    ON users (LOWER(username))
                    """
                )

                # =================================================
                # GOOGLE SIGN-IN SUPPORT
                # =================================================
                #
                # Google accounts have no password of ours to store,
                # and are matched by Google's own stable subject id.

                cursor.execute(
                    """
                    ALTER TABLE users
                    ALTER COLUMN password_hash DROP NOT NULL
                    """
                )

                cursor.execute(
                    """
                    ALTER TABLE users
                    ADD COLUMN IF NOT EXISTS google_sub TEXT
                    """
                )

                cursor.execute(
                    """
                    CREATE UNIQUE INDEX IF NOT EXISTS
                    users_google_sub_unique
                    ON users (google_sub)
                    WHERE google_sub IS NOT NULL
                    """
                )

            connection.commit()

        print("Database initialized successfully.")

    except psycopg.Error as error:

        print(
            f"Database initialization failed: {error}"
        )


# =====================================================
# PASSWORD HASHING
# =====================================================

def hash_password(password: str) -> str:

    salt = secrets.token_bytes(16)

    digest = hashlib.scrypt(
        password.encode("utf-8"),
        salt=salt,
        n=16384,
        r=8,
        p=1
    )

    encoded = base64.urlsafe_b64encode(
        salt + digest
    ).decode("ascii")

    return "scrypt$" + encoded


def verify_password(
    password: str,
    stored_hash: str
) -> bool:

    try:

        algorithm, encoded = stored_hash.split(
            "$",
            1
        )

        if algorithm != "scrypt":
            return False

        decoded = base64.urlsafe_b64decode(
            encoded.encode("ascii")
        )

        salt = decoded[:16]
        expected_digest = decoded[16:]

        actual_digest = hashlib.scrypt(
            password.encode("utf-8"),
            salt=salt,
            n=16384,
            r=8,
            p=1
        )

        return hmac.compare_digest(
            actual_digest,
            expected_digest
        )

    except Exception:
        return False


# =====================================================
# TOKEN
# =====================================================

def create_token(user_id: int) -> str:

    payload = {
        "user_id": user_id,
        "created_at": datetime.now(
            timezone.utc
        ).isoformat()
    }

    payload_json = json.dumps(
        payload,
        separators=(",", ":")
    )

    payload_encoded = base64.urlsafe_b64encode(
        payload_json.encode("utf-8")
    ).decode("ascii")

    signature = hmac.new(
        AUTH_SECRET.encode("utf-8"),
        payload_encoded.encode("utf-8"),
        hashlib.sha256
    ).hexdigest()

    return f"{payload_encoded}.{signature}"


def get_user_from_token(
    authorization: Optional[str]
) -> int:

    if not authorization:
        raise HTTPException(
            status_code=401,
            detail="Authentication required."
        )

    if not authorization.startswith("Bearer "):
        raise HTTPException(
            status_code=401,
            detail="Invalid authentication token."
        )

    token = authorization[7:].strip()

    try:

        payload_encoded, signature = token.split(
            ".",
            1
        )

        expected_signature = hmac.new(
            AUTH_SECRET.encode("utf-8"),
            payload_encoded.encode("utf-8"),
            hashlib.sha256
        ).hexdigest()

        if not hmac.compare_digest(
            signature,
            expected_signature
        ):
            raise HTTPException(
                status_code=401,
                detail="Invalid authentication token."
            )

        payload_json = base64.urlsafe_b64decode(
            payload_encoded.encode("ascii")
        ).decode("utf-8")

        payload = json.loads(payload_json)

        user_id = int(payload["user_id"])

        return user_id

    except HTTPException:
        raise

    except Exception:

        raise HTTPException(
            status_code=401,
            detail="Invalid authentication token."
        )


# =====================================================
# FASTAPI LIFESPAN
# =====================================================

@asynccontextmanager
async def lifespan(app: FastAPI):

    global connection_pool

    initialize_database()

    if DATABASE_URL:

        connection_pool = ConnectionPool(
            DATABASE_URL,
            min_size=1,
            max_size=10,
            kwargs={
                "connect_timeout": 5,
                # Avoids an implicit BEGIN on every query and the
                # extra rollback round-trip the pool otherwise has
                # to make when a connection is returned to it.
                "autocommit": True
            },
            open=True
        )

    print("Finance Dashboard API started.")

    yield

    if connection_pool:
        connection_pool.close()

    print("Finance Dashboard API stopped.")


# =====================================================
# FASTAPI APP
# =====================================================

app = FastAPI(
    title="Finance Dashboard API",
    lifespan=lifespan
)


# =====================================================
# CORS
# =====================================================

app.add_middleware(
    CORSMiddleware,

    allow_origins=[
        "http://localhost:4200",
        "http://127.0.0.1:4200"
    ],

    allow_credentials=True,

    allow_methods=["*"],

    allow_headers=["*"]
)


# =====================================================
# HOME
# =====================================================

@app.get("/")
def home():

    return {
        "message": "Finance Dashboard API is running"
    }


# =====================================================
# HEALTH
# =====================================================

@app.get("/api/health")
def health():

    if not DATABASE_URL:

        return {
            "status": "ok",
            "database": "not configured"
        }

    try:

        with get_connection() as connection:

            with connection.cursor() as cursor:

                cursor.execute("SELECT 1")

        return {
            "status": "ok",
            "database": "connected"
        }

    except psycopg.Error:

        raise HTTPException(
            status_code=503,
            detail="Database connection failed"
        )


# =====================================================
# UNIQUE USERNAME GENERATION
# =====================================================
#
# Used for Google sign-ins, where we invent a username from
# the person's name/email rather than asking them for one.

def generate_unique_username(
    cursor,
    base: str
) -> str:

    cleaned = "".join(
        ch for ch in base
        if ch.isalnum()
    ) or "user"

    candidate = cleaned

    suffix = 1

    while True:

        cursor.execute(
            """
            SELECT id
            FROM users
            WHERE LOWER(username) = LOWER(%s)
            LIMIT 1
            """,
            (candidate,)
        )

        if not cursor.fetchone():
            return candidate

        suffix += 1
        candidate = f"{cleaned}{suffix}"


# =====================================================
# REGISTER
# =====================================================

@app.post("/api/register")
def register(
    request: RegisterRequest
):

    username = request.username.strip()
    email = request.email.strip().lower()

    if not username:
        raise HTTPException(
            status_code=400,
            detail="Username is required."
        )

    try:

        with get_connection() as connection:

            with connection.cursor() as cursor:

                # Check username
                cursor.execute(
                    """
                    SELECT id
                    FROM users
                    WHERE LOWER(username) = LOWER(%s)
                    LIMIT 1
                    """,
                    (username,)
                )

                if cursor.fetchone():

                    raise HTTPException(
                        status_code=409,
                        detail="Username already exists."
                    )

                # Check email
                cursor.execute(
                    """
                    SELECT id
                    FROM users
                    WHERE LOWER(email) = LOWER(%s)
                    LIMIT 1
                    """,
                    (email,)
                )

                if cursor.fetchone():

                    raise HTTPException(
                        status_code=409,
                        detail="Email already exists."
                    )

                # Create user
                cursor.execute(
                    """
                    INSERT INTO users (
                        username,
                        email,
                        password_hash
                    )
                    VALUES (%s, %s, %s)
                    RETURNING
                        id,
                        username,
                        email
                    """,
                    (
                        username,
                        email,
                        hash_password(request.password)
                    )
                )

                user = cursor.fetchone()

            connection.commit()

        return {
            "success": True,
            "message": "Account created successfully.",
            "id": user[0],
            "username": user[1],
            "email": user[2]
        }

    except HTTPException:
        raise

    except psycopg.Error as error:

        raise HTTPException(
            status_code=503,
            detail="Could not create account."
        ) from error


# =====================================================
# LOGIN
# =====================================================

@app.post("/api/login")
@app.post("/api/auth/login")
def login(
    request: LoginRequest
):

    username = (
        request.username.strip()
        if request.username
        else ""
    )

    email = (
        request.email.strip().lower()
        if request.email
        else ""
    )

    if not username and not email:

        raise HTTPException(
            status_code=400,
            detail="Username or email is required."
        )

    try:

        with get_connection() as connection:

            with connection.cursor() as cursor:

                if email:

                    cursor.execute(
                        """
                        SELECT
                            id,
                            username,
                            email,
                            password_hash
                        FROM users
                        WHERE LOWER(email) = LOWER(%s)
                        LIMIT 1
                        """,
                        (email,)
                    )

                else:

                    cursor.execute(
                        """
                        SELECT
                            id,
                            username,
                            email,
                            password_hash
                        FROM users
                        WHERE LOWER(username) = LOWER(%s)
                        LIMIT 1
                        """,
                        (username,)
                    )

                user = cursor.fetchone()

                if not user:

                    raise HTTPException(
                        status_code=401,
                        detail="Invalid username/email or password."
                    )

                user_id = user[0]
                stored_username = user[1]
                stored_email = user[2]
                stored_password_hash = user[3]

                if not verify_password(
                    request.password,
                    stored_password_hash
                ):

                    raise HTTPException(
                        status_code=401,
                        detail="Invalid username/email or password."
                    )

                token = create_token(user_id)

            connection.commit()

        return {
            "success": True,
            "message": "Login successful.",
            "id": user_id,
            "username": stored_username,
            "email": stored_email,
            "token": token
        }

    except HTTPException:
        raise

    except psycopg.Error as error:

        raise HTTPException(
            status_code=503,
            detail="Database connection failed."
        ) from error


# =====================================================
# GOOGLE LOGIN
# =====================================================

@app.post("/api/auth/google")
def google_login(
    request: GoogleLoginRequest
):

    if not GOOGLE_CLIENT_ID:

        raise HTTPException(
            status_code=503,
            detail="Google sign-in is not configured."
        )

    try:

        payload = google_id_token.verify_oauth2_token(
            request.credential,
            google_requests.Request(),
            GOOGLE_CLIENT_ID,
            # Google ID tokens are only valid ~5 minutes past issuance
            # for clock-skew purposes; a slow dev machine clock or a
            # slightly stale credential is a common false failure.
            clock_skew_in_seconds=10
        )

    except ValueError as error:

        # The library folds every failure reason (expired token, bad
        # signature, audience mismatch, network error fetching Google's
        # public keys, ...) into ValueError with no other signal, so
        # log the real message to figure out which one this actually is.
        print(f"Google token verification failed: {error}")

        raise HTTPException(
            status_code=401,
            detail="Invalid Google credential."
        )

    if not payload.get("email_verified", False):

        raise HTTPException(
            status_code=401,
            detail="Google account email is not verified."
        )

    google_sub = payload["sub"]
    email = payload["email"].strip().lower()
    name = payload.get("name") or email.split("@")[0]

    try:

        with get_connection() as connection:

            with connection.cursor() as cursor:

                # Already linked to this Google account?
                cursor.execute(
                    """
                    SELECT id, username, email
                    FROM users
                    WHERE google_sub = %s
                    LIMIT 1
                    """,
                    (google_sub,)
                )
                user = cursor.fetchone()

                if not user:

                    # An existing password account with the same
                    # email? Link it instead of creating a duplicate.
                    cursor.execute(
                        """
                        SELECT id, username, email
                        FROM users
                        WHERE LOWER(email) = LOWER(%s)
                        LIMIT 1
                        """,
                        (email,)
                    )
                    user = cursor.fetchone()

                    if user:

                        cursor.execute(
                            """
                            UPDATE users
                            SET google_sub = %s
                            WHERE id = %s
                            """,
                            (google_sub, user[0])
                        )

                    else:

                        username = generate_unique_username(
                            cursor,
                            name
                        )

                        cursor.execute(
                            """
                            INSERT INTO users (
                                username,
                                email,
                                password_hash,
                                google_sub
                            )
                            VALUES (%s, %s, NULL, %s)
                            RETURNING id, username, email
                            """,
                            (username, email, google_sub)
                        )
                        user = cursor.fetchone()

                user_id, stored_username, stored_email = user

                token = create_token(user_id)

            connection.commit()

        return {
            "success": True,
            "message": "Login successful.",
            "id": user_id,
            "username": stored_username,
            "email": stored_email,
            "token": token
        }

    except HTTPException:
        raise

    except psycopg.Error as error:

        raise HTTPException(
            status_code=503,
            detail="Database connection failed."
        ) from error


# =====================================================
# GET CURRENT USER
# =====================================================

@app.get("/api/me")
def get_current_user(
    authorization: Optional[str] = Header(default=None)
):

    user_id = get_user_from_token(
        authorization
    )

    try:

        with get_connection() as connection:

            with connection.cursor() as cursor:

                cursor.execute(
                    """
                    SELECT
                        id,
                        username,
                        email
                    FROM users
                    WHERE id = %s
                    """,
                    (user_id,)
                )

                user = cursor.fetchone()

        if not user:

            raise HTTPException(
                status_code=404,
                detail="User not found."
            )

        return {
            "id": user[0],
            "username": user[1],
            "email": user[2]
        }

    except HTTPException:
        raise

    except psycopg.Error as error:

        raise HTTPException(
            status_code=503,
            detail="Could not read user."
        ) from error


# =====================================================
# GET TRANSACTION YEARS
# =====================================================

@app.get("/api/transactions/years")
def get_transaction_years(
    authorization: Optional[str] = Header(default=None)
):

    user_id = get_user_from_token(
        authorization
    )

    try:

        with get_connection() as connection:

            with connection.cursor() as cursor:

                cursor.execute(
                    """
                    SELECT DISTINCT
                        EXTRACT(YEAR FROM transaction_date)::int AS year
                    FROM transactions
                    WHERE user_id = %s
                    ORDER BY year DESC
                    """,
                    (user_id,)
                )

                rows = cursor.fetchall()

    except psycopg.Error as error:

        raise HTTPException(
            status_code=503,
            detail="Could not read transaction years."
        ) from error

    return [row[0] for row in rows]


# =====================================================
# GET TRANSACTIONS
# =====================================================

@app.get("/api/transactions")
def get_transactions(
    year: int = Query(
        ge=2000,
        le=2100
    ),
    authorization: Optional[str] = Header(default=None)
):

    user_id = get_user_from_token(
        authorization
    )

    try:

        with get_connection() as connection:

            with connection.cursor() as cursor:

                cursor.execute(
                    """
                    SELECT
                        id,
                        type,
                        category,
                        amount,
                        transaction_date
                    FROM transactions
                    WHERE user_id = %s
                    AND EXTRACT(
                        YEAR FROM transaction_date
                    ) = %s
                    ORDER BY
                        transaction_date DESC,
                        id DESC
                    """,
                    (
                        user_id,
                        year
                    )
                )

                rows = cursor.fetchall()

    except psycopg.Error as error:

        raise HTTPException(
            status_code=503,
            detail="Could not read transactions."
        ) from error

    return [

        {
            "id": row[0],
            "type": row[1],
            "category": row[2],
            "amount": float(row[3]),
            "date": row[4].isoformat()
        }

        for row in rows

    ]


# =====================================================
# CREATE TRANSACTION
# =====================================================

@app.post(
    "/api/transactions",
    status_code=201
)
def create_transaction(
    transaction: TransactionCreate,
    authorization: Optional[str] = Header(default=None)
):

    user_id = get_user_from_token(
        authorization
    )

    try:

        with get_connection() as connection:

            with connection.cursor() as cursor:

                cursor.execute(
                    """
                    INSERT INTO transactions (
                        user_id,
                        type,
                        category,
                        amount,
                        transaction_date
                    )
                    VALUES (
                        %s,
                        %s,
                        %s,
                        %s,
                        %s
                    )
                    RETURNING
                        id,
                        type,
                        category,
                        amount,
                        transaction_date
                    """,
                    (
                        user_id,
                        transaction.type,
                        transaction.category.strip(),
                        transaction.amount,
                        transaction.date
                    )
                )

                row = cursor.fetchone()

            connection.commit()

    except psycopg.Error as error:

        raise HTTPException(
            status_code=503,
            detail="Could not save transaction."
        ) from error

    return {

        "id": row[0],
        "type": row[1],
        "category": row[2],
        "amount": float(row[3]),
        "date": row[4].isoformat()

    }