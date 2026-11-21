import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/training_repository.dart';
import '../widgets/training_ui.dart';
import 'training_enrolment_screen.dart';

/// Every enrolment as a card with its stage and progress; filter by stage, search by dog / owner, tap to open the journey.
class TrainingEnrolmentsScreen extends StatefulWidget {
  final String? initialStatus;
  const TrainingEnrolmentsScreen({super.key, this.initialStatus});

  @override
  State<TrainingEnrolmentsScreen> createState() => _TrainingEnrolmentsScreenState();
}

class _TrainingEnrolmentsScreenState extends State<TrainingEnrolmentsScreen> {
  static const _filters = <(String, String?)>[
    ('All', null),
    ('Awaiting health', 'AwaitingHealthAssessment'),
    ('Invoice due', 'HealthPassed'),
    ('Awaiting payment', 'AwaitingPayment'),
    ('Needs handler', 'AwaitingHandler'),
    ('Ready', 'Ready'),
    ('In training', 'InTraining'),
    ('Final test', 'AwaitingFinalAssessment'),
    ('Approval', 'AwaitingApproval'),
    ('Completed', 'Completed'),
    ('Cancelled', 'Cancelled'),
  ];

  final _repo = TrainingRepository();
  final _search = TextEditingController();
  late Future<List<Map<String, dynamic>>> _future;
  String? _status;

  @override
  void initState() {
    super.initState();
    _status = widget.initialStatus;
    _future = _repo.rawList('Training/enrollments');
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _refresh() async {
    setState(() => _future = _repo.rawList('Training/enrollments'));
    await _future.catchError((_) => <Map<String, dynamic>>[]);
  }

  Future<void> _open(int id) async {
    await Navigator.of(context).push(MaterialPageRoute(builder: (_) => TrainingEnrolmentScreen(enrollmentId: id)));
    _refresh();
  }

  Future<void> _newEnrolment() async {
    final auth = context.read<AuthProvider>();
    List<Map<String, dynamic>> dogs, programmes;
    try {
      dogs = await _repo.rawList('Training/dogs');
      programmes = await _repo.rawList('Training/programmes/active');
    } catch (e) {
      if (mounted) trToast(context, errText(e), error: true);
      return;
    }
    if (!mounted) return;
    int? dogId, programmeId;
    final charges = TextEditingController();
    final chargesNote = TextEditingController();
    final notes = TextEditingController();
    final ok = await trSheet<bool>(context, title: 'New training enrolment', body: (ctx) => StatefulBuilder(
          builder: (ctx, set) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            DropdownButtonFormField<int>(
              initialValue: dogId,
              isExpanded: true,
              decoration: const InputDecoration(labelText: "Customer's dog *"),
              items: [for (final d in dogs) DropdownMenuItem(value: n(d['id']), child: Text('${s(d['name'])} · ${s(d['customerName'])}', overflow: TextOverflow.ellipsis))],
              onChanged: (v) => set(() => dogId = v),
            ),
            if (dogs.isEmpty) Padding(padding: EdgeInsets.only(top: 6.h), child: Text('Add the dog under Training Dogs on the web portal first.', style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted))),
            SizedBox(height: 10.h),
            DropdownButtonFormField<int>(
              initialValue: programmeId,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Programme *'),
              items: [for (final p in programmes) DropdownMenuItem(value: n(p['id']), child: Text('${s(p['name'])} · ${s(p['durationDays'])} days · ${tMoney(p['fee'])}', overflow: TextOverflow.ellipsis))],
              onChanged: (v) => set(() => programmeId = v),
            ),
            SizedBox(height: 10.h),
            TextField(controller: charges, keyboardType: const TextInputType.numberWithOptions(decimal: true), decoration: const InputDecoration(labelText: 'Additional charges (optional)')),
            SizedBox(height: 10.h),
            TextField(controller: chargesNote, decoration: const InputDecoration(labelText: 'What the extra charge is for')),
            SizedBox(height: 10.h),
            TextField(controller: notes, decoration: const InputDecoration(labelText: 'Notes')),
            SizedBox(height: 14.h),
            SizedBox(width: double.infinity, child: FilledButton(onPressed: dogId == null || programmeId == null ? null : () => Navigator.pop(ctx, true), child: const Text('Create enrolment'))),
          ]),
        ));
    if (ok != true) return;
    try {
      final created = await _repo.rawPost('Training/enrollments', {
        'companyId': auth.activeCompanyId,
        'dogId': dogId,
        'programmeId': programmeId,
        'additionalCharges': double.tryParse(charges.text.trim()) ?? 0,
        'additionalChargesNote': chargesNote.text.trim(),
        'notes': notes.text.trim(),
      });
      if (!mounted) return;
      trToast(context, 'Enrolment created.');
      final id = created is Map ? n(created['id']) : 0;
      if (id > 0) {
        _open(id);
      } else {
        _refresh();
      }
    } catch (e) {
      if (mounted) trToast(context, errText(e), error: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(title: const Text('Training Enrolments')),
      floatingActionButton: auth.can('training.enrolment.create')
          ? FloatingActionButton.extended(heroTag: 'fab-new-enrolment', onPressed: _newEnrolment, icon: const Icon(Icons.add_rounded), label: const Text('New enrolment'))
          : null,
      body: SafeArea(
        top: false,
        child: Column(children: [
          Padding(
            padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 6.h),
            child: TextField(controller: _search, onChanged: (_) => setState(() {}), decoration: const InputDecoration(prefixIcon: Icon(Icons.search_rounded), hintText: 'Search dog, owner or enrolment no.')),
          ),
          SizedBox(
            height: 44.h,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: EdgeInsets.symmetric(horizontal: 16.w),
              itemCount: _filters.length,
              separatorBuilder: (_, __) => SizedBox(width: 8.w),
              itemBuilder: (_, i) => ChoiceChip(label: Text(_filters[i].$1), selected: _status == _filters[i].$2, onSelected: (_) => setState(() => _status = _filters[i].$2)),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _refresh,
              child: AsyncView<List<Map<String, dynamic>>>(
                future: _future,
                onRetry: _refresh,
                builder: (context, data) {
                  final q = _search.text.trim().toLowerCase();
                  final rows = data.where((e) {
                    if (_status != null && s(e['status']) != _status) return false;
                    return q.isEmpty || '${s(e['dogName'])} ${s(e['customerName'])} ${s(e['enrollmentNumber'])}'.toLowerCase().contains(q);
                  }).toList();
                  if (rows.isEmpty) {
                    return ListView(children: [TrEmpty(Icons.assignment_outlined, _status == null && q.isEmpty ? 'No enrolments yet.' : 'Nothing matches this filter.')]);
                  }
                  return ListView.separated(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: EdgeInsets.fromLTRB(16.w, 10.h, 16.w, 100.h),
                    itemCount: rows.length,
                    separatorBuilder: (_, __) => SizedBox(height: 10.h),
                    itemBuilder: (_, i) => _card(rows[i]),
                  );
                },
              ),
            ),
          ),
        ]),
      ),
    );
  }

  Widget _card(Map<String, dynamic> e) {
    final inTraining = s(e['status']) == 'InTraining';
    final total = n(e['durationDays']) + n(e['extensionDays']);
    final balance = dbl(e['invoiceBalance']);
    return TrCard(
      onTap: () => _open(n(e['id'])),
      child: Column(children: [
        Row(children: [
          DogAvatar(url: e['dogPhotoUrl']?.toString(), name: s(e['dogName']), size: 50),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(s(e['dogName']), style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800)),
              Text('${s(e['dogBreed'])}${s(e['customerName']).isEmpty ? '' : ' · ${s(e['customerName'])}'}', maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
              Text(s(e['enrollmentNumber']), style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted, fontWeight: FontWeight.w600)),
            ]),
          ),
          const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
        ]),
        SizedBox(height: 10.h),
        Row(children: [
          TrPill(s(e['status'])),
          const Spacer(),
          if (s(e['handlerName']).isNotEmpty) Flexible(child: Text('🐾 ${s(e['handlerName'])}', maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary))),
        ]),
        if (inTraining) ...[
          SizedBox(height: 10.h),
          Row(children: [
            Text('Week ${s(e['currentWeek'])} · Day ${s(e['dayNumber'])} of $total', style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary)),
            const Spacer(),
            Text('${(n(e['dayNumber']) * 100 / (total == 0 ? 1 : total)).clamp(0, 100).round()}%', style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w800)),
          ]),
          SizedBox(height: 4.h),
          TrProgress(n(e['dayNumber']) / (total == 0 ? 1 : total)),
        ],
        if (e['invoiceId'] != null && balance > 0 && s(e['status']) != 'Cancelled') ...[
          SizedBox(height: 8.h),
          Align(
            alignment: Alignment.centerLeft,
            child: Text('Invoice ${s(e['invoiceNumber'])} · balance due ${tMoney(balance)}', style: TextStyle(fontSize: 11.sp, color: AppColors.warning, fontWeight: FontWeight.w700)),
          ),
        ],
      ]),
    );
  }
}
