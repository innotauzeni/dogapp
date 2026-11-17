int _asInt(dynamic v) => v is int ? v : int.tryParse('$v') ?? 0;

class SearchResultItem {
  final String type; // "Dog", "Customer", "Invoice"
  final int id;
  final String title;
  final String? subtitle;

  SearchResultItem({required this.type, required this.id, required this.title, this.subtitle});

  factory SearchResultItem.fromJson(Map<String, dynamic> j) => SearchResultItem(
        type: (j['type'] ?? '').toString(),
        id: _asInt(j['id']),
        title: (j['title'] ?? '').toString(),
        subtitle: j['subtitle'] as String?,
      );
}

class SearchResults {
  final List<SearchResultItem> dogs;
  final List<SearchResultItem> customers;
  final List<SearchResultItem> invoices;

  SearchResults({this.dogs = const [], this.customers = const [], this.invoices = const []});

  bool get isEmpty => dogs.isEmpty && customers.isEmpty && invoices.isEmpty;

  factory SearchResults.fromJson(Map<String, dynamic> j) => SearchResults(
        dogs: ((j['dogs'] as List?) ?? const []).map((e) => SearchResultItem.fromJson((e as Map).cast<String, dynamic>())).toList(),
        customers: ((j['customers'] as List?) ?? const []).map((e) => SearchResultItem.fromJson((e as Map).cast<String, dynamic>())).toList(),
        invoices: ((j['invoices'] as List?) ?? const []).map((e) => SearchResultItem.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );
}
