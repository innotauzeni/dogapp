import 'package:dio/dio.dart';

import '../../../core/network/api_client.dart';

String _s(dynamic v) => v?.toString() ?? '';
int _i(dynamic v) => v is num ? v.toInt() : int.tryParse(v?.toString() ?? '') ?? 0;

/// A dog the handler is training (or is about to start).
class TrainingDog {
  final int id;
  final String dogName;
  final String breed;
  final String customerName;
  final String status;
  final int currentWeek;
  final int dayNumber;
  final int totalDays;
  final String? photoUrl;

  TrainingDog({
    required this.id,
    required this.dogName,
    required this.breed,
    required this.customerName,
    required this.status,
    required this.currentWeek,
    required this.dayNumber,
    required this.totalDays,
    this.photoUrl,
  });

  bool get isActive => status == 'InTraining' || status == 'AwaitingFinalAssessment';

  factory TrainingDog.fromJson(Map<String, dynamic> j) => TrainingDog(
        id: _i(j['id']),
        dogName: _s(j['dogName']),
        breed: _s(j['dogBreed']),
        customerName: _s(j['customerName']),
        status: _s(j['status']),
        currentWeek: _i(j['currentWeek']),
        dayNumber: _i(j['dayNumber']),
        totalDays: _i(j['durationDays']) + _i(j['extensionDays']),
        photoUrl: j['dogPhotoUrl']?.toString(),
      );
}

/// One time slot of the day (07:00 Morning Walk …) with the handler's result.
class LogSlot {
  String time;
  String title;
  bool done;
  int? rating; // 0-10, as on the weekly plan sheets
  String notes;

  LogSlot({required this.time, required this.title, this.done = false, this.rating, this.notes = ''});

  factory LogSlot.fromJson(Map<String, dynamic> j) => LogSlot(
        time: _s(j['time']),
        title: _s(j['title']),
        done: j['done'] == true,
        rating: j['rating'] == null ? null : _i(j['rating']),
        notes: _s(j['notes']),
      );

  Map<String, dynamic> toJson() => {'time': time, 'title': title, 'done': done, 'rating': done ? rating : null, 'notes': notes};
}

class DailyLog {
  final int id;
  final int enrollmentId;
  final String dogName;
  final int weekNumber;
  final int dayNumber;
  final String activities;
  final String commands;
  final int durationMinutes;
  final List<LogSlot> slots;
  final String? performance;
  final String observations;
  final String challenges;
  final String improvements;
  final String recommendations;
  final String nextObjectives;

  DailyLog({
    required this.id,
    required this.enrollmentId,
    required this.dogName,
    required this.weekNumber,
    required this.dayNumber,
    required this.activities,
    required this.commands,
    required this.durationMinutes,
    required this.slots,
    required this.performance,
    required this.observations,
    required this.challenges,
    required this.improvements,
    required this.recommendations,
    required this.nextObjectives,
  });

  factory DailyLog.fromJson(Map<String, dynamic> j) => DailyLog(
        id: _i(j['id']),
        enrollmentId: _i(j['enrollmentId']),
        dogName: _s(j['dogName']),
        weekNumber: _i(j['weekNumber']),
        dayNumber: _i(j['dayNumber']),
        activities: _s(j['activitiesPerformed']),
        commands: _s(j['commandsPracticed']),
        durationMinutes: _i(j['durationMinutes']),
        slots: ((j['slots'] as List?) ?? const []).map((e) => LogSlot.fromJson((e as Map).cast<String, dynamic>())).toList(),
        // A brand-new (unsaved) log has no performance yet; the form suggests one from the ratings.
        performance: _i(j['id']) == 0 ? null : _s(j['performance']),
        observations: _s(j['behaviourObservations']),
        challenges: _s(j['challenges']),
        improvements: _s(j['improvements']),
        recommendations: _s(j['recommendations']),
        nextObjectives: _s(j['nextObjectives']),
      );
}

class WeekPlan {
  final int weekNumber;
  final String title;
  final String focus;
  final List<String> targets;

  WeekPlan({required this.weekNumber, required this.title, required this.focus, required this.targets});

  factory WeekPlan.fromJson(Map<String, dynamic> j) => WeekPlan(
        weekNumber: _i(j['weekNumber']),
        title: _s(j['title']),
        focus: _s(j['focus']),
        targets: _s(j['targets']).split('\n').where((t) => t.trim().isNotEmpty).toList(),
      );
}

/// Dog Training endpoints used by handlers on their phones.
class TrainingRepository {
  final ApiClient _api;
  TrainingRepository([ApiClient? api]) : _api = api ?? ApiClient.instance;

  Future<List<TrainingDog>> myDogs() => _api.get(
        'Training/my-dogs',
        parser: (d) => ((d as List?) ?? const []).map((e) => TrainingDog.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );

  /// Enrolment ids that already have a record for [date] (used for the "logged today" badge).
  Future<Set<int>> loggedOn(DateTime date) => _api.get(
        'Training/logs',
        query: {'date': _ymd(date)},
        parser: (d) => ((d as List?) ?? const []).map((e) => _i((e as Map)['enrollmentId'])).toSet(),
      );

  Future<DailyLog> logForDay(int enrollmentId, DateTime date) => _api.get(
        'Training/logs/day',
        query: {'enrollmentId': enrollmentId, 'date': _ymd(date)},
        parser: (d) => DailyLog.fromJson((d as Map).cast<String, dynamic>()),
      );

  Future<DailyLog> saveLog(Map<String, dynamic> body) => _api.post(
        'Training/logs',
        body: body,
        parser: (d) => DailyLog.fromJson((d as Map).cast<String, dynamic>()),
      );

  Future<List<WeekPlan>> plans(int enrollmentId) => _api.get(
        'Training/enrollments/$enrollmentId/plans',
        parser: (d) => ((d as List?) ?? const []).map((e) => WeekPlan.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );

  /// Any Dog Training list endpoint (enrolments, invoices, handlers ...) as plain maps.
  Future<List<Map<String, dynamic>>> rawList(String path, {Map<String, dynamic>? query}) => _api.get(
        path,
        query: query,
        parser: (d) => ((d as List?) ?? const []).map((e) => (e as Map).cast<String, dynamic>()).toList(),
      );

  Future<Map<String, dynamic>> rawMap(String path, {Map<String, dynamic>? query}) =>
      _api.get(path, query: query, parser: (d) => ((d as Map?) ?? const {}).cast<String, dynamic>());

  /// The week's report, pre-filled from the daily logs (or what was already saved).
  Future<Map<String, dynamic>> progressDraft(int enrollmentId, int week) =>
      rawMap('Training/progress-reports/draft', query: {'enrollmentId': enrollmentId, 'week': week});

  Future<void> saveProgressReport(Map<String, dynamic> body) => _api.post('Training/progress-reports', body: body, parser: (_) {});

  /// Uploads a photo / video and attaches it to the daily log.
  Future<void> uploadToLog(int logId, String filePath) async {
    final name = filePath.split(RegExp(r'[\\/]')).last;
    final form = FormData.fromMap({
      'entityType': 'TrainingLog',
      'entityId': logId,
      'file': await MultipartFile.fromFile(filePath, filename: name),
    });
    await _api.post<bool>('Training/attachments/upload', body: form, parser: (_) => true);
  }

  static String _ymd(DateTime d) => '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
}
