import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import 'app_colors.dart';

/// Builds the Material 3 light theme. Text sizes use `.sp` so typography scales
/// with the device just like the rest of the layout.
class AppTheme {
  AppTheme._();

  /// [seed] is a company's custom brand color (Settings → Branding, Admin/SystemOwner
  /// only). When null (the default for every company that hasn't customized it), the
  /// theme is pixel-identical to before — a white AppBar with dark text and the
  /// default charcoal accent. When set, it becomes the app's accent color throughout
  /// (AppBar background, buttons, bottom-nav selection, focused input borders), and
  /// the AppBar switches to white text/icons for contrast against the colored background.
  static ThemeData light({Color? seed}) {
    final primary = seed ?? AppColors.primary;
    final branded = seed != null;

    final scheme = ColorScheme.fromSeed(
      seedColor: primary,
      brightness: Brightness.light,
      primary: primary,
      surface: AppColors.surface,
    );

    final base = ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: AppColors.background,
      fontFamily: 'Roboto',
    );

    return base.copyWith(
      appBarTheme: AppBarTheme(
        backgroundColor: branded ? primary : AppColors.surface,
        foregroundColor: branded ? Colors.white : AppColors.textPrimary,
        elevation: 0,
        scrolledUnderElevation: 0.5,
        centerTitle: false,
        systemOverlayStyle: branded ? SystemUiOverlayStyle.light : SystemUiOverlayStyle.dark,
        titleTextStyle: TextStyle(
          color: branded ? Colors.white : AppColors.textPrimary,
          fontSize: 18.sp,
          fontWeight: FontWeight.w700,
        ),
      ),
      textTheme: _textTheme(base.textTheme),
      cardTheme: CardThemeData(
        color: AppColors.surface,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16.r),
          side: const BorderSide(color: AppColors.border),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.surfaceAlt,
        contentPadding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 16.h),
        hintStyle: TextStyle(color: AppColors.textMuted, fontSize: 14.sp),
        labelStyle: TextStyle(color: AppColors.textSecondary, fontSize: 14.sp),
        border: _inputBorder(AppColors.border),
        enabledBorder: _inputBorder(AppColors.border),
        focusedBorder: _inputBorder(primary, width: 1.6),
        errorBorder: _inputBorder(AppColors.danger),
        focusedErrorBorder: _inputBorder(AppColors.danger, width: 1.6),
        errorStyle: TextStyle(color: AppColors.danger, fontSize: 12.sp),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: primary,
          foregroundColor: Colors.white,
          elevation: 0,
          minimumSize: Size(double.infinity, 52.h),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14.r)),
          textStyle: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w700),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: primary,
          minimumSize: Size(0, 48.h),
          side: BorderSide(color: primary),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14.r)),
          textStyle: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: primary,
          textStyle: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600),
        ),
      ),
      chipTheme: base.chipTheme.copyWith(
        labelStyle: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w600),
        padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 2.h),
      ),
      dividerTheme: const DividerThemeData(color: AppColors.border, thickness: 1),
      bottomNavigationBarTheme: BottomNavigationBarThemeData(
        backgroundColor: AppColors.surface,
        selectedItemColor: primary,
        unselectedItemColor: AppColors.textMuted,
        type: BottomNavigationBarType.fixed,
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
      ),
    );
  }

  static OutlineInputBorder _inputBorder(Color color, {double width = 1}) =>
      OutlineInputBorder(
        borderRadius: BorderRadius.circular(14.r),
        borderSide: BorderSide(color: color, width: width),
      );

  static TextTheme _textTheme(TextTheme base) => base.copyWith(
        displaySmall: TextStyle(fontSize: 28.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary),
        headlineMedium: TextStyle(fontSize: 24.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary),
        headlineSmall: TextStyle(fontSize: 20.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary),
        titleLarge: TextStyle(fontSize: 18.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary),
        titleMedium: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
        bodyLarge: TextStyle(fontSize: 15.sp, color: AppColors.textPrimary),
        bodyMedium: TextStyle(fontSize: 13.sp, color: AppColors.textSecondary),
        bodySmall: TextStyle(fontSize: 12.sp, color: AppColors.textMuted),
        labelLarge: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
      );
}
