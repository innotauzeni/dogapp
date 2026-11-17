import '../../../core/network/api_client.dart';
import '../models/branch_models.dart';

class BranchesRepository {
  final ApiClient _api;
  BranchesRepository([ApiClient? api]) : _api = api ?? ApiClient.instance;

  Future<List<CompanyBranch>> getBranchesOf(int headOfficeId) => _api.get(
        'Companies/$headOfficeId/branches',
        parser: (d) => (d as List).map((e) => CompanyBranch.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );

  Future<CompanyBranch> getCompany(int id) => _api.get(
        'Companies/$id',
        parser: (d) => CompanyBranch.fromJson((d as Map).cast<String, dynamic>()),
      );

  Future<void> createBranch(Map<String, dynamic> body) => _api.post('Companies', body: body, parser: (_) {});

  Future<void> deleteBranch(int id) => _api.delete('Companies/$id', parser: (_) {});

  Future<List<LookupItem>> getCountries() => _api.get(
        'Country/GetPublicCountries',
        parser: (d) => (d as List).map((e) => LookupItem.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );

  Future<List<LookupItem>> getProvinces(int countryId) => _api.get(
        'Country/GetProvincesByCountry/$countryId',
        parser: (d) => (d as List).map((e) => LookupItem.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );

  Future<List<LookupItem>> getCities(int provinceId) => _api.get(
        'Country/GetCitiesByProvince/$provinceId',
        parser: (d) => (d as List).map((e) => LookupItem.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );

  Future<List<CurrencyOption>> getCurrencies() => _api.get(
        'Currency/GetPubicCurrencies',
        parser: (d) => (d as List).map((e) => CurrencyOption.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );
}
