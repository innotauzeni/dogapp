import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:printing/printing.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_client.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/training_repository.dart';
import '../widgets/training_ui.dart';
import 'training_final_form_screen.dart';
import 'training_health_form_screen.dart';
import 'training_invoice_screen.dart';
import 'training_log_screen.dart';

/// One dog's whole training journey - the same page as the web "Training Enrolment":
/// the 8-step progress strip, the actions that apply at this stage (and that the user may do), and the tabs.
class TrainingEnrolmentScreen extends StatefulWidget {
  final int enrollmentId;
  const TrainingEnrolmentScreen({super.key, required this.enrollmentId});

  @override
  State<TrainingEnrolmentScreen> createState() => _TrainingEnrolmentScreenState();
}

class _TrainingEnrolmentScreenState extends State<TrainingEnrolmentScreen> {
  static const _steps = [
    ('Health check', Icons.favorite_rounded),
    ('Invoice', Icons.receipt_rounded),
    ('Payment', Icons.payments_rounded),
    ('Handler', Icons.person_pin_rounded),
    ('Training', Icons.pets_rounded),
    ('Final test', Icons.assignment_turned_in_rounded),
    ('Approval', Icons.verified_rounded),
    ('Certificate', Icons.workspace_premium_rounded),
  ];
  static const _tabs = ['Overview', 'Health', 'Handler', 'Weekly plan', 'Daily logs', 'Reports', 'Assessments'];

  final _repo = TrainingRepository();
  late Future<Map<String, dynamic>> _future;
  int _tab = 0;
  Future<List<Map<String, dynamic>>>? _logs;
  Future<List<Map<String, dynamic>>>? _reports;

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  Future<Map<String, dynamic>> _load() => _repo.rawMap('Training/enrollments/${widget.enrollmentId}');

  Future<void> _refresh() async {
    setState(() {
      _future = _load();
      _logs = null;
      _reports = null;
    });
    await _future.catchError((_) => <String, dynamic>{});
  }

  int _current(String status, bool hasCert) {
    switch (status) {
      case 'AwaitingHealthAssessment':
      case 'HealthFailed':
        return 0;
      case 'HealthPassed':
        return 1;
      case 'AwaitingPayment':
        return 2;
      case 'AwaitingHandler':
        return 3;
      case 'Ready':
      case 'InTraining':
        return 4;
      case 'AwaitingFinalAssessment':
        return 5;
      case 'AwaitingApproval':
        return 6;
      case 'Completed':
        return hasCert ? 8 : 7;
      default:
        return 0;
    }
  }

  Future<void> _run(Future<dynamic> Function() action, String ok) async {
    try {
      await action();
      if (mounted) trToast(context, ok);
      _refresh();
    } catch (e) {
      if (mounted) trToast(context, errText(e), error: true);
    }
  }

  // ── Actions ──────────────────────────────────────────────────────────────

  Future<void> _generateInvoice(Map<String, dynamic> e) async {
    final days = TextEditingController(text: '7');
    final note = TextEditingController();
    final go = await trSheet<bool>(context, title: 'Generate training invoice', body: (ctx) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      KV('Programme', s(e['programmeName'])),
      KV('Fee', tMoney(e['fee'])),
      KV('Additional charges', tMoney(e['additionalCharges'])),
      KV('Total', tMoney(e['total'])),
      TextField(controller: days, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Payment due in (days)')),
      SizedBox(height: 10.h),
      TextField(controller: note, decoration: const InputDecoration(labelText: 'Note on invoice (optional)')),
      SizedBox(height: 6.h),
      Text('Training can start only once the invoice is paid in full.', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
      SizedBox(height: 14.h),
      SizedBox(width: double.infinity, child: FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Create invoice'))),
    ]));
    if (go == true) {
      await _run(() => _repo.rawPost('Training/enrollments/${widget.enrollmentId}/invoice', {'dueInDays': int.tryParse(days.text) ?? 7, 'notes': note.text.trim()}), 'Invoice created.');
    }
  }

  Future<void> _allocateHandler(Map<String, dynamic> e) async {
    List<Map<String, dynamic>> handlers;
    try {
      handlers = await _repo.rawList('Training/handlers');
    } catch (err) {
      trToast(context, errText(err), error: true);
      return;
    }
    if (!mounted) return;
    String? pick = s(e['handlerUserId']).isEmpty ? null : s(e['handlerUserId']);
    final notes = TextEditingController();
    final go = await trSheet<bool>(context, title: 'Allocate a handler', body: (ctx) => StatefulBuilder(
          builder: (ctx, set) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            if (handlers.isEmpty) const TrEmpty(Icons.person_off_rounded, 'No active users were found. Add users under Configuration → Users.'),
            for (final h in handlers)
              Builder(builder: (_) {
                final uid = s(h['userId']);
                final disabled = h['canAccept'] != true && uid != s(e['handlerUserId']);
                final frac = n(h['currentDogs']) / (n(h['maxDogs']) == 0 ? 1 : n(h['maxDogs']));
                return Opacity(
                  opacity: disabled ? 0.5 : 1,
                  child: Padding(
                    padding: EdgeInsets.only(bottom: 8.h),
                    child: TrCard(
                      border: pick == uid ? AppColors.primary : null,
                      onTap: disabled ? null : () => set(() => pick = uid),
                      child: Row(children: [
                        Icon(pick == uid ? Icons.radio_button_checked : Icons.radio_button_off, color: pick == uid ? AppColors.primary : AppColors.textMuted),
                        SizedBox(width: 10.w),
                        Expanded(
                          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            Row(children: [
                              Expanded(child: Text(s(h['name']), style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700))),
                              Text(h['isAvailable'] == true ? 'Available' : 'Unavailable', style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: h['isAvailable'] == true ? AppColors.success : AppColors.textMuted)),
                            ]),
                            Text('${s(h['specialisation']).isEmpty ? 'No specialisation' : s(h['specialisation'])} · ${s(h['currentDogs'])}/${s(h['maxDogs'])} dogs', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                            SizedBox(height: 6.h),
                            TrProgress(frac, color: frac >= 1 ? AppColors.danger : AppColors.primary),
                          ]),
                        ),
                      ]),
                    ),
                  ),
                );
              }),
            SizedBox(height: 4.h),
            TextField(controller: notes, decoration: const InputDecoration(labelText: 'Allocation notes')),
            SizedBox(height: 14.h),
            SizedBox(width: double.infinity, child: FilledButton(onPressed: pick == null ? null : () => Navigator.pop(ctx, true), child: const Text('Allocate'))),
          ]),
        ));
    if (go == true && pick != null) {
      await _run(() => _repo.rawPost('Training/enrollments/${widget.enrollmentId}/assign-handler', {'handlerUserId': pick, 'notes': notes.text.trim()}), 'Handler allocated.');
    }
  }

  Future<void> _activate(Map<String, dynamic> e) async {
    var date = DateTime.now();
    final go = await trSheet<bool>(context, title: 'Start training', body: (ctx) => StatefulBuilder(
          builder: (ctx, set) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text('Payment is confirmed and a handler is allocated. Training runs for ${s(e['durationDays'])} days and the weekly plans are copied from "${s(e['programmeName'])}".', style: TextStyle(fontSize: 13.sp, color: AppColors.textSecondary)),
            SizedBox(height: 12.h),
            OutlinedButton.icon(
              icon: const Icon(Icons.event),
              label: Text('Start date: ${tDate(date.toIso8601String())}'),
              onPressed: () async {
                final d = await showDatePicker(context: ctx, initialDate: date, firstDate: DateTime.now().subtract(const Duration(days: 30)), lastDate: DateTime.now().add(const Duration(days: 60)));
                if (d != null) set(() => date = d);
              },
            ),
            SizedBox(height: 14.h),
            SizedBox(width: double.infinity, child: FilledButton.icon(onPressed: () => Navigator.pop(ctx, true), icon: const Icon(Icons.play_arrow_rounded), label: const Text('Start training'))),
          ]),
        ));
    if (go == true) await _run(() => _repo.rawPost('Training/enrollments/${widget.enrollmentId}/activate', {'startDate': date.toIso8601String()}), 'Training started.');
  }

  Future<void> _extend() async {
    final days = TextEditingController(text: '7');
    final acts = TextEditingController();
    final go = await trSheet<bool>(context, title: 'Extend training', body: (ctx) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      TextField(controller: days, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Extra days *')),
      SizedBox(height: 10.h),
      TextField(controller: acts, minLines: 3, maxLines: 6, decoration: const InputDecoration(labelText: 'Additional activities (one per line)', alignLabelWithHint: true)),
      SizedBox(height: 14.h),
      SizedBox(width: double.infinity, child: FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Extend training'))),
    ]));
    if (go == true) await _run(() => _repo.rawPost('Training/enrollments/${widget.enrollmentId}/extend', {'days': int.tryParse(days.text) ?? 0, 'additionalActivities': acts.text}), 'Training extended.');
  }

  Future<void> _cancel() async {
    final reason = TextEditingController();
    final go = await trSheet<bool>(context, title: 'Cancel enrolment', body: (ctx) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      TextField(controller: reason, minLines: 2, maxLines: 4, decoration: const InputDecoration(labelText: 'Reason')),
      SizedBox(height: 14.h),
      SizedBox(width: double.infinity, child: FilledButton(style: FilledButton.styleFrom(backgroundColor: AppColors.danger), onPressed: () => Navigator.pop(ctx, true), child: const Text('Cancel enrolment'))),
    ]));
    if (go == true) await _run(() => _repo.rawPost('Training/enrollments/${widget.enrollmentId}/cancel', {'reason': reason.text.trim()}), 'Enrolment cancelled.');
  }

  Future<void> _issueCertificate(Map<String, dynamic> e) async {
    final fa = ((e['finalAssessments'] as List?) ?? const []).whereType<Map>().where((f) => s(f['approvalStatus']) == 'Approved').toList();
    if (fa.isEmpty) return trToast(context, 'No approved assessment found.', error: true);
    await _run(() => _repo.rawPost('Training/certificates/issue/${fa.first['id']}'), 'Certificate issued.');
  }

  Future<void> _certificatePdf(int id) async {
    try {
      final bytes = await ApiClient.instance.getBytes('Training/certificates/$id/pdf');
      await Printing.layoutPdf(onLayout: (_) async => bytes, name: 'Training_Certificate.pdf');
    } catch (e) {
      if (mounted) trToast(context, 'Could not open the certificate: ${errText(e)}', error: true);
    }
  }

  Future<void> _decideHealth(int id, String result) async {
    final c = TextEditingController();
    final go = await trSheet<bool>(context, title: result == 'Passed' ? 'Pass this health check' : 'Fail this health check', body: (ctx) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      TextField(controller: c, minLines: 2, maxLines: 4, decoration: const InputDecoration(labelText: 'Comments')),
      SizedBox(height: 14.h),
      SizedBox(width: double.infinity, child: FilledButton(style: FilledButton.styleFrom(backgroundColor: result == 'Passed' ? AppColors.success : AppColors.danger), onPressed: () => Navigator.pop(ctx, true), child: Text(result == 'Passed' ? 'Mark as passed' : 'Mark as failed'))),
    ]));
    if (go == true) await _run(() => _repo.rawPost('Training/health-assessments/$id/decide', {'result': result, 'comments': c.text.trim()}), 'Health assessment $result.');
  }

  Future<void> _approve(int id, bool approve) async {
    final c = TextEditingController();
    final go = await trSheet<bool>(context, title: approve ? 'Approve completion' : 'Return the assessment', body: (ctx) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      TextField(controller: c, minLines: 2, maxLines: 4, decoration: const InputDecoration(labelText: 'Comments')),
      SizedBox(height: 14.h),
      SizedBox(width: double.infinity, child: FilledButton(onPressed: () => Navigator.pop(ctx, true), child: Text(approve ? 'Approve' : 'Return'))),
    ]));
    if (go == true) await _run(() => _repo.rawPost('Training/final-assessments/$id/approve', {'approve': approve, 'comments': c.text.trim()}), approve ? 'Approved.' : 'Returned.');
  }

  // ── Build ────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(title: const Text('Training enrolment')),
      body: SafeArea(
        top: false,
        child: AsyncView<Map<String, dynamic>>(
          future: _future,
          onRetry: _refresh,
          builder: (context, e) => RefreshIndicator(
            onRefresh: _refresh,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 28.h),
              children: [
                _header(e),
                SizedBox(height: 12.h),
                _stepper(e),
                ..._message(e),
                SizedBox(height: 12.h),
                _actions(e, auth),
                SizedBox(height: 14.h),
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(children: [
                    for (var i = 0; i < _tabs.length; i++)
                      Padding(
                        padding: EdgeInsets.only(right: 8.w),
                        child: ChoiceChip(label: Text(_tabLabel(i, e)), selected: _tab == i, onSelected: (_) => setState(() => _tab = i)),
                      ),
                  ]),
                ),
                SizedBox(height: 10.h),
                _tabBody(e, auth),
              ],
            ),
          ),
        ),
      ),
    );
  }

  String _tabLabel(int i, Map<String, dynamic> e) => switch (i) {
        1 => 'Health (${((e['healthAssessments'] as List?) ?? const []).length})',
        4 => 'Daily logs (${s(e['dailyLogCount'])})',
        5 => 'Reports (${s(e['reportCount'])})',
        6 => 'Assessments (${((e['finalAssessments'] as List?) ?? const []).length})',
        _ => _tabs[i],
      };

  Widget _header(Map<String, dynamic> e) {
    final inTraining = s(e['status']) == 'InTraining';
    final total = n(e['durationDays']) + n(e['extensionDays']);
    return TrCard(
      child: Column(children: [
        Row(children: [
          DogAvatar(url: e['dogPhotoUrl']?.toString(), name: s(e['dogName']), size: 56),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(s(e['dogName']), style: TextStyle(fontSize: 18.sp, fontWeight: FontWeight.w800)),
              Text('${s(e['dogBreed'])}${s(e['customerName']).isEmpty ? '' : ' · Owner: ${s(e['customerName'])}'}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
            ]),
          ),
        ]),
        SizedBox(height: 10.h),
        Row(children: [
          Text(s(e['enrollmentNumber']), style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w800)),
          const Spacer(),
          TrPill(s(e['status'])),
        ]),
        if (inTraining) ...[
          SizedBox(height: 10.h),
          Row(children: [
            Text('Week ${s(e['currentWeek'])} · Day ${s(e['dayNumber'])} of $total', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
            const Spacer(),
            Text('${(n(e['dayNumber']) * 100 / (total == 0 ? 1 : total)).clamp(0, 100).round()}%', style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w800)),
          ]),
          SizedBox(height: 6.h),
          TrProgress(n(e['dayNumber']) / (total == 0 ? 1 : total)),
        ],
      ]),
    );
  }

  Widget _stepper(Map<String, dynamic> e) {
    final cancelled = s(e['status']) == 'Cancelled';
    final failed = s(e['status']) == 'HealthFailed';
    final cur = _current(s(e['status']), e['certificate'] != null);
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(children: [
        for (var i = 0; i < _steps.length; i++) ...[
          Builder(builder: (_) {
            final done = !cancelled && i < cur;
            final isCur = !cancelled && i == cur;
            final bad = failed && i == 0;
            final color = bad ? AppColors.danger : done ? AppColors.success : isCur ? AppColors.primary : AppColors.textMuted;
            return Container(
              width: 84.w,
              padding: EdgeInsets.symmetric(vertical: 10.h, horizontal: 4.w),
              decoration: BoxDecoration(
                color: isCur ? AppColors.primary.withValues(alpha: 0.08) : Colors.white,
                borderRadius: BorderRadius.circular(14.r),
                border: Border.all(color: isCur || bad ? color : AppColors.border),
              ),
              child: Column(children: [
                Icon(done ? Icons.check_circle_rounded : _steps[i].$2, color: color, size: 22.r),
                SizedBox(height: 4.h),
                Text(_steps[i].$1, textAlign: TextAlign.center, style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w700, color: color)),
              ]),
            );
          }),
          if (i < _steps.length - 1) SizedBox(width: 6.w),
        ],
      ]),
    );
  }

  List<Widget> _message(Map<String, dynamic> e) {
    String? text;
    Color color = AppColors.warning;
    switch (s(e['status'])) {
      case 'HealthFailed':
        text = 'This dog did not pass its health assessment and cannot start training. It can be re-assessed.';
        color = AppColors.danger;
      case 'AwaitingPayment':
        text = 'Invoice ${s(e['invoiceNumber'])} is ${s(e['invoiceStatus']).toLowerCase()}'
            '${dbl(e['invoiceBalance']) > 0 ? ' — balance due ${tMoney(e['invoiceBalance'])}' : ''}. Training cannot start until it is paid in full.';
      case 'Cancelled':
        text = 'Cancelled${s(e['cancelReason']).isEmpty ? '' : ': ${s(e['cancelReason'])}'}';
        color = AppColors.danger;
    }
    if (text == null) return const [];
    return [
      SizedBox(height: 12.h),
      Container(
        padding: EdgeInsets.all(12.w),
        decoration: BoxDecoration(color: color.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(12.r), border: Border.all(color: color.withValues(alpha: 0.4))),
        child: Text(text, style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w600, color: color)),
      ),
    ];
  }

  Widget _actions(Map<String, dynamic> e, AuthProvider auth) {
    final st = s(e['status']);
    final id = widget.enrollmentId;
    final hasInvoice = e['invoiceId'] != null;
    final cert = e['certificate'] as Map?;

    Widget fill(String label, IconData icon, VoidCallback onTap, {Color? color}) => FilledButton.icon(
          style: color == null ? null : FilledButton.styleFrom(backgroundColor: color),
          onPressed: onTap,
          icon: Icon(icon, size: 18),
          label: Text(label),
        );
    Widget soft(String label, IconData icon, VoidCallback onTap, {Color? color}) => OutlinedButton.icon(
          style: color == null ? null : OutlinedButton.styleFrom(foregroundColor: color),
          onPressed: onTap,
          icon: Icon(icon, size: 18),
          label: Text(label),
        );

    final a = <Widget>[
      if ((st == 'AwaitingHealthAssessment' || st == 'HealthFailed') && auth.can('training.health.create'))
        fill(st == 'HealthFailed' ? 'Re-assess health' : 'Record health assessment', Icons.favorite_rounded, () async {
          final ok = await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => TrainingHealthFormScreen(enrollmentId: id, dogName: s(e['dogName']))));
          if (ok == true) _refresh();
        }),
      if (e['canInvoice'] == true && auth.can('training.invoice.generate')) fill('Generate invoice', Icons.receipt_rounded, () => _generateInvoice(e)),
      if (hasInvoice)
        soft('Invoice ${s(e['invoiceNumber'])}', Icons.description_outlined, () async {
          await Navigator.of(context).push(MaterialPageRoute(builder: (_) => TrainingInvoiceScreen(invoiceId: n(e['invoiceId']))));
          _refresh();
        }),
      if (hasInvoice && st == 'AwaitingPayment' && auth.can('training.payment.record'))
        fill('Record payment', Icons.payments_rounded, () async {
          await Navigator.of(context).push(MaterialPageRoute(builder: (_) => TrainingInvoiceScreen(invoiceId: n(e['invoiceId']))));
          _refresh();
        }, color: AppColors.success),
      if (e['canAssignHandler'] == true && auth.can('training.handler.assign')) soft(s(e['handlerUserId']).isEmpty ? 'Allocate handler' : 'Change handler', Icons.person_add_alt_1_rounded, () => _allocateHandler(e)),
      if (e['canActivate'] == true && auth.can('training.handler.assign')) fill('Start training', Icons.play_arrow_rounded, () => _activate(e), color: AppColors.success),
      if ((st == 'InTraining' || st == 'AwaitingFinalAssessment') && (auth.can('training.log.record') || auth.can('training.log.view')))
        fill("Log today's training", Icons.edit_note_rounded, () async {
          await Navigator.of(context).push(MaterialPageRoute(
            builder: (_) => TrainingLogScreen(
              dog: TrainingDog(
                id: id,
                dogName: s(e['dogName']),
                breed: s(e['dogBreed']),
                customerName: s(e['customerName']),
                status: st,
                currentWeek: n(e['currentWeek']),
                dayNumber: n(e['dayNumber']),
                totalDays: n(e['durationDays']) + n(e['extensionDays']),
                photoUrl: e['dogPhotoUrl']?.toString(),
              ),
            ),
          ));
          _refresh();
        }),
      if (st == 'AwaitingFinalAssessment' && auth.can('training.assessment.conduct'))
        fill('Final assessment', Icons.assignment_turned_in_rounded, () async {
          final ok = await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => TrainingFinalFormScreen(enrollmentId: id, dogName: s(e['dogName']))));
          if (ok == true) _refresh();
        }),
      if ((st == 'AwaitingFinalAssessment' || st == 'InTraining') && auth.can('training.assessment.approve')) soft('Extend training', Icons.update_rounded, _extend),
      if (st == 'Completed' && cert != null) fill('Download certificate', Icons.workspace_premium_rounded, () => _certificatePdf(n(cert['id'])), color: AppColors.success),
      if (st == 'Completed' && cert == null && auth.can('training.certificate.issue')) fill('Issue certificate', Icons.workspace_premium_rounded, () => _issueCertificate(e), color: AppColors.success),
      if (st != 'Completed' && st != 'Cancelled' && auth.can('training.enrolment.create')) soft('Cancel', Icons.cancel_outlined, _cancel, color: AppColors.danger),
    ];
    if (a.isEmpty) return const SizedBox.shrink();
    return Wrap(spacing: 8.w, runSpacing: 8.h, children: a);
  }

  Widget _tabBody(Map<String, dynamic> e, AuthProvider auth) {
    switch (_tab) {
      case 0:
        return TrCard(
          child: Column(children: [
            Row(children: [Expanded(child: KV('Programme', s(e['programmeName']))), Expanded(child: KV('Duration', '${s(e['durationDays'])} days${n(e['extensionDays']) > 0 ? ' (+${s(e['extensionDays'])})' : ''}'))]),
            Row(children: [Expanded(child: KV('Fee', tMoney(e['fee']))), Expanded(child: KV('Additional charges', tMoney(e['additionalCharges'])))]),
            Row(children: [Expanded(child: KV('Total', tMoney(e['total']))), Expanded(child: KV('Customer', s(e['customerName'])))]),
            Row(children: [Expanded(child: KV('Handler', s(e['handlerName']))), Expanded(child: KV('Started', tDate(e['startDate'])))]),
            Row(children: [Expanded(child: KV('Planned end', tDate(e['plannedEndDate']))), Expanded(child: KV('Created', tDate(e['createdAt'])))]),
            if (s(e['notes']).isNotEmpty) Align(alignment: Alignment.centerLeft, child: KV('Notes', s(e['notes']))),
          ]),
        );
      case 1:
        final list = ((e['healthAssessments'] as List?) ?? const []).whereType<Map>().toList();
        if (list.isEmpty) return const TrCard(child: TrEmpty(Icons.favorite_border_rounded, 'No health assessment recorded yet.'));
        return Column(children: [
          for (final h in list)
            Padding(
              padding: EdgeInsets.only(bottom: 8.h),
              child: TrCard(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Row(children: [Expanded(child: Text(tDate(h['assessmentDate']), style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800))), TrPill(s(h['result']))]),
                  SizedBox(height: 4.h),
                  Text('${s(h['examinerName']).isEmpty ? '—' : s(h['examinerName'])}${h['weightKg'] == null ? '' : ' · ${s(h['weightKg'])} kg'}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                  if (s(h['physicalCondition']).isNotEmpty) KV('Condition', s(h['physicalCondition'])),
                  if (s(h['recommendation']).isNotEmpty) KV('Recommendation', s(h['recommendation'])),
                  if (s(h['comments']).isNotEmpty) KV('Comments', s(h['comments'])),
                  if (s(h['result']) == 'Pending' && auth.can('training.health.approve'))
                    Padding(
                      padding: EdgeInsets.only(top: 6.h),
                      child: Row(children: [
                        Expanded(child: FilledButton(style: FilledButton.styleFrom(backgroundColor: AppColors.success), onPressed: () => _decideHealth(n(h['id']), 'Passed'), child: const Text('Pass'))),
                        SizedBox(width: 8.w),
                        Expanded(child: FilledButton(style: FilledButton.styleFrom(backgroundColor: AppColors.danger), onPressed: () => _decideHealth(n(h['id']), 'Failed'), child: const Text('Fail'))),
                      ]),
                    ),
                ]),
              ),
            ),
        ]);
      case 2:
        final list = ((e['assignments'] as List?) ?? const []).whereType<Map>().toList();
        if (list.isEmpty) return const TrCard(child: TrEmpty(Icons.person_outline_rounded, 'No handler allocated yet.'));
        return Column(children: [
          for (final a in list)
            Padding(
              padding: EdgeInsets.only(bottom: 8.h),
              child: TrCard(
                child: Row(children: [
                  const Icon(Icons.person_pin_rounded, color: AppColors.primary),
                  SizedBox(width: 10.w),
                  Expanded(
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Text(s(a['handlerName']), style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                      Text('From ${tDate(a['assignedOn'])}${s(a['endedOn']).isEmpty ? '' : ' to ${tDate(a['endedOn'])}'} · by ${s(a['assignedByName']).isEmpty ? '—' : s(a['assignedByName'])}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                      if (s(a['notes']).isNotEmpty) Text(s(a['notes']), style: TextStyle(fontSize: 12.sp)),
                    ]),
                  ),
                  if (a['isCurrent'] == true) const TrPill('Approved'),
                ]),
              ),
            ),
        ]);
      case 3:
        final plans = ((e['weeklyPlans'] as List?) ?? const []).whereType<Map>().toList();
        if (plans.isEmpty) return const TrCard(child: TrEmpty(Icons.calendar_month_rounded, "This dog's weekly plan is created when training is started."));
        return Column(children: [
          for (final p in plans)
            Padding(
              padding: EdgeInsets.only(bottom: 10.h),
              child: TrCard(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text('Week ${s(p['weekNumber'])}: ${s(p['title'])}', style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800)),
                  if (s(p['focus']).isNotEmpty) Text(s(p['focus']), style: TextStyle(fontSize: 12.sp, color: AppColors.primary, fontWeight: FontWeight.w700)),
                  Text('${tDate(p['startDate'])} – ${tDate(p['endDate'])}', style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                  if (s(p['targets']).isNotEmpty) ...[
                    SizedBox(height: 8.h),
                    for (final t in s(p['targets']).split('\n').where((x) => x.trim().isNotEmpty))
                      Padding(padding: EdgeInsets.only(bottom: 2.h), child: Text('• $t', style: TextStyle(fontSize: 12.sp))),
                  ],
                  SizedBox(height: 8.h),
                  for (final sl in ((p['schedule'] as List?) ?? const []).whereType<Map>())
                    Padding(
                      padding: EdgeInsets.symmetric(vertical: 4.h),
                      child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        SizedBox(width: 56.w, child: Text(s(sl['time']), style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w800))),
                        Expanded(
                          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            Text(s(sl['title']), style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600)),
                            if (s(sl['purpose']).isNotEmpty) Text(s(sl['purpose']), style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary)),
                          ]),
                        ),
                      ]),
                    ),
                ]),
              ),
            ),
        ]);
      case 4:
        _logs ??= _repo.rawList('Training/logs', query: {'enrollmentId': widget.enrollmentId});
        return FutureBuilder<List<Map<String, dynamic>>>(
          future: _logs,
          builder: (context, snap) {
            if (snap.connectionState != ConnectionState.done) return const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator()));
            final list = snap.data ?? const [];
            if (list.isEmpty) return TrCard(child: TrEmpty(Icons.menu_book_outlined, snap.hasError ? 'You do not have access to the logbook.' : 'No daily records yet.'));
            return Column(children: [
              for (final l in list)
                Padding(
                  padding: EdgeInsets.only(bottom: 8.h),
                  child: TrCard(
                    child: Row(children: [
                      Container(
                        width: 44.r,
                        height: 44.r,
                        decoration: BoxDecoration(color: AppColors.surfaceAlt, borderRadius: BorderRadius.circular(12.r)),
                        child: Center(child: Text('W${s(l['weekNumber'])}\nD${s(l['dayNumber'])}', textAlign: TextAlign.center, style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w800))),
                      ),
                      SizedBox(width: 12.w),
                      Expanded(
                        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Text(tDate(l['trainingDate']), style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
                          Text('${s(l['handlerName']).isEmpty ? '—' : s(l['handlerName'])} · ${s(l['durationMinutes'])} min', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                        ]),
                      ),
                      Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
                        if (l['averageSlotRating'] != null) Text('${s(l['averageSlotRating'])}/10', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800, color: AppColors.primary)),
                        Text(s(l['performance']), style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                      ]),
                    ]),
                  ),
                ),
            ]);
          },
        );
      case 5:
        _reports ??= _repo.rawList('Training/progress-reports', query: {'enrollmentId': widget.enrollmentId});
        final weeks = ((e['weeklyPlans'] as List?) ?? const []).whereType<Map>().map((p) => n(p['weekNumber'])).toList();
        if (weeks.isEmpty) return const TrCard(child: TrEmpty(Icons.fact_check_outlined, 'Reports appear once training is active.'));
        return FutureBuilder<List<Map<String, dynamic>>>(
          future: _reports,
          builder: (context, snap) {
            if (snap.connectionState != ConnectionState.done) return const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator()));
            final list = snap.data ?? const [];
            return Column(children: [
              for (final w in weeks)
                Builder(builder: (_) {
                  final rep = list.where((x) => n(x['weekNumber']) == w).toList();
                  return Padding(
                    padding: EdgeInsets.only(bottom: 8.h),
                    child: TrCard(
                      child: Row(children: [
                        Expanded(
                          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            Text('Week $w', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800)),
                            Text(rep.isEmpty ? 'Not started' : '${s(rep.first['handlerName'])}${s(rep.first['submittedOn']).isEmpty ? '' : ' · submitted ${tDate(rep.first['submittedOn'])}'}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                            if (rep.isNotEmpty && s(rep.first['reviewComments']).isNotEmpty) Text('Review: ${s(rep.first['reviewComments'])}', style: TextStyle(fontSize: 12.sp)),
                          ]),
                        ),
                        if (rep.isNotEmpty) TrPill(s(rep.first['status'])),
                      ]),
                    ),
                  );
                }),
            ]);
          },
        );
      default:
        final list = ((e['finalAssessments'] as List?) ?? const []).whereType<Map>().toList();
        if (list.isEmpty) return const TrCard(child: TrEmpty(Icons.assignment_outlined, 'No final assessment yet.'));
        return Column(children: [
          for (final f in list)
            Padding(
              padding: EdgeInsets.only(bottom: 8.h),
              child: TrCard(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Row(children: [
                    Expanded(child: Text('Attempt #${s(f['attemptNumber'])} · ${tDate(f['assessmentDate'])}', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800))),
                    TrPill(s(f['result'])),
                  ]),
                  Text('${s(f['assessorName']).isEmpty ? '—' : s(f['assessorName'])}${f['overallScore'] == null ? '' : ' · score ${s(f['overallScore'])}/10'}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                  SizedBox(height: 6.h),
                  for (final it in ((f['items'] as List?) ?? const []).whereType<Map>())
                    Padding(
                      padding: EdgeInsets.symmetric(vertical: 3.h),
                      child: Row(children: [
                        Expanded(child: Text(s(it['area']), style: TextStyle(fontSize: 12.sp))),
                        SizedBox(width: 90.w, child: TrProgress(n(it['score']) / 10, color: n(it['score']) >= 6 ? AppColors.success : AppColors.warning)),
                        SizedBox(width: 8.w),
                        Text('${s(it['score'])}', style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w800)),
                      ]),
                    ),
                  if (s(f['comments']).isNotEmpty) KV('Comments', s(f['comments'])),
                  if (s(f['result']) == 'Passed') Row(children: [TrPill(s(f['approvalStatus']).isEmpty ? 'Pending' : s(f['approvalStatus'])), if (s(f['certificateNumber']).isNotEmpty) Padding(padding: EdgeInsets.only(left: 8.w), child: Text(s(f['certificateNumber']), style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w700)))]),
                  if (s(f['result']) == 'Passed' && s(f['approvalStatus']) == 'Pending' && auth.can('training.assessment.approve'))
                    Padding(
                      padding: EdgeInsets.only(top: 8.h),
                      child: Row(children: [
                        Expanded(child: FilledButton(onPressed: () => _approve(n(f['id']), true), child: const Text('Approve'))),
                        SizedBox(width: 8.w),
                        Expanded(child: OutlinedButton(onPressed: () => _approve(n(f['id']), false), child: const Text('Return'))),
                      ]),
                    ),
                ]),
              ),
            ),
        ]);
    }
  }
}
