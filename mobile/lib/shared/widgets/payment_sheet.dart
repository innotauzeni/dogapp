import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../core/theme/app_colors.dart';
import '../../core/utils/formatters.dart';

/// "Record payment" sheet shared by company invoices and Dog Training invoices - same fields and rules as the web portal:
/// every method asks for what is needed to trace the money (payer, reference, receiving account ...).
class PaymentSheet extends StatefulWidget {
  final String invoiceNumber;
  final String currency;

  /// What can still be paid right now (balance minus payments waiting for confirmation).
  final double open;
  final bool canVerify;
  final Future<List<Map<String, dynamic>>> Function() loadBanks;
  final Future<void> Function(Map<String, dynamic> body) submit;

  const PaymentSheet({
    super.key,
    required this.invoiceNumber,
    required this.currency,
    required this.open,
    required this.loadBanks,
    required this.submit,
    this.canVerify = false,
  });

  @override
  State<PaymentSheet> createState() => _PaymentSheetState();
}

class _PaymentSheetState extends State<PaymentSheet> {
  // code, label, required fields, confirmed straight away?
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
    amount.text = widget.open.toStringAsFixed(2);
    widget.loadBanks().then((b) {
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
    if (amt <= 0) return setState(() => error = 'Enter the amount received.');
    if (amt > widget.open + 0.0001) return setState(() => error = 'The amount is more than what is still open (${widget.open.toStringAsFixed(2)}).');
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
      await widget.submit({
        'method': method,
        'amount': amt,
        'currencyCode': widget.currency,
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
          error = e.toString().replaceFirst('Exception: ', '');
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final req = m.$3;
    final fields = ['payerName', 'payerContact', 'payerBank', 'reference', 'cheque', 'notes'].where(req.contains).toList();
    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
      child: SingleChildScrollView(
        padding: EdgeInsets.all(16.w),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Record payment', style: TextStyle(fontSize: 17.sp, fontWeight: FontWeight.w800)),
            Text('${widget.invoiceNumber} · open ${widget.currency} ${Fmt.money(widget.open, symbol: '')}',
                style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
            SizedBox(height: 12.h),
            Wrap(
              spacing: 8.w,
              runSpacing: 4.h,
              children: [
                for (final e in _methods) ChoiceChip(label: Text(e.$2), selected: method == e.$1, onSelected: (_) => setState(() => method = e.$1)),
              ],
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: amount,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(labelText: 'Amount received (${widget.currency})'),
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
                initialValue: bankId,
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
