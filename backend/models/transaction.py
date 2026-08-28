from datetime import date
from typing import Literal

from pydantic import BaseModel, Field


class TransactionCreate(BaseModel):
    type: Literal["income", "expense"]
    category: str = Field(
        min_length=1,
        max_length=100
    )
    amount: float = Field(gt=0)
    date: date
