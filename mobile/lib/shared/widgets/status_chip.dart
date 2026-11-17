import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../core/theme/app_colors.dart';

/// Small coloured pill for statuses (Active, Paid, Deployed, …).
class StatusChip extends StatelessWidget {
  final String label;
  final Color color;
  final Color background;
  final IconData? icon;

  const StatusChip({
    super.key,
    required this.label,
    required this.color,
    required this.background,
    this.icon,
  });

  /// Maps a free-text status to sensible colours.
  factory StatusChip.forStatus(String status) {
    final s = status.trim().toUpperCase();
    switch (s) {
      case 'ACTIVE':
      case 'PAID':
      case 'DEPLOYED':
      case 'APPROVED':
      case 'COMPLETED':
        return StatusChip(label: _title(status), color: AppColors.success, background: AppColors.successLight);
      case 'PENDING':
      case 'PARTIAL':
      case 'SENT':
      case 'DRAFT':
        return StatusChip(label: _title(status), color: AppColors.warning, background: AppColors.warningLight);
      case 'INACTIVE':
      case 'CANCELLED':
      case 'OVERDUE':
      case 'REJECTED':
      case 'DECEASED':
        return StatusChip(label: _title(status), color: AppColors.danger, background: AppColors.dangerLight);
      default:
        return StatusChip(label: _title(status), color: AppColors.info, background: AppColors.infoLight);
    }
  }

  factory StatusChip.boolStatus(bool active, {String activeLabel = 'Active', String inactiveLabel = 'Inactive'}) =>
      active
          ? StatusChip(label: activeLabel, color: AppColors.success, background: AppColors.successLight)
          : StatusChip(label: inactiveLabel, color: AppColors.danger, background: AppColors.dangerLight);

  static String _title(String s) =>
      s.isEmpty ? s : s[0].toUpperCase() + s.substring(1).toLowerCase();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
      decoration: BoxDecoration(color: background, borderRadius: BorderRadius.circular(20.r)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[Icon(icon, size: 12.r, color: color), SizedBox(width: 4.w)],
          Text(label, style: TextStyle(color: color, fontSize: 11.sp, fontWeight: FontWeight.w700)),
        ],
      ),
    );
  }
}
