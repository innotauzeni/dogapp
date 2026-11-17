import 'package:dio/dio.dart';

import '../../../core/network/api_client.dart';
import '../models/animal.dart';

/// All Animal / breeding / vaccination endpoints.
class DogsRepository {
  final ApiClient _api;
  DogsRepository([ApiClient? api]) : _api = api ?? ApiClient.instance;

  List<T> _list<T>(dynamic data, T Function(Map<String, dynamic>) fromJson) =>
      ((data as List?) ?? const [])
          .map((e) => fromJson((e as Map).cast<String, dynamic>()))
          .toList();

  Future<List<Animal>> getPets(int companyId) => _api.get(
        'Animal/GetPets',
        query: {'CompanyId': companyId},
        parser: (d) => _list(d, Animal.fromJson),
      );

  Future<DogProfile> getProfile(int id) => _api.get(
        'Animal/GetDogProfile/$id',
        parser: (d) => DogProfile.fromJson((d as Map).cast<String, dynamic>()),
      );

  Future<List<VaccineDue>> getVaccineDue(int companyId, {int days = 30}) => _api.get(
        'Animal/vaccine-due',
        query: {'companyId': companyId, 'days': days},
        parser: (d) => _list(d, VaccineDue.fromJson),
      );

  /// Dogs assigned to a handler (for the Dog Handler screen).
  Future<List<Animal>> getAssignedDogs(String handlerUserId) => _api.get(
        'Animal/GetAssignedDogs',
        query: {'handlerUserId': handlerUserId},
        parser: (d) => _list(d, Animal.fromJson),
      );

  /// Records a care activity a handler performed on a dog.
  Future<void> addCareRecord(Map<String, dynamic> body) =>
      _api.post('Animal/AddCareRecord', body: body, parser: (_) {});

  /// Handler's activity history — all care records they've recorded.
  Future<List<HandlerActivity>> getHandlerActivities(String handlerUserId) => _api.get(
        'Animal/GetHandlerActivities',
        query: {'userId': handlerUserId},
        parser: (d) => _list(d, HandlerActivity.fromJson),
      );

  Future<void> createPet(Map<String, dynamic> body) =>
      _api.post('Animal/CreatePet', body: body, parser: (_) {});

  Future<void> updatePet(int id, Map<String, dynamic> body) =>
      _api.put('Animal/UpdatePet/$id', body: body, parser: (_) {});

  Future<void> deletePet(int id) =>
      _api.delete('Animal/DeletePet/$id', parser: (_) {});

  Future<void> toggleStatus(int id) =>
      _api.post('Animal/ToggleStatus/$id', parser: (_) {});

  /// Assigns [dogId] to [customerId]; pass null to unassign the current owner.
  Future<void> assignCustomer({required int dogId, int? customerId}) =>
      _api.post('Animal/AssignCustomer',
          body: {'dogId': dogId, 'customerId': customerId}, parser: (_) {});

  /// Deploys a dog to a customer's site/premise (creates a Deployment record).
  Future<void> createDeployment(Map<String, dynamic> body) =>
      _api.post('Deployment/CreateDeployment', body: body, parser: (_) {});

  /// Recalls a deployed dog back to its branch/head office (ends the deployment).
  Future<void> returnDeployment(int deploymentId) =>
      _api.post('Deployment/Deployment/$deploymentId/return', body: const {}, parser: (_) {});

  Future<List<DogCertification>> getCertifications(int dogId) => _api.get(
        'DogCertification',
        query: {'dogId': dogId},
        parser: (d) => _list(d, DogCertification.fromJson),
      );

  Future<void> createCertification(Map<String, dynamic> body) =>
      _api.post('DogCertification', body: body, parser: (_) {});

  Future<void> addVaccination(Map<String, dynamic> body) =>
      _api.post('Animal/AddVaccination', body: body, parser: (_) {});

  Future<List<Vaccination>> getPuppyVaccinations(int puppyId) => _api.get(
        'Animal/GetPuppyVaccinations/$puppyId',
        parser: (d) => (d as List<dynamic>?)?.map((v) => Vaccination.fromJson(v as Map<String, dynamic>)).toList() ?? [],
      );

  /// IDs of dogs that have at least one vaccination record.
  Future<Set<int>> getVaccinatedDogIds(int companyId) => _api.get(
        'Animal/vaccinated',
        query: {'companyId': companyId},
        parser: (d) => ((d as List?) ?? const [])
            .map((e) => _count((e as Map)['dogId']))
            .toSet(),
      );

  /// Uploads a dog photo to the server (which stores it on Cloudflare R2) and
  /// returns the public URL to persist in the dog's `photoUrl`.
  Future<String> uploadPhoto(String filePath) async {
    final ext = filePath.split('.').last.toLowerCase();
    final mime = switch (ext) {
      'png' => DioMediaType('image', 'png'),
      'gif' => DioMediaType('image', 'gif'),
      'webp' => DioMediaType('image', 'webp'),
      _ => DioMediaType('image', 'jpeg'),
    };
    final form = FormData.fromMap({
      'file': await MultipartFile.fromFile(filePath, filename: 'dog.$ext', contentType: mime),
    });
    return _api.post<String>('Animal/UploadPhoto', body: form, parser: (d) => d.toString());
  }

  Future<List<DocumentAttachment>> getDocuments(String entityType, int entityId) => _api.get(
        'Document',
        query: {'entityType': entityType, 'entityId': entityId},
        parser: (d) => _list(d, DocumentAttachment.fromJson),
      );

  /// Uploads a file (photo of a certificate, insurance doc...) and attaches it to
  /// [entityType]/[entityId] (e.g. "Dog"/12) in one call.
  Future<void> uploadDocument({
    required String filePath,
    required int companyId,
    required String entityType,
    required int entityId,
  }) async {
    final ext = filePath.split('.').last.toLowerCase();
    final mime = switch (ext) {
      'png' => DioMediaType('image', 'png'),
      'gif' => DioMediaType('image', 'gif'),
      'webp' => DioMediaType('image', 'webp'),
      _ => DioMediaType('image', 'jpeg'),
    };
    final form = FormData.fromMap({
      'file': await MultipartFile.fromFile(filePath, filename: 'document.$ext', contentType: mime),
      'companyId': companyId,
      'entityType': entityType,
      'entityId': entityId,
    });
    await _api.post('Document/Upload', body: form, parser: (_) {});
  }

  Future<void> deleteDocument(int id) => _api.delete('Document/$id', parser: (_) {});

  // ── Breeding ───────────────────────────────────────────────────────────────

  Future<List<MatingRecord>> getMatingRecords(int companyId) => _api.get(
        'Animal/GetMatingRecords',
        query: {'CompanyId': companyId},
        parser: (d) => _list(d, MatingRecord.fromJson),
      );

  Future<List<Litter>> getLitters(int companyId) => _api.get(
        'Animal/GetLitters',
        query: {'CompanyId': companyId},
        parser: (d) => _list(d, Litter.fromJson),
      );

  Future<void> addMatingRecord(Map<String, dynamic> body) =>
      _api.post('Animal/handler/AddMatingRecord', body: body, parser: (_) {});

  Future<void> addLitter(Map<String, dynamic> body) =>
      _api.post('Animal/handler/AddLitter', body: body, parser: (_) {});

  Future<void> addPuppy(Map<String, dynamic> body) =>
      _api.post('Animal/handler/AddPuppy', body: body, parser: (_) {});

  // ── Dashboard counts ────────────────────────────────────────────────────────

  Future<int> vaccinatedCount(int companyId) => _api.get(
        'Animal/vaccinated-count',
        query: {'companyId': companyId},
        parser: (d) => _count(d),
      );
  Future<int> littersCount(int companyId) => _api.get(
        'Animal/litters-count',
        query: {'companyId': companyId},
        parser: (d) => _count(d),
      );
  Future<int> matingCount(int companyId) => _api.get(
        'Animal/mating-count',
        query: {'companyId': companyId},
        parser: (d) => _count(d),
      );

  int _count(dynamic d) {
    if (d is int) return d;
    if (d is num) return d.toInt();
    if (d is Map && d['count'] != null) return int.tryParse('${d['count']}') ?? 0;
    return int.tryParse('$d') ?? 0;
  }
}
