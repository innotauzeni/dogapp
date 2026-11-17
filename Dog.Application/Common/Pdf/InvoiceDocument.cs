using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dog.Application.Common.Pdf
{
    public class InvoiceDocParty
    {
        public string Name { get; set; } = string.Empty;
        public string? TradingName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? TaxNumber { get; set; }
        public string? VatNumber { get; set; }
        public string? ContactPerson { get; set; }
        public byte[]? Logo { get; set; }
    }

    public class InvoiceDocLine
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal TaxRate { get; set; }
    }

    public class InvoiceDocPayment
    {
        public string Number { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string Status { get; set; } = "CONFIRMED";
        public decimal Amount { get; set; }
    }

    /// <summary>Everything the professional invoice layout needs (company invoices and dog-training invoices share it).</summary>
    public class InvoiceDocModel
    {
        public string Title { get; set; } = "INVOICE";
        public string Number { get; set; } = string.Empty;
        public string Status { get; set; } = "DRAFT";
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Currency { get; set; } = "USD";
        public string? PurchaseOrder { get; set; }
        public string? BrandColor { get; set; }
        public InvoiceDocParty Seller { get; set; } = new();
        public InvoiceDocParty BillTo { get; set; } = new();
        public List<(string Key, string Value)> Extra { get; set; } = new();
        public List<InvoiceDocLine> Lines { get; set; } = new();
        /// <summary>Extra discount / tax typed on the invoice header (on top of the line figures).</summary>
        public decimal HeaderDiscount { get; set; }
        public decimal HeaderTax { get; set; }
        public decimal Paid { get; set; }
        public List<InvoiceDocPayment> Payments { get; set; } = new();
        /// <summary>"Bank · Account name · Account no. · Branch" - one per line.</summary>
        public List<string> BankDetails { get; set; } = new();
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public string? Footer { get; set; }

        public bool IsFiscal { get; set; }
        public string? FiscalNumber { get; set; }
        public string? FiscalDeviceSerial { get; set; }
        public string? FiscalVerificationCode { get; set; }
        public DateTime? FiscalDate { get; set; }
    }

    public static class InvoiceDocRenderer
    {
        private const string Ink = "#0f172a";
        private const string Muted = "#64748b";
        private const string Line = "#e2e8f0";
        private const string Soft = "#f8fafc";

        public static byte[] Render(InvoiceDocModel m)
        {
            var brand = Normalise(m.BrandColor) ?? "#1D4ED8";
            var tint = "#eef2ff";
            string Money(decimal v) => $"{m.Currency} {v:N2}";

            // Figures (per line: net after discount, VAT on the net)
            var rows = m.Lines.Select(l =>
            {
                var gross = l.Quantity * l.UnitPrice;
                var net = Math.Round(gross * (1 - l.DiscountRate / 100), 2);
                var vat = Math.Round(net * l.TaxRate / 100, 2);
                return (Line: l, Gross: gross, Net: net, Vat: vat, Total: net + vat);
            }).ToList();
            var subtotal = rows.Sum(r => r.Gross);
            var lineDiscount = rows.Sum(r => r.Gross - r.Net);
            var vatTotal = rows.Sum(r => r.Vat) + m.HeaderTax;
            var total = Math.Max(0, rows.Sum(r => r.Total) - m.HeaderDiscount + m.HeaderTax);
            var balance = Math.Max(0, total - m.Paid);
            var hasVat = vatTotal != 0 || rows.Any(r => r.Line.TaxRate > 0);

            byte[]? qr = null;
            if (m.IsFiscal && !string.IsNullOrWhiteSpace(m.FiscalVerificationCode))
            {
                var payload = $"{m.Seller.Name}|VAT:{m.Seller.VatNumber}|FDN:{m.FiscalNumber}|DEV:{m.FiscalDeviceSerial}|VC:{m.FiscalVerificationCode}|INV:{m.Number}|TOTAL:{total:0.00}";
                qr = new PngByteQRCode(new QRCodeGenerator().CreateQrCode(payload, QRCodeGenerator.ECCLevel.M)).GetGraphic(6);
            }

            return Document.Create(container => container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(34);
                page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor(Ink).FontFamily("Lato"));

                // ── Letterhead ──
                page.Header().Column(h =>
                {
                    h.Item().Row(row =>
                    {
                        row.RelativeItem().Row(b =>
                        {
                            if (m.Seller.Logo is { Length: > 0 }) { b.ConstantItem(64).Height(64).Image(m.Seller.Logo).FitArea(); b.ConstantItem(12); }
                            b.RelativeItem().Column(c =>
                            {
                                c.Item().Text(m.Seller.Name).FontSize(17).Bold().FontColor(brand);
                                if (!string.IsNullOrWhiteSpace(m.Seller.Address)) c.Item().Text(m.Seller.Address!).FontSize(8.5f).FontColor(Muted);
                                var l2 = string.Join("  •  ", new[] { m.Seller.Phone, m.Seller.Email, m.Seller.Website }.Where(s => !string.IsNullOrWhiteSpace(s)));
                                if (l2.Length > 0) c.Item().Text(l2).FontSize(8.5f).FontColor(Muted);
                                var ids = string.Join("   ", new[]
                                {
                                    Pair("Reg", m.Seller.RegistrationNumber), Pair("TIN/BP", m.Seller.TaxNumber), Pair("VAT", m.Seller.VatNumber)
                                }.Where(s => s != null));
                                if (ids.Length > 0) c.Item().PaddingTop(2).Text(ids).FontSize(8.5f).SemiBold();
                            });
                        });
                        row.ConstantItem(185).Column(c =>
                        {
                            c.Item().AlignRight().Text(m.Title).FontSize(m.Title.Length > 14 ? 15 : 21).Bold();
                            c.Item().AlignRight().Text(m.Number).FontSize(12).SemiBold().FontColor(brand);
                            c.Item().PaddingTop(4).AlignRight().Background(StatusBg(m.Status)).PaddingHorizontal(8).PaddingVertical(2)
                                .Text(m.Status).FontSize(9).Bold().FontColor(StatusFg(m.Status));
                        });
                    });
                    h.Item().PaddingTop(10).LineHorizontal(2.5f).LineColor(brand);
                });

                // ── Body ──
                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Border(0.8f).BorderColor(Line).Background(Soft).Padding(9).Column(c =>
                        {
                            c.Item().Text("BILL TO").FontSize(7.5f).Bold().FontColor(Muted).LetterSpacing(0.08f);
                            c.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(m.BillTo.Name) ? "—" : m.BillTo.Name).FontSize(12).Bold();
                            if (!string.IsNullOrWhiteSpace(m.BillTo.Address)) c.Item().Text(m.BillTo.Address!).FontColor(Muted);
                            if (!string.IsNullOrWhiteSpace(m.BillTo.VatNumber)) c.Item().Text($"VAT No: {m.BillTo.VatNumber}").FontSize(8.5f);
                            if (!string.IsNullOrWhiteSpace(m.BillTo.TaxNumber)) c.Item().Text($"TIN/BP No: {m.BillTo.TaxNumber}").FontSize(8.5f);
                            if (!string.IsNullOrWhiteSpace(m.BillTo.RegistrationNumber)) c.Item().Text($"Reg No: {m.BillTo.RegistrationNumber}").FontSize(8.5f);
                            if (!string.IsNullOrWhiteSpace(m.BillTo.ContactPerson)) c.Item().PaddingTop(3).Text($"Attention: {m.BillTo.ContactPerson}").FontSize(9);
                            var cl = string.Join("  •  ", new[] { m.BillTo.Phone, m.BillTo.Email }.Where(s => !string.IsNullOrWhiteSpace(s)));
                            if (cl.Length > 0) c.Item().Text(cl).FontSize(8.5f).FontColor(Muted);
                        });
                        r.ConstantItem(12);
                        r.RelativeItem().Border(0.8f).BorderColor(Line).Padding(9).Column(c =>
                        {
                            c.Item().Text("INVOICE DETAILS").FontSize(7.5f).Bold().FontColor(Muted).LetterSpacing(0.08f);
                            Kv(c, "Invoice date", m.InvoiceDate.ToString("dd MMM yyyy"));
                            Kv(c, "Due date", m.DueDate.ToString("dd MMM yyyy"));
                            if (!string.IsNullOrWhiteSpace(m.PurchaseOrder)) Kv(c, "PO number", m.PurchaseOrder!);
                            Kv(c, "Currency", m.Currency);
                            foreach (var (k, v) in m.Extra) if (!string.IsNullOrWhiteSpace(v)) Kv(c, k, v);
                        });
                    });

                    // Items
                    col.Item().PaddingTop(14).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(22); c.RelativeColumn(5); c.ConstantColumn(36); c.ConstantColumn(66); c.ConstantColumn(40); c.ConstantColumn(40); c.ConstantColumn(72); });
                        t.Header(h =>
                        {
                            Th(h, "#", brand); Th(h, "Description", brand); Th(h, "Qty", brand, true); Th(h, "Unit price", brand, true);
                            Th(h, "Disc %", brand, true); Th(h, "VAT %", brand, true); Th(h, "Amount", brand, true);
                        });
                        var i = 0;
                        foreach (var r in rows)
                        {
                            var bg = i % 2 == 1 ? Soft : "#ffffff"; i++;
                            Td(t, i.ToString(), bg); Td(t, r.Line.Description, bg); Td(t, r.Line.Quantity.ToString("0.##"), bg, true); Td(t, r.Line.UnitPrice.ToString("N2"), bg, true);
                            Td(t, r.Line.DiscountRate == 0 ? "–" : r.Line.DiscountRate.ToString("0.#"), bg, true);
                            Td(t, r.Line.TaxRate == 0 ? "–" : r.Line.TaxRate.ToString("0.#"), bg, true); Td(t, r.Net.ToString("N2"), bg, true);
                        }
                    });

                    // Totals + how to pay
                    col.Item().PaddingTop(10).Row(r =>
                    {
                        r.RelativeItem().PaddingRight(14).Column(c =>
                        {
                            if (m.BankDetails.Count > 0)
                            {
                                c.Item().Border(0.8f).BorderColor(Line).Padding(8).Column(b =>
                                {
                                    b.Item().Text("HOW TO PAY").FontSize(7.5f).Bold().FontColor(brand).LetterSpacing(0.08f);
                                    foreach (var line in m.BankDetails) b.Item().PaddingTop(2).Text(line).FontSize(8.5f);
                                    b.Item().PaddingTop(4).Text($"Please quote {m.Number} as the payment reference.").FontSize(8).Italic().FontColor(Muted);
                                });
                            }
                        });
                        r.ConstantItem(230).Column(c =>
                        {
                            Total(c, "Subtotal", Money(subtotal));
                            if (lineDiscount + m.HeaderDiscount != 0) Total(c, "Discount", "-" + Money(lineDiscount + m.HeaderDiscount));
                            if (hasVat) Total(c, "VAT", Money(vatTotal));
                            c.Item().PaddingVertical(3).LineHorizontal(0.8f).LineColor(Line);
                            Total(c, "Total", Money(total), true);
                            if (m.Paid != 0) Total(c, "Paid", "-" + Money(m.Paid));
                            c.Item().PaddingTop(3).Background(tint).Padding(7).Row(x =>
                            {
                                x.RelativeItem().Text("Balance due").Bold().FontColor(brand);
                                x.ConstantItem(130).AlignRight().Text(Money(balance)).Bold().FontSize(11).FontColor(balance > 0 ? brand : "#059669");
                            });
                        });
                    });

                    // VAT summary
                    if (hasVat)
                    {
                        var groups = rows.Where(x => x.Line.TaxRate > 0).GroupBy(x => x.Line.TaxRate).OrderBy(g => g.Key).ToList();
                        if (groups.Count > 0)
                        {
                            col.Item().PaddingTop(12).Text("VAT SUMMARY").FontSize(7.5f).Bold().FontColor(Muted).LetterSpacing(0.08f);
                            col.Item().PaddingTop(3).Width(300).Table(t =>
                            {
                                t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                                t.Header(h => { Th(h, "Rate", "#475569"); Th(h, "Net amount", "#475569", true); Th(h, "VAT", "#475569", true); });
                                foreach (var g in groups) { Td(t, g.Key.ToString("0.#") + "%", "#fff"); Td(t, g.Sum(x => x.Net).ToString("N2"), "#fff", true); Td(t, g.Sum(x => x.Vat).ToString("N2"), "#fff", true); }
                            });
                        }
                    }

                    // Payments
                    if (m.Payments.Count > 0)
                    {
                        col.Item().PaddingTop(14).Text("PAYMENTS").FontSize(7.5f).Bold().FontColor(Muted).LetterSpacing(0.08f);
                        col.Item().PaddingTop(3).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.ConstantColumn(78); c.ConstantColumn(68); c.ConstantColumn(78); c.RelativeColumn(); c.ConstantColumn(60); c.ConstantColumn(72); });
                            t.Header(h => { Th(h, "Receipt", "#475569"); Th(h, "Date", "#475569"); Th(h, "Method", "#475569"); Th(h, "Reference", "#475569"); Th(h, "Status", "#475569"); Th(h, "Amount", "#475569", true); });
                            foreach (var p in m.Payments)
                            {
                                Td(t, p.Number, "#fff"); Td(t, p.Date.ToString("dd MMM yyyy"), "#fff"); Td(t, p.Method, "#fff"); Td(t, p.Reference ?? "–", "#fff");
                                Td(t, p.Status == "CONFIRMED" ? "Confirmed" : p.Status == "PENDING" ? "Pending" : "Rejected", "#fff"); Td(t, p.Amount.ToString("N2"), "#fff", true);
                            }
                        });
                    }

                    // Fiscal block
                    if (m.IsFiscal)
                    {
                        col.Item().PaddingTop(14).Border(0.8f).BorderColor(brand).Padding(8).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("FISCAL INVOICE").FontSize(8).Bold().FontColor(brand).LetterSpacing(0.08f);
                                if (!string.IsNullOrWhiteSpace(m.FiscalNumber)) Kv(c, "Fiscal invoice no.", m.FiscalNumber!);
                                if (!string.IsNullOrWhiteSpace(m.FiscalDeviceSerial)) Kv(c, "Fiscal device serial", m.FiscalDeviceSerial!);
                                if (m.FiscalDate.HasValue) Kv(c, "Fiscalised on", m.FiscalDate.Value.ToString("dd MMM yyyy HH:mm"));
                                if (!string.IsNullOrWhiteSpace(m.FiscalVerificationCode)) Kv(c, "Verification code", m.FiscalVerificationCode!);
                            });
                            if (qr != null) r.ConstantItem(70).Height(70).Image(qr).FitArea();
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(m.Notes))
                    {
                        col.Item().PaddingTop(14).Text("NOTES").FontSize(7.5f).Bold().FontColor(Muted).LetterSpacing(0.08f);
                        col.Item().PaddingTop(2).Text(m.Notes!).FontSize(9);
                    }
                    if (!string.IsNullOrWhiteSpace(m.Terms))
                    {
                        col.Item().ShowEntire().PaddingTop(10).Column(tc => { tc.Item().Text("TERMS & CONDITIONS").FontSize(7.5f).Bold().FontColor(Muted).LetterSpacing(0.08f);
                        tc.Item().PaddingTop(2).Text(m.Terms!).FontSize(8.5f).FontColor("#334155"); });
                    }
                });

                page.Footer().Column(f =>
                {
                    f.Item().LineHorizontal(0.6f).LineColor(Line);
                    f.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text(string.IsNullOrWhiteSpace(m.Footer) ? "Thank you for your business." : m.Footer!).FontSize(8).FontColor(Muted);
                        r.ConstantItem(190).AlignRight().Text(t =>
                        {
                            t.Span($"{m.Number}  •  Page ").FontSize(8).FontColor(Muted);
                            t.CurrentPageNumber().FontSize(8).FontColor(Muted);
                            t.Span(" / ").FontSize(8).FontColor(Muted);
                            t.TotalPages().FontSize(8).FontColor(Muted);
                        });
                    });
                });
            })).GeneratePdf();
        }

        private static string? Pair(string k, string? v) => string.IsNullOrWhiteSpace(v) ? null : $"{k}: {v}";

        private static string? Normalise(string? hex)
            => !string.IsNullOrWhiteSpace(hex) && System.Text.RegularExpressions.Regex.IsMatch(hex.Trim(), "^#[0-9a-fA-F]{6}$") ? hex.Trim() : null;

        private static void Kv(ColumnDescriptor c, string k, string v)
            => c.Item().PaddingTop(2).Row(r => { r.ConstantItem(92).Text(k).FontSize(8.5f).FontColor(Muted); r.RelativeItem().Text(v).FontSize(9).SemiBold(); });

        private static void Th(TableCellDescriptor h, string text, string bg, bool right = false)
        {
            var cell = h.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6);
            (right ? cell.AlignRight() : cell).Text(text).FontSize(8.5f).Bold().FontColor("#ffffff");
        }

        private static void Td(TableDescriptor t, string text, string bg, bool right = false)
        {
            var cell = t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Line).PaddingVertical(5).PaddingHorizontal(6);
            (right ? cell.AlignRight() : cell).Text(text).FontSize(9);
        }

        private static void Total(ColumnDescriptor c, string label, string value, bool bold = false)
            => c.Item().PaddingVertical(2).Row(r =>
            {
                var l = r.RelativeItem().Text(label).FontColor(bold ? Ink : Muted);
                var v = r.ConstantItem(130).AlignRight().Text(value);
                if (bold) { l.Bold(); v.Bold(); }
            });

        private static string StatusFg(string s) => s?.ToUpperInvariant() switch
        {
            "PAID" => "#047857", "PARTIAL" => "#b45309", "SENT" => "#1d4ed8", "CANCELLED" => "#b91c1c", _ => "#475569",
        };

        private static string StatusBg(string s) => s?.ToUpperInvariant() switch
        {
            "PAID" => "#d1fae5", "PARTIAL" => "#fef3c7", "SENT" => "#dbeafe", "CANCELLED" => "#fee2e2", _ => "#e2e8f0",
        };
    }
}
