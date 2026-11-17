import 'dart:convert';
import 'dart:typed_data';

import '../config/app_config.dart';

/// Helpers for the project's mixed photo storage:
///  * `data:image/...;base64,...`  → uploaded from mobile (decode in-memory)
///  * `http(s)://...`              → absolute URL
///  * `/uploads/dogs/x.jpg`        → relative path served by the web portal
class PhotoUtil {
  PhotoUtil._();

  static bool isDataUri(String? url) => url != null && url.startsWith('data:image');

  /// Decoded bytes for a base64 data URI, or null if not a data URI.
  static Uint8List? bytesFromDataUri(String? url) {
    if (!isDataUri(url)) return null;
    final comma = url!.indexOf(',');
    if (comma < 0) return null;
    try {
      return base64Decode(url.substring(comma + 1));
    } catch (_) {
      return null;
    }
  }

  /// Absolute URL for a network photo, resolving portal-relative paths.
  /// Returns null for empty values or data URIs.
  static String? resolveNetworkUrl(String? url) {
    if (url == null || url.isEmpty || isDataUri(url)) return null;
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    final path = url.startsWith('/') ? url : '/$url';
    return '${AppConfig.portalBaseUrl}$path';
  }

  /// Build a data URI from raw JPEG/PNG bytes for upload via `PhotoUrl`.
  static String toDataUri(Uint8List bytes, {String mime = 'image/jpeg'}) =>
      'data:$mime;base64,${base64Encode(bytes)}';
}
