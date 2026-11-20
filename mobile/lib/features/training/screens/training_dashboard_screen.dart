import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../data/training_repository.dart';

/// Dog Training dashboard - the same pipeline counts as the web dashboard. For a handler the server
/// counts only the dogs allocated to them.
class TrainingDashboardScreen extends StatefulWidget {
  const TrainingDashboardScreen({super.key});

  @override
  State<TrainingDashboardScreen> createState() => _TrainingDashboardScreenState();
}

class _TrainingDashboardScreenState extends State<TrainingDashboardScreen> {
  final _repo = TrainingRepository();
  late Future<Map<String, dynamic>> _future;

  @override
  void initState() {
    super.initState();
    _future = _repo.rawMap('Training/dashboard');
  }

  Future<void> _refresh() async {
    setState(() => _future = _repo.rawMap('Training/dashboard'));
    await _future.catchError((_) => <String, dynamic>{});
  }

  int _n(Map<String, dynamic> d, String k) => d[k] is num ? (d[k] as num).toInt() : 0;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Training Dashboard')),
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: AsyncView<Map<String, dynamic>>(
            future: _future,
            onRetry: _refresh,
            builder: (context, d) {
              final tiles = <(String, int, IconData, Color)>[
                ('Awaiting health check', _n(d, 'awaitingHealthAssessment'), Icons.health_and_safety_rounded, const Color(0xFFDC2626)),
                ('Health passed – invoice due', _n(d, 'healthPassed'), Icons.receipt_rounded, const Color(0xFFD97706)),
                ('Awaiting payment', _n(d, 'awaitingPayment'), Icons.payments_rounded, const Color(0xFFCA8A04)),
                ('Awaiting handler', _n(d, 'awaitingHandler'), Icons.person_search_rounded, const Color(0xFF7C3AED)),
                ('Ready to start', _n(d, 'readyToActivate'), Icons.play_circle_rounded, const Color(0xFF2563EB)),
                ('In training', _n(d, 'inTraining'), Icons.pets_rounded, const Color(0xFF059669)),
                ('Week 1', _n(d, 'week1'), Icons.looks_one_rounded, const Color(0xFF0891B2)),
                ('Week 2', _n(d, 'week2'), Icons.looks_two_rounded, const Color(0xFF0891B2)),
                ('Week 3', _n(d, 'week3'), Icons.looks_3_rounded, const Color(0xFF0891B2)),
                ('Awaiting final assessment', _n(d, 'awaitingFinalAssessment'), Icons.assignment_turned_in_rounded, const Color(0xFF7C3AED)),
                ('Awaiting approval', _n(d, 'awaitingApproval'), Icons.approval_rounded, const Color(0xFF4F46E5)),
                ('Need more training', _n(d, 'requiringAdditionalTraining'), Icons.replay_rounded, const Color(0xFFEA580C)),
                ('Completed', _n(d, 'completed'), Icons.check_circle_rounded, const Color(0xFF16A34A)),
                ('Certificates issued', _n(d, 'certificatesIssued'), Icons.workspace_premium_rounded, const Color(0xFFCA8A04)),
                ('Reports to review', _n(d, 'reportsAwaitingReview'), Icons.fact_check_rounded, const Color(0xFF0D9488)),
                ('Health failed', _n(d, 'healthFailed'), Icons.cancel_rounded, const Color(0xFFB91C1C)),
              ];
              final recent = ((d['recent'] as List?) ?? const []).whereType<Map>().take(8).toList();
              return ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
                children: [
                  GridView.count(
                    crossAxisCount: 2,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    mainAxisSpacing: 10.h,
                    crossAxisSpacing: 10.w,
                    childAspectRatio: 1.55,
                    children: [
                      for (final t in tiles)
                        AppCard(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Row(children: [
                                Icon(t.$3, color: t.$4, size: 20.r),
                                const Spacer(),
                                Text('${t.$2}', style: TextStyle(fontSize: 24.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                              ]),
                              SizedBox(height: 6.h),
                              Text(t.$1, maxLines: 2, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                            ],
                          ),
                        ),
                    ],
                  ),
                  if (recent.isNotEmpty) ...[
                    SizedBox(height: 20.h),
                    const SectionHeader(title: 'Recent enrolments'),
                    for (final r in recent)
                      Padding(
                        padding: EdgeInsets.only(bottom: 8.h),
                        child: AppCard(
                          child: Row(children: [
                            Expanded(
                              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                Text('${r['dogName'] ?? ''} · ${r['enrollmentNumber'] ?? ''}', style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
                                Text('${r['customerName'] ?? ''}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                              ]),
                            ),
                            Text('${r['status'] ?? ''}', style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: AppColors.primary)),
                          ]),
                        ),
                      ),
                  ],
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}
