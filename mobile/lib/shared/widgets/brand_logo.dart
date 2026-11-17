import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../core/theme/app_colors.dart';

/// App mark — the Black Eagle logo. Uses `assets/logo_mark.png` (a transparent
/// eagle silhouette); on dark backgrounds it's tinted white for contrast.
class BrandLogo extends StatelessWidget {
  /// Logical size of the badge (scaled via `.r`).
  final double size;
  final bool onDark;

  const BrandLogo({super.key, this.size = 64, this.onDark = false});

  @override
  Widget build(BuildContext context) {
    final bg = onDark ? Colors.white.withValues(alpha: 0.15) : AppColors.primaryLight;
    return Container(
      width: size.r,
      height: size.r,
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(size.r * 0.24),
        border: onDark ? Border.all(color: Colors.white24, width: 1.5) : null,
      ),
      alignment: Alignment.center,
      padding: EdgeInsets.all(size.r * 0.16),
      child: Image.asset(
        'assets/logo_mark.png',
        fit: BoxFit.contain,
        color: onDark ? Colors.white : null,
        colorBlendMode: BlendMode.srcIn,
      ),
    );
  }
}
