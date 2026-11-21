import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/training_repository.dart';
import '../widgets/training_ui.dart';
import 'training_enrolment_screen.dart';
import 'training_enrolments_screen.dart';

/// Dog Training dashboard - the same pipeline as the web dashboard. For a handler the server counts only their own dogs.
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

  void _openList(String status) {
    if (!context.read<AuthProvider>().can('training.enrolment.view')) return;
    Navigator.of(context).push(MaterialPageRoute(builder: (_) => TrainingEnrolmentsScreen(initialStatus: status)));
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final brand = auth.brandColor ?? AppColors.primary;
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
              final stages = <(String, String, int, IconData, Color)>[
                ('Awaiting health check', 'AwaitingHealthAssessment', n(d['awaitingHealthAssessment']), Icons.favorite_rounded, const Color(0xFFDC2626)),
                ('Health passed – invoice due', 'HealthPassed', n(d['healthPassed']), Icons.receipt_rounded, const Color(0xFFD97706)),
                ('Awaiting payment', 'AwaitingPayment', n(d['awaitingPayment']), Icons.payments_rounded, const Color(0xFFCA8A04)),
                ('Paid – needs a handler', 'AwaitingHandler', n(d['awaitingHandler']), Icons.person_search_rounded, const Color(0xFF7C3AED)),
                ('Ready to start', 'Ready', n(d['readyToActivate']), Icons.play_circle_rounded, const Color(0xFF2563EB)),
                ('In training', 'InTraining', n(d['inTraining']), Icons.pets_rounded, const Color(0xFF059669)),
                ('Awaiting final assessment', 'AwaitingFinalAssessment', n(d['awaitingFinalAssessment']), Icons.assignment_turned_in_rounded, const Color(0xFF7C3AED)),
                ('Awaiting approval', 'AwaitingApproval', n(d['awaitingApproval']), Icons.verified_rounded, const Color(0xFF4F46E5)),
                ('Completed', 'Completed', n(d['completed']), Icons.check_circle_rounded, const Color(0xFF16A34A)),
              ];
              final maxStage = stages.map((e) => e.$3).fold<int>(1, (a, b) => b > a ? b : a);
              final weeks = [n(d['week1']), n(d['week2']), n(d['week3'])];
              final maxWeek = weeks.fold<int>(1, (a, b) => b > a ? b : a);
              final recent = ((d['recent'] as List?) ?? const []).whereType<Map>().take(8).toList();
              return ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 28.h),
                children: [
                  // Hero
                  Container(
                    padding: EdgeInsets.all(18.w),
                    decoration: BoxDecoration(
                      gradient: LinearGradient(colors: [Color.lerp(brand, Colors.black, 0.45)!, brand], begin: Alignment.topLeft, end: Alignment.bottomRight),
                      borderRadius: BorderRadius.circular(20.r),
                    ),
                    child: Row(children: [
                      Expanded(
                        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Text('In training now', style: TextStyle(color: Colors.white70, fontSize: 12.sp)),
                          Text('${n(d['inTraining'])}', style: TextStyle(color: Colors.white, fontSize: 40.sp, fontWeight: FontWeight.w800, height: 1.1)),
                          SizedBox(height: 4.h),
                          Text('${n(d['completed'])} completed · ${n(d['certificatesIssued'])} certificates', style: TextStyle(color: Colors.white70, fontSize: 12.sp)),
                        ]),
                      ),
                      Icon(Icons.school_rounded, color: Colors.white24, size: 64.r),
                    ]),
                  ),
                  SizedBox(height: 12.h),
                  Row(children: [
                    _alert('Reports to review', n(d['reportsAwaitingReview']), Icons.fact_check_rounded, const Color(0xFF0D9488)),
                    SizedBox(width: 8.w),
                    _alert('Need more training', n(d['requiringAdditionalTraining']), Icons.replay_rounded, const Color(0xFFEA580C)),
                    SizedBox(width: 8.w),
                    _alert('Health failed', n(d['healthFailed']), Icons.cancel_rounded, const Color(0xFFB91C1C)),
                  ]),
                  SizedBox(height: 18.h),
                  Text('Pipeline', style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800)),
                  SizedBox(height: 8.h),
                  TrCard(
                    padding: EdgeInsets.symmetric(vertical: 6.h, horizontal: 12.w),
                    child: Column(children: [
                      for (final st in stages)
                        InkWell(
                          onTap: () => _openList(st.$2),
                          child: Padding(
                            padding: EdgeInsets.symmetric(vertical: 9.h),
                            child: Row(children: [
                              Container(
                                width: 34.r,
                                height: 34.r,
                                decoration: BoxDecoration(color: st.$5.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(10.r)),
                                child: Icon(st.$4, color: st.$5, size: 18.r),
                              ),
                              SizedBox(width: 10.w),
                              Expanded(
                                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                  Text(st.$1, style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w600)),
                                  SizedBox(height: 4.h),
                                  TrProgress(st.$3 / maxStage, color: st.$5),
                                ]),
                              ),
                              SizedBox(width: 10.w),
                              Text('${st.$3}', style: TextStyle(fontSize: 17.sp, fontWeight: FontWeight.w800)),
                            ]),
                          ),
                        ),
                    ]),
                  ),
                  SizedBox(height: 18.h),
                  Text('Dogs in training by week', style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800)),
                  SizedBox(height: 8.h),
                  Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
                    for (var i = 0; i < 3; i++) ...[
                      Expanded(
                        child: TrCard(
                          child: Column(children: [
                            Text('${weeks[i]}', style: TextStyle(fontSize: 22.sp, fontWeight: FontWeight.w800)),
                            SizedBox(height: 6.h),
                            Container(
                              height: 8.h + 52.h * (weeks[i] / maxWeek),
                              decoration: BoxDecoration(color: const Color(0xFF0891B2).withValues(alpha: 0.85), borderRadius: BorderRadius.circular(8.r)),
                            ),
                            SizedBox(height: 6.h),
                            Text('Week ${i + 1}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary, fontWeight: FontWeight.w600)),
                          ]),
                        ),
                      ),
                      if (i < 2) SizedBox(width: 8.w),
                    ],
                  ]),
                  if (recent.isNotEmpty) ...[
                    SizedBox(height: 18.h),
                    Text('Recent enrolments', style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800)),
                    SizedBox(height: 8.h),
                    for (final r in recent)
                      Padding(
                        padding: EdgeInsets.only(bottom: 8.h),
                        child: TrCard(
                          onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => TrainingEnrolmentScreen(enrollmentId: n(r['id'])))),
                          child: Row(children: [
                            DogAvatar(url: r['dogPhotoUrl']?.toString(), name: s(r['dogName']), size: 40),
                            SizedBox(width: 10.w),
                            Expanded(
                              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                Text(s(r['dogName']), style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
                                Text('${s(r['customerName'])} · ${s(r['enrollmentNumber'])}', style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary)),
                              ]),
                            ),
                            TrPill(s(r['status'])),
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

  Widget _alert(String label, int value, IconData icon, Color color) => Expanded(
        child: Container(
          padding: EdgeInsets.all(10.w),
          decoration: BoxDecoration(color: color.withValues(alpha: value > 0 ? 0.12 : 0.05), borderRadius: BorderRadius.circular(14.r)),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Icon(icon, color: color, size: 18.r),
            SizedBox(height: 4.h),
            Text('$value', style: TextStyle(fontSize: 20.sp, fontWeight: FontWeight.w800, color: color)),
            Text(label, style: TextStyle(fontSize: 10.sp, color: color, fontWeight: FontWeight.w700)),
          ]),
        ),
      );
}
