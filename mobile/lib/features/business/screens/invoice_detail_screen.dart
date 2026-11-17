import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:printing/printing.dart';

import '../../../core/network/api_client.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/status_chip.dart';
import '../models/business_models.dart';

class InvoiceDetailScreen extends StatelessWidget {
  final Invoice invoice;
  const InvoiceDetailScreen({super.key, required this.invoice});

  /// Downloads the branded invoice PDF (JWT-auth) and opens the native preview/print/share sheet.
  Future<void> _openPdf(BuildContext context) async {
    final messenger = ScaffoldMessenger.of(context);
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => const Center(child: CircularProgressIndicator()),
    );
    try {
      final bytes = await ApiClient.instance.getBytes('Invoice/${invoice.id}/pdf');
      if (context.mounted) Navigator.of(context).pop(); // dismiss loader
      await Printing.layoutPdf(
        onLayout: (_) async => bytes,
        name: 'Invoice_${invoice.invoiceNumber}.pdf',
      );
    } catch (e) {
      if (context.mounted) Navigator.of(context).pop();
      messenger.showSnackBar(SnackBar(content: Text('Could not open PDF: $e')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(invoice.invoiceNumber.isEmpty ? 'Invoice' : invoice.invoiceNumber),
        actions: [
          IconButton(
            icon: const Icon(Icons.picture_as_pdf_outlined),
            tooltip: 'Invoice PDF',
            onPressed: () => _openPdf(context),
          ),
        ],
      ),
      body: SafeArea(
        top: false,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
          children: [
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(invoice.customerName ?? 'No customer',
                            style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w700)),
                      ),
                      StatusChip.forStatus(invoice.status),
                    ],
                  ),
                  if ((invoice.customerPhone ?? '').isNotEmpty) ...[
                    SizedBox(height: 4.h),
                    Text(invoice.customerPhone!, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                  ],
                  SizedBox(height: 12.h),
                  InfoRow(label: 'Invoice Date', value: Fmt.date(invoice.invoiceDate), icon: Icons.event_outlined),
                  InfoRow(label: 'Due Date', value: Fmt.date(invoice.dueDate), icon: Icons.schedule_outlined),
                ],
              ),
            ),
            SizedBox(height: 16.h),
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SectionHeader(title: 'Items (${invoice.items.length})'),
                  if (invoice.items.isEmpty)
                    Text('No line items', style: TextStyle(fontSize: 13.sp, color: AppColors.textMuted))
                  else
                    ...invoice.items.map((it) => Padding(
                          padding: EdgeInsets.symmetric(vertical: 6.h),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(it.description, style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600)),
                                    Text('${it.quantity} × ${Fmt.money(it.unitPrice)}',
                                        style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted)),
                                  ],
                                ),
                              ),
                              SizedBox(width: 8.w),
                              Text(Fmt.money(it.amount), style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
                            ],
                          ),
                        )),
                  const Divider(height: 24),
                  _totalRow('Total', invoice.totalAmount, bold: true),
                  _totalRow('Paid', invoice.paidAmount, color: AppColors.success),
                  _totalRow('Balance', invoice.balanceAmount,
                      color: invoice.balanceAmount > 0 ? AppColors.danger : AppColors.textMuted, bold: true),
                ],
              ),
            ),
            if ((invoice.notes ?? '').isNotEmpty) ...[
              SizedBox(height: 16.h),
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const SectionHeader(title: 'Notes'),
                    Text(invoice.notes!, style: TextStyle(fontSize: 13.sp, color: AppColors.textSecondary)),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _totalRow(String label, double value, {Color? color, bool bold = false}) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 4.h),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(fontSize: 13.sp, fontWeight: bold ? FontWeight.w700 : FontWeight.w500, color: color ?? AppColors.textPrimary)),
          Text(Fmt.money(value), style: TextStyle(fontSize: 14.sp, fontWeight: bold ? FontWeight.w800 : FontWeight.w600, color: color ?? AppColors.textPrimary)),
        ],
      ),
    );
  }
}
