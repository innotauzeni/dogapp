import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/config/app_config.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/brand_logo.dart';
import '../providers/auth_provider.dart';

/// Shown while the session is restored. Navigation away is driven by the router.
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    // A company can customize this screen's background via Settings; fall
    // back to the default brand gradient when no color has been chosen.
    final brand = context.watch<AuthProvider>().brandColor;
    final gradientColors = brand == null
        ? AppColors.brandGradient
        : [brand, Color.lerp(brand, Colors.black, 0.35)!];

    return Scaffold(
      body: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            colors: gradientColors,
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
        ),
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const BrandLogo(size: 96, onDark: true),
              SizedBox(height: 20.h),
              Text(
                AppConfig.appName.toUpperCase(),
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 26.sp,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 2,
                ),
              ),
              SizedBox(height: 4.h),
              Text(
                AppConfig.appTagline.toUpperCase(),
                style: TextStyle(
                  color: Colors.white70,
                  fontSize: 12.sp,
                  letterSpacing: 4,
                ),
              ),
              SizedBox(height: 40.h),
              SizedBox(
                width: 26.r,
                height: 26.r,
                child: const CircularProgressIndicator(strokeWidth: 2.6, color: Colors.white),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
