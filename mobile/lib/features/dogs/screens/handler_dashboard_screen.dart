import 'package:flutter/material.dart';
import '../widgets/feed_all_sheet.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/dogs_repository.dart';
import '../models/animal.dart';
import '../widgets/dog_avatar.dart';
import '../../training/screens/training_dogs_screen.dart';
import 'handler_dogs_screen.dart';

class HandlerDashboardScreen extends StatefulWidget {
  const HandlerDashboardScreen({super.key});

  @override
  State<HandlerDashboardScreen> createState() => _HandlerDashboardScreenState();
}

class _HandlerDashboardScreenState extends State<HandlerDashboardScreen> {
  final _repo = DogsRepository();
  Future<List<Animal>>? _future;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    final userId = context.read<AuthProvider>().user?.id ?? '';
    _future = _repo.getAssignedDogs(userId);
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8F9FA),
      body: RefreshIndicator(
        onRefresh: _refresh,
        child: FutureBuilder<List<Animal>>(
          future: _future,
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snapshot.hasError) {
              return ListView(
                children: [
                  SizedBox(height: 120.h),
                  StatusView.error(
                    message: 'Error loading dogs. Please try again.',
                    onRetry: _refresh,
                  ),
                ],
              );
            }

            final dogs = snapshot.data ?? [];
            if (dogs.isEmpty) {
              return ListView(
                children: [
                  SizedBox(height: 120.h),
                  StatusView.empty(
                    title: 'No assigned dogs',
                    message: 'Dogs assigned to you will appear here.',
                    icon: Icons.pets_rounded,
                  ),
                ],
              );
            }

            // Count mating records (for overview)
            final activeMatingRecords = dogs
                .where((d) => d.matingRecords.isNotEmpty)
                .length;

            // Count total litters
            final totalLitters = dogs
                .fold<int>(0, (sum, d) => sum + d.matingRecords.fold<int>(0, (ls, m) => ls + m.litters.length));

            // Count total puppies
            final totalPuppies = dogs.fold<int>(0, (sum, d) =>
                sum + d.matingRecords.fold<int>(0, (ls, m) =>
                    ls + m.litters.fold<int>(0, (ps, l) => ps + l.puppies.length)));

            // Use mating records or puppies as "attention" metric
            final needsAttention = activeMatingRecords;

            // Falls back to the default charcoal-grey gradient when the
            // company hasn't set a custom brand color.
            final brand = context.watch<AuthProvider>().brandColor;
            final headerGradient = brand == null
                ? AppColors.brandGradient
                : [
                    Color.lerp(brand, Colors.black, 0.35)!,
                    brand,
                    Color.lerp(brand, Colors.white, 0.25)!,
                  ];

            return ListView(
              padding: EdgeInsets.zero,
              physics: const AlwaysScrollableScrollPhysics(),
              children: [
                // Gradient header
                Container(
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      colors: headerGradient,
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                    ),
                  ),
                  padding: EdgeInsets.fromLTRB(16.w, 48.h, 16.w, 28.h),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '👋 Welcome, Handler!',
                        style: TextStyle(
                          fontSize: 22.sp,
                          fontWeight: FontWeight.w800,
                          color: Colors.white,
                        ),
                      ),
                      SizedBox(height: 6.h),
                      Text(
                        'Here\'s an overview of your assigned dogs',
                        style: TextStyle(
                          fontSize: 12.sp,
                          color: Colors.white.withValues(alpha: 0.9),
                        ),
                      ),
                    ],
                  ),
                ),
                Padding(
                  padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Stats section — rows size to their tallest tile's actual
                      // content instead of a fixed aspect ratio, so a longer label
                      // or a larger system font never overflows on smaller screens.
                      IntrinsicHeight(
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Expanded(
                              child: _StatTile(
                                label: 'Assigned Dogs',
                                value: dogs.length.toString(),
                                icon: Icons.pets,
                                color: Colors.blue,
                              ),
                            ),
                            SizedBox(width: 12.w),
                            Expanded(
                              child: _StatTile(
                                label: 'Litters',
                                value: totalLitters.toString(),
                                icon: Icons.child_care,
                                color: Colors.green,
                              ),
                            ),
                          ],
                        ),
                      ),
                      SizedBox(height: 12.h),
                      IntrinsicHeight(
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Expanded(
                              child: _StatTile(
                                label: 'Puppies',
                                value: totalPuppies.toString(),
                                icon: Icons.favorite,
                                color: Colors.orange,
                              ),
                            ),
                            SizedBox(width: 12.w),
                            Expanded(
                              child: _StatTile(
                                label: 'Active Matings',
                                value: needsAttention.toString(),
                                icon: Icons.favorite,
                                color: Colors.red,
                              ),
                            ),
                          ],
                        ),
                      ),
                      SizedBox(height: 16.h),
                      // Feed the whole kennel in one go (works even with no dogs allocated)
                      Material(
                        color: const Color(0xFFFFFBEB),
                        borderRadius: BorderRadius.circular(14),
                        child: InkWell(
                          borderRadius: BorderRadius.circular(14),
                          onTap: () => showFeedAllSheet(context),
                          child: Container(
                            padding: EdgeInsets.all(14.w),
                            decoration: BoxDecoration(borderRadius: BorderRadius.circular(14), border: Border.all(color: const Color(0xFFFDE68A))),
                            child: Row(children: [
                              const Icon(Icons.restaurant_rounded, color: Color(0xFFD97706)),
                              SizedBox(width: 12.w),
                              Expanded(
                                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                  Text('Feed all dogs', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800)),
                                  Text("Record today's feeding for the whole kennel at once", style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                                ]),
                              ),
                              const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
                            ]),
                          ),
                        ),
                      ),
                      SizedBox(height: 12.h),
                      // Dog Training: today's sessions, daily logbook and photos
                      Material(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(14),
                        child: InkWell(
                          borderRadius: BorderRadius.circular(14),
                          onTap: () => Navigator.of(context).push(
                            MaterialPageRoute(builder: (_) => const TrainingDogsScreen()),
                          ),
                          child: Container(
                            padding: EdgeInsets.all(14.w),
                            decoration: BoxDecoration(
                              borderRadius: BorderRadius.circular(14),
                              border: Border.all(color: AppColors.border),
                            ),
                            child: Row(
                              children: [
                                Container(
                                  padding: EdgeInsets.all(10.w),
                                  decoration: BoxDecoration(
                                    color: AppColors.primaryLight,
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: const Icon(Icons.school_rounded, color: AppColors.primary),
                                ),
                                SizedBox(width: 12.w),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text('Dog Training', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800)),
                                      Text("Log today's sessions, ratings and photos",
                                          style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                                    ],
                                  ),
                                ),
                                const Icon(Icons.chevron_right),
                              ],
                            ),
                          ),
                        ),
                      ),
                      SizedBox(height: 28.h),
                      Text(
                        'Your Dogs',
                        style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary),
                      ),
                      SizedBox(height: 12.h),
                      ...dogs.map((d) => Column(
                        children: [
                          _DogTile(
                            dog: d,
                            onTap: () => Navigator.of(context).push(
                              MaterialPageRoute(builder: (_) => HandlerDogsScreen(initialDog: d)),
                            ),
                          ),
                          // Mating records and litters
                          if (d.matingRecords.isNotEmpty) ...[
                            SizedBox(height: 10.h),
                            Padding(
                              padding: EdgeInsets.only(left: 12.w),
                              child: Text(
                                '❤️ Mating & Litters',
                                style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: Colors.pink[700]),
                              ),
                            ),
                            ...d.matingRecords.map((m) => _MatingTile(mating: m)).toList(),
                          ],
                        ],
                      )),
                      SizedBox(height: 20.h),
                    ],
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }

}

class _MatingTile extends StatelessWidget {
  final MatingRecord mating;
  const _MatingTile({required this.mating});

  @override
  Widget build(BuildContext context) {
    final litterCount = mating.litters.length;
    final puppyCount = mating.litters.fold<int>(0, (sum, l) => sum + l.puppies.length);

    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).cardColor,
        borderRadius: BorderRadius.circular(10.r),
        border: Border.all(color: Colors.pink.withValues(alpha: 0.3)),
      ),
      margin: EdgeInsets.fromLTRB(12.w, 6.h, 12.w, 6.h),
      padding: EdgeInsets.all(10.w),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.favorite, color: Colors.pink, size: 16.r),
              SizedBox(width: 6.w),
              Expanded(
                child: Text(
                  '${mating.sireName} × ${mating.damName}',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700),
                ),
              ),
            ],
          ),
          SizedBox(height: 4.h),
          Row(
            children: [
              if (mating.matingDate != null) ...[
                Text(
                  '${mating.matingDate!.month}/${mating.matingDate!.day}',
                  style: TextStyle(fontSize: 9.sp, color: AppColors.textMuted),
                ),
                SizedBox(width: 12.w),
              ],
              Text(
                '$litterCount litter${litterCount != 1 ? 's' : ''}',
                style: TextStyle(fontSize: 9.sp, color: Colors.green[700]),
              ),
              SizedBox(width: 12.w),
              Text(
                '$puppyCount pup${puppyCount != 1 ? 's' : ''}',
                style: TextStyle(fontSize: 9.sp, color: Colors.orange[700]),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _StatTile extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color color;

  const _StatTile({
    required this.label,
    required this.value,
    required this.icon,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).cardColor,
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.border),
      ),
      padding: EdgeInsets.all(10.w),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, color: color, size: 22.r),
          SizedBox(height: 6.h),
          Text(
            value,
            style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary),
          ),
          SizedBox(height: 3.h),
          Text(
            label,
            textAlign: TextAlign.center,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(fontSize: 9.sp, color: AppColors.textSecondary),
          ),
        ],
      ),
    );
  }
}

class _DogTile extends StatelessWidget {
  final Animal dog;
  final VoidCallback onTap;

  const _DogTile({required this.dog, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final hasMatings = dog.matingRecords.isNotEmpty;

    final puppyCount = dog.matingRecords.fold<int>(0, (sum, m) =>
        sum + m.litters.fold<int>(0, (ls, l) => ls + l.puppies.length));

    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).cardColor,
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: hasMatings ? Colors.pink.withValues(alpha: 0.5) : AppColors.border),
      ),
      margin: EdgeInsets.only(bottom: 10.h),
      child: ListTile(
        onTap: onTap,
        contentPadding: EdgeInsets.all(12.w),
        leading: DogAvatar(photoUrl: dog.photoUrl, size: 48),
        title: Text(
          dog.name,
          style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13.sp),
        ),
        subtitle: Text(
          dog.breed,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted),
        ),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            if (hasMatings)
              Container(
                padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 2.h),
                decoration: BoxDecoration(
                  color: Colors.pink[100],
                  borderRadius: BorderRadius.circular(4.r),
                ),
                child: Text(
                  '$puppyCount pups',
                  style: TextStyle(fontSize: 9.sp, color: Colors.pink[900], fontWeight: FontWeight.w600),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

extension<T> on T? {
  R? let<R>(R Function(T) fn) => this != null ? fn(this as T) : null;
}
