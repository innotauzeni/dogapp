import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:printing/printing.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_client.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/payment_sheet.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/training_repository.dart';
import '../widgets/training_ui.dart';

/// A training invoice: items, totals, every payment with its trace, and (with permission) record / confirm / reject.
class TrainingInvoiceScreen extends StatefulWidget {
  final int invoiceId;
  const TrainingInvoiceScreen({super.key, required this.invoiceId});

  @override
  State<TrainingInvoiceScreen> createState() => _TrainingInvoiceScreenState();
}

class _TrainingInvoiceScreenState extends State<TrainingInvoiceScreen> {
  final _repo = TrainingRepository();
  late Future<Map<String, dynamic>> _future;

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  Future<Map<String, dynamic>> _load() => _repo.rawMap('Training/invoices/${widget.invoiceId}');

  Future<void> _refresh() async {
    setState(() => _future = _load());
    await _future.catchError((_) => <String, dynamic>{});
  }

  Future<void> _pdf(Map<String, dynamic> inv) async {
    try {
      final bytes = await ApiClient.instance.getBytes('Training/invoices/${widget.invoiceId}/pdf');
      await Printing.layoutPdf(onLayout: (_) async => bytes, name: 'Training_Invoice_${s(inv['invoiceNumber'])}.pdf');
    } catch (e) {
      if (mounted) trToast(context, 'Could not open the PDF: ${errText(e)}', error: true);
    }
  }

  Future<void> _pay(Map<String, dynamic> inv, bool canVerify) async {
    final open = dbl(inv['balanceAmount']) - dbl(inv['pendingAmount']);
    final ok = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => PaymentSheet(
        invoiceNumber: s(inv['invoiceNumber']),
        currency: 'USD',
        open: open,
        canVerify: canVerify,
        loadBanks: _repo.banks,
        submit: (body) => _repo.rawPost('Training/invoices/${widget.invoiceId}/payments', body),
      ),
    );
    if (ok == true) {
      if (mounted) trToast(context, 'Payment recorded.');
      _refresh();
    }
  }

  Future<void> _review(int paymentId, bool confirm) async {
    final c = TextEditingController();
    final go = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(confirm ? 'Confirm payment' : 'Reject payment'),
        content: TextField(
          controller: c,
          decoration: InputDecoration(labelText: confirm ? 'Bank-statement line / reference' : 'Reason'),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: Text(confirm ? 'Confirm' : 'Reject')),
        ],
      ),
    );
    if (go != true || c.text.trim().isEmpty) return;
    try {
      await _repo.rawPost('Training/payments/$paymentId/${confirm ? 'confirm' : 'reject'}', confirm ? {'statementReference': c.text.trim()} : {'reason': c.text.trim()});
      if (mounted) trToast(context, confirm ? 'Payment confirmed.' : 'Payment rejected.');
      _refresh();
    } catch (e) {
      if (mounted) trToast(context, errText(e), error: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(title: const Text('Training invoice')),
      body: SafeArea(
        top: false,
        child: AsyncView<Map<String, dynamic>>(
          future: _future,
          onRetry: _refresh,
          builder: (context, inv) {
            final status = s(inv['status']);
            final open = dbl(inv['balanceAmount']) - dbl(inv['pendingAmount']);
            final payments = ((inv['payments'] as List?) ?? const []).whereType<Map>().toList();
            final items = ((inv['items'] as List?) ?? const []).whereType<Map>().toList();
            return Stack(children: [
              RefreshIndicator(
                onRefresh: _refresh,
                child: ListView(
                  padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 110.h),
                  children: [
                    TrCard(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Row(children: [
                          Expanded(child: Text(s(inv['invoiceNumber']), style: TextStyle(fontSize: 18.sp, fontWeight: FontWeight.w800))),
                          TrPill(status),
                        ]),
                        SizedBox(height: 4.h),
                        Text(s(inv['customerName']), style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600)),
                        if (s(inv['dogName']).isNotEmpty) Text('Dog: ${s(inv['dogName'])} · ${s(inv['enrollmentNumber'])}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                        Divider(height: 22.h),
                        Row(children: [
                          Expanded(child: KV('Invoice date', tDate(inv['invoiceDate']))),
                          Expanded(child: KV('Due', tDate(inv['dueDate']))),
                        ]),
                      ]),
                    ),
                    SizedBox(height: 12.h),
                    Row(children: [
                      _total('Total', tMoney(inv['totalAmount']), AppColors.textPrimary),
                      SizedBox(width: 8.w),
                      _total('Paid', tMoney(inv['paidAmount']), AppColors.success),
                      SizedBox(width: 8.w),
                      _total('Balance', tMoney(inv['balanceAmount']), dbl(inv['balanceAmount']) > 0 ? AppColors.danger : AppColors.success),
                    ]),
                    if (dbl(inv['pendingAmount']) > 0)
                      Padding(
                        padding: EdgeInsets.only(top: 8.h),
                        child: Container(
                          padding: EdgeInsets.all(10.w),
                          decoration: BoxDecoration(color: const Color(0xFFFFFBEB), borderRadius: BorderRadius.circular(12.r), border: Border.all(color: const Color(0xFFFDE68A))),
                          child: Row(children: [
                            const Icon(Icons.hourglass_top_rounded, color: Color(0xFF92400E), size: 18),
                            SizedBox(width: 8.w),
                            Expanded(child: Text('${tMoney(inv['pendingAmount'])} is waiting to be confirmed on the bank statement.', style: TextStyle(fontSize: 12.sp, color: const Color(0xFF92400E), fontWeight: FontWeight.w600))),
                          ]),
                        ),
                      ),
                    SizedBox(height: 16.h),
                    Text('Items', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800)),
                    SizedBox(height: 8.h),
                    TrCard(
                      child: Column(children: [
                        for (final it in items)
                          Padding(
                            padding: EdgeInsets.symmetric(vertical: 6.h),
                            child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                              Expanded(child: Text('${s(it['description'])}\n${s(it['quantity'])} × ${tMoney(it['unitPrice'])}', style: TextStyle(fontSize: 13.sp))),
                              Text(tMoney(it['amount']), style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
                            ]),
                          ),
                      ]),
                    ),
                    SizedBox(height: 16.h),
                    Text('Payments (${payments.length})', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800)),
                    SizedBox(height: 8.h),
                    if (payments.isEmpty)
                      const TrCard(child: TrEmpty(Icons.payments_outlined, 'No payments yet.'))
                    else
                      for (final p in payments)
                        Padding(
                          padding: EdgeInsets.only(bottom: 8.h),
                          child: TrCard(
                            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                              Row(children: [
                                Expanded(child: Text('${s(p['methodLabel'] ?? p['method'])} · ${s(p['paymentNumber'])}', style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700))),
                                TrPill(s(p['status'])),
                              ]),
                              SizedBox(height: 4.h),
                              Text(tMoney(p['amount']), style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800)),
                              ..._trace(p),
                              if (s(p['status']) == 'PENDING' && auth.can('training.payment.verify'))
                                Padding(
                                  padding: EdgeInsets.only(top: 8.h),
                                  child: Row(children: [
                                    Expanded(child: FilledButton.icon(onPressed: () => _review(n(p['id']), true), icon: const Icon(Icons.check, size: 18), label: const Text('Confirm'))),
                                    SizedBox(width: 8.w),
                                    Expanded(child: OutlinedButton(onPressed: () => _review(n(p['id']), false), child: const Text('Reject'))),
                                  ]),
                                ),
                            ]),
                          ),
                        ),
                  ],
                ),
              ),
              Positioned(
                left: 0,
                right: 0,
                bottom: 0,
                child: Container(
                  padding: EdgeInsets.fromLTRB(16.w, 10.h, 16.w, 12.h),
                  decoration: const BoxDecoration(color: Colors.white, border: Border(top: BorderSide(color: AppColors.border))),
                  child: SafeArea(
                    top: false,
                    child: Row(children: [
                      OutlinedButton.icon(onPressed: () => _pdf(inv), icon: const Icon(Icons.picture_as_pdf_outlined), label: const Text('PDF')),
                      SizedBox(width: 10.w),
                      if (auth.can('training.payment.record') && status != 'PAID' && status != 'CANCELLED' && open > 0)
                        Expanded(
                          child: FilledButton.icon(
                            onPressed: () => _pay(inv, auth.can('training.payment.verify')),
                            icon: const Icon(Icons.payments_rounded),
                            label: const Text('Record payment'),
                          ),
                        ),
                    ]),
                  ),
                ),
              ),
            ]);
          },
        ),
      ),
    );
  }

  List<Widget> _trace(Map p) {
    final lines = <String>[
      if (s(p['payerName']).isNotEmpty) 'From ${s(p['payerName'])}${s(p['payerBank']).isEmpty ? '' : ' · ${s(p['payerBank'])}'}',
      if (s(p['payerContact']).isNotEmpty) 'Mobile ${s(p['payerContact'])}',
      if (s(p['referenceNumber']).isNotEmpty) 'Reference ${s(p['referenceNumber'])}',
      if (s(p['chequeNumber']).isNotEmpty) 'Cheque ${s(p['chequeNumber'])}',
      if (s(p['destinationAccountLabel']).isNotEmpty) 'Into ${s(p['destinationAccountLabel'])}',
      if (s(p['valueDate']).isNotEmpty) 'Date ${tDate(p['valueDate'])}',
      'Recorded by ${s(p['receivedByName']).isEmpty ? '—' : s(p['receivedByName'])}',
      if (s(p['confirmedByName']).isNotEmpty && s(p['status']) != 'PENDING')
        '${s(p['status']) == 'REJECTED' ? 'Rejected' : 'Confirmed'} by ${s(p['confirmedByName'])}${s(p['statementReference']).isEmpty ? '' : ' · statement ${s(p['statementReference'])}'}',
      if (s(p['rejectReason']).isNotEmpty) 'Reason: ${s(p['rejectReason'])}',
    ];
    return [for (final l in lines) Text(l, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary))];
  }

  Widget _total(String label, String value, Color color) => Expanded(
        child: Container(
          padding: EdgeInsets.symmetric(vertical: 12.h, horizontal: 10.w),
          decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(14.r), border: Border.all(color: AppColors.border)),
          child: Column(children: [
            Text(value, style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800, color: color)),
            SizedBox(height: 2.h),
            Text(label, style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted, fontWeight: FontWeight.w600)),
          ]),
        ),
      );
}
