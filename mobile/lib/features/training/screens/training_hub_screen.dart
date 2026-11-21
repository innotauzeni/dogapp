import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/cards.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/training_menus.dart';
import 'training_dashboard_screen.dart';
import 'training_dogs_screen.dart';
import 'training_enrolments_screen.dart';
import 'training_invoices_screen.dart';
import 'training_list_screen.dart';

/// "Dog Training" on the phone: the same menus as the web portal, each shown only when the signed-in
/// user's role holds its permission. A handler sees their dashboard, dogs, logbook and reports; finance
/// sees invoices; managers see everything.
class TrainingHubScreen extends StatelessWidget {
  const TrainingHubScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final brand = auth.brandColor ?? AppColors.primary;
    final items = <_HubItem>[
      if (auth.can('training.dashboard.view'))
        _HubItem('Training Dashboard', 'Pipeline and progress at a glance', Icons.speed_rounded, const Color(0xFF6366F1),
            (c) => const TrainingDashboardScreen()),
      if (auth.can('training.log.view'))
        _HubItem('Daily Logbook', 'Your dogs in training — record today', Icons.menu_book_rounded, const Color(0xFF059669),
            (c) => const TrainingDogsScreen()),
      if (auth.can('training.enrolment.view'))
        _HubItem('Training Enrolments', 'Every dog, step by step', Icons.assignment_rounded, const Color(0xFF2563EB),
            (c) => const TrainingEnrolmentsScreen()),
      if (auth.can('training.invoice.view'))
        _HubItem('Invoices & Payments', 'Training fees and money received', Icons.receipt_long_rounded, const Color(0xFFD97706),
            (c) => const TrainingInvoicesScreen()),
      for (final m in trainingMenus)
        if (auth.can(m.permission))
          _HubItem(m.title, m.subtitle, m.icon, m.color, (c) => TrainingListScreen(config: m)),
    ];

    return Scaffold(
      appBar: AppBar(title: const Text('Dog Training')),
      body: SafeArea(
        top: false,
        child: items.isEmpty
            ? Center(
                child: Padding(
                  padding: EdgeInsets.all(32.w),
                  child: Text(
                    'Your role has no Dog Training access yet.\nAsk an administrator to grant the training permissions.',
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 14.sp, color: AppColors.textSecondary),
                  ),
                ),
              )
            : ListView(
                padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
                children: [
                  Container(
                    padding: EdgeInsets.all(16.w),
                    decoration: BoxDecoration(
                      gradient: LinearGradient(colors: [Color.lerp(brand, Colors.black, 0.4)!, brand]),
                      borderRadius: BorderRadius.circular(16.r),
                    ),
                    child: Row(
                      children: [
                        Icon(Icons.school_rounded, color: Colors.white, size: 30.r),
                        SizedBox(width: 12.w),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('Dog Training', style: TextStyle(color: Colors.white, fontSize: 17.sp, fontWeight: FontWeight.w800)),
                              Text('Health check → payment → handler → training → assessment → certificate',
                                  style: TextStyle(color: Colors.white70, fontSize: 11.sp)),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  SizedBox(height: 14.h),
                  for (final it in items)
                    Padding(
                      padding: EdgeInsets.only(bottom: 10.h),
                      child: AppCard(
                        onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: it.build)),
                        child: Row(
                          children: [
                            Container(
                              width: 44.r,
                              height: 44.r,
                              decoration: BoxDecoration(color: it.color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(12.r)),
                              child: Icon(it.icon, color: it.color, size: 24.r),
                            ),
                            SizedBox(width: 12.w),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(it.title, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                                  Text(it.subtitle, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                                ],
                              ),
                            ),
                            const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
                          ],
                        ),
                      ),
                    ),
                ],
              ),
      ),
    );
  }
}

class _HubItem {
  final String title;
  final String subtitle;
  final IconData icon;
  final Color color;
  final WidgetBuilder build;
  _HubItem(this.title, this.subtitle, this.icon, this.color, this.build);
}
