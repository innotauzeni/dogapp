/// A normalised error surfaced to the UI layer.
class ApiException implements Exception {
  final String message;
  final int? statusCode;
  final List<String>? errors;

  ApiException(this.message, {this.statusCode, this.errors});

  /// Whether the session is no longer valid and the user must re-authenticate.
  bool get isUnauthorized => statusCode == 401;

  @override
  String toString() => message;
}
