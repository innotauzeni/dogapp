import '../../../core/network/api_client.dart';
import '../models/company_info.dart';

/// Company branding endpoints.
class CompanyRepository {
  final ApiClient _api;
  CompanyRepository([ApiClient? api]) : _api = api ?? ApiClient.instance;

  /// Updates the company's welcome-screen brand color. Pass null to reset to
  /// the app default.
  Future<void> updateBrandColor(int companyId, String? hexColor) => _api.put(
        'Companies/$companyId/brand-color',
        body: {'brandColor': hexColor},
        parser: (_) {},
      );

  Future<CompanyInfo> getCompany(int id) => _api.get(
        'Companies/$id',
        parser: (d) => CompanyInfo.fromJson((d as Map).cast<String, dynamic>()),
      );

  /// Resolves the ultimate head-office company for [companyId] — if it's a
  /// branch, follows its parent link once (branches sit one level under head
  /// office in this app's model).
  Future<CompanyInfo> getRootCompany(int companyId) async {
    final company = await getCompany(companyId);
    if (company.parentCompanyId == null) return company;
    return getCompany(company.parentCompanyId!);
  }
}
