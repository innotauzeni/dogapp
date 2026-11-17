import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../data/training_repository.dart';
import 'training_log_screen.dart';

/// The handler's dogs in training. Tap a dog to record today's session.
class TrainingDogsScreen extends StatefulWidget {
  const TrainingDogsScreen({super.key});

  @override
  State<TrainingDogsScreen> createState() => _TrainingDogsScreenState();
}

class _TrainingDogsScreenState extends State<TrainingDogsScreen> {
  final _repo = TrainingRepository();
  List<TrainingDog> _dogs = const [];
  Set<int> _loggedToday = const {};
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final dogs = await _repo.myDogs();
      Set<int> logged = {};
      try {
        logged = await _repo.loggedOn(DateTime.now());
      } catch (_) {/* the badge is optional */}
      if (!mounted) return;
      setState(() {
        _dogs = dogs;
        _loggedToday = logged;
        _loading = false;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() { _error = e.message; _loading = false; });
    } catch (_) {
      if (mounted) setState(() { _error = 'Could not load your training dogs.'; _loading = false; });
    }
  }

  Future<void> _open(TrainingDog d) async {
    await Navigator.of(context).push(MaterialPageRoute(builder: (_) => TrainingLogScreen(dog: d)));
    _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(title: const Text('My Training Dogs')),
      body: RefreshIndicator(
        onRefresh: _load,
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? ListView(children: [
                    SizedBox(height: 120.h),
                    Padding(
                      padding: EdgeInsets.all(24.w),
                      child: Column(children: [
                        Icon(Icons.error_outline, size: 44.sp, color: AppColors.danger),
                        SizedBox(height: 10.h),
                        Text(_error!, textAlign: TextAlign.center),
                        SizedBox(height: 12.h),
                        FilledButton(onPressed: _load, child: const Text('Try again')),
                      ]),
                    ),
                  ])
                : _dogs.isEmpty
                    ? ListView(children: [
                        SizedBox(height: 120.h),
                        Icon(Icons.pets_rounded, size: 52.sp, color: AppColors.textMuted),
                        SizedBox(height: 10.h),
                        const Center(child: Text('No dogs are allocated to you for training right now.')),
                      ])
                    : ListView.separated(
                        padding: EdgeInsets.all(16.w),
                        itemCount: _dogs.length,
                        separatorBuilder: (_, _) => SizedBox(height: 12.h),
                        itemBuilder: (_, i) => _DogCard(
                          dog: _dogs[i],
                          loggedToday: _loggedToday.contains(_dogs[i].id),
                          onTap: () => _open(_dogs[i]),
                        ),
                      ),
      ),
    );
  }
}

class _DogCard extends StatelessWidget {
  final TrainingDog dog;
  final bool loggedToday;
  final VoidCallback onTap;
  const _DogCard({required this.dog, required this.loggedToday, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final started = dog.isActive;
    final progress = started && dog.totalDays > 0 ? (dog.dayNumber / dog.totalDays).clamp(0.0, 1.0) : 0.0;
    return Material(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: started ? onTap : null,
        child: Container(
          padding: EdgeInsets.all(14.w),
          decoration: BoxDecoration(borderRadius: BorderRadius.circular(16), border: Border.all(color: AppColors.border)),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(children: [
              CircleAvatar(
                radius: 24.r,
                backgroundColor: AppColors.primaryLight,
                backgroundImage: dog.photoUrl != null && dog.photoUrl!.startsWith('http') ? NetworkImage(dog.photoUrl!) : null,
                child: dog.photoUrl == null ? Text(dog.dogName.isEmpty ? '?' : dog.dogName[0].toUpperCase(), style: const TextStyle(fontWeight: FontWeight.w800)) : null,
              ),
              SizedBox(width: 12.w),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(dog.dogName, style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800)),
                  Text([dog.breed, dog.customerName].where((s) => s.isNotEmpty).join(' · '),
                      style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                ]),
              ),
              if (started) Icon(Icons.chevron_right, color: AppColors.textMuted),
            ]),
            SizedBox(height: 12.h),
            if (started) ...[
              Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
                Text('Week ${dog.currentWeek} · Day ${dog.dayNumber} of ${dog.totalDays}', style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w600)),
                _Chip(
                  text: loggedToday ? 'Logged today' : 'Not logged yet',
                  bg: loggedToday ? AppColors.successLight : AppColors.warningLight,
                  fg: loggedToday ? AppColors.success : AppColors.warning,
                ),
              ]),
              SizedBox(height: 8.h),
              ClipRRect(borderRadius: BorderRadius.circular(6), child: LinearProgressIndicator(value: progress, minHeight: 8, backgroundColor: AppColors.border)),
              SizedBox(height: 12.h),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(onPressed: onTap, icon: const Icon(Icons.edit_note_rounded), label: Text(loggedToday ? 'Edit today\'s record' : 'Log today\'s training')),
              ),
            ] else
              Text('Waiting for the Training Manager to start training.', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
          ]),
        ),
      ),
    );
  }
}

class _Chip extends StatelessWidget {
  final String text;
  final Color bg;
  final Color fg;
  const _Chip({required this.text, required this.bg, required this.fg});

  @override
  Widget build(BuildContext context) => Container(
        padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
        decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(20)),
        child: Text(text, style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: fg)),
      );
}
