import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../core/theme/app_colors.dart';

/// Centered spinner.
class AppLoader extends StatelessWidget {
  final String? label;
  const AppLoader({super.key, this.label});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            width: 34.r,
            height: 34.r,
            child: const CircularProgressIndicator(strokeWidth: 3),
          ),
          if (label != null) ...[
            SizedBox(height: 14.h),
            Text(label!, style: TextStyle(color: AppColors.textSecondary, fontSize: 13.sp)),
          ],
        ],
      ),
    );
  }
}

/// Full-screen empty / error / no-data placeholder with optional retry.
class StatusView extends StatelessWidget {
  final IconData icon;
  final String title;
  final String? message;
  final String? actionLabel;
  final VoidCallback? onAction;
  final Color? iconColor;

  const StatusView({
    super.key,
    required this.icon,
    required this.title,
    this.message,
    this.actionLabel,
    this.onAction,
    this.iconColor,
  });

  factory StatusView.error({String? message, VoidCallback? onRetry}) => StatusView(
        icon: Icons.cloud_off_rounded,
        title: 'Something went wrong',
        message: message,
        actionLabel: onRetry == null ? null : 'Retry',
        onAction: onRetry,
        iconColor: AppColors.danger,
      );

  factory StatusView.empty({required String title, String? message, IconData icon = Icons.inbox_rounded}) =>
      StatusView(icon: icon, title: title, message: message, iconColor: AppColors.textMuted);

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.all(28.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 84.r,
              height: 84.r,
              decoration: BoxDecoration(
                color: (iconColor ?? AppColors.primary).withValues(alpha: 0.10),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, size: 40.r, color: iconColor ?? AppColors.primary),
            ),
            SizedBox(height: 18.h),
            Text(title, textAlign: TextAlign.center, style: Theme.of(context).textTheme.titleMedium),
            if (message != null) ...[
              SizedBox(height: 8.h),
              Text(message!, textAlign: TextAlign.center, style: Theme.of(context).textTheme.bodyMedium),
            ],
            if (actionLabel != null) ...[
              SizedBox(height: 20.h),
              OutlinedButton.icon(
                onPressed: onAction,
                icon: const Icon(Icons.refresh_rounded),
                label: Text(actionLabel!),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
