
from datetime import date, timedelta

from app.schemas import AnalyticsRequest, Invoice, Payment
from app.analytics import (
    calculate_dashboard,
    monthly_revenue,
    payment_analytics,
)


def create_test_data():
    today = date.today()

    return AnalyticsRequest(
        tenant_id="test_company",
        invoices=[
            Invoice(
                invoice_id="INV-001",
                customer_id="CUST-001",
                amount=100,
                status="partial",
                issue_date=today - timedelta(days=30),
                due_date=today - timedelta(days=5),
            ),
            Invoice(
                invoice_id="INV-002",
                customer_id="CUST-002",
                amount=50,
                status="unpaid",
                issue_date=today,
                due_date=today + timedelta(days=10),
            ),
        ],
        payments=[
            Payment(
                payment_id="PAY-001",
                invoice_id="INV-001",
                amount=40,
                status="success",
                payment_date=today,
                method="UPI",
            ),
            Payment(
                payment_id="PAY-002",
                invoice_id="INV-002",
                amount=20,
                status="failed",
                payment_date=today,
                method="Card",
            ),
            Payment(
                payment_id="PAY-003",
                invoice_id="INV-002",
                amount=10,
                status="pending",
                payment_date=today,
                method="UPI",
            ),
        ],
    )


def test_dashboard_calculations():
    data = create_test_data()

    result = calculate_dashboard(data)

    assert result["total_invoices"] == 2
    assert result["total_billed"] == 150
    assert result["total_collected"] == 40
    assert result["total_outstanding"] == 110
    assert result["paid_invoices"] == 0
    assert result["unpaid_invoices"] == 1
    assert result["overdue_invoices"] == 1
    assert result["collection_rate"] == 26.67


def test_monthly_revenue():
    data = create_test_data()

    result = monthly_revenue(data)

    current_month = date.today().strftime("%Y-%m")

    assert result == [
        {
            "month": current_month,
            "revenue": 40.0,
        }
    ]


def test_payment_analytics():
    data = create_test_data()

    result = payment_analytics(data)

    assert result["total_payments"] == 3
    assert result["successful_payments"] == 1
    assert result["failed_payments"] == 1
    assert result["pending_payments"] == 1
    assert result["success_rate"] == 33.33


def test_empty_payment_analytics():
    data = AnalyticsRequest(
        tenant_id="test_company",
        invoices=[],
        payments=[],
    )

    result = payment_analytics(data)

    assert result["total_payments"] == 0
    assert result["success_rate"] == 0
