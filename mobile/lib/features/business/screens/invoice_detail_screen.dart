import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:printing/printing.dart';

import '../../../core/network/api_client.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/status_chip.dart';
import '../data/business_repository.dart';
import '../models/business_models.dart';

class InvoiceDetailScreen extends StatefulWidget {
  final Invoice invoice;
  const InvoiceDetailScreen({super.key, required this.invoice});

  @override
  State<InvoiceDetailScreen> createState() => _InvoiceDetailScreenState();
}

class _InvoiceDetailScreenState extends State<InvoiceDetailScreen> {
  final _repo = BusinessRepository();
  late Invoice invoice = widget.invoice;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  Future<void> _reload() async {
    try {
      final fresh = await _repo.getInvoice(widget.invoice.id);
      if (mounted) setState(() => invoice = fresh);
    } catch (_) {/* keep what the list gave us */}
  }

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

  Future<void> _recordPayment() async {
    final done = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _PaymentSheet(invoice: invoice, repo: _repo),
    );
    if (done == true) {
      await _reload();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Payment recorded.')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final canPay = (invoice.status == 'SENT' || invoice.status == 'PARTIAL') && invoice.balanceAmount - invoice.pendingAmount > 0;
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
      floatingActionButton: canPay
          ? FloatingActionButton.extended(
              onPressed: _recordPayment,
              icon: const Icon(Icons.payments_outlined),
              label: const Text('Record payment'),
            )
          : null,
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _reload,
          child: ListView(
            padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 96.h),
            children: [
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(invoice.billToName ?? invoice.customerName ?? 'No customer',
                              style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w700)),
                        ),
                        StatusChip.forStatus(invoice.status),
                      ],
                    ),
                    if ((invoice.billToVat ?? '').isNotEmpty) ...[
                      SizedBox(height: 4.h),
                      Text('VAT ${invoice.billToVat}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                    ],
                    if ((invoice.contactPerson ?? '').isNotEmpty) ...[
                      SizedBox(height: 2.h),
                      Text('Attention: ${invoice.contactPerson}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                    ],
                    if ((invoice.customerPhone ?? '').isNotEmpty) ...[
                      SizedBox(height: 2.h),
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
                    if (invoice.pendingAmount > 0) _totalRow('Awaiting confirmation', invoice.pendingAmount, color: AppColors.warning),
                    _totalRow('Balance', invoice.balanceAmount,
                        color: invoice.balanceAmount > 0 ? AppColors.danger : AppColors.textMuted, bold: true),
                  ],
                ),
              ),
              SizedBox(height: 16.h),
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SectionHeader(title: 'Payments (${invoice.payments.length})'),
                    if (invoice.payments.isEmpty)
                      Text('No payments yet', style: TextStyle(fontSize: 13.sp, color: AppColors.textMuted))
                    else
                      ...invoice.payments.map(_paymentTile),
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
      ),
    );
  }

  Widget _paymentTile(InvoicePayment p) {
    final color = p.status == 'CONFIRMED' ? AppColors.success : (p.status == 'PENDING' ? AppColors.warning : AppColors.danger);
    final trace = [p.payerName, if ((p.referenceNumber ?? '').isNotEmpty) 'Ref ${p.referenceNumber}', p.destination]
        .where((e) => e != null && e.isNotEmpty)
        .join(' · ');
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 6.h),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('${p.method} · ${p.paymentNumber}', style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600)),
                if (trace.isNotEmpty) Text(trace, style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted)),
                Text(p.status, style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: color)),
              ],
            ),
          ),
          Text(Fmt.money(p.amount), style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
        ],
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

/// Record a payment with everything needed to trace the money (same rules as the web portal).
class _PaymentSheet extends StatefulWidget {
  final Invoice invoice;
  final BusinessRepository repo;
  const _PaymentSheet({required this.invoice, required this.repo});

  @override
  State<_PaymentSheet> createState() => _PaymentSheetState();
}

class _PaymentSheetState extends State<_PaymentSheet> {
  // code, label, required fields, auto-confirm
  static const _methods = <(String, String, List<String>, bool)>[
    ('CASH', 'Cash', ['payerName'], true),
    ('ECOCASH', 'EcoCash', ['payerName', 'payerContact', 'reference'], true),
    ('ONEMONEY', 'OneMoney', ['payerName', 'payerContact', 'reference'], true),
    ('INNBUCKS', 'InnBucks', ['payerName', 'payerContact', 'reference'], true),
    ('BANK', 'Bank transfer', ['payerName', 'payerBank', 'reference', 'valueDate', 'destinationAccount'], false),
    ('CARD', 'Card / POS', ['reference'], true),
    ('CHEQUE', 'Cheque', ['payerName', 'payerBank', 'cheque', 'valueDate'], false),
    ('OTHER', 'Other', ['reference', 'notes'], false),
  ];
  static const _labels = {
    'payerName': 'Paid by (name)',
    'payerContact': "Payer's mobile number",
    'payerBank': "Payer's bank",
    'reference': 'Reference / transaction code',
    'cheque': 'Cheque number',
    'notes': 'Note',
  };

  String method = 'CASH';
  final amount = TextEditingController();
  final c = <String, TextEditingController>{
    for (final k in ['payerName', 'payerContact', 'payerBank', 'reference', 'cheque', 'notes']) k: TextEditingController(),
  };
  DateTime valueDate = DateTime.now();
  List<Map<String, dynamic>> banks = [];
  int? bankId;
  bool busy = false;
  String? error;

  @override
  void initState() {
    super.initState();
    final open = widget.invoice.balanceAmount - widget.invoice.pendingAmount;
    amount.text = open.toStringAsFixed(2);
    widget.repo.getBankAccounts().then((b) {
      if (mounted) setState(() => banks = b);
    }).catchError((_) {});
  }

  @override
  void dispose() {
    amount.dispose();
    for (final x in c.values) {
      x.dispose();
    }
    super.dispose();
  }

  (String, String, List<String>, bool) get m => _methods.firstWhere((e) => e.$1 == method);

  Future<void> _submit() async {
    final req = m.$3;
    final amt = double.tryParse(amount.text.trim()) ?? 0;
    final open = widget.invoice.balanceAmount - widget.invoice.pendingAmount;
    if (amt <= 0) return setState(() => error = 'Enter the amount received.');
    if (amt > open + 0.0001) return setState(() => error = 'The amount is more than what is still open (${open.toStringAsFixed(2)}).');
    for (final k in req) {
      if (k == 'valueDate') continue;
      if (k == 'destinationAccount') {
        if (bankId == null) return setState(() => error = 'Choose the account that received the money.');
        continue;
      }
      if ((c[k]?.text.trim() ?? '').isEmpty) return setState(() => error = '${_labels[k] ?? k} is required for ${m.$2} payments.');
    }
    setState(() {
      busy = true;
      error = null;
    });
    try {
      await widget.repo.payInvoice({
        'invoiceId': widget.invoice.id,
        'method': method,
        'amount': amt,
        'currencyCode': widget.invoice.currency,
        'valueDate': valueDate.toIso8601String(),
        'payerName': c['payerName']!.text.trim(),
        'payerContact': c['payerContact']!.text.trim(),
        'payerBank': c['payerBank']!.text.trim(),
        'referenceNumber': c['reference']!.text.trim(),
        'chequeNumber': c['cheque']!.text.trim(),
        'notes': c['notes']!.text.trim(),
        'destinationBankAccountId': bankId,
      });
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) {
        setState(() {
          busy = false;
          error = e.toString();
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final req = m.$3;
    final fields = <String>[
      ...['payerName', 'payerContact', 'payerBank', 'reference', 'cheque', 'notes'].where(req.contains),
    ];
    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
      child: SingleChildScrollView(
        padding: EdgeInsets.all(16.w),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Record payment', style: TextStyle(fontSize: 17.sp, fontWeight: FontWeight.w800)),
            Text(widget.invoice.invoiceNumber, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
            SizedBox(height: 12.h),
            Wrap(
              spacing: 8.w,
              runSpacing: 4.h,
              children: [
                for (final e in _methods)
                  ChoiceChip(label: Text(e.$2), selected: method == e.$1, onSelected: (_) => setState(() => method = e.$1)),
              ],
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: amount,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(labelText: 'Amount received (${widget.invoice.currency})'),
            ),
            for (final k in fields) ...[
              SizedBox(height: 10.h),
              TextField(controller: c[k], decoration: InputDecoration(labelText: '${_labels[k]} *')),
            ],
            if (req.contains('valueDate')) ...[
              SizedBox(height: 10.h),
              OutlinedButton.icon(
                icon: const Icon(Icons.event),
                label: Text('Payment date: ${Fmt.date(valueDate)}'),
                onPressed: () async {
                  final d = await showDatePicker(context: context, initialDate: valueDate, firstDate: DateTime(2020), lastDate: DateTime.now());
                  if (d != null) setState(() => valueDate = d);
                },
              ),
            ],
            if (req.contains('destinationAccount')) ...[
              SizedBox(height: 10.h),
              DropdownButtonFormField<int>(
                value: bankId,
                isExpanded: true,
                decoration: const InputDecoration(labelText: 'Received into (our account) *'),
                items: [for (final b in banks) DropdownMenuItem(value: b['id'] as int, child: Text('${b['label']}', overflow: TextOverflow.ellipsis))],
                onChanged: (v) => setState(() => bankId = v),
              ),
            ],
            SizedBox(height: 10.h),
            Text(
              m.$4
                  ? 'Confirmed straight away and the balance drops.'
                  : 'Recorded as PENDING — the balance only drops once finance confirms it on the bank statement.',
              style: TextStyle(fontSize: 12.sp, color: m.$4 ? AppColors.success : AppColors.warning, fontWeight: FontWeight.w600),
            ),
            if (error != null) ...[
              SizedBox(height: 8.h),
              Text(error!, style: TextStyle(fontSize: 12.sp, color: AppColors.danger, fontWeight: FontWeight.w600)),
            ],
            SizedBox(height: 14.h),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: busy ? null : _submit,
                icon: busy ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.check),
                label: const Text('Record payment'),
              ),
            ),
            SizedBox(height: 8.h),
          ],
        ),
      ),
    );
  }
}
