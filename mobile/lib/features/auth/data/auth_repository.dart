import '../../../core/network/api_client.dart';
import '../models/auth_models.dart';

class AuthRepository {
  final ApiClient _api;
  AuthRepository(this._api);

  Future<LoginResult> login({required String identifier, required String password}) {
    return _api.post<LoginResult>(
      'auth/login',
      body: {'identifier': identifier, 'password': password},
      parser: (data) => LoginResult.fromJson((data as Map).cast<String, dynamic>()),
    );
  }
}
