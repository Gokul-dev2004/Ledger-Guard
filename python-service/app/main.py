
from fastapi import FastAPI, HTTPException, Query
from fastapi.responses import StreamingResponse

from app.schemas import AnalyticsRequest
from app.analytics import (
    calculate_dashboard,
    monthly_revenue,
    payment_analytics
)
from app.reports import generate_report


app = FastAPI(
    title="LedgerGuard Analytics API",
    description="Billing analytics and reporting service",
    version="1.0.0"
)


@app.get("/")
def home():
    return {
        "message": "LedgerGuard Analytics API is running"
    }


@app.get("/health")
def health():
    return {
        "status": "healthy",
        "service": "python-analytics"
    }


@app.post("/analytics/dashboard")
def dashboard(data: AnalyticsRequest):
    return {
        "tenant_id": data.tenant_id,
        "summary": calculate_dashboard(data),
        "monthly_revenue": monthly_revenue(data),
        "payment_analytics": payment_analytics(data)
    }


@app.post("/analytics/monthly-revenue")
def revenue(data: AnalyticsRequest):
    return {
        "monthly_revenue": monthly_revenue(data)
    }


@app.post("/analytics/payments")
def payments(data: AnalyticsRequest):
    return payment_analytics(data)


@app.post("/reports/export")
def export_report(
    data: AnalyticsRequest,
    format: str = Query(default="csv")
):
    if format not in ["csv", "xlsx"]:
        raise HTTPException(
            status_code=400,
            detail="Format must be csv or xlsx"
        )

    output, media_type, filename = generate_report(
        data, format
    )

    return StreamingResponse(
        output,
        media_type=media_type,
        headers={
            "Content-Disposition":
            f'attachment; filename="{filename}"'
        }
    )
