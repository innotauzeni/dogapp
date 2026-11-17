using Dog.Application.Common.Pdf;
using Dog.Application.Contracts;
using Dog.Application.Features.BillingSetup;
using Dog.Application.Features.Companies.DTOs;
using Dog.Application.Features.Companies.Queries;
using Dog.Application.Features.DogTraining.DTOs;
using Dog.Application.Features.DogTraining.Queries;
using Dog.Application.Features.Invoices.DTOs;
using Dog.Application.Features.Invoices.Queries;
using MediatR;
using System.Net;

namespace Dog.Api.Services
{
    public class EmailInvoiceRequest
    {
        /// <summary>Recipients (separated by ; or ,). Empty = the company's billing email saved on the invoice.</summary>
        public string? To { get; set; }
        public string? Cc { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>Builds invoice PDFs (company + training) and emails them through the company's own SMTP account.</summary>
    public class BillingDocuments
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private readonly IMediator _mediator;
        private readonly IEmailService _email;
        public BillingDocuments(IMediator mediator, IEmailService email) { _mediator = mediator; _email = email; }

        private async Task<(CompanyDto? company, byte[]? logo, PdfInfoDto info)> ContextAsync(int companyId, CancellationToken ct)
        {
            CompanyDto? company = null;
            try { company = (await _mediator.Send(new GetCompanyByIdQuery(companyId), ct))?.Data; } catch { /* branding is optional */ }
            byte[]? logo = null;
            try
            {
                if (Uri.TryCreate(company?.Logo, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    logo = await Http.GetByteArrayAsync(uri, ct);
            }
            catch { /* logo is optional */ }
            var info = (await _mediator.Send(new GetPdfInfoQuery(companyId), ct))?.Data ?? new PdfInfoDto();
            return (company, logo, info);
        }

        public async Task<(byte[] Pdf, InvoiceDto Invoice)?> InvoicePdfAsync(int id, CancellationToken ct)
        {
            var r = await _mediator.Send(new GetInvoiceByIdQuery(id), ct);
            if (r?.Success != true || r.Data == null) return null;
            var (company, logo, info) = await ContextAsync(r.Data.CompanyId, ct);
            return (InvoicePdfBuilder.Build(r.Data, company, logo, info.Profile, info.Banks), r.Data);
        }

        public async Task<(byte[] Pdf, TrainingInvoiceDto Invoice)?> TrainingInvoicePdfAsync(int id, CancellationToken ct)
        {
            var r = await _mediator.Send(new GetTrainingInvoiceByIdQuery(id), ct);
            if (r?.Success != true || r.Data == null) return null;
            var (company, logo, info) = await ContextAsync(r.Data.CompanyId, ct);
            return (TrainingInvoicePdfBuilder.Build(r.Data, company, logo, info.Profile, info.Banks), r.Data);
        }

        private static string Body(string company, string greetingName, string number, string total, string balance, string due, string? message)
        {
            string E(string s) => WebUtility.HtmlEncode(s);
            var note = string.IsNullOrWhiteSpace(message) ? "" : $"<p>{E(message).Replace("\n", "<br>")}</p>";
            return $@"<div style=""font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#0f172a"">
<p>Dear {E(greetingName)},</p>
{note}
<p>Please find attached invoice <b>{E(number)}</b> from <b>{E(company)}</b>.</p>
<table style=""border-collapse:collapse;margin:10px 0""><tr><td style=""padding:3px 14px 3px 0;color:#64748b"">Invoice total</td><td><b>{E(total)}</b></td></tr>
<tr><td style=""padding:3px 14px 3px 0;color:#64748b"">Balance due</td><td><b>{E(balance)}</b></td></tr>
<tr><td style=""padding:3px 14px 3px 0;color:#64748b"">Due date</td><td>{E(due)}</td></tr></table>
<p>Please quote <b>{E(number)}</b> as your payment reference. Thank you for your business.</p></div>";
        }

        public async Task<(bool Ok, string Message)> EmailInvoiceAsync(int id, EmailInvoiceRequest req, string? userId, string? userName, CancellationToken ct)
        {
            var built = await InvoicePdfAsync(id, ct);
            if (built == null) return (false, "Invoice not found.");
            var inv = built.Value.Invoice;
            var to = string.IsNullOrWhiteSpace(req.To) ? (inv.BillToEmail ?? inv.CustomerEmail) : req.To;
            if (string.IsNullOrWhiteSpace(to)) return (false, "This invoice has no email address. Enter the address to send it to.");
            var company = (await ContextAsync(inv.CompanyId, ct)).company?.CompanyName ?? "Dog Section";
            var cur = string.IsNullOrWhiteSpace(inv.CurrencyCode) ? "USD" : inv.CurrencyCode;

            var res = await _email.SendAsync(new EmailMessage
            {
                CompanyId = inv.CompanyId, To = { to }, Cc = { req.Cc ?? "" },
                Subject = $"Invoice {inv.InvoiceNumber} from {company}",
                HtmlBody = Body(company, inv.BillToName ?? inv.CustomerName ?? "Customer", inv.InvoiceNumber, $"{cur} {inv.TotalAmount:N2}", $"{cur} {inv.BalanceAmount:N2}", inv.DueDate.ToString("dd MMM yyyy"), req.Message),
                Attachments = { new EmailAttachment { FileName = $"Invoice_{inv.InvoiceNumber}.pdf", Content = built.Value.Pdf } },
                RelatedType = "Invoice", RelatedId = inv.Id, SentByUserId = userId, SentByName = userName,
            }, ct);
            if (!res.Success) return (false, "The email could not be sent: " + res.Error);
            await _mediator.Send(new MarkInvoiceEmailedCommand(false, inv.Id, to), ct);
            return (true, $"Invoice {inv.InvoiceNumber} emailed to {to}.");
        }

        public async Task<(bool Ok, string Message)> EmailTrainingInvoiceAsync(int id, EmailInvoiceRequest req, string? userId, string? userName, CancellationToken ct)
        {
            var built = await TrainingInvoicePdfAsync(id, ct);
            if (built == null) return (false, "Invoice not found.");
            var inv = built.Value.Invoice;
            var to = string.IsNullOrWhiteSpace(req.To) ? inv.CustomerEmail : req.To;
            if (string.IsNullOrWhiteSpace(to)) return (false, "This customer has no email address. Enter the address to send it to.");
            var company = (await ContextAsync(inv.CompanyId, ct)).company?.CompanyName ?? "Dog Training";

            var res = await _email.SendAsync(new EmailMessage
            {
                CompanyId = inv.CompanyId, To = { to }, Cc = { req.Cc ?? "" },
                Subject = $"Training invoice {inv.InvoiceNumber} from {company}",
                HtmlBody = Body(company, inv.CustomerName ?? "Customer", inv.InvoiceNumber, $"USD {inv.TotalAmount:N2}", $"USD {inv.BalanceAmount:N2}", inv.DueDate.ToString("dd MMM yyyy"), req.Message),
                Attachments = { new EmailAttachment { FileName = $"Training_Invoice_{inv.InvoiceNumber}.pdf", Content = built.Value.Pdf } },
                RelatedType = "TrainingInvoice", RelatedId = inv.Id, SentByUserId = userId, SentByName = userName,
            }, ct);
            if (!res.Success) return (false, "The email could not be sent: " + res.Error);
            await _mediator.Send(new MarkInvoiceEmailedCommand(true, inv.Id, to), ct);
            return (true, $"Invoice {inv.InvoiceNumber} emailed to {to}.");
        }
    }
}
