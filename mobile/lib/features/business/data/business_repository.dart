import '../../../core/network/api_client.dart';
import '../models/business_models.dart';

class BusinessRepository {
  final ApiClient _api;
  BusinessRepository([ApiClient? api]) : _api = api ?? ApiClient.instance;

  List<T> _list<T>(dynamic data, T Function(Map<String, dynamic>) fromJson) =>
      ((data as List?) ?? const [])
          .map((e) => fromJson((e as Map).cast<String, dynamic>()))
          .toList();

  Future<List<Invoice>> getInvoices(int companyId) => _api.get(
        'Invoice',
        query: {'CompanyId': companyId},
        parser: (d) => _list(d, Invoice.fromJson),
      );

  Future<Invoice> getInvoice(int id) => _api.get('Invoice/$id', parser: (d) => Invoice.fromJson((d as Map).cast<String, dynamic>()));

  /// Our bank accounts a customer can pay into (id + label).
  Future<List<Map<String, dynamic>>> getBankAccounts() => _api.get(
        'BillingSetup/pdf-info',
        query: {'CompanyId': 0},
        parser: (d) => (((d as Map)['bankAccounts'] as List?) ?? const []).map((e) => (e as Map).cast<String, dynamic>()).toList(),
      );

  Future<void> payInvoice(Map<String, dynamic> body) => _api.post('Invoice/pay', body: body, parser: (_) {});

  Future<void> createInvoice(Map<String, dynamic> body) =>
      _api.post('Invoice', body: body, parser: (_) {});

  Future<List<Customer>> getCustomers(int companyId) => _api.get(
        'Customer',
        query: {'CompanyId': companyId},
        parser: (d) => _list(d, Customer.fromJson),
      );

  Future<void> createCustomer(Map<String, dynamic> body) =>
      _api.post('Customer/CreateCustomer', body: body, parser: (_) {});

  Future<List<CustomerSite>> getCustomerSites(int customerId) => _api.get(
        'CustomerSite',
        query: {'customerId': customerId},
        parser: (d) => _list(d, CustomerSite.fromJson),
      );

  Future<void> createCustomerSite(Map<String, dynamic> body) =>
      _api.post('CustomerSite', body: body, parser: (_) {});

  Future<List<Expense>> getExpenses(int companyId) => _api.get(
        'Expense',
        query: {'CompanyId': companyId},
        parser: (d) => _list(d, Expense.fromJson),
      );

  Future<void> createExpense(Map<String, dynamic> body) =>
      _api.post('Expense', body: body, parser: (_) {});
}
