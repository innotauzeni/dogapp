import 'dart:convert';

import '../../../core/network/api_client.dart';
import '../models/system_log_models.dart';

class SystemLogsRepository {
  final ApiClient _api;
  SystemLogsRepository([ApiClient? api]) : _api = api ?? ApiClient.instance;

  Future<ErrorLogPage> getErrors({String? severity, bool? resolved, bool deleted = false, int page = 1, int pageSize = 50}) => _api.get(
        'SystemLogs/errors',
        query: {
          if (severity != null && severity.isNotEmpty) 'severity': severity,
          if (resolved != null) 'resolved': resolved,
          if (deleted) 'deleted': true,
          'page': page,
          'pageSize': pageSize,
        },
        parser: (d) => ErrorLogPage.fromJson((d as Map).cast<String, dynamic>()),
      );

  Future<void> resolve(int id, String resolution) => _api.post(
        'SystemLogs/errors/$id/resolve',
        body: jsonEncode(resolution),
        parser: (_) {},
      );

  Future<void> delete(int id) => _api.delete('SystemLogs/errors/$id', parser: (_) {});

  Future<void> resolveAll({String? severity}) => _api.post(
        'SystemLogs/errors/resolve-all',
        query: severity != null && severity.isNotEmpty ? {'severity': severity} : null,
        parser: (_) {},
      );

  Future<void> deleteAll({String? severity, bool? resolved}) => _api.delete(
        'SystemLogs/errors/all',
        query: {
          if (severity != null && severity.isNotEmpty) 'severity': severity,
          if (resolved != null) 'resolved': resolved,
        },
        parser: (_) {},
      );

  Future<void> restore(int id) => _api.post('SystemLogs/errors/$id/restore', body: const {}, parser: (_) {});

  Future<void> permanentDelete(int id) => _api.delete('SystemLogs/errors/$id/permanent', parser: (_) {});

  Future<void> emptyTrash() => _api.delete('SystemLogs/errors/trash', parser: (_) {});

  Future<AuditPage> getAuditEntries({String? entity, String? action, String? username, int page = 1, int pageSize = 50}) => _api.get(
        'SystemLogs/audit',
        query: {
          if (entity != null && entity.isNotEmpty) 'entity': entity,
          if (action != null && action.isNotEmpty) 'action': action,
          if (username != null && username.isNotEmpty) 'username': username,
          'page': page,
          'pageSize': pageSize,
        },
        parser: (d) => AuditPage.fromJson((d as Map).cast<String, dynamic>()),
      );

  Future<AuditItem> getAuditEntry(int id) => _api.get(
        'SystemLogs/audit/$id',
        parser: (d) => AuditItem.fromJson((d as Map).cast<String, dynamic>()),
      );
}
