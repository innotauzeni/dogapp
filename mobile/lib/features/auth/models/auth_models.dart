/// Mirrors the API's auth DTOs (camelCase JSON).
library;

class UserInfo {
  final String id;
  final String companyId;
  final String email;
  final String name;
  final String? role;

  UserInfo({
    required this.id,
    required this.companyId,
    required this.email,
    required this.name,
    this.role,
  });

  /// CompanyId comes back as a string; expose an int for query params.
  int get companyIdInt => int.tryParse(companyId) ?? 0;

  factory UserInfo.fromJson(Map<String, dynamic> j) => UserInfo(
        id: (j['id'] ?? '').toString(),
        companyId: (j['companyId'] ?? '').toString(),
        email: (j['email'] ?? '').toString(),
        name: (j['name'] ?? '').toString(),
        role: j['role'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'companyId': companyId,
        'email': email,
        'name': name,
        'role': role,
      };

  String get initials {
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.isEmpty || parts.first.isEmpty) return '?';
    if (parts.length == 1) return parts.first.substring(0, 1).toUpperCase();
    return (parts.first.substring(0, 1) + parts.last.substring(0, 1)).toUpperCase();
  }
}

class Branch {
  final int id;
  final String? companyName;
  final String? companyCode;
  final String? location;
  final bool isActive;

  Branch({required this.id, this.companyName, this.companyCode, this.location, required this.isActive});

  factory Branch.fromJson(Map<String, dynamic> j) => Branch(
        id: (j['id'] is int) ? j['id'] as int : int.tryParse('${j['id']}') ?? 0,
        companyName: j['companyName'] as String?,
        companyCode: j['companyCode'] as String?,
        location: j['location'] as String?,
        isActive: j['isActive'] as bool? ?? false,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'companyName': companyName,
        'companyCode': companyCode,
        'location': location,
        'isActive': isActive,
      };
}

class LoginResult {
  final String accessToken;
  final String refreshToken;
  final UserInfo user;
  final List<String> permissions;
  final List<Branch> branches;
  final bool isHeadOffice;
  final String? brandColor;

  LoginResult({
    required this.accessToken,
    required this.refreshToken,
    required this.user,
    required this.permissions,
    required this.branches,
    required this.isHeadOffice,
    this.brandColor,
  });

  factory LoginResult.fromJson(Map<String, dynamic> j) => LoginResult(
        accessToken: (j['accessToken'] ?? '').toString(),
        refreshToken: (j['refreshToken'] ?? '').toString(),
        user: UserInfo.fromJson((j['user'] as Map).cast<String, dynamic>()),
        permissions: ((j['permissions'] as List?) ?? const []).map((e) => e.toString()).toList(),
        branches: ((j['branches'] as List?) ?? const [])
            .map((e) => Branch.fromJson((e as Map).cast<String, dynamic>()))
            .toList(),
        isHeadOffice: j['isHeadOffice'] as bool? ?? false,
        brandColor: j['brandColor'] as String?,
      );
}
