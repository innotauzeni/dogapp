import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../core/theme/app_colors.dart';

/// Labelled text field used across all forms.
class AppTextField extends StatelessWidget {
  final TextEditingController? controller;
  final String label;
  final String? hint;
  final IconData? icon;
  final bool obscure;
  final TextInputType keyboardType;
  final List<TextInputFormatter>? formatters;
  final String? Function(String?)? validator;
  final Widget? suffix;
  final int maxLines;
  final bool enabled;
  final void Function(String)? onChanged;
  final TextInputAction? textInputAction;

  const AppTextField({
    super.key,
    this.controller,
    required this.label,
    this.hint,
    this.icon,
    this.obscure = false,
    this.keyboardType = TextInputType.text,
    this.formatters,
    this.validator,
    this.suffix,
    this.maxLines = 1,
    this.enabled = true,
    this.onChanged,
    this.textInputAction,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
        SizedBox(height: 6.h),
        TextFormField(
          controller: controller,
          obscureText: obscure,
          keyboardType: keyboardType,
          inputFormatters: formatters,
          validator: validator,
          maxLines: obscure ? 1 : maxLines,
          enabled: enabled,
          onChanged: onChanged,
          textInputAction: textInputAction,
          style: TextStyle(fontSize: 14.sp, color: AppColors.textPrimary),
          decoration: InputDecoration(
            hintText: hint,
            prefixIcon: icon == null ? null : Icon(icon, size: 20.r, color: AppColors.textMuted),
            suffixIcon: suffix,
          ),
        ),
      ],
    );
  }
}

/// Primary CTA with an integrated loading state.
class PrimaryButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final bool loading;
  final IconData? icon;

  const PrimaryButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.loading = false,
    this.icon,
  });

  @override
  Widget build(BuildContext context) {
    return ElevatedButton(
      onPressed: loading ? null : onPressed,
      child: loading
          ? SizedBox(
              width: 22.r,
              height: 22.r,
              child: const CircularProgressIndicator(strokeWidth: 2.4, color: Colors.white),
            )
          : Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (icon != null) ...[Icon(icon, size: 18.r), SizedBox(width: 8.w)],
                Text(label),
              ],
            ),
    );
  }
}
