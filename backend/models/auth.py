from typing import Optional

from pydantic import BaseModel, Field


class RegisterRequest(BaseModel):
    username: str = Field(min_length=1, max_length=100)
    email: str = Field(min_length=3, max_length=320)
    password: str = Field(min_length=8, max_length=200)


class LoginRequest(BaseModel):
    username: Optional[str] = Field(
        default=None,
        max_length=100
    )
    email: Optional[str] = Field(
        default=None,
        max_length=320
    )
    password: str = Field(
        min_length=1,
        max_length=200
    )


class GoogleLoginRequest(BaseModel):
    credential: str = Field(min_length=1)
