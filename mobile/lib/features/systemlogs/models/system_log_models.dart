/// Mirrors `ErrorLogItemDto` / `ErrorLogPageDto` (Dog.Api/Controllers/SystemLogsController.cs).
class ErrorLogItem {
  final int id;
  final String? uuid;
  final String? message;
  final String? severity;
  final String? source;
  final String? exceptionType;
  final String? path;
  final String? method;
  final String? ipAddress;
  final String? machineName;
  final String? environment;
  final bool isResolved;
  final String? resolution;
  final DateTime? resolvedAt;
  final DateTime createdAt;
  final String? stackTrace;
  final bool isDeleted;
  final DateTime? deletedAt;

  ErrorLogItem({
    required this.id,
    this.uuid,
    this.message,
    this.severity,
    this.source,
    this.exceptionType,
    this.path,
    this.method,
    this.ipAddress,
    this.machineName,
    this.environment,
    required this.isResolved,
    this.resolution,
    this.resolvedAt,
    required this.createdAt,
    this.stackTrace,
    this.isDeleted = false,
    this.deletedAt,
  });

  factory ErrorLogItem.fromJson(Map<String, dynamic> j) => ErrorLogItem(
        id: (j['id'] is int) ? j['id'] as int : int.tryParse('${j['id']}') ?? 0,
        uuid: j['uuid'] as String?,
        message: j['message'] as String?,
        severity: j['severity'] as String?,
        source: j['source'] as String?,
        exceptionType: j['exceptionType'] as String?,
        path: j['path'] as String?,
        method: j['method'] as String?,
        ipAddress: j['ipAddress'] as String?,
        machineName: j['machineName'] as String?,
        environment: j['environment'] as String?,
        isResolved: j['isResolved'] as bool? ?? false,
        resolution: j['resolution'] as String?,
        resolvedAt: j['resolvedAt'] == null ? null : DateTime.tryParse('${j['resolvedAt']}'),
        createdAt: DateTime.tryParse('${j['createdAt']}') ?? DateTime.now(),
        stackTrace: j['stackTrace'] as String?,
        isDeleted: j['isDeleted'] as bool? ?? false,
        deletedAt: j['deletedAt'] == null ? null : DateTime.tryParse('${j['deletedAt']}'),
      );
}

class ErrorLogPage {
  final int total;
  final int totalDeleted;
  final int page;
  final int pageSize;
  final List<ErrorLogItem> data;

  ErrorLogPage({required this.total, required this.totalDeleted, required this.page, required this.pageSize, required this.data});

  factory ErrorLogPage.fromJson(Map<String, dynamic> j) => ErrorLogPage(
        total: (j['total'] is int) ? j['total'] as int : int.tryParse('${j['total']}') ?? 0,
        totalDeleted: (j['totalDeleted'] is int) ? j['totalDeleted'] as int : int.tryParse('${j['totalDeleted']}') ?? 0,
        page: (j['page'] is int) ? j['page'] as int : int.tryParse('${j['page']}') ?? 1,
        pageSize: (j['pageSize'] is int) ? j['pageSize'] as int : int.tryParse('${j['pageSize']}') ?? 50,
        data: ((j['data'] as List?) ?? const []).map((e) => ErrorLogItem.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );
}

/// Mirrors `AuditItemDto` / `AuditPageDto`.
class AuditItem {
  final int id;
  final String? entityName;
  final String? actionType;
  final String? username;
  final DateTime timeStamp;
  final String? entityId;
  final Map<String, dynamic>? changes;

  AuditItem({
    required this.id,
    this.entityName,
    this.actionType,
    this.username,
    required this.timeStamp,
    this.entityId,
    this.changes,
  });

  factory AuditItem.fromJson(Map<String, dynamic> j) => AuditItem(
        id: (j['id'] is int) ? j['id'] as int : int.tryParse('${j['id']}') ?? 0,
        entityName: j['entityName'] as String?,
        actionType: j['actionType'] as String?,
        username: j['username'] as String?,
        timeStamp: DateTime.tryParse('${j['timeStamp']}') ?? DateTime.now(),
        entityId: j['entityId'] as String?,
        changes: (j['changes'] as Map?)?.cast<String, dynamic>(),
      );
}

class AuditPage {
  final int total;
  final int page;
  final int pageSize;
  final List<AuditItem> data;
  final List<String> entities;

  AuditPage({required this.total, required this.page, required this.pageSize, required this.data, required this.entities});

  factory AuditPage.fromJson(Map<String, dynamic> j) => AuditPage(
        total: (j['total'] is int) ? j['total'] as int : int.tryParse('${j['total']}') ?? 0,
        page: (j['page'] is int) ? j['page'] as int : int.tryParse('${j['page']}') ?? 1,
        pageSize: (j['pageSize'] is int) ? j['pageSize'] as int : int.tryParse('${j['pageSize']}') ?? 50,
        data: ((j['data'] as List?) ?? const []).map((e) => AuditItem.fromJson((e as Map).cast<String, dynamic>())).toList(),
        entities: ((j['entities'] as List?) ?? const []).map((e) => e.toString()).toList(),
      );
}
