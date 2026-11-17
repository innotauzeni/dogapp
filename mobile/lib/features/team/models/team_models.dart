/// Mirrors `UserDto` (`Dog.Application/Features/Users/DTOs/UserDtos.cs`).
class TeamUser {
  final String id;
  final String? name;
  final String? surname;
  final String? username;
  final String? email;
  final String? phoneNumber;
  final String? status;
  final String? roleName;
  final int companyId;
  final DateTime? lastLoginDate;
  final DateTime createdAt;

  TeamUser({
    required this.id,
    this.name,
    this.surname,
    this.username,
    this.email,
    this.phoneNumber,
    this.status,
    this.roleName,
    required this.companyId,
    this.lastLoginDate,
    required this.createdAt,
  });

  String get fullName => [name, surname].where((s) => s != null && s.isNotEmpty).join(' ');
  bool get isActive => (status ?? '').toUpperCase() == 'ACTIVE';

  factory TeamUser.fromJson(Map<String, dynamic> j) => TeamUser(
        id: (j['id'] ?? '').toString(),
        name: j['name'] as String?,
        surname: j['surname'] as String?,
        username: j['username'] as String?,
        email: j['email'] as String?,
        phoneNumber: j['phoneNumber'] as String?,
        status: j['status'] as String?,
        roleName: j['roleName'] as String?,
        companyId: (j['companyId'] is int) ? j['companyId'] as int : int.tryParse('${j['companyId']}') ?? 0,
        lastLoginDate: j['lastLoginDate'] == null ? null : DateTime.tryParse('${j['lastLoginDate']}'),
        createdAt: DateTime.tryParse('${j['createdAt']}') ?? DateTime.now(),
      );
}

/// Mirrors `RoleDto` (`Dog.Application/Features/Roles/DTOs/RoleDtos.cs`).
class RoleOption {
  final String id;
  final String name;
  RoleOption({required this.id, required this.name});

  factory RoleOption.fromJson(Map<String, dynamic> j) => RoleOption(
        id: (j['id'] ?? '').toString(),
        name: (j['name'] ?? '').toString(),
      );
}
