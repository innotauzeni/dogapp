import 'package:flutter/widgets.dart';

/// Responsive helpers.
///
/// The whole app is built on [flutter_screenutil]. The design baseline is a
/// 375 x 812 logical-pixel phone (see [kDesignSize]); every size written as
/// `12.sp`, `16.w`, `8.r` is scaled to the *actual* device width/height, so a
/// layout authored once looks proportionally identical on a 4.5" budget phone
/// and a 6.9" flagship. No screen hard-codes pixel values.
const Size kDesignSize = Size(375, 812);

/// Width breakpoints (in logical px) used to switch *layout* (e.g. grid
/// column count) — proportional scaling handles everything else.
class Breakpoints {
  Breakpoints._();
  static const double phone = 0;
  static const double largePhone = 400;
  static const double tablet = 600;
  static const double desktop = 1024;
}

extension ResponsiveContext on BuildContext {
  double get screenWidth => MediaQuery.sizeOf(this).width;
  double get screenHeight => MediaQuery.sizeOf(this).height;

  bool get isTablet => screenWidth >= Breakpoints.tablet;
  bool get isLargePhone => screenWidth >= Breakpoints.largePhone;

  /// Number of columns for a responsive grid given a desired min tile width.
  int gridColumns({double minTileWidth = 170, int max = 4}) {
    final padded = screenWidth - 32; // rough horizontal padding allowance
    final count = (padded / minTileWidth).floor();
    return count.clamp(1, max);
  }

  /// Pick a value based on the current width class.
  T responsive<T>({required T phone, T? largePhone, T? tablet}) {
    if (isTablet && tablet != null) return tablet;
    if (isLargePhone && largePhone != null) return largePhone;
    return phone;
  }
}
