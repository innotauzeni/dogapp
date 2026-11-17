/// Global, environment-level configuration.
class AppConfig {
  AppConfig._();

  /// Live API base URL. All endpoints are appended to this (e.g. `auth/login`).
  static const String apiBaseUrl = 'https://api.blackeaglesurveillance.co.zw/api/';

  /// Web portal that serves uploaded files. Dog photos stored by the portal are
  /// relative paths like `/uploads/dogs/xyz.jpg`; resolve them against this.
  static const String portalBaseUrl = 'https://portal.blackeaglesurveillance.co.zw';

  static const String appName = 'Black Eagle';
  static const String appTagline = 'Surveillance';

  /// Connection / receive timeouts.
  static const Duration connectTimeout = Duration(seconds: 20);
  static const Duration receiveTimeout = Duration(seconds: 30);
}
