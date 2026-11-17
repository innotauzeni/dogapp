import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/dogs_repository.dart';
import '../models/animal.dart';
import '../widgets/dog_avatar.dart';

/// Handler's activity log: all care records they've recorded for their assigned dogs.
class HandlerActivitiesScreen extends StatefulWidget {
  const HandlerActivitiesScreen({super.key});

  @override
  State<HandlerActivitiesScreen> createState() => _HandlerActivitiesScreenState();
}

class _HandlerActivitiesScreenState extends State<HandlerActivitiesScreen> {
  final _repo = DogsRepository();
  Future<List<HandlerActivity>>? _future;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    final userId = context.read<AuthProvider>().user?.id ?? '';
    _future = _repo.getHandlerActivities(userId);
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My Activities')),
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: AsyncView<List<HandlerActivity>>(
            future: _future,
            onRetry: _refresh,
            builder: (context, activities) {
              if (activities.isEmpty) {
                return ListView(
                  children: [
                    SizedBox(height: 120.h),
                    StatusView.empty(
                      title: 'No activities yet',
                      message: 'Care records you add will appear here.',
                      icon: Icons.history_rounded,
                    ),
                  ],
                );
              }
              return ListView.separated(
                padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
                physics: const AlwaysScrollableScrollPhysics(),
                itemCount: activities.length,
                separatorBuilder: (_, _) => SizedBox(height: 10.h),
                itemBuilder: (context, i) {
                  final a = activities[i];
                  return _ActivityCard(activity: a);
                },
              );
            },
          ),
        ),
      ),
    );
  }
}

class _ActivityCard extends StatelessWidget {
  final HandlerActivity activity;

  const _ActivityCard({required this.activity});

  Color _eventColor(String event) {
    return switch (event.toLowerCase()) {
      'vaccinated' => AppColors.success,
      'illness' || 'injured' => AppColors.danger,
      'mating' => AppColors.warning,
      'birth' => AppColors.info,
      _ => AppColors.primary,
    };
  }

  IconData _eventIcon(String event) {
    return switch (event.toLowerCase()) {
      'feeding' => Icons.restaurant_rounded,
      'vaccinated' => Icons.shield_rounded,
      'illness' => Icons.local_hospital_rounded,
      'injured' => Icons.health_and_safety_rounded,
      'bite someone' => Icons.warning_rounded,
      'heat' => Icons.favorite_rounded,
      'mating' => Icons.favorite_rounded,
      'birth' => Icons.child_care_rounded,
      _ => Icons.assignment_rounded,
    };
  }

  @override
  Widget build(BuildContext context) {
    final color = _eventColor(activity.eventType);
    final icon = _eventIcon(activity.eventType);

    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).cardColor,
        borderRadius: BorderRadius.circular(14.r),
        border: Border.all(color: AppColors.border),
      ),
      child: Padding(
        padding: EdgeInsets.all(12.w),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                DogAvatar(photoUrl: activity.dogPhotoUrl, size: 44),
                SizedBox(width: 12.w),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(activity.dogName,
                          style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w800)),
                      Text(Fmt.eventWhen(activity.occurredOn),
                          style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                    ],
                  ),
                ),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 6.h),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(icon, size: 14.r, color: color),
                      SizedBox(width: 4.w),
                      Text(activity.eventType,
                          style: TextStyle(
                            fontSize: 10.sp,
                            fontWeight: FontWeight.w600,
                            color: color,
                          )),
                    ],
                  ),
                ),
              ],
            ),
            if ((activity.details ?? '').isNotEmpty) ...[
              SizedBox(height: 8.h),
              Text(activity.details!,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary)),
            ],
            if ((activity.quantityOrOutcome ?? '').isNotEmpty) ...[
              SizedBox(height: 4.h),
              Text('📊 ${activity.quantityOrOutcome}',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 10.sp, color: AppColors.textMuted)),
            ],
            if (activity.requiresAttention) ...[
              SizedBox(height: 6.h),
              Container(
                padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
                decoration: BoxDecoration(
                  color: AppColors.warningLight,
                  borderRadius: BorderRadius.circular(6.r),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.priority_high_rounded, size: 12.r, color: AppColors.warning),
                    SizedBox(width: 4.w),
                    Text('Requires attention',
                        style: TextStyle(fontSize: 9.sp, color: AppColors.warning)),
                  ],
                ),
              ),
            ],
            SizedBox(height: 6.h),
            Text('Updated ${Fmt.dateTime(activity.updatedAt ?? activity.createdAt)}',
                style: TextStyle(fontSize: 9.sp, color: AppColors.textMuted, fontStyle: FontStyle.italic)),
          ],
        ),
      ),
    );
  }
}
