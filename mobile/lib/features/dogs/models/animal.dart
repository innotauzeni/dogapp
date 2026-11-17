/// Dog / animal domain models mirroring the API DTOs (camelCase JSON).
library;

int _asInt(dynamic v) => v is int ? v : int.tryParse('$v') ?? 0;
int? _asIntN(dynamic v) => v == null ? null : (v is int ? v : int.tryParse('$v'));
bool _asBool(dynamic v) => v == true || v == 'true' || v == 1;
DateTime? _asDate(dynamic v) => v == null ? null : DateTime.tryParse('$v');

class Animal {
  final int id;
  final String name;
  final String breed;
  final String color;
  final DateTime? dateOfBirth;
  final DateTime? dateOfDeath;
  final String? gender;
  final String? microchipNumber;
  final String? photoUrl;
  final int? sireId;
  final String? sireName;
  final int? damId;
  final String? damName;
  final int companyId;
  final String? companyName;
  final int? customerId;
  final String? customerName;
  final bool isActive;
  final bool isDeployed;
  final String? deployedLocation;
  final String? deployedTo;
  final List<MatingRecord> matingRecords;

  Animal({
    required this.id,
    required this.name,
    required this.breed,
    required this.color,
    this.dateOfBirth,
    this.dateOfDeath,
    this.gender,
    this.microchipNumber,
    this.photoUrl,
    this.sireId,
    this.sireName,
    this.damId,
    this.damName,
    required this.companyId,
    this.companyName,
    this.customerId,
    this.customerName,
    required this.isActive,
    this.isDeployed = false,
    this.deployedLocation,
    this.deployedTo,
    this.matingRecords = const [],
  });

  bool get isDeceased => dateOfDeath != null;

  factory Animal.fromJson(Map<String, dynamic> j) => Animal(
        id: _asInt(j['id']),
        name: (j['name'] ?? '').toString(),
        breed: (j['breed'] ?? '').toString(),
        color: (j['color'] ?? '').toString(),
        dateOfBirth: _asDate(j['dateOfBirth']),
        dateOfDeath: _asDate(j['dateOfDeath']),
        gender: j['gender'] as String?,
        microchipNumber: j['microchipNumber'] as String?,
        photoUrl: j['photoUrl'] as String?,
        sireId: _asIntN(j['sireId']),
        sireName: j['sireName'] as String?,
        damId: _asIntN(j['damId']),
        damName: j['damName'] as String?,
        companyId: _asInt(j['companyId']),
        companyName: j['companyName'] as String?,
        customerId: _asIntN(j['customerId']),
        customerName: j['customerName'] as String?,
        isActive: _asBool(j['isActive']),
        isDeployed: _asBool(j['isDeployed']),
        deployedLocation: j['deployedLocation'] as String?,
        deployedTo: j['deployedTo'] as String?,
        matingRecords: ((j['matingRecords'] as List?) ?? const [])
            .map((e) => MatingRecord.fromJson((e as Map).cast<String, dynamic>()))
            .toList(),
      );
}

class Vaccination {
  final int id;
  final int? dogId;
  final int? puppyId;
  final String? name;
  final String? vaccType;
  final String? batchNumber;
  final String? vetclinicName;
  final DateTime? vaccinationDate;
  final DateTime? nextVaccinationDate;
  final String? notes;

  Vaccination({
    required this.id,
    this.dogId,
    this.puppyId,
    this.name,
    this.vaccType,
    this.batchNumber,
    this.vetclinicName,
    this.vaccinationDate,
    this.nextVaccinationDate,
    this.notes,
  });

  factory Vaccination.fromJson(Map<String, dynamic> j) => Vaccination(
        id: _asInt(j['id']),
        dogId: _asIntN(j['dogId']),
        puppyId: _asIntN(j['puppyId']),
        name: j['name'] as String?,
        vaccType: j['vaccType'] as String?,
        batchNumber: j['batchNumber'] as String?,
        vetclinicName: j['vetclinicName'] as String?,
        vaccinationDate: _asDate(j['vaccinationDate']),
        nextVaccinationDate: _asDate(j['nextVaccinationDate']),
        notes: j['notes'] as String?,
      );
}

class Puppy {
  final int id;
  final String name;
  final String color;
  final String? gender;
  final DateTime? dateOfBirth;
  final DateTime? dateOfDeath;
  final List<Vaccination> vaccinations;

  Puppy({required this.id, required this.name, required this.color, this.gender, this.dateOfBirth, this.dateOfDeath, this.vaccinations = const []});

  factory Puppy.fromJson(Map<String, dynamic> j) => Puppy(
        id: _asInt(j['id']),
        name: (j['name'] ?? '').toString(),
        color: (j['color'] ?? '').toString(),
        gender: j['gender'] as String?,
        dateOfBirth: _asDate(j['dateOfBirth']),
        dateOfDeath: _asDate(j['dateOfDeath']),
        vaccinations: (j['vaccinations'] as List<dynamic>?)?.map((v) => Vaccination.fromJson(v as Map<String, dynamic>)).toList() ?? [],
      );
}

class Litter {
  final int id;
  final DateTime? birthDate;
  final int totalBorn;
  final int survivedCount;
  final int diedCount;
  final String? notes;
  final String? sireName;
  final String? damName;
  final List<Puppy> puppies;

  Litter({
    required this.id,
    this.birthDate,
    required this.totalBorn,
    required this.survivedCount,
    required this.diedCount,
    this.notes,
    this.sireName,
    this.damName,
    this.puppies = const [],
  });

  factory Litter.fromJson(Map<String, dynamic> j) => Litter(
        id: _asInt(j['id']),
        birthDate: _asDate(j['birthDate']),
        totalBorn: _asInt(j['totalBorn']),
        survivedCount: _asInt(j['survivedCount']),
        diedCount: _asInt(j['diedCount']),
        notes: j['notes'] as String?,
        sireName: j['sireName'] as String?,
        damName: j['damName'] as String?,
        puppies: ((j['puppies'] as List?) ?? const [])
            .map((e) => Puppy.fromJson((e as Map).cast<String, dynamic>()))
            .toList(),
      );
}

class MatingRecord {
  final int id;
  final int sireId;
  final String? sireName;
  final int damId;
  final String? damName;
  final DateTime? matingDate;
  final String? location;
  final String? notes;
  final List<Litter> litters;

  MatingRecord({
    required this.id,
    required this.sireId,
    this.sireName,
    required this.damId,
    this.damName,
    this.matingDate,
    this.location,
    this.notes,
    this.litters = const [],
  });

  factory MatingRecord.fromJson(Map<String, dynamic> j) => MatingRecord(
        id: _asInt(j['id']),
        sireId: _asInt(j['sireId']),
        sireName: j['sireName'] as String?,
        damId: _asInt(j['damId']),
        damName: j['damName'] as String?,
        matingDate: _asDate(j['matingDate']),
        location: j['location'] as String?,
        notes: j['notes'] as String?,
        litters: ((j['litters'] as List?) ?? const [])
            .map((e) => Litter.fromJson((e as Map).cast<String, dynamic>()))
            .toList(),
      );
}

class CareRecord {
  final int id;
  final String eventType;
  final DateTime? occurredOn;
  final String? details;
  final String? quantityOrOutcome;
  final bool requiresAttention;
  final String? recordedByName;

  CareRecord({
    required this.id,
    required this.eventType,
    this.occurredOn,
    this.details,
    this.quantityOrOutcome,
    required this.requiresAttention,
    this.recordedByName,
  });

  factory CareRecord.fromJson(Map<String, dynamic> j) => CareRecord(
        id: _asInt(j['id']),
        eventType: (j['eventType'] ?? '').toString(),
        occurredOn: _asDate(j['occurredOn']),
        details: j['details'] as String?,
        quantityOrOutcome: j['quantityOrOutcome'] as String?,
        requiresAttention: _asBool(j['requiresAttention']),
        recordedByName: j['recordedByName'] as String?,
      );
}

class DocumentAttachment {
  final int id;
  final String fileName;
  final String fileUrl;
  final String? contentType;
  final DateTime? createdAt;

  DocumentAttachment({required this.id, required this.fileName, required this.fileUrl, this.contentType, this.createdAt});

  factory DocumentAttachment.fromJson(Map<String, dynamic> j) => DocumentAttachment(
        id: _asInt(j['id']),
        fileName: (j['fileName'] ?? '').toString(),
        fileUrl: (j['fileUrl'] ?? '').toString(),
        contentType: j['contentType'] as String?,
        createdAt: _asDate(j['createdAt']),
      );
}

class DeploymentInfo {
  final int id;
  final String? location;
  final String? companyDeployedTo;
  final int? customerId;
  final String? customerName;
  final int? customerSiteId;
  final String? customerSiteName;
  final String? customerSiteAddress;
  final DateTime? startDate;
  final DateTime? endDate;

  DeploymentInfo({
    required this.id,
    this.location,
    this.companyDeployedTo,
    this.customerId,
    this.customerName,
    this.customerSiteId,
    this.customerSiteName,
    this.customerSiteAddress,
    this.startDate,
    this.endDate,
  });

  factory DeploymentInfo.fromJson(Map<String, dynamic> j) => DeploymentInfo(
        id: _asInt(j['id']),
        location: j['location'] as String?,
        companyDeployedTo: j['companyDeployedTo'] as String?,
        customerId: _asIntN(j['customerId']),
        customerName: j['customerName'] as String?,
        customerSiteId: _asIntN(j['customerSiteId']),
        customerSiteName: j['customerSiteName'] as String?,
        customerSiteAddress: j['customerSiteAddress'] as String?,
        startDate: _asDate(j['startDate']),
        endDate: _asDate(j['endDate']),
      );
}

class DogCertification {
  final int id;
  final int dogId;
  final String certificationName;
  final String? issuingBody;
  final DateTime? issueDate;
  final DateTime? expiryDate;
  final String? trainerName;
  final String? notes;

  DogCertification({
    required this.id,
    required this.dogId,
    required this.certificationName,
    this.issuingBody,
    this.issueDate,
    this.expiryDate,
    this.trainerName,
    this.notes,
  });

  bool get isExpired => expiryDate != null && expiryDate!.isBefore(DateTime.now());

  factory DogCertification.fromJson(Map<String, dynamic> j) => DogCertification(
        id: _asInt(j['id']),
        dogId: _asInt(j['dogId']),
        certificationName: (j['certificationName'] ?? '').toString(),
        issuingBody: j['issuingBody'] as String?,
        issueDate: _asDate(j['issueDate']),
        expiryDate: _asDate(j['expiryDate']),
        trainerName: j['trainerName'] as String?,
        notes: j['notes'] as String?,
      );
}

class DogProfile {
  final Animal dog;
  final List<MatingRecord> matingRecords;
  final List<Vaccination> vaccinations;
  final List<CareRecord> careRecords;
  final DeploymentInfo? currentDeployment;

  DogProfile({
    required this.dog,
    required this.matingRecords,
    required this.vaccinations,
    this.careRecords = const [],
    this.currentDeployment,
  });

  factory DogProfile.fromJson(Map<String, dynamic> j) => DogProfile(
        dog: Animal.fromJson((j['dog'] as Map).cast<String, dynamic>()),
        matingRecords: ((j['matingRecords'] as List?) ?? const [])
            .map((e) => MatingRecord.fromJson((e as Map).cast<String, dynamic>()))
            .toList(),
        vaccinations: ((j['vaccinations'] as List?) ?? const [])
            .map((e) => Vaccination.fromJson((e as Map).cast<String, dynamic>()))
            .toList(),
        careRecords: ((j['careRecords'] as List?) ?? const [])
            .map((e) => CareRecord.fromJson((e as Map).cast<String, dynamic>()))
            .toList(),
        currentDeployment: j['currentDeployment'] == null
            ? null
            : DeploymentInfo.fromJson((j['currentDeployment'] as Map).cast<String, dynamic>()),
      );
}

/// A dog whose next vaccination is due soon or overdue.
class VaccineDue {
  final int dogId;
  final String dogName;
  final String vaccineName;
  final DateTime? nextDue;
  final bool isOverdue;
  final int daysUntilDue;

  VaccineDue({
    required this.dogId,
    required this.dogName,
    required this.vaccineName,
    this.nextDue,
    required this.isOverdue,
    required this.daysUntilDue,
  });

  factory VaccineDue.fromJson(Map<String, dynamic> j) => VaccineDue(
        dogId: _asInt(j['dogId']),
        dogName: (j['dogName'] ?? '').toString(),
        vaccineName: (j['vaccineName'] ?? 'Vaccination').toString(),
        nextDue: _asDate(j['nextDue']),
        isOverdue: _asBool(j['isOverdue']),
        daysUntilDue: _asInt(j['daysUntilDue']),
      );
}

class HandlerActivity {
  final int id;
  final int dogId;
  final String dogName;
  final String? dogPhotoUrl;
  final String eventType;
  final DateTime occurredOn;
  final String? details;
  final String? quantityOrOutcome;
  final bool requiresAttention;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final bool canEdit;

  HandlerActivity({
    required this.id,
    required this.dogId,
    required this.dogName,
    this.dogPhotoUrl,
    required this.eventType,
    required this.occurredOn,
    this.details,
    this.quantityOrOutcome,
    required this.requiresAttention,
    required this.createdAt,
    this.updatedAt,
    required this.canEdit,
  });

  factory HandlerActivity.fromJson(Map<String, dynamic> j) => HandlerActivity(
        id: _asInt(j['id']),
        dogId: _asInt(j['dogId']),
        dogName: (j['dogName'] ?? '').toString(),
        dogPhotoUrl: j['dogPhotoUrl'] as String?,
        eventType: (j['eventType'] ?? '').toString(),
        occurredOn: _asDate(j['occurredOn']) ?? DateTime.now(),
        details: j['details'] as String?,
        quantityOrOutcome: j['quantityOrOutcome'] as String?,
        requiresAttention: _asBool(j['requiresAttention']),
        createdAt: _asDate(j['createdAt']) ?? DateTime.now(),
        updatedAt: _asDate(j['updatedAt']),
        canEdit: _asBool(j['canEdit']),
      );
}
