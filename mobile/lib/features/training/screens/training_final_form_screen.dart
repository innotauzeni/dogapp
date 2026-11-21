import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/theme/app_colors.dart';
import '../data/training_repository.dart';
import '../widgets/training_ui.dart';

/// The final assessment: a 0-10 score for each skill area, the result, and (if more training is needed) the extra days.
class TrainingFinalFormScreen extends StatefulWidget {
  final int enrollmentId;
  final String dogName;
  const TrainingFinalFormScreen({super.key, required this.enrollmentId, required this.dogName});

  @override
  State<TrainingFinalFormScreen> createState() => _TrainingFinalFormScreenState();
}

class _TrainingFinalFormScreenState extends State<TrainingFinalFormScreen> {
  static const _areas = ['Obedience', 'Sit', 'Stay', 'Recall', 'Heel', 'Leash control', 'Socialisation', 'Behaviour', 'Responsiveness', 'Specialised skills'];
  final _repo = TrainingRepository();
  final Map<String, double> _score = {for (final a in _areas) a: 7};
  final _comments = TextEditingController();
  final _days = TextEditingController(text: '7');
  final _extra = TextEditingController();
  String _result = 'Passed';
  bool _busy = false;
  String? _error;

  double get _overall => _score.values.fold<double>(0, (a, b) => a + b) / _areas.length;

  @override
  void dispose() {
    _comments.dispose();
    _days.dispose();
    _extra.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final more = _result == 'RequiresAdditionalTraining';
    if (more && (int.tryParse(_days.text) ?? 0) <= 0) return setState(() => _error = 'Enter the extra days of training needed.');
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await _repo.rawPost('Training/final-assessments', {
        'enrollmentId': widget.enrollmentId,
        'assessmentDate': DateTime.now().toIso8601String(),
        'items': [for (final a in _areas) {'area': a, 'score': _score[a]!.round(), 'comments': ''}],
        'result': _result,
        'comments': _comments.text.trim(),
        'extensionDays': more ? int.tryParse(_days.text) ?? 0 : 0,
        'additionalActivities': more ? _extra.text : null,
      });
      if (!mounted) return;
      trToast(context, 'Final assessment saved.');
      Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) setState(() => _error = errText(e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Final assessment — ${widget.dogName}')),
      body: SafeArea(
        top: false,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 28.h),
          children: [
            TrCard(
              child: Row(children: [
                Expanded(child: Text('Overall score', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700))),
                Text('${_overall.toStringAsFixed(1)} / 10', style: TextStyle(fontSize: 22.sp, fontWeight: FontWeight.w800, color: _overall >= 6 ? AppColors.success : AppColors.warning)),
              ]),
            ),
            SizedBox(height: 10.h),
            for (final a in _areas)
              Padding(
                padding: EdgeInsets.only(bottom: 8.h),
                child: TrCard(
                  padding: EdgeInsets.fromLTRB(14.w, 8.h, 14.w, 2.h),
                  child: Column(children: [
                    Row(children: [
                      Expanded(child: Text(a, style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700))),
                      Text('${_score[a]!.round()}', style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800, color: AppColors.primary)),
                    ]),
                    Slider(value: _score[a]!, min: 0, max: 10, divisions: 10, onChanged: (v) => setState(() => _score[a] = v)),
                  ]),
                ),
              ),
            SizedBox(height: 8.h),
            Text('Result', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800)),
            SizedBox(height: 8.h),
            Wrap(spacing: 8.w, runSpacing: 4.h, children: [
              for (final r in const [('Passed', 'Passed'), ('Failed', 'Failed'), ('RequiresAdditionalTraining', 'More training needed')])
                ChoiceChip(label: Text(r.$2), selected: _result == r.$1, onSelected: (_) => setState(() => _result = r.$1)),
            ]),
            if (_result == 'RequiresAdditionalTraining') ...[
              SizedBox(height: 10.h),
              TextField(controller: _days, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Extra days needed *')),
              SizedBox(height: 10.h),
              TextField(controller: _extra, minLines: 2, maxLines: 5, decoration: const InputDecoration(labelText: 'Additional activities (one per line)', alignLabelWithHint: true)),
            ],
            SizedBox(height: 10.h),
            TextField(controller: _comments, minLines: 3, maxLines: 6, decoration: const InputDecoration(labelText: 'Comments', alignLabelWithHint: true)),
            if (_error != null) Padding(padding: EdgeInsets.only(top: 8.h), child: Text(_error!, style: TextStyle(color: AppColors.danger, fontSize: 12.sp, fontWeight: FontWeight.w600))),
            SizedBox(height: 14.h),
            FilledButton.icon(
              onPressed: _busy ? null : _save,
              icon: _busy ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.check_circle_rounded),
              label: const Text('Save assessment'),
            ),
            SizedBox(height: 6.h),
            Text('A pass goes to management for approval before the certificate is issued.', style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
          ],
        ),
      ),
    );
  }
}
