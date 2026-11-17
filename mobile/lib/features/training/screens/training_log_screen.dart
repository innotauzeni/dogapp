import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../data/training_repository.dart';

const _levels = ['Excellent', 'Very Good', 'Good', 'Needs Improvement', 'Poor'];

String _suggest(double avg) => avg >= 9
    ? 'Excellent'
    : avg >= 7
        ? 'Very Good'
        : avg >= 5
            ? 'Good'
            : avg >= 3
                ? 'Needs Improvement'
                : 'Poor';

/// Daily training record for one dog: tick each scheduled activity, rate it out of 10,
/// add observations and attach photos or video straight from the phone.
class TrainingLogScreen extends StatefulWidget {
  final TrainingDog dog;
  const TrainingLogScreen({super.key, required this.dog});

  @override
  State<TrainingLogScreen> createState() => _TrainingLogScreenState();
}

class _TrainingLogScreenState extends State<TrainingLogScreen> {
  final _repo = TrainingRepository();
  final _picker = ImagePicker();
  final DateTime _date = DateTime.now();

  DailyLog? _log;
  List<WeekPlan> _plans = const [];
  List<LogSlot> _slots = [];
  String? _performance;
  bool _loading = true;
  bool _saving = false;
  bool _uploading = false;
  bool _showTarget = false;
  String? _error;
  int _uploaded = 0;

  final _duration = TextEditingController();
  final _commands = TextEditingController();
  final _activities = TextEditingController();
  final _observations = TextEditingController();
  final _challenges = TextEditingController();
  final _improvements = TextEditingController();
  final _recommendations = TextEditingController();
  final _next = TextEditingController();

  List<TextEditingController> get _controllers =>
      [_duration, _commands, _activities, _observations, _challenges, _improvements, _recommendations, _next];

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    for (final c in _controllers) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final log = await _repo.logForDay(widget.dog.id, _date);
      List<WeekPlan> plans = const [];
      try {
        plans = await _repo.plans(widget.dog.id);
      } catch (_) {/* the target card is optional */}
      if (!mounted) return;
      _apply(log);
      setState(() {
        _plans = plans;
        _loading = false;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() { _error = e.message; _loading = false; });
    } catch (_) {
      if (mounted) setState(() { _error = 'Could not open this dog.'; _loading = false; });
    }
  }

  void _apply(DailyLog log) {
    _log = log;
    _slots = log.slots;
    _performance = log.performance;
    _duration.text = log.durationMinutes == 0 ? '' : log.durationMinutes.toString();
    _commands.text = log.commands;
    _activities.text = log.activities;
    _observations.text = log.observations;
    _challenges.text = log.challenges;
    _improvements.text = log.improvements;
    _recommendations.text = log.recommendations;
    _next.text = log.nextObjectives;
  }

  double? get _average {
    final r = _slots.where((s) => s.done && s.rating != null).map((s) => s.rating!).toList();
    if (r.isEmpty) return null;
    return (r.reduce((a, b) => a + b) * 10 / r.length).round() / 10;
  }

  void _toast(String m, {bool error = false}) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(m), backgroundColor: error ? AppColors.danger : AppColors.success));
  }

  Future<void> _save() async {
    if (_slots.any((s) => s.done && s.rating == null)) {
      final ok = await showDialog<bool>(
        context: context,
        builder: (c) => AlertDialog(
          title: const Text('Missing ratings'),
          content: const Text('Some completed activities have no rating. Save anyway?'),
          actions: [
            TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Go back')),
            FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Save anyway')),
          ],
        ),
      );
      if (ok != true) return;
    }
    setState(() => _saving = true);
    try {
      final suggested = _average == null ? null : _suggest(_average!);
      final saved = await _repo.saveLog({
        'enrollmentId': widget.dog.id,
        'trainingDate': _date.toIso8601String(),
        'activitiesPerformed': _activities.text,
        'commandsPracticed': _commands.text,
        'durationMinutes': int.tryParse(_duration.text) ?? 0,
        'slots': _slots.map((s) => s.toJson()).toList(),
        'performance': _performance ?? suggested,
        'behaviourObservations': _observations.text,
        'challenges': _challenges.text,
        'improvements': _improvements.text,
        'recommendations': _recommendations.text,
        'nextObjectives': _next.text,
      });
      if (!mounted) return;
      setState(() => _log = saved);
      _toast('Training recorded.');
    } on ApiException catch (e) {
      if (mounted) _toast(e.message, error: true);
    } catch (_) {
      if (mounted) _toast('Could not save. Check your connection and try again.', error: true);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _addMedia(ImageSource source, {bool video = false}) async {
    final log = _log;
    if (log == null || log.id == 0) {
      _toast('Save the record first, then add photos or video.', error: true);
      return;
    }
    try {
      final XFile? file = video
          ? await _picker.pickVideo(source: source, maxDuration: const Duration(minutes: 3))
          : await _picker.pickImage(source: source, maxWidth: 1600, maxHeight: 1600, imageQuality: 85);
      if (file == null) return;
      setState(() => _uploading = true);
      await _repo.uploadToLog(log.id, file.path);
      if (!mounted) return;
      setState(() => _uploaded++);
      _toast(video ? 'Video added.' : 'Photo added.');
    } on ApiException catch (e) {
      if (mounted) _toast(e.message, error: true);
    } catch (_) {
      if (mounted) _toast('Upload failed. Try again.', error: true);
    } finally {
      if (mounted) setState(() => _uploading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(title: Text(widget.dog.dogName)),
      bottomNavigationBar: _loading || _error != null
          ? null
          : SafeArea(
              child: Padding(
                padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 8.h),
                child: FilledButton.icon(
                  onPressed: _saving ? null : _save,
                  icon: _saving ? SizedBox(width: 18.w, height: 18.w, child: const CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.check_circle_outline),
                  label: Text(_saving ? 'Saving…' : 'Save today\'s record'),
                  style: FilledButton.styleFrom(minimumSize: Size.fromHeight(48.h)),
                ),
              ),
            ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Padding(
                    padding: EdgeInsets.all(24.w),
                    child: Column(mainAxisSize: MainAxisSize.min, children: [
                      Icon(Icons.error_outline, size: 44.sp, color: AppColors.danger),
                      SizedBox(height: 10.h),
                      Text(_error!, textAlign: TextAlign.center),
                      SizedBox(height: 12.h),
                      FilledButton(onPressed: _load, child: const Text('Try again')),
                    ]),
                  ),
                )
              : _form(),
    );
  }

  Widget _form() {
    final log = _log!;
    final avg = _average;
    final suggested = avg == null ? null : _suggest(avg);
    final plan = _plans.where((p) => p.weekNumber == log.weekNumber).toList();
    return ListView(
      padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 24.h),
      children: [
        _card(Row(children: [
          Icon(Icons.event, color: AppColors.textSecondary, size: 20.sp),
          SizedBox(width: 8.w),
          Expanded(
            child: Text(
              '${_date.day}/${_date.month}/${_date.year}  ·  ${log.weekNumber > 0 ? 'Week ${log.weekNumber}, Day ${log.dayNumber}' : 'Training not active today'}',
              style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13.sp),
            ),
          ),
        ])),
        if (plan.isNotEmpty && plan.first.targets.isNotEmpty) ...[
          SizedBox(height: 10.h),
          _card(Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            InkWell(
              onTap: () => setState(() => _showTarget = !_showTarget),
              child: Row(children: [
                Icon(Icons.flag_rounded, color: AppColors.primary, size: 20.sp),
                SizedBox(width: 8.w),
                Expanded(child: Text('This week\'s target: ${plan.first.title}', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 13.sp))),
                Icon(_showTarget ? Icons.expand_less : Icons.expand_more),
              ]),
            ),
            if (_showTarget)
              Padding(
                padding: EdgeInsets.only(top: 8.h),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: plan.first.targets
                      .map((t) => Padding(padding: EdgeInsets.only(bottom: 4.h), child: Text('•  $t', style: TextStyle(fontSize: 12.sp))))
                      .toList(),
                ),
              ),
          ])),
        ],
        SizedBox(height: 14.h),
        Text('Today\'s schedule', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800)),
        SizedBox(height: 8.h),
        if (_slots.isEmpty)
          _card(Text('No schedule is set for today. Fill in the details below.', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)))
        else
          ..._slots.map(_slotCard),
        SizedBox(height: 4.h),
        _card(Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
            Text('Average rating', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
            Text(avg == null ? '—' : '$avg / 10', style: TextStyle(fontSize: 20.sp, fontWeight: FontWeight.w800)),
          ]),
          SizedBox(height: 8.h),
          Text('Overall performance for the day', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
          SizedBox(height: 6.h),
          Wrap(
            spacing: 6.w,
            runSpacing: 6.h,
            children: _levels.map((l) {
              final selected = (_performance ?? suggested) == l;
              return ChoiceChip(
                label: Text(suggested == l && _performance == null ? '$l · suggested' : l),
                selected: selected,
                onSelected: (_) => setState(() => _performance = l),
              );
            }).toList(),
          ),
        ])),
        SizedBox(height: 10.h),
        _card(Column(children: [
          _field(_duration, 'Training duration (minutes)', keyboard: TextInputType.number),
          _field(_commands, 'Commands practised'),
          _field(_activities, 'Activities performed', lines: 3),
          _field(_observations, 'Behaviour observations', lines: 3),
          _field(_challenges, 'Challenges', lines: 2),
          _field(_improvements, 'Improvements', lines: 2),
          _field(_recommendations, 'Recommendations', lines: 2),
          _field(_next, 'Next training objectives', lines: 2),
        ])),
        SizedBox(height: 10.h),
        _card(Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('Photos & video', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 13.sp)),
          SizedBox(height: 4.h),
          Text(log.id == 0 ? 'Save the record first, then add photos or video from your phone.' : (_uploaded > 0 ? '$_uploaded file(s) added this session.' : 'Add photos or a short video of today\'s training.'),
              style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
          SizedBox(height: 8.h),
          Wrap(spacing: 8.w, runSpacing: 8.h, children: [
            OutlinedButton.icon(onPressed: _uploading ? null : () => _addMedia(ImageSource.camera), icon: const Icon(Icons.photo_camera_outlined), label: const Text('Take photo')),
            OutlinedButton.icon(onPressed: _uploading ? null : () => _addMedia(ImageSource.gallery), icon: const Icon(Icons.photo_library_outlined), label: const Text('Gallery')),
            OutlinedButton.icon(onPressed: _uploading ? null : () => _addMedia(ImageSource.camera, video: true), icon: const Icon(Icons.videocam_outlined), label: const Text('Record video')),
          ]),
          if (_uploading) Padding(padding: EdgeInsets.only(top: 10.h), child: const LinearProgressIndicator()),
        ])),
      ],
    );
  }

  Widget _slotCard(LogSlot s) {
    return Container(
      margin: EdgeInsets.only(bottom: 8.h),
      padding: EdgeInsets.all(12.w),
      decoration: BoxDecoration(
        color: s.done ? const Color(0xFFF0FDF9) : AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: s.done ? const Color(0xFFA7F3D0) : AppColors.border, width: 1.5),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(s.time, style: TextStyle(fontWeight: FontWeight.w800, color: AppColors.primary, fontSize: 12.sp)),
              Text(s.title, style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14.sp)),
            ]),
          ),
          FilledButton.tonal(
            onPressed: () => setState(() {
              s.done = !s.done;
              if (!s.done) s.rating = null;
            }),
            child: Text(s.done ? '✓ Done' : 'Mark done'),
          ),
        ]),
        if (s.done) ...[
          SizedBox(height: 8.h),
          Text('Rate out of 10', style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary)),
          SizedBox(height: 4.h),
          Wrap(
            spacing: 5.w,
            runSpacing: 5.h,
            children: List.generate(11, (n) {
              final on = s.rating == n;
              return InkWell(
                onTap: () => setState(() => s.rating = n),
                borderRadius: BorderRadius.circular(8),
                child: Container(
                  width: 38.w,
                  height: 38.w,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: on ? AppColors.primary : Colors.white,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: on ? AppColors.primary : AppColors.border, width: 1.5),
                  ),
                  child: Text('$n', style: TextStyle(fontWeight: FontWeight.w700, color: on ? Colors.white : AppColors.textPrimary)),
                ),
              );
            }),
          ),
          SizedBox(height: 8.h),
          TextFormField(
            initialValue: s.notes,
            onChanged: (v) => s.notes = v,
            decoration: const InputDecoration(hintText: 'Notes (optional)', isDense: true, border: OutlineInputBorder()),
          ),
        ],
      ]),
    );
  }

  Widget _card(Widget child) => Container(
        width: double.infinity,
        padding: EdgeInsets.all(14.w),
        decoration: BoxDecoration(color: AppColors.surface, borderRadius: BorderRadius.circular(14), border: Border.all(color: AppColors.border)),
        child: child,
      );

  Widget _field(TextEditingController c, String label, {int lines = 1, TextInputType? keyboard}) => Padding(
        padding: EdgeInsets.only(bottom: 10.h),
        child: TextField(
          controller: c,
          maxLines: lines,
          keyboardType: keyboard,
          textCapitalization: TextCapitalization.sentences,
          decoration: InputDecoration(labelText: label, border: const OutlineInputBorder(), isDense: true),
        ),
      );
}
