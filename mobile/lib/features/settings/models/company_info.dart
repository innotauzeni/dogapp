/// Mirrors the API's CompanyDto (camelCase JSON).
class CompanyInfo {
  final int id;
  final String? companyName;
  final String? companyCode;
  final String? emailAddress;
  final String? phoneNumber;
  final String? website;
  final String? address;
  final String? logo;
  final String? location;
  final String? countryName;
  final String? provinceName;
  final String? cityName;
  final int? parentCompanyId;
  final String? parentCompanyName;
  final bool isActive;
  final String companyType;

  CompanyInfo({
    required this.id,
    this.companyName,
    this.companyCode,
    this.emailAddress,
    this.phoneNumber,
    this.website,
    this.address,
    this.logo,
    this.location,
    this.countryName,
    this.provinceName,
    this.cityName,
    this.parentCompanyId,
    this.parentCompanyName,
    required this.isActive,
    required this.companyType,
  });

  /// A single-line "City, Province, Country" summary, skipping blanks.
  String get fullLocation => [location, cityName, provinceName, countryName]
      .where((s) => (s ?? '').trim().isNotEmpty)
      .toSet() // Location often duplicates cityName; avoid repeating it.
      .join(', ');

  factory CompanyInfo.fromJson(Map<String, dynamic> j) => CompanyInfo(
        id: j['id'] is int ? j['id'] as int : int.tryParse('${j['id']}') ?? 0,
        companyName: j['companyName'] as String?,
        companyCode: j['companyCode'] as String?,
        emailAddress: j['emailAddress'] as String?,
        phoneNumber: j['phoneNumber'] as String?,
        website: j['website'] as String?,
        address: j['address'] as String?,
        logo: j['logo'] as String?,
        location: j['location'] as String?,
        countryName: j['countryName'] as String?,
        provinceName: j['provinceName'] as String?,
        cityName: j['cityName'] as String?,
        parentCompanyId: j['parentCompanyId'] == null ? null : int.tryParse('${j['parentCompanyId']}'),
        parentCompanyName: j['parentCompanyName'] as String?,
        isActive: j['isActive'] as bool? ?? false,
        companyType: (j['companyType'] ?? '').toString(),
      );
}
