import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../core/theme/app_colors.dart';

/// Labelled dropdown matching [AppTextField]'s styling.
class AppDropdownField<T> extends StatelessWidget {
  final String label;
  final T? value;
  final List<DropdownMenuItem<T>> items;
  final ValueChanged<T?> onChanged;
  final IconData? icon;
  final String? Function(T?)? validator;
  final String? hint;

  const AppDropdownField({
    super.key,
    required this.label,
    required this.value,
    required this.items,
    required this.onChanged,
    this.icon,
    this.validator,
    this.hint,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
        SizedBox(height: 6.h),
        DropdownButtonFormField<T>(
          // Deliberately using the deprecated `value:` param, not `initialValue:` —
          // initialValue only seeds the field on its first build and then ignores
          // further changes to this prop, so the dropdown stops reflecting state
          // updates (e.g. a value reset, or picking Sire then Dam back-to-back).
          // ignore: deprecated_member_use
          value: value,
          isExpanded: true,
          items: items,
          onChanged: onChanged,
          validator: validator,
          hint: hint == null ? null : Text(hint!, style: TextStyle(fontSize: 14.sp, color: AppColors.textMuted)),
          icon: const Icon(Icons.keyboard_arrow_down_rounded),
          decoration: InputDecoration(
            prefixIcon: icon == null ? null : Icon(icon, size: 20.r, color: AppColors.textMuted),
          ),
        ),
      ],
    );
  }
}

/// Read-only field that opens a picker on tap (used for dates).
class DateField extends StatelessWidget {
  final String label;
  final String? value;
  final VoidCallback onTap;
  final IconData icon;

  const DateField({
    super.key,
    required this.label,
    required this.value,
    required this.onTap,
    this.icon = Icons.calendar_today_rounded,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
        SizedBox(height: 6.h),
        InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(14.r),
          child: InputDecorator(
            decoration: InputDecoration(
              prefixIcon: Icon(icon, size: 20.r, color: AppColors.textMuted),
              suffixIcon: Icon(Icons.edit_calendar_outlined, size: 18.r, color: AppColors.textMuted),
            ),
            child: Text(
              value ?? 'Select date',
              style: TextStyle(
                fontSize: 14.sp,
                color: value == null ? AppColors.textMuted : AppColors.textPrimary,
              ),
            ),
          ),
        ),
      ],
    );
  }
}
