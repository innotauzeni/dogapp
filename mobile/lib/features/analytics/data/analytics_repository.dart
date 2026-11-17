import '../../../core/network/api_client.dart';
import '../models/analytics_summary.dart';

class AnalyticsRepository {
  final ApiClient _api;
  AnalyticsRepository([ApiClient? api]) : _api = api ?? ApiClient.instance;

  Future<AnalyticsSummary> getSummary(int companyId, {int months = 6}) => _api.get(
        'Analytics/summary',
        query: {'companyId': companyId, 'months': months},
        parser: (d) => AnalyticsSummary.fromJson((d as Map).cast<String, dynamic>()),
      );
}
