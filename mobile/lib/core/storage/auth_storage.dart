import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Persists auth tokens & the cached user payload in the platform secure store.
class AuthStorage {
  AuthStorage._();
  static final AuthStorage instance = AuthStorage._();

  static const _kAccess = 'access_token';
  static const _kRefresh = 'refresh_token';
  static const _kUser = 'user_json';
  static const _kPerms = 'permissions_json';
  static const _kBranches = 'branches_json';
  static const _kHeadOffice = 'is_head_office';
  static const _kBranch = 'current_branch_id';
  static const _kBrandColor = 'brand_color';

  final FlutterSecureStorage _storage = const FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
  );

  Future<void> saveTokens({required String access, required String refresh}) async {
    await _storage.write(key: _kAccess, value: access);
    await _storage.write(key: _kRefresh, value: refresh);
  }

  Future<void> saveUserJson(String json) => _storage.write(key: _kUser, value: json);

  /// Persists the parts of the session the app must restore on restart so
  /// permission checks and branch context keep working without a re-login.
  Future<void> saveSessionExtras({
    required String permissionsJson,
    required String branchesJson,
    required bool isHeadOffice,
  }) async {
    await _storage.write(key: _kPerms, value: permissionsJson);
    await _storage.write(key: _kBranches, value: branchesJson);
    await _storage.write(key: _kHeadOffice, value: isHeadOffice ? 'true' : 'false');
  }

  Future<void> saveCurrentBranchId(int? id) =>
      id == null ? _storage.delete(key: _kBranch) : _storage.write(key: _kBranch, value: '$id');

  /// Persists the company's welcome-screen brand color so it's available
  /// immediately on the next cold start, before the network has responded.
  Future<void> saveBrandColor(String? hex) =>
      hex == null ? _storage.delete(key: _kBrandColor) : _storage.write(key: _kBrandColor, value: hex);

  Future<String?> get accessToken => _storage.read(key: _kAccess);
  Future<String?> get refreshToken => _storage.read(key: _kRefresh);
  Future<String?> get userJson => _storage.read(key: _kUser);
  Future<String?> get permissionsJson => _storage.read(key: _kPerms);
  Future<String?> get branchesJson => _storage.read(key: _kBranches);
  Future<bool> get isHeadOffice async => (await _storage.read(key: _kHeadOffice)) == 'true';
  Future<int?> get currentBranchId async {
    final v = await _storage.read(key: _kBranch);
    return v == null ? null : int.tryParse(v);
  }

  Future<String?> get brandColor => _storage.read(key: _kBrandColor);

  Future<bool> get hasSession async => (await accessToken)?.isNotEmpty == true;

  Future<void> clear() async {
    await _storage.delete(key: _kAccess);
    await _storage.delete(key: _kRefresh);
    await _storage.delete(key: _kUser);
    await _storage.delete(key: _kPerms);
    await _storage.delete(key: _kBranches);
    await _storage.delete(key: _kHeadOffice);
    await _storage.delete(key: _kBranch);
    await _storage.delete(key: _kBrandColor);
  }
}
