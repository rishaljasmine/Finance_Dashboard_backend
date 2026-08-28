"""
Produces a small, curated subset of dataset_raw.csv: per year, keeps a
handful of income rows plus one representative (highest-amount) expense
row per distinct category, capped at max_per_year total rows.

Usage:
    venv/Scripts/python.exe trim_dataset.py dataset_raw.csv dataset_trimmed.csv 13
"""

import csv
import sys
from collections import defaultdict


def main():
    src_path = sys.argv[1]
    dest_path = sys.argv[2]
    max_per_year = int(sys.argv[3]) if len(sys.argv) > 3 else 13

    with open(src_path, encoding="utf-8") as f:
        reader = csv.DictReader(f)
        rows = [r for r in reader if r["Income/Expense"] in ("Income", "Expense")]

    by_year = defaultdict(list)
    for r in rows:
        year = r["Date"].strip().split(" ")[0].split("/")[-1]
        by_year[year].append(r)

    selected = []

    for year in sorted(by_year):
        year_rows = by_year[year]

        income_rows = sorted(
            (r for r in year_rows if r["Income/Expense"] == "Income"),
            key=lambda r: float(r["Amount"]),
            reverse=True
        )[:3]

        best_per_category = {}
        for r in year_rows:
            if r["Income/Expense"] != "Expense":
                continue
            category = r["Category"].strip() or "Other"
            amount = float(r["Amount"])
            if category not in best_per_category or amount > float(best_per_category[category]["Amount"]):
                best_per_category[category] = r

        remaining_slots = max(0, max_per_year - len(income_rows))

        expense_rows = sorted(
            best_per_category.values(),
            key=lambda r: float(r["Amount"]),
            reverse=True
        )[:remaining_slots]

        year_selection = income_rows + expense_rows
        year_selection.sort(key=lambda r: r["Date"])

        print(f"{year}: {len(income_rows)} income + {len(expense_rows)} expense = {len(year_selection)} rows")
        selected.extend(year_selection)

    fieldnames = ["Date", "Mode", "Category", "Subcategory", "Note", "Amount", "Income/Expense", "Currency"]

    with open(dest_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        for r in selected:
            writer.writerow(r)

    print(f"\nWrote {len(selected)} total rows to {dest_path}")


if __name__ == "__main__":
    main()
