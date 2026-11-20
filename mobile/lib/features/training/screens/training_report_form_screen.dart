import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../data/training_repository.dart';

/// The handler's weekly progress report: pick a dog, review what the daily logs already say,
/// add the summary, then save as a draft or submit for review.
class TrainingReportFormScreen extends StatefulWidget {
  const TrainingReportFormScreen({super.key});

  @override
  State<TrainingReportFormScreen> createState() => _TrainingReportFormScreenState();
}

class _TrainingReportFormScreenState extends State<TrainingReportFormScreen> {
  final _repo = TrainingRepository();
  late Future<List<TrainingDog>> _dogs;
  TrainingDog? _dog;
  Map<String, dynamic>? _draft;
  bool _loadingDraft = false;
  bool _busy = false;
  String? _error;

  final _summary = TextEditingController();
  final _challenges = TextEditingController();
  final _improvements = TextEditingController();
  final _recommendations = TextEditingController();

  @override
  void initState() {
    super.initState();
    _dogs = _repo.myDogs().then((l) => l.where((d) => d.isActive).toList());
  }

  @override
  void dispose() {
    for (final c in [_summary, _challenges, _improvements, _recommendations]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _pick(TrainingDog d) async {
    setState(() {
      _dog = d;
      _draft = null;
      _loadingDraft = true;
      _error = null;
    });
    try {
      final draft = await _repo.progressDraft(d.id, d.currentWeek);
      _summary.text = '${draft['performanceSummary'] ?? ''}';
      _challenges.text = '${draft['challenges'] ?? ''}';
      _improvements.text = '${draft['improvements'] ?? ''}';
      _recommendations.text = '${draft['recommendations'] ?? ''}';
      if (mounted) setState(() => _draft = draft);
    } catch (e) {
      if (mounted) setState(() => _error = e is ApiException ? e.message : 'Could not open the report.');
    } finally {
      if (mounted) setState(() => _loadingDraft = false);
    }
  }

  Future<void> _save(bool submit) async {
    final d = _draft, dog = _dog;
    if (d == null || dog == null) return;
    if (submit && _summary.text.trim().length < 5) {
      setState(() => _error = 'Write a short summary of how the dog performed this week before submitting.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await _repo.saveProgressReport({
        'enrollmentId': dog.id,
        'weekNumber': dog.currentWeek,
        'objectives': d['objectives'],
        'activitiesCompleted': d['activitiesCompleted'],
        'activitiesOutstanding': d['activitiesOutstanding'],
        'performanceSummary': _summary.text.trim(),
        'challenges': _challenges.text.trim(),
        'improvements': _improvements.text.trim(),
        'recommendations': _recommendations.text.trim(),
        'submit': submit,
      });
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(submit ? 'Report submitted for review.' : 'Draft saved.')));
      Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) setState(() => _error = e is ApiException ? e.message : 'Could not save the report.');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Weekly progress report')),
      body: SafeArea(
        top: false,
        child: _dog == null
            ? AsyncView<List<TrainingDog>>(
                future: _dogs,
                builder: (context, dogs) {
                  if (dogs.isEmpty) {
                    return Center(child: Text('No dogs are in training for you right now.', style: TextStyle(fontSize: 14.sp, color: AppColors.textMuted)));
                  }
                  return ListView(
                    padding: EdgeInsets.all(16.w),
                    children: [
                      Text('Which dog is the report for?', style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w700)),
                      SizedBox(height: 10.h),
                      for (final d in dogs)
                        Padding(
                          padding: EdgeInsets.only(bottom: 10.h),
                          child: AppCard(
                            onTap: () => _pick(d),
                            child: Row(children: [
                              const Icon(Icons.pets_rounded, color: AppColors.primary),
                              SizedBox(width: 12.w),
                              Expanded(
                                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                  Text(d.dogName, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                                  Text('Week ${d.currentWeek} · Day ${d.dayNumber} of ${d.totalDays}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                                ]),
                              ),
                              const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
                            ]),
                          ),
                        ),
                    ],
                  );
                },
              )
            : _loadingDraft
                ? const Center(child: CircularProgressIndicator())
                : _draft == null
                    ? Center(
                        child: Padding(
                          padding: EdgeInsets.all(24.w),
                          child: Column(mainAxisSize: MainAxisSize.min, children: [
                            Text(_error ?? 'Could not open the report.', textAlign: TextAlign.center, style: TextStyle(color: AppColors.danger, fontSize: 13.sp)),
                            SizedBox(height: 12.h),
                            OutlinedButton(onPressed: () => setState(() => _dog = null), child: const Text('Choose another dog')),
                          ]),
                        ),
                      )
                    : _form(),
      ),
    );
  }

  Widget _form() {
    final d = _draft!;
    final status = '${d['status'] ?? 'Draft'}';
    final locked = status == 'Submitted' || status == 'Approved';
    String line(String k) => '${d[k] ?? ''}'.trim();
    return ListView(
      padding: EdgeInsets.all(16.w),
      children: [
        AppCard(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text('${_dog!.dogName} — Week ${_dog!.currentWeek}', style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800)),
            SizedBox(height: 6.h),
            Text(
              (d['logsInWeek'] ?? 0) == 0
                  ? 'No daily training has been logged for this week yet.'
                  : '${d['logsInWeek']} training day(s) logged${d['averageRating'] == null ? '' : ', average rating ${d['averageRating']}/10'}.',
              style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary),
            ),
            if (line('activitiesCompleted').isNotEmpty) ...[
              SizedBox(height: 8.h),
              Text('Done: ${line('activitiesCompleted')}', style: TextStyle(fontSize: 12.sp)),
            ],
            if (line('activitiesOutstanding').isNotEmpty) ...[
              SizedBox(height: 4.h),
              Text('Outstanding: ${line('activitiesOutstanding')}', style: TextStyle(fontSize: 12.sp, color: AppColors.warning)),
            ],
            if (locked) ...[
              SizedBox(height: 8.h),
              Text('This report is already ${status.toLowerCase()}.', style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w700, color: AppColors.success)),
            ],
          ]),
        ),
        SizedBox(height: 14.h),
        _field(_summary, 'Summary of performance this week *', 4, locked),
        _field(_challenges, 'Challenges', 3, locked),
        _field(_improvements, 'Improvements seen', 3, locked),
        _field(_recommendations, 'Recommendations', 3, locked),
        if (_error != null)
          Padding(
            padding: EdgeInsets.only(bottom: 8.h),
            child: Text(_error!, style: TextStyle(color: AppColors.danger, fontSize: 12.sp, fontWeight: FontWeight.w600)),
          ),
        if (!locked)
          Row(children: [
            Expanded(child: OutlinedButton(onPressed: _busy ? null : () => _save(false), child: const Text('Save draft'))),
            SizedBox(width: 12.w),
            Expanded(
              child: FilledButton.icon(
                onPressed: _busy ? null : () => _save(true),
                icon: _busy ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.send_rounded),
                label: const Text('Submit'),
              ),
            ),
          ]),
      ],
    );
  }

  Widget _field(TextEditingController c, String label, int lines, bool locked) => Padding(
        padding: EdgeInsets.only(bottom: 12.h),
        child: TextField(controller: c, enabled: !locked, minLines: lines, maxLines: lines + 3, textCapitalization: TextCapitalization.sentences, decoration: InputDecoration(labelText: label, alignLabelWithHint: true)),
      );
}
