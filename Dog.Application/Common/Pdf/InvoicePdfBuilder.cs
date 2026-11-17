using Dog.Application.Features.BillingSetup;
using Dog.Application.Features.Companies.DTOs;
using Dog.Application.Features.Invoices.DTOs;

namespace Dog.Application.Common.Pdf
{
    /// <summary>Professional company-invoice PDF. Used by the web Portal, the API (mobile) and the "email invoice" feature.</summary>
    public static class InvoicePdfBuilder
    {
        /// <param name="logo">Optional logo image bytes.</param>
        /// <param name="profile">The company's registration / TIN / VAT details, terms and footer.</param>
        /// <param name="banks">Bank accounts to print under "How to pay" (one line each).</param>
        public static byte[] Build(InvoiceDto inv, CompanyDto? company, byte[]? logo, BillingProfileDto? profile = null, IEnumerable<string>? banks = null)
        {
            var m = new InvoiceDocModel
            {
                Title = inv.IsFiscal ? "FISCAL TAX INVOICE" : !string.IsNullOrWhiteSpace(profile?.VatNumber) && inv.Items.Any(i => i.TaxRate > 0) ? "TAX INVOICE" : "INVOICE",
                Number = inv.InvoiceNumber, Status = inv.Status, InvoiceDate = inv.InvoiceDate, DueDate = inv.DueDate,
                Currency = string.IsNullOrWhiteSpace(inv.CurrencyCode) ? "USD" : inv.CurrencyCode, PurchaseOrder = inv.PurchaseOrderNumber, BrandColor = company?.BrandColor,
                Seller = new InvoiceDocParty
                {
                    Name = company?.CompanyName ?? "Dog Section", Address = company?.Address, Phone = company?.PhoneNumber, Email = company?.EmailAddress, Website = company?.Website,
                    RegistrationNumber = profile?.RegistrationNumber, TaxNumber = profile?.TaxNumber, VatNumber = profile?.VatNumber, Logo = logo,
                },
                BillTo = new InvoiceDocParty
                {
                    Name = inv.BillToName ?? inv.CustomerName ?? "", Address = inv.BillToAddress, VatNumber = inv.BillToVatNumber, TaxNumber = inv.BillToTaxNumber,
                    Email = inv.BillToEmail ?? inv.CustomerEmail, Phone = inv.BillToPhone ?? inv.CustomerPhone,
                    ContactPerson = inv.ContactPerson,
                },
                Lines = inv.Items.Select(i => new InvoiceDocLine { Description = i.Description, Quantity = i.Quantity, UnitPrice = i.UnitPrice, DiscountRate = i.DiscountRate, TaxRate = i.TaxRate }).ToList(),
                HeaderDiscount = inv.DiscountAmount, HeaderTax = inv.TaxAmount, Paid = inv.PaidAmount,
                Payments = inv.Payments.Where(p => p.Status != "REJECTED").Select(p => new InvoiceDocPayment
                {
                    Number = p.PaymentNumber, Date = p.ValueDate ?? p.PaymentDate, Method = p.MethodLabel ?? p.Method, Reference = p.ReferenceNumber ?? p.ChequeNumber, Status = p.Status, Amount = p.Amount
                }).ToList(),
                BankDetails = banks?.ToList() ?? new(),
                Notes = inv.Notes, Terms = string.IsNullOrWhiteSpace(inv.Terms) ? profile?.InvoiceTerms : inv.Terms, Footer = profile?.InvoiceFooter,
                IsFiscal = inv.IsFiscal, FiscalNumber = inv.FiscalInvoiceNumber, FiscalDeviceSerial = inv.FiscalDeviceSerial,
                FiscalVerificationCode = inv.FiscalVerificationCode, FiscalDate = inv.FiscalDate,
            };
            return InvoiceDocRenderer.Render(m);
        }
    }
}
