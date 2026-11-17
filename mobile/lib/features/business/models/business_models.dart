/// Business domain models (invoices, customers) mirroring the API DTOs.
library;

int _asInt(dynamic v) => v is int ? v : int.tryParse('$v') ?? 0;
double _asDouble(dynamic v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
DateTime? _asDate(dynamic v) => v == null ? null : DateTime.tryParse('$v');

class Expense {
  final int id;
  final String category;
  final String description;
  final double amount;
  final DateTime? expenseDate;
  final int? dogId;
  final String? dogName;
  final String? vendor;
  final String? paymentMethod;
  final String? notes;

  Expense({
    required this.id,
    required this.category,
    required this.description,
    required this.amount,
    this.expenseDate,
    this.dogId,
    this.dogName,
    this.vendor,
    this.paymentMethod,
    this.notes,
  });

  factory Expense.fromJson(Map<String, dynamic> j) => Expense(
        id: _asInt(j['id']),
        category: (j['category'] ?? '').toString(),
        description: (j['description'] ?? '').toString(),
        amount: _asDouble(j['amount']),
        expenseDate: _asDate(j['expenseDate']),
        dogId: j['dogId'] == null ? null : _asInt(j['dogId']),
        dogName: j['dogName'] as String?,
        vendor: j['vendor'] as String?,
        paymentMethod: j['paymentMethod'] as String?,
        notes: j['notes'] as String?,
      );
}

class Customer {
  final int id;
  final int companyId;
  final String? firstName;
  final String? lastName;
  final String? fullName;
  final String? gender;
  final String? idNumber;
  final String? cellphone;
  final String? email;
  final String? address;
  final String? dob;

  Customer({
    required this.id,
    required this.companyId,
    this.firstName,
    this.lastName,
    this.fullName,
    this.gender,
    this.idNumber,
    this.cellphone,
    this.email,
    this.address,
    this.dob,
  });

  String get displayName {
    final fn = (fullName ?? '').trim();
    if (fn.isNotEmpty) return fn;
    return '${firstName ?? ''} ${lastName ?? ''}'.trim();
  }

  factory Customer.fromJson(Map<String, dynamic> j) => Customer(
        id: _asInt(j['id']),
        companyId: _asInt(j['companyId']),
        firstName: j['firstName'] as String?,
        lastName: j['lastName'] as String?,
        fullName: j['fullName'] as String?,
        gender: j['gender'] as String?,
        idNumber: j['iDnumber'] as String? ?? j['idNumber'] as String?,
        cellphone: j['cellphone'] as String?,
        email: j['email'] as String?,
        address: j['address'] as String?,
        dob: j['dob'] as String?,
      );
}

class CustomerSite {
  final int id;
  final int customerId;
  final String siteName;
  final String? address;
  final double? latitude;
  final double? longitude;
  final String? contactPerson;
  final String? contactPhone;

  CustomerSite({
    required this.id,
    required this.customerId,
    required this.siteName,
    this.address,
    this.latitude,
    this.longitude,
    this.contactPerson,
    this.contactPhone,
  });

  factory CustomerSite.fromJson(Map<String, dynamic> j) => CustomerSite(
        id: _asInt(j['id']),
        customerId: _asInt(j['customerId']),
        siteName: (j['siteName'] ?? '').toString(),
        address: j['address'] as String?,
        latitude: j['latitude'] == null ? null : _asDouble(j['latitude']),
        longitude: j['longitude'] == null ? null : _asDouble(j['longitude']),
        contactPerson: j['contactPerson'] as String?,
        contactPhone: j['contactPhone'] as String?,
      );
}

class InvoiceItem {
  final int id;
  final String description;
  final int quantity;
  final double unitPrice;
  final double amount;

  InvoiceItem({required this.id, required this.description, required this.quantity, required this.unitPrice, required this.amount});

  factory InvoiceItem.fromJson(Map<String, dynamic> j) => InvoiceItem(
        id: _asInt(j['id']),
        description: (j['description'] ?? '').toString(),
        quantity: _asInt(j['quantity']),
        unitPrice: _asDouble(j['unitPrice']),
        amount: _asDouble(j['amount']),
      );
}

class Invoice {
  final int id;
  final String invoiceNumber;
  final String? customerName;
  final String? customerPhone;
  final DateTime? invoiceDate;
  final DateTime? dueDate;
  final double totalAmount;
  final double paidAmount;
  final double balanceAmount;
  final String status;
  final String? notes;
  final List<InvoiceItem> items;

  Invoice({
    required this.id,
    required this.invoiceNumber,
    this.customerName,
    this.customerPhone,
    this.invoiceDate,
    this.dueDate,
    required this.totalAmount,
    required this.paidAmount,
    required this.balanceAmount,
    required this.status,
    this.notes,
    this.items = const [],
  });

  factory Invoice.fromJson(Map<String, dynamic> j) => Invoice(
        id: _asInt(j['id']),
        invoiceNumber: (j['invoiceNumber'] ?? '').toString(),
        customerName: j['customerName'] as String?,
        customerPhone: j['customerPhone'] as String?,
        invoiceDate: _asDate(j['invoiceDate']),
        dueDate: _asDate(j['dueDate']),
        totalAmount: _asDouble(j['totalAmount']),
        paidAmount: _asDouble(j['paidAmount']),
        balanceAmount: _asDouble(j['balanceAmount']),
        status: (j['status'] ?? 'DRAFT').toString(),
        notes: j['notes'] as String?,
        items: ((j['items'] as List?) ?? const [])
            .map((e) => InvoiceItem.fromJson((e as Map).cast<String, dynamic>()))
            .toList(),
      );
}
