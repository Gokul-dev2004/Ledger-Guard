
import pandas as pd
from io import BytesIO


def generate_report(data, file_format):

    invoice_rows = [
        {
            "Invoice ID": inv.invoice_id,
            "Customer ID": inv.customer_id,
            "Amount": inv.amount,
            "Status": inv.status,
            "Issue Date": inv.issue_date,
            "Due Date": inv.due_date,
            "Paid Date": inv.paid_date
        }
        for inv in data.invoices
    ]

    payment_rows = [
        {
            "Payment ID": payment.payment_id,
            "Invoice ID": payment.invoice_id,
            "Amount": payment.amount,
            "Status": payment.status,
            "Payment Date": payment.payment_date,
            "Method": payment.method
        }
        for payment in data.payments
    ]

    invoices_df = pd.DataFrame(invoice_rows)
    payments_df = pd.DataFrame(payment_rows)

    output = BytesIO()

    if file_format == "csv":
        invoices_df.to_csv(output, index=False)
        filename = "ledgerguard_invoices.csv"
        media_type = "text/csv"

    elif file_format == "xlsx":
        with pd.ExcelWriter(output, engine="openpyxl") as writer:
            invoices_df.to_excel(
                writer, sheet_name="Invoices", index=False
            )
            payments_df.to_excel(
                writer, sheet_name="Payments", index=False
            )

        filename = "ledgerguard_report.xlsx"
        media_type = (
            "application/vnd.openxmlformats-officedocument."
            "spreadsheetml.sheet"
        )

    else:
        raise ValueError("Unsupported format")

    output.seek(0)

    return output, media_type, filename
