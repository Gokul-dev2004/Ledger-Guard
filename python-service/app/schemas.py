
from datetime import date
from typing import Optional, Literal
from pydantic import BaseModel, Field


class Invoice(BaseModel):
    invoice_id: str
    customer_id: str
    amount: float = Field(ge=0)
    status: Literal["paid", "unpaid", "partial", "overdue"]
    issue_date: date
    due_date: date
    paid_date: Optional[date] = None


class Payment(BaseModel):
    payment_id: str
    invoice_id: str
    amount: float = Field(gt=0)
    status: Literal["success", "failed", "pending"]
    payment_date: date
    method: Optional[str] = None


class AnalyticsRequest(BaseModel):
    tenant_id: str
    invoices: list[Invoice] = Field(default_factory=list)
    payments: list[Payment] = Field(default_factory=list)
