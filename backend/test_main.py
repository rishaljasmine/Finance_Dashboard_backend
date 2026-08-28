import uuid

from fastapi.testclient import TestClient
from main import app


# =====================================================
# 1. Health check
# =====================================================

def test_health():
    with TestClient(app) as client:
        response = client.get("/api/health")

        assert response.status_code == 200
        assert response.json()["status"] == "ok"
        assert response.json()["database"] == "connected"


# =====================================================
# 2. Register with missing username
# =====================================================

def test_register_missing_username():
    with TestClient(app) as client:
        response = client.post(
            "/api/register",
            json={
                "username": "",
                "email": "test@example.com",
                "password": "12345678"
            }
        )

        assert response.status_code == 422


# =====================================================
# 3. Register with valid data
# =====================================================

def test_register_success():
    unique = uuid.uuid4().hex[:12]
    username = f"test_user_{unique}"
    email = f"test_{unique}@example.com"

    with TestClient(app) as client:
        response = client.post(
            "/api/register",
            json={
                "username": username,
                "email": email,
                "password": "12345678"
            }
        )

        assert response.status_code == 200
        assert response.json()["success"] is True
        assert response.json()["username"] == username
        assert response.json()["email"] == email



# =====================================================
# 4. Login without username/email
# =====================================================

def test_login_missing_username_and_email():
    with TestClient(app) as client:
        response = client.post(
            "/api/login",
            json={
                "password": "12345678"
            }
        )

        assert response.status_code == 400
        assert response.json()["detail"] == (
            "Username or email is required."
        )


# =====================================================
# 5. Login with wrong credentials
# =====================================================

def test_login_invalid_credentials():
    with TestClient(app) as client:
        response = client.post(
            "/api/login",
            json={
                "username": "definitely_not_a_real_user",
                "password": "wrongpassword"
            }
        )

        assert response.status_code == 401
        assert response.json()["detail"] == (
            "Invalid username/email or password."
        )