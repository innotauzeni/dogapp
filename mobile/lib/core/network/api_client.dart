import 'dart:typed_data';

import 'package:dio/dio.dart';

import '../config/app_config.dart';
import '../storage/auth_storage.dart';
import 'api_exception.dart';

/// Thin wrapper around Dio that:
///  * prefixes every call with [AppConfig.apiBaseUrl];
///  * attaches the bearer token;
///  * transparently refreshes an expired token once on 401;
///  * unwraps the API's `{ success, message, data, errors }` envelope.
class ApiClient {
  ApiClient._() {
    _dio = Dio(
      BaseOptions(
        baseUrl: AppConfig.apiBaseUrl,
        connectTimeout: AppConfig.connectTimeout,
        receiveTimeout: AppConfig.receiveTimeout,
        // We validate status manually so we can read the envelope on 4xx too.
        validateStatus: (s) => s != null && s < 500,
        headers: {'Content-Type': 'application/json'},
      ),
    );
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await AuthStorage.instance.accessToken;
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
      ),
    );
  }

  static final ApiClient instance = ApiClient._();
  late final Dio _dio;

  /// Invoked when the session can no longer be refreshed — the app should
  /// route back to login. Wired up by [AuthProvider].
  void Function()? onSessionExpired;

  // ── Public verbs ─────────────────────────────────────────────────────────

  Future<T> get<T>(String path, {Map<String, dynamic>? query, required T Function(dynamic) parser}) =>
      _send<T>(() => _dio.get(path, queryParameters: query), parser);

  Future<T> post<T>(String path, {Object? body, Map<String, dynamic>? query, required T Function(dynamic) parser}) =>
      _send<T>(() => _dio.post(path, data: body, queryParameters: query), parser);

  Future<T> put<T>(String path, {Object? body, required T Function(dynamic) parser}) =>
      _send<T>(() => _dio.put(path, data: body), parser);

  Future<T> delete<T>(String path, {Map<String, dynamic>? query, required T Function(dynamic) parser}) =>
      _send<T>(() => _dio.delete(path, queryParameters: query), parser);

  /// Fetches a raw binary payload (e.g. a PDF) with auth + one-time 401 refresh.
  /// Bypasses the JSON envelope since binary responses are not enveloped.
  Future<Uint8List> getBytes(String path, {Map<String, dynamic>? query}) async {
    try {
      Future<Response> request() => _dio.get(
            path,
            queryParameters: query,
            options: Options(responseType: ResponseType.bytes),
          );

      var response = await request();
      if (response.statusCode == 401 && await _tryRefresh()) {
        response = await request();
      }

      final status = response.statusCode ?? 0;
      if (status >= 200 && status < 300 && response.data != null) {
        return Uint8List.fromList(List<int>.from(response.data as List));
      }
      if (status == 401) throw ApiException('Session expired', statusCode: 401);
      throw ApiException('Could not load file ($status)', statusCode: status);
    } on DioException catch (e) {
      throw _fromDio(e);
    }
  }

  // ── Core ──────────────────────────────────────────────────────────────────

  Future<T> _send<T>(Future<Response> Function() request, T Function(dynamic) parser) async {
    try {
      var response = await request();

      if (response.statusCode == 401 && await _tryRefresh()) {
        response = await request(); // retry once with the new token
      }

      final data = _unwrap(response);
      return parser(data);
    } on DioException catch (e) {
      throw _fromDio(e);
    }
  }

  /// Validates the envelope and returns the `data` payload, or throws.
  dynamic _unwrap(Response response) {
    final status = response.statusCode ?? 0;
    final body = response.data;

    if (body is Map && body.containsKey('success')) {
      if (body['success'] == true) return body['data'];
      throw ApiException(
        (body['message'] as String?) ?? 'Request failed',
        statusCode: status,
        errors: (body['errors'] as List?)?.map((e) => e.toString()).toList(),
      );
    }

    if (status >= 200 && status < 300) return body; // non-enveloped success
    if (status == 401) throw ApiException('Session expired', statusCode: 401);
    // Include the failing endpoint — a bare "Request failed (404)" gives no way to tell
    // which of the app's many API calls actually broke without reproducing it live.
    final path = response.requestOptions.path;
    throw ApiException('Request failed ($status): $path', statusCode: status);
  }

  bool _refreshing = false;

  Future<bool> _tryRefresh() async {
    if (_refreshing) return false;
    _refreshing = true;
    try {
      final refresh = await AuthStorage.instance.refreshToken;
      if (refresh == null || refresh.isEmpty) return false;

      // Use a bare Dio so the interceptor doesn't attach the dead token.
      final res = await Dio(BaseOptions(baseUrl: AppConfig.apiBaseUrl))
          .post('auth/refresh', data: {'refreshToken': refresh});
      final body = res.data;
      if (body is Map && body['success'] == true && body['data'] is Map) {
        final data = body['data'] as Map;
        await AuthStorage.instance.saveTokens(
          access: data['accessToken'] as String,
          refresh: data['refreshToken'] as String,
        );
        return true;
      }
      return false;
    } catch (_) {
      return false;
    } finally {
      _refreshing = false;
      // If we reach here without a token the caller will receive a 401 again
      // and the parser throws; signal the app to log out.
      final stillValid = (await AuthStorage.instance.accessToken)?.isNotEmpty == true;
      if (!stillValid) onSessionExpired?.call();
    }
  }

  ApiException _fromDio(DioException e) {
    if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.receiveTimeout ||
        e.type == DioExceptionType.sendTimeout) {
      return ApiException('Connection timed out. Check your network and try again.');
    }
    if (e.type == DioExceptionType.connectionError) {
      return ApiException('Cannot reach the server. Check your internet connection.');
    }
    final data = e.response?.data;
    if (data is Map && data['message'] is String) {
      return ApiException(data['message'] as String, statusCode: e.response?.statusCode);
    }
    return ApiException('Something went wrong. Please try again.', statusCode: e.response?.statusCode);
  }
}
