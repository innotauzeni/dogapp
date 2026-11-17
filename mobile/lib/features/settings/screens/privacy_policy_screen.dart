import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/cards.dart';

class PrivacyPolicyScreen extends StatelessWidget {
  const PrivacyPolicyScreen({super.key});

  static const _sections = [
    (
      'Data We Collect',
      'Account details you provide (name, email, phone), the dog and breeding '
          'records you enter (profiles, vaccinations, matings, litters, care '
          'activity), and photos you upload for a dog\'s profile.',
    ),
    (
      'How We Use It',
      'To run the app\'s core features — showing your dogs, tracking health and '
          'breeding records, and coordinating handlers within your company. We do '
          'not use your data for advertising.',
    ),
    (
      'Sharing',
      'Data is only visible within your own company and its branches. We do not '
          'sell or share your data with third parties, except service providers '
          'that help us operate the app (e.g. hosting and file storage).',
    ),
    (
      'Data Retention & Security',
      'Records are kept for as long as your company account is active, and '
          'access is protected by your login credentials. You can request '
          'deletion of your account data at any time.',
    ),
    (
      'Your Rights',
      'You can review, correct, or request deletion of your personal data by '
          'contacting us using the details below.',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Privacy & Security')),
      body: SafeArea(
        top: false,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 32.h),
          children: [
            for (final (title, body) in _sections) ...[
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                    SizedBox(height: 8.h),
                    Text(body, style: TextStyle(fontSize: 13.sp, color: AppColors.textSecondary, height: 1.5)),
                  ],
                ),
              ),
              SizedBox(height: 12.h),
            ],
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Contact Us', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                  SizedBox(height: 8.h),
                  Row(
                    children: [
                      Icon(Icons.email_outlined, size: 16.r, color: AppColors.textMuted),
                      SizedBox(width: 8.w),
                      Text('support@daraerp.co.zw',
                          style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600, color: AppColors.primary)),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
