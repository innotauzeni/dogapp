import 'dart:convert';

import 'package:flutter/material.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/storage/auth_storage.dart';
import '../../../core/theme/app_colors.dart';
import '../../settings/data/company_repository.dart';
import '../data/auth_repository.dart';
import '../models/auth_models.dart';

enum AuthStatus { unknown, authenticated, unauthenticated }

/// Owns the authentication lifecycle and exposes the current user + permissions
/// to the whole widget tree.
class AuthProvider extends ChangeNotifier {
  final AuthRepository _repo = AuthRepository(ApiClient.instance);
  final CompanyRepository _companyRepo = CompanyRepository(ApiClient.instance);

  AuthStatus _status = AuthStatus.unknown;
  UserInfo? _user;
  List<String> _permissions = const [];
  List<Branch> _branches = const [];
  bool _isHeadOffice = false;
  int? _currentBranchId;
  bool _busy = false;
  String? _error;
  String? _brandColorHex;

  AuthProvider() {
    ApiClient.instance.onSessionExpired = _onSessionExpired;
  }

  AuthStatus get status => _status;
  UserInfo? get user => _user;
  List<String> get permissions => _permissions;
  List<Branch> get branches => _branches;
  bool get isHeadOffice => _isHeadOffice;
  int? get currentBranchId => _currentBranchId;
  bool get busy => _busy;
  String? get error => _error;

  int get companyId => _user?.companyIdInt ?? 0;

  /// The company whose data should be shown: the switched branch for a head-office
  /// user, otherwise the user's own company.
  int get activeCompanyId => (_isHeadOffice && (_currentBranchId ?? 0) > 0) ? _currentBranchId! : companyId;

  bool get isDogHandler => (_user?.role ?? '') == 'Dog Handler';

  /// Users who can customize company-wide settings (e.g. the welcome screen color).
  bool get isAdmin {
    final role = (_user?.role ?? '').toLowerCase();
    return role == 'admin' || role == 'systemowner' || _isHeadOffice;
  }

  /// Branding (app-wide accent color) is deliberately narrower than [isAdmin] —
  /// only the Admin/SystemOwner roles themselves, not every head-office user.
  bool get canManageBranding {
    final role = (_user?.role ?? '').toLowerCase();
    return role == 'admin' || role == 'systemowner';
  }

  /// System Logs and the Audit Trail are global, cross-company tables — the API hard-blocks
  /// anyone but the SystemOwner role regardless of permissions, so gate the UI the same way
  /// rather than showing a tile that always 403s for anyone else.
  bool get isSystemOwner => (_user?.role ?? '').toLowerCase() == 'systemowner';

  /// The company's custom welcome-screen color, if one has been set.
  Color? get brandColor => AppColors.fromHex(_brandColorHex);
  String? get brandColorHex => _brandColorHex;

  bool can(String permission) => _permissions.contains(permission);

  /// Saves a new welcome-screen brand color for the current company (admin only).
  /// Pass null to reset to the app default.
  Future<void> updateBrandColor(String? hex) async {
    await _companyRepo.updateBrandColor(companyId, hex);
    _brandColorHex = hex;
    await AuthStorage.instance.saveBrandColor(hex);
    notifyListeners();
  }

  /// Head-office context switch. Pass null (or 0) to return to "All / own".
  Future<void> switchBranch(int? branchId) async {
    _currentBranchId = (branchId == null || branchId == 0) ? null : branchId;
    await AuthStorage.instance.saveCurrentBranchId(_currentBranchId);
    notifyListeners();
  }

  /// Restores a session from secure storage on app start.
  Future<void> bootstrap() async {
    try {
      final hasSession = await AuthStorage.instance.hasSession;
      if (!hasSession) {
        _status = AuthStatus.unauthenticated;
        notifyListeners();
        return;
      }
      final json = await AuthStorage.instance.userJson;
      if (json != null) {
        try {
          _user = UserInfo.fromJson(jsonDecode(json) as Map<String, dynamic>);
        } catch (_) {/* ignore corrupt cache */}
      }
      // Restore permissions + branch context so gating & scoping work after a restart.
      final permsJson = await AuthStorage.instance.permissionsJson;
      if (permsJson != null) {
        try {
          _permissions = (jsonDecode(permsJson) as List).map((e) => e.toString()).toList();
        } catch (_) {/* ignore */}
      }
      final branchesJson = await AuthStorage.instance.branchesJson;
      if (branchesJson != null) {
        try {
          _branches = (jsonDecode(branchesJson) as List)
              .map((e) => Branch.fromJson((e as Map).cast<String, dynamic>()))
              .toList();
        } catch (_) {/* ignore */}
      }
      _isHeadOffice = await AuthStorage.instance.isHeadOffice;
      _currentBranchId = await AuthStorage.instance.currentBranchId;
      _brandColorHex = await AuthStorage.instance.brandColor;
      _status = AuthStatus.authenticated;
    } catch (_) {
      // Storage unavailable — treat as a fresh, signed-out session.
      _status = AuthStatus.unauthenticated;
    }
    notifyListeners();
  }

  Future<bool> login(String identifier, String password) async {
    _busy = true;
    _error = null;
    notifyListeners();
    try {
      final result = await _repo.login(identifier: identifier, password: password);
      await AuthStorage.instance.saveTokens(access: result.accessToken, refresh: result.refreshToken);
      await AuthStorage.instance.saveUserJson(jsonEncode(result.user.toJson()));
      await AuthStorage.instance.saveSessionExtras(
        permissionsJson: jsonEncode(result.permissions),
        branchesJson: jsonEncode(result.branches.map((b) => b.toJson()).toList()),
        isHeadOffice: result.isHeadOffice,
      );
      await AuthStorage.instance.saveCurrentBranchId(null);
      await AuthStorage.instance.saveBrandColor(result.brandColor);
      _user = result.user;
      _permissions = result.permissions;
      _branches = result.branches;
      _isHeadOffice = result.isHeadOffice;
      _currentBranchId = null;
      _brandColorHex = result.brandColor;
      _status = AuthStatus.authenticated;
      _busy = false;
      notifyListeners();
      return true;
    } on ApiException catch (e) {
      _error = e.message;
      _busy = false;
      notifyListeners();
      return false;
    } catch (_) {
      _error = 'Unexpected error. Please try again.';
      _busy = false;
      notifyListeners();
      return false;
    }
  }

  Future<void> logout() async {
    await AuthStorage.instance.clear();
    _user = null;
    _permissions = const [];
    _branches = const [];
    _isHeadOffice = false;
    _currentBranchId = null;
    _brandColorHex = null;
    _status = AuthStatus.unauthenticated;
    notifyListeners();
  }

  void _onSessionExpired() {
    AuthStorage.instance.clear();
    _user = null;
    _permissions = const [];
    _branches = const [];
    _isHeadOffice = false;
    _currentBranchId = null;
    _brandColorHex = null;
    _status = AuthStatus.unauthenticated;
    notifyListeners();
  }
}
