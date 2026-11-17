using Dog.Application.Features.BillingSetup;
using Dog.Application.Features.Companies.DTOs;
using Dog.Application.Features.DogTraining.DTOs;

namespace Dog.Application.Common.Pdf
{
    /// <summary>Training invoice with its payment history (doubles as a statement/receipt). Same professional layout as company invoices.</summary>
    public static class TrainingInvoicePdfBuilder
    {
        public static byte[] Build(TrainingInvoiceDto inv, CompanyDto? company, byte[]? logo, BillingProfileDto? profile = null, IEnumerable<string>? banks = null)
        {
            var m = new InvoiceDocModel
            {
                Title = "TRAINING INVOICE", Number = inv.InvoiceNumber, Status = inv.Status, InvoiceDate = inv.InvoiceDate, DueDate = inv.DueDate,
                Currency = "USD", BrandColor = company?.BrandColor,
                Seller = new InvoiceDocParty
                {
                    Name = company?.CompanyName ?? "Dog Training", Address = company?.Address, Phone = company?.PhoneNumber, Email = company?.EmailAddress, Website = company?.Website,
                    RegistrationNumber = profile?.RegistrationNumber, TaxNumber = profile?.TaxNumber, VatNumber = profile?.VatNumber, Logo = logo,
                },
                BillTo = new InvoiceDocParty { Name = inv.CustomerName ?? "", Phone = inv.CustomerPhone, Email = inv.CustomerEmail },
                Lines = inv.Items.Select(i => new InvoiceDocLine { Description = i.Description, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList(),
                Paid = inv.PaidAmount,
                Payments = inv.Payments.Where(p => p.Status != "REJECTED").Select(p => new InvoiceDocPayment
                {
                    Number = p.PaymentNumber, Date = p.ValueDate ?? p.PaymentDate, Method = p.MethodLabel ?? p.Method, Reference = p.ReferenceNumber ?? p.ChequeNumber, Status = p.Status, Amount = p.Amount
                }).ToList(),
                BankDetails = banks?.ToList() ?? new(),
                Notes = inv.Notes, Terms = string.IsNullOrWhiteSpace(inv.Terms) ? profile?.InvoiceTerms : inv.Terms, Footer = profile?.InvoiceFooter,
            };
            if (!string.IsNullOrWhiteSpace(inv.DogName)) m.Extra.Add(("Dog", inv.DogName!));
            if (!string.IsNullOrWhiteSpace(inv.EnrollmentNumber)) m.Extra.Add(("Enrolment", inv.EnrollmentNumber!));
            return InvoiceDocRenderer.Render(m);
        }
    }
}
