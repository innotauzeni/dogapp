import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../core/theme/app_colors.dart';

/// A bordered white container — the building block for most content.
class AppCard extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry? padding;
  final VoidCallback? onTap;
  final Color? color;

  const AppCard({super.key, required this.child, this.padding, this.onTap, this.color});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: color ?? AppColors.surface,
      borderRadius: BorderRadius.circular(16.r),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16.r),
        child: Container(
          padding: padding ?? EdgeInsets.all(16.w),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16.r),
            border: Border.all(color: AppColors.border),
          ),
          child: child,
        ),
      ),
    );
  }
}

/// Compact metric tile used on the dashboard grid.
class StatCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color color;
  final VoidCallback? onTap;

  const StatCard({
    super.key,
    required this.label,
    required this.value,
    required this.icon,
    required this.color,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      onTap: onTap,
      padding: EdgeInsets.all(14.w),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 40.r,
            height: 40.r,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(12.r),
            ),
            child: Icon(icon, color: color, size: 22.r),
          ),
          SizedBox(height: 12.h),
          FittedBox(
            fit: BoxFit.scaleDown,
            alignment: Alignment.centerLeft,
            child: Text(value, style: TextStyle(fontSize: 22.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
          ),
          SizedBox(height: 2.h),
          Text(label, maxLines: 1, overflow: TextOverflow.ellipsis,
              style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
        ],
      ),
    );
  }
}

/// A titled section wrapper with consistent spacing.
class SectionHeader extends StatelessWidget {
  final String title;
  final Widget? trailing;
  const SectionHeader({super.key, required this.title, this.trailing});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(bottom: 12.h),
      child: Row(
        children: [
          Expanded(child: Text(title, style: Theme.of(context).textTheme.titleMedium)),
          trailing ?? const SizedBox.shrink(),
        ],
      ),
    );
  }
}

/// Label/value row used inside detail cards.
class InfoRow extends StatelessWidget {
  final String label;
  final String value;
  final IconData? icon;
  const InfoRow({super.key, required this.label, required this.value, this.icon});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 7.h),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 16.r, color: AppColors.textMuted),
            SizedBox(width: 8.w),
          ],
          SizedBox(
            width: 120.w,
            child: Text(label, style: TextStyle(fontSize: 13.sp, color: AppColors.textSecondary)),
          ),
          Expanded(
            child: Text(
              value.isEmpty ? '—' : value,
              style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
            ),
          ),
        ],
      ),
    );
  }
}
