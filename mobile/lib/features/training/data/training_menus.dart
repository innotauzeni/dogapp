import 'package:flutter/material.dart';

import '../../../core/utils/formatters.dart';

String _s(dynamic v) => v?.toString() ?? '';
String _d(dynamic v) {
  final t = DateTime.tryParse(_s(v));
  return t == null ? '' : Fmt.date(t);
}

String _m(dynamic v) => Fmt.money(v is num ? v : num.tryParse(_s(v)) ?? 0);

/// One row on a Dog Training list: what to show in the list and in the detail sheet.
class TRow {
  final String title;
  final String subtitle;
  final String trailing;
  final String badge;
  final List<MapEntry<String, String>> details;
  TRow({required this.title, this.subtitle = '', this.trailing = '', this.badge = '', this.details = const []});
}

/// A Dog Training menu: the web portal has the same set, each guarded by the same permission.
class TrainingMenu {
  final String title;
  final String subtitle;
  final String permission;
  final String path;
  final IconData icon;
  final Color color;
  final TRow Function(Map<String, dynamic> j) row;
  final String empty;

  /// Offers "New report" on this list (weekly progress reports).
  final String? createPermission;

  const TrainingMenu({
    required this.title,
    required this.subtitle,
    required this.permission,
    required this.path,
    required this.icon,
    required this.color,
    required this.row,
    required this.empty,
    this.createPermission,
  });
}

List<MapEntry<String, String>> _kv(Map<String, String?> m) =>
    [for (final e in m.entries) if ((e.value ?? '').trim().isNotEmpty) MapEntry(e.key, e.value!.trim())];

final List<TrainingMenu> trainingMenus = [
  TrainingMenu(
    title: 'Weekly Progress Reports',
    subtitle: 'Handler reports and their review',
    permission: 'training.progress.view',
    path: 'Training/progress-reports',
    icon: Icons.fact_check_rounded,
    color: const Color(0xFF0891B2),
    createPermission: 'training.progress.submit',
    empty: 'No progress reports yet.',
    row: (j) => TRow(
      title: '${_s(j['dogName'])} — Week ${_s(j['weekNumber'])}',
      subtitle: _s(j['handlerName']),
      trailing: j['averageRating'] == null ? '' : '${_s(j['averageRating'])}/10',
      badge: _s(j['status']),
      details: _kv({
        'Dog': _s(j['dogName']),
        'Customer': _s(j['customerName']),
        'Week': _s(j['weekNumber']),
        'Handler': _s(j['handlerName']),
        'Status': _s(j['status']),
        'Days logged': _s(j['logsInWeek']),
        'Average rating': j['averageRating'] == null ? '' : '${_s(j['averageRating'])}/10',
        'Objectives': _s(j['objectives']),
        'Completed': _s(j['activitiesCompleted']),
        'Outstanding': _s(j['activitiesOutstanding']),
        'Performance': _s(j['performanceSummary']),
        'Challenges': _s(j['challenges']),
        'Improvements': _s(j['improvements']),
        'Recommendations': _s(j['recommendations']),
        'Submitted': _d(j['submittedOn']),
        'Reviewed by': _s(j['reviewedByName']),
        'Review comments': _s(j['reviewComments']),
      }),
    ),
  ),
  TrainingMenu(
    title: 'Final Assessments',
    subtitle: 'Pass / fail results and approvals',
    permission: 'training.assessment.view',
    path: 'Training/final-assessments',
    icon: Icons.verified_rounded,
    color: const Color(0xFF7C3AED),
    empty: 'No final assessments yet.',
    row: (j) => TRow(
      title: _s(j['dogName']),
      subtitle: 'Attempt ${_s(j['attemptNumber'])} · ${_s(j['assessorName'])}',
      trailing: j['overallScore'] == null ? '' : '${_s(j['overallScore'])}',
      badge: _s(j['result']),
      details: _kv({
        'Dog': _s(j['dogName']),
        'Customer': _s(j['customerName']),
        'Enrolment': _s(j['enrollmentNumber']),
        'Attempt': _s(j['attemptNumber']),
        'Date': _d(j['assessmentDate']),
        'Assessor': _s(j['assessorName']),
        'Overall score': _s(j['overallScore']),
        'Result': _s(j['result']),
        'Approval': _s(j['approvalStatus']),
        'Approved by': _s(j['approvedByName']),
        'Approval comments': _s(j['approvalComments']),
        'Comments': _s(j['comments']),
        'Extra days': (j['extensionDays'] ?? 0) == 0 ? '' : _s(j['extensionDays']),
        'Certificate': _s(j['certificateNumber']),
      }),
    ),
  ),
  TrainingMenu(
    title: 'Health Assessments',
    subtitle: 'Pre-training health checks',
    permission: 'training.health.view',
    path: 'Training/health-assessments',
    icon: Icons.health_and_safety_rounded,
    color: const Color(0xFFDC2626),
    empty: 'No health assessments yet.',
    row: (j) => TRow(
      title: _s(j['dogName']),
      subtitle: [_s(j['customerName']), _d(j['assessmentDate'])].where((e) => e.isNotEmpty).join(' · '),
      badge: _s(j['result']),
      details: _kv({
        'Dog': '${_s(j['dogName'])} ${_s(j['dogBreed']).isEmpty ? '' : '(${_s(j['dogBreed'])})'}',
        'Customer': _s(j['customerName']),
        'Date': _d(j['assessmentDate']),
        'Examiner': _s(j['examinerName']),
        'Weight': j['weightKg'] == null ? '' : '${_s(j['weightKg'])} kg',
        'Condition': _s(j['physicalCondition']),
        'Vaccinations': _s(j['vaccinationStatus']),
        'Deworming': _s(j['dewormingStatus']),
        'Medical conditions': _s(j['medicalConditions']),
        'Recommendation': _s(j['recommendation']),
        'Result': _s(j['result']),
        'Comments': _s(j['comments']),
      }),
    ),
  ),
  TrainingMenu(
    title: 'Certificates',
    subtitle: 'Issued training certificates',
    permission: 'training.certificate.view',
    path: 'Training/certificates',
    icon: Icons.workspace_premium_rounded,
    color: const Color(0xFFCA8A04),
    empty: 'No certificates issued yet.',
    row: (j) => TRow(
      title: _s(j['certificateNumber']),
      subtitle: '${_s(j['dogName'])} · ${_s(j['ownerName'])}',
      trailing: _d(j['issuedOn']),
      badge: j['isRevoked'] == true ? 'Revoked' : 'Valid',
      details: _kv({
        'Certificate': _s(j['certificateNumber']),
        'Dog': '${_s(j['dogName'])} ${_s(j['breed']).isEmpty ? '' : '(${_s(j['breed'])})'}',
        'Owner': _s(j['ownerName']),
        'Programme': _s(j['programmeName']),
        'Handler': _s(j['handlerName']),
        'Started': _d(j['startDate']),
        'Completed': _d(j['completionDate']),
        'Issued': _d(j['issuedOn']),
        'Score': _s(j['overallScore']),
        'Revoked': j['isRevoked'] == true ? _s(j['revokedReason']) : '',
      }),
    ),
  ),
  TrainingMenu(
    title: 'Handlers & Allocation',
    subtitle: 'Who is training which dogs',
    permission: 'training.handler.view',
    path: 'Training/handlers',
    icon: Icons.groups_rounded,
    color: const Color(0xFF0D9488),
    empty: 'No handlers found.',
    row: (j) => TRow(
      title: _s(j['name']),
      subtitle: '${_s(j['currentDogs'])}/${_s(j['maxDogs'])} dogs${_s(j['specialisation']).isEmpty ? '' : ' · ${_s(j['specialisation'])}'}',
      badge: j['isAvailable'] == true ? 'Available' : 'Unavailable',
      details: [
        ..._kv({
          'Name': _s(j['name']),
          'Email': _s(j['email']),
          'Phone': _s(j['phone']),
          'Specialisation': _s(j['specialisation']),
          'Workload': '${_s(j['currentDogs'])} of ${_s(j['maxDogs'])} dogs',
          'Notes': _s(j['notes']),
        }),
        for (final d in ((j['dogs'] as List?) ?? const []).whereType<Map>())
          MapEntry('Dog', '${_s(d['dogName'])} · week ${_s(d['currentWeek'])} · ${_s(d['status'])}'),
      ],
    ),
  ),
  TrainingMenu(
    title: 'Training Customers',
    subtitle: 'People who bring dogs for training',
    permission: 'training.customer.view',
    path: 'Training/customers',
    icon: Icons.people_alt_rounded,
    color: const Color(0xFF4F46E5),
    empty: 'No training customers yet.',
    row: (j) => TRow(
      title: _s(j['fullName']),
      subtitle: [_s(j['phone']), _s(j['email'])].where((e) => e.isNotEmpty).join(' · '),
      trailing: '${_s(j['dogCount'])} dog(s)',
      details: _kv({
        'Name': _s(j['fullName']),
        'Phone': _s(j['phone']),
        'Email': _s(j['email']),
        'Address': _s(j['address']),
        'ID number': _s(j['idNumber']),
        'Dogs': _s(j['dogCount']),
        'Enrolments': _s(j['enrolmentCount']),
        'Notes': _s(j['notes']),
      }),
    ),
  ),
  TrainingMenu(
    title: 'Training Dogs',
    subtitle: 'Customers\' dogs sent for training',
    permission: 'training.dog.view',
    path: 'Training/dogs',
    icon: Icons.pets_rounded,
    color: const Color(0xFF78350F),
    empty: 'No training dogs yet.',
    row: (j) => TRow(
      title: _s(j['name']),
      subtitle: [_s(j['breed']), _s(j['customerName'])].where((e) => e.isNotEmpty).join(' · '),
      trailing: '${_s(j['enrolmentCount'])} enrolment(s)',
      details: _kv({
        'Name': _s(j['name']),
        'Owner': _s(j['customerName']),
        'Breed': _s(j['breed']),
        'Colour': _s(j['color']),
        'Gender': _s(j['gender']),
        'Born': _d(j['dateOfBirth']),
        'Microchip': _s(j['microchipNumber']),
        'Notes': _s(j['notes']),
      }),
    ),
  ),
  TrainingMenu(
    title: 'Training Programmes',
    subtitle: 'Programmes and fees',
    permission: 'training.programme.view',
    path: 'Training/programmes',
    icon: Icons.calendar_month_rounded,
    color: const Color(0xFF65A30D),
    empty: 'No programmes yet.',
    row: (j) => TRow(
      title: _s(j['name']),
      subtitle: '${_s(j['durationDays'])} days · ${_m(j['fee'])}',
      trailing: '${_s(j['enrolmentCount'])} enrolled',
      badge: j['isActive'] == true ? 'Active' : 'Inactive',
      details: [
        ..._kv({
          'Programme': _s(j['name']),
          'Description': _s(j['description']),
          'Duration': '${_s(j['durationDays'])} days',
          'Fee': _m(j['fee']),
          'Enrolments': _s(j['enrolmentCount']),
        }),
        for (final w in ((j['weeks'] as List?) ?? const []).whereType<Map>())
          MapEntry('Week ${_s(w['weekNumber'])}', '${_s(w['title'])}${_s(w['focus']).isEmpty ? '' : '\n${_s(w['focus'])}'}'),
      ],
    ),
  ),
];
