
from collections import defaultdict
from datetime import date


SUCCESS_STATUSES = {"success"}


def calculate_dashboard(data):
    invoices = data.invoices
    payments = data.payments

    # Count successful payments against each invoice
    collected_by_invoice = defaultdict(float)

    for payment in payments:
        if payment.status.lower() in SUCCESS_STATUSES:
            collected_by_invoice[payment.invoice_id] += payment.amount

    total_billed = sum(invoice.amount for invoice in invoices)
    total_collected = sum(collected_by_invoice.values())

    paid_count = 0
    unpaid_count = 0
    overdue_count = 0
    outstanding = 0

    for invoice in invoices:
        collected = collected_by_invoice[invoice.invoice_id]
        remaining = max(invoice.amount - collected, 0)

        outstanding += remaining

        if remaining == 0:
            paid_count += 1
        elif invoice.due_date < date.today():
            overdue_count += 1
        else:
            unpaid_count += 1

    return {
        "total_invoices": len(invoices),
        "total_billed": round(total_billed, 2),
        "total_collected": round(total_collected, 2),
        "total_outstanding": round(outstanding, 2),
        "paid_invoices": paid_count,
        "unpaid_invoices": unpaid_count,
        "overdue_invoices": overdue_count,
        "collection_rate": round(
            total_collected / total_billed * 100, 2
        ) if total_billed else 0
    }


def monthly_revenue(data):
    revenue = defaultdict(float)

    for payment in data.payments:
        if payment.status.lower() == "success":
            month = payment.payment_date.strftime("%Y-%m")
            revenue[month] += payment.amount

    return [
        {
            "month": month,
            "revenue": round(amount, 2)
        }
        for month, amount in sorted(revenue.items())
    ]


def payment_analytics(data):
    payments = data.payments

    successful = sum(
        p.status.lower() == "success" for p in payments
    )

    failed = sum(
        p.status.lower() == "failed" for p in payments
    )

    pending = sum(
        p.status.lower() == "pending" for p in payments
    )

    total = len(payments)

    return {
        "total_payments": total,
        "successful_payments": successful,
        "failed_payments": failed,
        "pending_payments": pending,
        "success_rate": round(
            successful / total * 100, 2
        ) if total else 0
    }
