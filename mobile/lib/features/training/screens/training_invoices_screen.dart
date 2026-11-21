import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../data/training_repository.dart';
import '../widgets/training_ui.dart';
import 'training_invoice_screen.dart';

/// Training invoices & payments: totals on top, a card per invoice, tap for the payments and to record one.
class TrainingInvoicesScreen extends StatefulWidget {
  const TrainingInvoicesScreen({super.key});

  @override
  State<TrainingInvoicesScreen> createState() => _TrainingInvoicesScreenState();
}

class _TrainingInvoicesScreenState extends State<TrainingInvoicesScreen> {
  static const _filters = <(String, String?)>[('All', null), ('Unpaid', 'SENT'), ('Part paid', 'PARTIAL'), ('Paid', 'PAID'), ('Cancelled', 'CANCELLED')];
  final _repo = TrainingRepository();
  final _search = TextEditingController();
  late Future<List<Map<String, dynamic>>> _future;
  String? _status;

  @override
  void initState() {
    super.initState();
    _future = _repo.rawList('Training/invoices');
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _refresh() async {
    setState(() => _future = _repo.rawList('Training/invoices'));
    await _future.catchError((_) => <Map<String, dynamic>>[]);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Invoices & Payments')),
      body: SafeArea(
        top: false,
        child: AsyncView<List<Map<String, dynamic>>>(
          future: _future,
          onRetry: _refresh,
          builder: (context, all) {
            final live = all.where((i) => s(i['status']) != 'CANCELLED').toList();
            final billed = live.fold<double>(0, (a, i) => a + dbl(i['totalAmount']));
            final paid = live.fold<double>(0, (a, i) => a + dbl(i['paidAmount']));
            final pending = live.fold<double>(0, (a, i) => a + dbl(i['pendingAmount']));
            final q = _search.text.trim().toLowerCase();
            final rows = all.where((i) {
              if (_status != null && s(i['status']) != _status) return false;
              return q.isEmpty || '${s(i['invoiceNumber'])} ${s(i['customerName'])} ${s(i['dogName'])}'.toLowerCase().contains(q);
            }).toList();
            return RefreshIndicator(
              onRefresh: _refresh,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 24.h),
                children: [
                  Row(children: [
                    _kpi('Invoiced', tMoney(billed), Icons.receipt_long_rounded, const Color(0xFF2563EB)),
                    SizedBox(width: 8.w),
                    _kpi('Received', tMoney(paid), Icons.payments_rounded, AppColors.success),
                    SizedBox(width: 8.w),
                    _kpi('Outstanding', tMoney(billed - paid), Icons.hourglass_top_rounded, AppColors.warning),
                  ]),
                  if (pending > 0)
                    Padding(
                      padding: EdgeInsets.only(top: 8.h),
                      child: Text('${tMoney(pending)} is waiting to be confirmed on the bank statement.', style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w600, color: const Color(0xFF92400E))),
                    ),
                  SizedBox(height: 12.h),
                  TextField(controller: _search, onChanged: (_) => setState(() {}), decoration: const InputDecoration(prefixIcon: Icon(Icons.search_rounded), hintText: 'Search invoice, customer or dog')),
                  SizedBox(height: 10.h),
                  SizedBox(
                    height: 40.h,
                    child: ListView.separated(
                      scrollDirection: Axis.horizontal,
                      itemCount: _filters.length,
                      separatorBuilder: (_, __) => SizedBox(width: 8.w),
                      itemBuilder: (_, i) => ChoiceChip(label: Text(_filters[i].$1), selected: _status == _filters[i].$2, onSelected: (_) => setState(() => _status = _filters[i].$2)),
                    ),
                  ),
                  SizedBox(height: 8.h),
                  if (rows.isEmpty)
                    const TrEmpty(Icons.receipt_long_outlined, 'No invoices here. They are created from an enrolment once the dog passes its health check.')
                  else
                    for (final i in rows)
                      Padding(
                        padding: EdgeInsets.only(bottom: 10.h),
                        child: TrCard(
                          onTap: () async {
                            await Navigator.of(context).push(MaterialPageRoute(builder: (_) => TrainingInvoiceScreen(invoiceId: n(i['id']))));
                            _refresh();
                          },
                          child: Row(children: [
                            Expanded(
                              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                Row(children: [
                                  Expanded(child: Text(s(i['invoiceNumber']), style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800))),
                                  TrPill(s(i['status'])),
                                ]),
                                SizedBox(height: 2.h),
                                Text('${s(i['customerName'])}${s(i['dogName']).isEmpty ? '' : ' · ${s(i['dogName'])}'}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                                SizedBox(height: 6.h),
                                Row(children: [
                                  Text('Total ${tMoney(i['totalAmount'])}', style: TextStyle(fontSize: 12.sp)),
                                  const Spacer(),
                                  Text('Balance ${tMoney(i['balanceAmount'])}', style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w800, color: dbl(i['balanceAmount']) > 0 ? AppColors.danger : AppColors.success)),
                                ]),
                                if (dbl(i['pendingAmount']) > 0)
                                  Text('${tMoney(i['pendingAmount'])} awaiting confirmation', style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: const Color(0xFF92400E))),
                              ]),
                            ),
                          ]),
                        ),
                      ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }

  Widget _kpi(String label, String value, IconData icon, Color color) => Expanded(
        child: Container(
          padding: EdgeInsets.all(10.w),
          decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(14.r), border: Border.all(color: AppColors.border)),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Icon(icon, size: 18.r, color: color),
            SizedBox(height: 6.h),
            FittedBox(fit: BoxFit.scaleDown, child: Text(value, style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800))),
            Text(label, style: TextStyle(fontSize: 10.sp, color: AppColors.textMuted, fontWeight: FontWeight.w600)),
          ]),
        ),
      );
}
