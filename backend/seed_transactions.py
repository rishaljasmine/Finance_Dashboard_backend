"""
One-off seed script: imports the public "Daily Household Transactions"
dataset (github.com/marcosfabietti/pivot_in_r) into the transactions
table for the given users, replacing whatever transactions they had.

Usage:
    venv/Scripts/python.exe seed_transactions.py dataset_raw.csv Rishal Jasmine
"""

import csv
import os
import sys
from datetime import datetime

import psycopg
from dotenv import load_dotenv

load_dotenv()

DATABASE_URL = os.environ["DATABASE_URL"]


def parse_rows(csv_path):
    rows = []

    with open(csv_path, encoding="utf-8") as f:
        reader = csv.DictReader(f)

        for r in reader:
            kind = r["Income/Expense"]

            if kind not in ("Income", "Expense"):
                continue

            date_part = r["Date"].strip().split(" ")[0]
            day, month, year = date_part.split("/")
            iso_date = datetime(int(year), int(month), int(day)).date()

            rows.append((
                "income" if kind == "Income" else "expense",
                r["Category"].strip() or "Other",
                float(r["Amount"]),
                iso_date
            ))

    return rows


def main():
    csv_path = sys.argv[1]
    usernames = sys.argv[2:]

    if not usernames:
        print("Provide at least one username to seed.")
        sys.exit(1)

    rows = parse_rows(csv_path)
    print(f"Parsed {len(rows)} income/expense rows from {csv_path}")

    with psycopg.connect(DATABASE_URL, connect_timeout=5, autocommit=True) as conn:
        with conn.cursor() as cur:

            for username in usernames:

                cur.execute(
                    "SELECT id FROM users WHERE LOWER(username) = LOWER(%s)",
                    (username,)
                )
                result = cur.fetchone()

                if not result:
                    print(f"  ! user '{username}' not found, skipping")
                    continue

                user_id = result[0]

                cur.execute(
                    "DELETE FROM transactions WHERE user_id = %s",
                    (user_id,)
                )
                print(f"  {username}: cleared {cur.rowcount} existing rows")

                batch_size = 500
                inserted = 0

                for i in range(0, len(rows), batch_size):
                    batch = rows[i:i + batch_size]

                    values_sql = ",".join(["(%s,%s,%s,%s,%s)"] * len(batch))
                    params = []
                    for (t_type, category, amount, t_date) in batch:
                        params.extend([user_id, t_type, category, amount, t_date])

                    cur.execute(
                        f"""
                        INSERT INTO transactions
                            (user_id, type, category, amount, transaction_date)
                        VALUES {values_sql}
                        """,
                        params
                    )
                    inserted += len(batch)

                print(f"  {username}: inserted {inserted} rows (user_id={user_id})")


if __name__ == "__main__":
    main()
