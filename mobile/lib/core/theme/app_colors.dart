import 'package:flutter/material.dart';

/// Centralised palette — "light & clean" with a charcoal grey/black accent
/// (Black Eagle's brand colors).
/// Nothing in the UI should reference a raw hex value; everything comes from here.
class AppColors {
  AppColors._();

  // Black Eagle's brand is grey / black / white, not blue — this is the default
  // look for everyone until a company customizes its own color in Settings.
  static const Color primary = Color(0xFF3F3F46); // zinc 700 (charcoal grey)
  static const Color primaryDark = Color(0xFF18181B); // zinc 900 (near-black)
  static const Color primaryLight = Color(0xFFE4E4E7); // zinc 200

  static const Color accent = Color(0xFF71717A); // zinc 500

  static const Color background = Color(0xFFF6F8FB);
  static const Color surface = Color(0xFFFFFFFF);
  static const Color surfaceAlt = Color(0xFFF1F5F9);

  static const Color textPrimary = Color(0xFF0F172A); // slate 900
  static const Color textSecondary = Color(0xFF64748B); // slate 500
  static const Color textMuted = Color(0xFF94A3B8); // slate 400

  static const Color border = Color(0xFFE2E8F0); // slate 200

  static const Color success = Color(0xFF16A34A);
  static const Color successLight = Color(0xFFDCFCE7);
  static const Color warning = Color(0xFFD97706);
  static const Color warningLight = Color(0xFFFEF3C7);
  static const Color danger = Color(0xFFDC2626);
  static const Color dangerLight = Color(0xFFFEE2E2);
  static const Color info = Color(0xFF0284C7);
  static const Color infoLight = Color(0xFFE0F2FE);

  /// Gradient used on the login header & dashboard hero.
  static const List<Color> brandGradient = [primaryDark, primary, accent];

  /// Parses a "#RRGGBB" / "#AARRGGBB" hex string (as stored on Company.BrandColor)
  /// into a [Color]. Returns null for anything empty or malformed.
  static Color? fromHex(String? hex) {
    if (hex == null || hex.isEmpty) return null;
    var clean = hex.trim().replaceFirst('#', '');
    if (clean.length == 6) clean = 'FF$clean';
    if (clean.length != 8) return null;
    final value = int.tryParse(clean, radix: 16);
    return value == null ? null : Color(value);
  }

  /// Formats a [Color] back to the "#RRGGBB" form the backend expects.
  static String toHex(Color color) =>
      '#${color.toARGB32().toRadixString(16).substring(2).padLeft(6, '0').toUpperCase()}';

  /// A small curated palette an admin can pick from in Settings.
  static const List<Color> brandPresets = [
    Color(0xFF3F3F46), // default charcoal grey (Black Eagle)
    Color(0xFF0F172A), // near-black
    Color(0xFF1D4ED8), // blue
    Color(0xFFB45309), // amber/gold
    Color(0xFF991B1B), // deep red
    Color(0xFF15803D), // forest green
    Color(0xFF6D28D9), // violet
    Color(0xFF0E7490), // teal
  ];
}
