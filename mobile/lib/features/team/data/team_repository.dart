import '../../../core/network/api_client.dart';
import '../models/team_models.dart';

class TeamRepository {
  final ApiClient _api;
  TeamRepository([ApiClient? api]) : _api = api ?? ApiClient.instance;

  Future<List<TeamUser>> getUsers({required int companyId}) => _api.get(
        'users',
        query: {'companyId': companyId},
        parser: (d) => (d as List).map((e) => TeamUser.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );

  Future<void> createUser(Map<String, dynamic> body) => _api.post('users', body: body, parser: (_) {});

  Future<TeamUser> getUser(String id) => _api.get(
        'users/$id',
        parser: (d) => TeamUser.fromJson((d as Map).cast<String, dynamic>()),
      );

  Future<void> updateUser(String id, Map<String, dynamic> body) => _api.put('users/$id', body: body, parser: (_) {});

  Future<void> deleteUser(String id) => _api.delete('users/$id', parser: (_) {});

  Future<List<RoleOption>> getRoles() => _api.get(
        'Roles/GetRoles',
        parser: (d) => (d as List).map((e) => RoleOption.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );
}
