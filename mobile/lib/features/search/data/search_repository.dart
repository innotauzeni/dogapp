import '../../../core/network/api_client.dart';
import '../models/search_result.dart';

class SearchRepository {
  final ApiClient _api;
  SearchRepository([ApiClient? api]) : _api = api ?? ApiClient.instance;

  Future<SearchResults> search(int companyId, String query) => _api.get(
        'Search',
        query: {'companyId': companyId, 'query': query},
        parser: (d) => SearchResults.fromJson((d as Map).cast<String, dynamic>()),
      );
}
