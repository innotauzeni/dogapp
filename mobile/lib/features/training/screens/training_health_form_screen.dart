import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/training_repository.dart';
import '../widgets/training_ui.dart';

/// The pre-training health check (the vet's examination form). Saved as "Pending"; someone with
/// approval rights then marks it Passed or Failed from the enrolment page.
class TrainingHealthFormScreen extends StatefulWidget {
  final int enrollmentId;
  final String dogName;
  const TrainingHealthFormScreen({super.key, required this.enrollmentId, required this.dogName});

  @override
  State<TrainingHealthFormScreen> createState() => _TrainingHealthFormScreenState();
}

class _TrainingHealthFormScreenState extends State<TrainingHealthFormScreen> {
  static const _exam = ['Eyes', 'Ears', 'Teeth', 'Mucous membranes', 'Heart', 'Body score', 'Lungs', 'Musculoskeletal system', 'Lymph nodes', 'Skin', 'General fitness', 'Behaviour & temperament'];
  static const _recommendations = ['Fit for training', 'Fit with conditions', 'Not fit for training', 'Further examination required'];

  final _repo = TrainingRepository();
  final _examiner = TextEditingController();
  final _phone = TextEditingController();
  final _weight = TextEditingController();
  final _condition = TextEditingController();
  final _vacc = TextEditingController();
  final _deworm = TextEditingController();
  final _medical = TextEditingController();
  final _comments = TextEditingController();
  final Map<String, String> _status = {for (final e in _exam) e: 'Normal'};
  DateTime _date = DateTime.now();
  bool _goodHealth = true, _vaccCurrent = true, _noParasites = true, _noBite = true;
  String _recommendation = _recommendations.first;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _examiner.text = context.read<AuthProvider>().user?.name ?? '';
  }

  @override
  void dispose() {
    for (final c in [_examiner, _phone, _weight, _condition, _vacc, _deworm, _medical, _comments]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    if (_examiner.text.trim().isEmpty) return setState(() => _error = 'Enter the examiner\'s name.');
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      var i = 0;
      await _repo.rawPost('Training/health-assessments', {
        'enrollmentId': widget.enrollmentId,
        'assessmentDate': _date.toIso8601String(),
        'examinerName': _examiner.text.trim(),
        'examinerPhone': _phone.text.trim(),
        'weightKg': double.tryParse(_weight.text.trim()),
        'physicalCondition': _condition.text.trim(),
        'vaccinationStatus': _vacc.text.trim(),
        'dewormingStatus': _deworm.text.trim(),
        'medicalConditions': _medical.text.trim(),
        'certGoodHealth': _goodHealth,
        'certVaccinationsCurrent': _vaccCurrent,
        'certNoParasites': _noParasites,
        'certNoBiteHistory': _noBite,
        'recommendation': _recommendation,
        'comments': _comments.text.trim(),
        'inspectorSignatureName': _examiner.text.trim(),
        'signedOn': DateTime.now().toIso8601String(),
        'items': [
          for (final e in _exam) {'category': 'Exam', 'name': e, 'status': _status[e], 'notes': '', 'sortOrder': ++i},
        ],
      });
      if (!mounted) return;
      trToast(context, 'Health assessment saved.');
      Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) setState(() => _error = errText(e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _t(TextEditingController c, String label, {int lines = 1, TextInputType? type}) => Padding(
        padding: EdgeInsets.only(bottom: 10.h),
        child: TextField(controller: c, minLines: lines, maxLines: lines + 2, keyboardType: type, decoration: InputDecoration(labelText: label, alignLabelWithHint: true)),
      );

  Widget _h(String t) => Padding(
        padding: EdgeInsets.only(top: 14.h, bottom: 8.h),
        child: Text(t, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800)),
      );

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Health check — ${widget.dogName}')),
      body: SafeArea(
        top: false,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 28.h),
          children: [
            OutlinedButton.icon(
              icon: const Icon(Icons.event),
              label: Text('Examination date: ${tDate(_date.toIso8601String())}'),
              onPressed: () async {
                final d = await showDatePicker(context: context, initialDate: _date, firstDate: DateTime(2020), lastDate: DateTime.now());
                if (d != null) setState(() => _date = d);
              },
            ),
            SizedBox(height: 10.h),
            _t(_examiner, 'Examiner (vet) name *'),
            _t(_phone, 'Examiner phone', type: TextInputType.phone),
            _t(_weight, 'Weight (kg)', type: const TextInputType.numberWithOptions(decimal: true)),
            _t(_condition, 'Physical condition'),
            _t(_vacc, 'Vaccination status'),
            _t(_deworm, 'Deworming status'),
            _t(_medical, 'Medical conditions', lines: 2),
            _h('Examination'),
            for (final e in _exam)
              TrCard(
                padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 8.h),
                child: Row(children: [
                  Expanded(child: Text(e, style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600))),
                  for (final o in const ['Normal', 'Abnormal', 'N/A'])
                    Padding(
                      padding: EdgeInsets.only(left: 4.w),
                      child: ChoiceChip(
                        label: Text(o, style: TextStyle(fontSize: 11.sp)),
                        selected: _status[e] == o,
                        selectedColor: o == 'Abnormal' ? const Color(0xFFFEE2E2) : o == 'Normal' ? const Color(0xFFD1FAE5) : AppColors.surfaceAlt,
                        onSelected: (_) => setState(() => _status[e] = o),
                        visualDensity: VisualDensity.compact,
                      ),
                    ),
                ]),
              ),
            _h('Veterinary certification'),
            SwitchListTile(value: _goodHealth, onChanged: (v) => setState(() => _goodHealth = v), title: const Text('The dog is in good health'), contentPadding: EdgeInsets.zero),
            SwitchListTile(value: _vaccCurrent, onChanged: (v) => setState(() => _vaccCurrent = v), title: const Text('Vaccinations are current'), contentPadding: EdgeInsets.zero),
            SwitchListTile(value: _noParasites, onChanged: (v) => setState(() => _noParasites = v), title: const Text('No parasites found'), contentPadding: EdgeInsets.zero),
            SwitchListTile(value: _noBite, onChanged: (v) => setState(() => _noBite = v), title: const Text('No bite history'), contentPadding: EdgeInsets.zero),
            _h('Recommendation'),
            Wrap(spacing: 8.w, runSpacing: 4.h, children: [
              for (final r in _recommendations) ChoiceChip(label: Text(r), selected: _recommendation == r, onSelected: (_) => setState(() => _recommendation = r)),
            ]),
            SizedBox(height: 12.h),
            _t(_comments, 'Comments', lines: 3),
            if (_error != null) Padding(padding: EdgeInsets.only(bottom: 8.h), child: Text(_error!, style: TextStyle(color: AppColors.danger, fontSize: 12.sp, fontWeight: FontWeight.w600))),
            FilledButton.icon(
              onPressed: _busy ? null : _save,
              icon: _busy ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.check_circle_rounded),
              label: const Text('Save health assessment'),
            ),
            SizedBox(height: 6.h),
            Text('After saving, someone with approval rights marks it Passed or Failed from the enrolment page.', style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
          ],
        ),
      ),
    );
  }
}
