/// Mirrors `CompanyDto` (`Dog.Application/Features/Companies/DTOs/CompanyDtos.cs`).
class CompanyBranch {
  final int id;
  final String companyName;
  final String? companyCode;
  final String? emailAddress;
  final String? phoneNumber;
  final String? website;
  final String? address;
  final String? location;
  final String? countryName;
  final String? provinceName;
  final String? cityName;
  final String? currencyCode;
  final int? parentCompanyId;
  final String? parentCompanyName;
  final bool isActive;
  final String companyType;
  final int branchCount;
  final int userCount;

  CompanyBranch({
    required this.id,
    required this.companyName,
    this.companyCode,
    this.emailAddress,
    this.phoneNumber,
    this.website,
    this.address,
    this.location,
    this.countryName,
    this.provinceName,
    this.cityName,
    this.currencyCode,
    this.parentCompanyId,
    this.parentCompanyName,
    required this.isActive,
    required this.companyType,
    this.branchCount = 0,
    this.userCount = 0,
  });

  bool get isHeadOffice => parentCompanyId == null;

  factory CompanyBranch.fromJson(Map<String, dynamic> j) => CompanyBranch(
        id: (j['id'] is int) ? j['id'] as int : int.tryParse('${j['id']}') ?? 0,
        companyName: (j['companyName'] ?? '').toString(),
        companyCode: j['companyCode'] as String?,
        emailAddress: j['emailAddress'] as String?,
        phoneNumber: j['phoneNumber'] as String?,
        website: j['website'] as String?,
        address: j['address'] as String?,
        location: j['location'] as String?,
        countryName: j['countryName'] as String?,
        provinceName: j['provinceName'] as String?,
        cityName: j['cityName'] as String?,
        currencyCode: j['currencyCode'] as String?,
        parentCompanyId: j['parentCompanyId'] == null ? null : int.tryParse('${j['parentCompanyId']}'),
        parentCompanyName: j['parentCompanyName'] as String?,
        isActive: j['isActive'] as bool? ?? false,
        companyType: (j['companyType'] ?? '').toString(),
        branchCount: (j['branchCount'] is int) ? j['branchCount'] as int : int.tryParse('${j['branchCount']}') ?? 0,
        userCount: (j['userCount'] is int) ? j['userCount'] as int : int.tryParse('${j['userCount']}') ?? 0,
      );
}

/// A simple id/name lookup row — used for countries, provinces and cities,
/// whose list endpoints only reliably populate `id`/`name`.
class LookupItem {
  final int id;
  final String name;
  LookupItem({required this.id, required this.name});

  factory LookupItem.fromJson(Map<String, dynamic> j) => LookupItem(
        id: (j['id'] is int) ? j['id'] as int : int.tryParse('${j['id']}') ?? 0,
        name: (j['name'] ?? '').toString(),
      );
}

class CurrencyOption {
  final int id;
  final String code;
  final String name;
  final String? symbol;
  CurrencyOption({required this.id, required this.code, required this.name, this.symbol});

  factory CurrencyOption.fromJson(Map<String, dynamic> j) => CurrencyOption(
        id: (j['id'] is int) ? j['id'] as int : int.tryParse('${j['id']}') ?? 0,
        code: (j['code'] ?? '').toString(),
        name: (j['name'] ?? '').toString(),
        symbol: j['symbol'] as String?,
      );
}
