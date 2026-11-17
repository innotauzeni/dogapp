import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/system_logs_repository.dart';
import '../models/system_log_models.dart';
import 'audit_trail_screen.dart';

/// System error log monitor — mirrors the Portal's System Logs page: view/filter
/// errors, resolve or delete them, and manage the trash (restore or permanently
/// remove). Resolve/delete actions are gated on systemworker.manage.
class SystemLogsScreen extends StatefulWidget {
  const SystemLogsScreen({super.key});

  @override
  State<SystemLogsScreen> createState() => _SystemLogsScreenState();
}

class _SystemLogsScreenState extends State<SystemLogsScreen> {
  static const _severities = ['Error', 'Warning', 'Info', 'Debug'];

  final _repo = SystemLogsRepository();
  Future<ErrorLogPage>? _future;
  bool _showDeleted = false;
  String? _severity;
  bool? _resolved;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    _future = _repo.getErrors(severity: _severity, resolved: _resolved, deleted: _showDeleted, pageSize: 100);
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  void _toast(String msg, {bool error = false}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(msg),
      backgroundColor: error ? AppColors.danger : AppColors.success,
    ));
  }

  Future<void> _runAction(Future<void> Function() action, {String? successMsg}) async {
    setState(() => _busy = true);
    try {
      await action();
      if (successMsg != null) _toast(successMsg);
      await _refresh();
    } catch (e) {
      _toast('Failed: $e', error: true);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<bool> _confirm({required String title, required String message, bool danger = false}) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: TextButton.styleFrom(foregroundColor: danger ? AppColors.danger : AppColors.success),
            child: Text(danger ? 'Delete' : 'Confirm'),
          ),
        ],
      ),
    );
    return ok == true;
  }

  Future<void> _resolveWithNotes(ErrorLogItem item) async {
    final controller = TextEditingController();
    final resolution = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Mark as Resolved'),
        content: TextField(
          controller: controller,
          maxLines: 3,
          autofocus: true,
          decoration: const InputDecoration(hintText: 'Describe how this was resolved...', border: OutlineInputBorder()),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, controller.text.trim()),
            child: const Text('Mark Resolved'),
          ),
        ],
      ),
    );
    if (resolution == null || resolution.isEmpty) return;
    if (!mounted) return;
    Navigator.of(context).maybePop();
    await _runAction(() => _repo.resolve(item.id, resolution), successMsg: 'Marked as resolved.');
  }

  Future<void> _delete(ErrorLogItem item) async {
    if (!await _confirm(title: 'Delete Log', message: "It'll move to Trash — you can restore it later.", danger: true)) return;
    if (mounted) Navigator.of(context).maybePop();
    await _runAction(() => _repo.delete(item.id), successMsg: 'Moved to trash.');
  }

  Future<void> _restore(ErrorLogItem item) async {
    if (mounted) Navigator.of(context).maybePop();
    await _runAction(() => _repo.restore(item.id), successMsg: 'Restored.');
  }

  Future<void> _permanentDelete(ErrorLogItem item) async {
    if (!await _confirm(title: 'Delete Permanently', message: 'This cannot be undone.', danger: true)) return;
    if (mounted) Navigator.of(context).maybePop();
    await _runAction(() => _repo.permanentDelete(item.id), successMsg: 'Permanently deleted.');
  }

  Future<void> _resolveAll() async {
    if (!await _confirm(title: 'Mark All Resolved', message: 'Mark every unresolved log (matching the current filter) as resolved?')) return;
    await _runAction(() => _repo.resolveAll(severity: _severity), successMsg: 'All resolved.');
  }

  Future<void> _deleteAll() async {
    if (!await _confirm(title: 'Delete All', message: "Logs matching the current filter will move to Trash.", danger: true)) return;
    await _runAction(() => _repo.deleteAll(severity: _severity, resolved: _resolved), successMsg: 'Moved to trash.');
  }

  Future<void> _emptyTrash() async {
    if (!await _confirm(title: 'Empty Trash', message: 'Permanently delete everything in the trash? This cannot be undone.', danger: true)) return;
    await _runAction(_repo.emptyTrash, successMsg: 'Trash emptied.');
  }

  void _openDetails(ErrorLogItem item, bool canManage) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _LogDetailSheet(
        item: item,
        showDeleted: _showDeleted,
        canManage: canManage,
        onResolve: () => _resolveWithNotes(item),
        onDelete: () => _delete(item),
        onRestore: () => _restore(item),
        onPermanentDelete: () => _permanentDelete(item),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final canManage = auth.isSystemOwner && auth.can('systemworker.manage');
    final canAudit = auth.isSystemOwner && auth.can('systemworker.audit');

    return Scaffold(
      appBar: AppBar(
        title: Text(_showDeleted ? 'Trash' : 'System Logs'),
        actions: [
          if (canAudit)
            IconButton(
              icon: const Icon(Icons.history_rounded),
              tooltip: 'Audit Trail',
              onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const AuditTrailScreen())),
            ),
          IconButton(
            icon: Icon(_showDeleted ? Icons.list_alt_rounded : Icons.delete_outline_rounded),
            tooltip: _showDeleted ? 'Back to Logs' : 'Trash',
            onPressed: () => setState(() {
              _showDeleted = !_showDeleted;
              _resolved = null;
              _load();
            }),
          ),
        ],
      ),
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: AsyncView<ErrorLogPage>(
            future: _future,
            onRetry: _refresh,
            builder: (context, pageData) {
              final items = pageData.data;
              final unresolvedOnPage = items.where((e) => !e.isResolved).length;
              return ListView(
                padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 32.h),
                physics: const AlwaysScrollableScrollPhysics(),
                children: [
                  _FilterBar(
                    severity: _severity,
                    resolved: _resolved,
                    showDeleted: _showDeleted,
                    severities: _severities,
                    onSeverityChanged: (v) => setState(() {
                      _severity = v;
                      _load();
                    }),
                    onResolvedChanged: (v) => setState(() {
                      _resolved = v;
                      _load();
                    }),
                  ),
                  SizedBox(height: 12.h),
                  Row(
                    children: [
                      Expanded(
                        child: _CountChip(
                          icon: _showDeleted ? Icons.delete_outline_rounded : Icons.list_alt_rounded,
                          label: _showDeleted ? 'In Trash' : 'Total',
                          value: pageData.total,
                          color: _showDeleted ? AppColors.textMuted : AppColors.primary,
                        ),
                      ),
                      if (!_showDeleted) ...[
                        SizedBox(width: 8.w),
                        Expanded(
                          child: _CountChip(icon: Icons.delete_sweep_outlined, label: 'Trash', value: pageData.totalDeleted, color: AppColors.textMuted),
                        ),
                      ],
                    ],
                  ),
                  if (canManage) ...[
                    SizedBox(height: 12.h),
                    Row(
                      children: [
                        if (_showDeleted) ...[
                          if (pageData.totalDeleted > 0)
                            Expanded(
                              child: OutlinedButton.icon(
                                onPressed: _busy ? null : _emptyTrash,
                                icon: const Icon(Icons.delete_forever_rounded, size: 16, color: AppColors.danger),
                                label: const Text('Empty Trash', style: TextStyle(color: AppColors.danger)),
                              ),
                            ),
                        ] else ...[
                          if (unresolvedOnPage > 0)
                            Expanded(
                              child: OutlinedButton.icon(
                                onPressed: _busy ? null : _resolveAll,
                                icon: const Icon(Icons.check_circle_outline_rounded, size: 16, color: AppColors.success),
                                label: const Text('Resolve All', style: TextStyle(color: AppColors.success)),
                              ),
                            ),
                          if (items.isNotEmpty) ...[
                            if (unresolvedOnPage > 0) SizedBox(width: 8.w),
                            Expanded(
                              child: OutlinedButton.icon(
                                onPressed: _busy ? null : _deleteAll,
                                icon: const Icon(Icons.delete_outline_rounded, size: 16, color: AppColors.danger),
                                label: const Text('Delete All', style: TextStyle(color: AppColors.danger)),
                              ),
                            ),
                          ],
                        ],
                      ],
                    ),
                  ],
                  SizedBox(height: 14.h),
                  if (items.isEmpty)
                    Padding(
                      padding: EdgeInsets.only(top: 40.h),
                      child: StatusView.empty(
                        icon: _showDeleted ? Icons.delete_outline_rounded : Icons.fact_check_outlined,
                        title: _showDeleted ? 'Trash is empty' : 'No logs found',
                        message: _showDeleted ? 'Deleted logs will show up here.' : 'Everything looks clean.',
                      ),
                    )
                  else
                    for (final item in items) ...[
                      _LogCard(item: item, showDeleted: _showDeleted, onTap: () => _openDetails(item, canManage)),
                      SizedBox(height: 10.h),
                    ],
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}

class _FilterBar extends StatelessWidget {
  final String? severity;
  final bool? resolved;
  final bool showDeleted;
  final List<String> severities;
  final ValueChanged<String?> onSeverityChanged;
  final ValueChanged<bool?> onResolvedChanged;

  const _FilterBar({
    required this.severity,
    required this.resolved,
    required this.showDeleted,
    required this.severities,
    required this.onSeverityChanged,
    required this.onResolvedChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: DropdownButtonFormField<String?>(
            // ignore: deprecated_member_use
            value: severity,
            isExpanded: true,
            decoration: InputDecoration(
              labelText: 'Severity',
              isDense: true,
              contentPadding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 8.h),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(9.r)),
            ),
            items: [
              const DropdownMenuItem(value: null, child: Text('All')),
              for (final s in severities) DropdownMenuItem(value: s, child: Text(s)),
            ],
            onChanged: onSeverityChanged,
          ),
        ),
        if (!showDeleted) ...[
          SizedBox(width: 10.w),
          Expanded(
            child: DropdownButtonFormField<bool?>(
              // ignore: deprecated_member_use
              value: resolved,
              isExpanded: true,
              decoration: InputDecoration(
                labelText: 'Status',
                isDense: true,
                contentPadding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 8.h),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(9.r)),
              ),
              items: const [
                DropdownMenuItem(value: null, child: Text('All')),
                DropdownMenuItem(value: true, child: Text('Resolved')),
                DropdownMenuItem(value: false, child: Text('Unresolved')),
              ],
              onChanged: onResolvedChanged,
            ),
          ),
        ],
      ],
    );
  }
}

class _CountChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final int value;
  final Color color;
  const _CountChip({required this.icon, required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 10.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .08),
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: color.withValues(alpha: .18)),
      ),
      child: Row(
        children: [
          Icon(icon, size: 16.r, color: color),
          SizedBox(width: 8.w),
          Text('$value', style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800, color: color)),
          SizedBox(width: 5.w),
          Expanded(child: Text(label, style: TextStyle(fontSize: 11.sp, color: color, fontWeight: FontWeight.w600), overflow: TextOverflow.ellipsis)),
        ],
      ),
    );
  }
}

(IconData, Color) _severityStyle(String? severity) {
  switch ((severity ?? '').toLowerCase()) {
    case 'error':
      return (Icons.cancel_rounded, AppColors.danger);
    case 'warning':
      return (Icons.warning_rounded, AppColors.warning);
    case 'info':
      return (Icons.info_rounded, AppColors.info);
    default:
      return (Icons.bug_report_rounded, AppColors.textMuted);
  }
}

class _LogCard extends StatelessWidget {
  final ErrorLogItem item;
  final bool showDeleted;
  final VoidCallback onTap;
  const _LogCard({required this.item, required this.showDeleted, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final (icon, color) = _severityStyle(item.severity);
    return AppCard(
      onTap: onTap,
      padding: EdgeInsets.all(14.w),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 34.r,
            height: 34.r,
            decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(10.r)),
            child: Icon(icon, color: color, size: 17.r),
          ),
          SizedBox(width: 10.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.message ?? 'No message',
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary),
                ),
                if ((item.source ?? '').isNotEmpty) ...[
                  SizedBox(height: 3.h),
                  Text(item.source!, maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                ],
                SizedBox(height: 8.h),
                Wrap(
                  spacing: 6.w,
                  runSpacing: 6.h,
                  children: [
                    _Badge(text: item.severity ?? 'Unknown', color: color),
                    if (showDeleted)
                      _Badge(text: 'Deleted ${Fmt.dateTime(item.deletedAt)}', color: AppColors.textMuted)
                    else
                      _Badge(
                        text: item.isResolved ? 'Resolved' : 'Unresolved',
                        color: item.isResolved ? AppColors.success : AppColors.danger,
                      ),
                    _Badge(text: Fmt.dateTime(item.createdAt), color: AppColors.textMuted, subtle: true),
                  ],
                ),
              ],
            ),
          ),
          Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 20.r),
        ],
      ),
    );
  }
}

class _Badge extends StatelessWidget {
  final String text;
  final Color color;
  final bool subtle;
  const _Badge({required this.text, required this.color, this.subtle = false});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: subtle ? AppColors.surfaceAlt : color.withValues(alpha: .12),
        borderRadius: BorderRadius.circular(20.r),
      ),
      child: Text(text, style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w700, color: subtle ? AppColors.textMuted : color)),
    );
  }
}

class _LogDetailSheet extends StatelessWidget {
  final ErrorLogItem item;
  final bool showDeleted;
  final bool canManage;
  final VoidCallback onResolve;
  final VoidCallback onDelete;
  final VoidCallback onRestore;
  final VoidCallback onPermanentDelete;

  const _LogDetailSheet({
    required this.item,
    required this.showDeleted,
    required this.canManage,
    required this.onResolve,
    required this.onDelete,
    required this.onRestore,
    required this.onPermanentDelete,
  });

  @override
  Widget build(BuildContext context) {
    final (icon, color) = _severityStyle(item.severity);
    return DraggableScrollableSheet(
      initialChildSize: 0.75,
      maxChildSize: 0.92,
      minChildSize: 0.4,
      expand: false,
      builder: (context, scrollController) => Container(
        decoration: BoxDecoration(
          color: Theme.of(context).scaffoldBackgroundColor,
          borderRadius: BorderRadius.vertical(top: Radius.circular(24.r)),
        ),
        child: ListView(
          controller: scrollController,
          padding: EdgeInsets.fromLTRB(20.w, 12.h, 20.w, 28.h),
          children: [
            Center(
              child: Container(width: 40.w, height: 4.h, decoration: BoxDecoration(color: AppColors.border, borderRadius: BorderRadius.circular(4.r))),
            ),
            SizedBox(height: 18.h),
            Row(
              children: [
                Container(
                  width: 40.r,
                  height: 40.r,
                  decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(12.r)),
                  child: Icon(icon, color: color, size: 20.r),
                ),
                SizedBox(width: 12.w),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(item.severity ?? 'Unknown', style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                      if ((item.exceptionType ?? '').isNotEmpty)
                        Text(item.exceptionType!, style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                    ],
                  ),
                ),
              ],
            ),
            SizedBox(height: 16.h),
            _sectionLabel('Message'),
            Container(
              width: double.infinity,
              padding: EdgeInsets.all(12.w),
              decoration: BoxDecoration(color: AppColors.dangerLight, borderRadius: BorderRadius.circular(10.r)),
              child: Text(item.message ?? '—', style: TextStyle(fontSize: 13.sp, color: AppColors.textPrimary)),
            ),
            if ((item.source ?? '').isNotEmpty || (item.path ?? '').isNotEmpty) ...[
              SizedBox(height: 14.h),
              _sectionLabel('Location'),
              if ((item.source ?? '').isNotEmpty) Text(item.source!, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
              if ((item.path ?? '').isNotEmpty) Text(item.path!, style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted)),
            ],
            SizedBox(height: 14.h),
            _sectionLabel('Timeline'),
            _infoRow('Created', Fmt.dateTime(item.createdAt)),
            if (item.isResolved) _infoRow('Resolved', Fmt.dateTime(item.resolvedAt)),
            if ((item.resolution ?? '').isNotEmpty) _infoRow('Resolution', item.resolution!),
            if (showDeleted) _infoRow('Deleted', Fmt.dateTime(item.deletedAt)),
            if ((item.stackTrace ?? '').isNotEmpty) ...[
              SizedBox(height: 14.h),
              _sectionLabel('Stack Trace'),
              Container(
                width: double.infinity,
                padding: EdgeInsets.all(12.w),
                decoration: BoxDecoration(color: const Color(0xFF0F172A), borderRadius: BorderRadius.circular(10.r)),
                child: Text(
                  item.stackTrace!,
                  style: TextStyle(fontSize: 10.5.sp, color: const Color(0xFFE2E8F0), fontFamily: 'monospace', height: 1.5),
                ),
              ),
            ],
            if (canManage) ...[
              SizedBox(height: 22.h),
              if (showDeleted)
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: onRestore,
                        icon: const Icon(Icons.restore_rounded, size: 16),
                        label: const Text('Restore'),
                      ),
                    ),
                    SizedBox(width: 10.w),
                    Expanded(
                      child: FilledButton.icon(
                        onPressed: onPermanentDelete,
                        style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
                        icon: const Icon(Icons.delete_forever_rounded, size: 16),
                        label: const Text('Delete'),
                      ),
                    ),
                  ],
                )
              else
                Row(
                  children: [
                    if (!item.isResolved)
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: onResolve,
                          icon: const Icon(Icons.check_circle_outline_rounded, size: 16, color: AppColors.success),
                          label: const Text('Resolve', style: TextStyle(color: AppColors.success)),
                        ),
                      ),
                    if (!item.isResolved) SizedBox(width: 10.w),
                    Expanded(
                      child: FilledButton.icon(
                        onPressed: onDelete,
                        style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
                        icon: const Icon(Icons.delete_outline_rounded, size: 16),
                        label: const Text('Delete'),
                      ),
                    ),
                  ],
                ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _sectionLabel(String text) => Padding(
        padding: EdgeInsets.only(bottom: 6.h),
        child: Text(text, style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: AppColors.textMuted, letterSpacing: .4)),
      );

  Widget _infoRow(String label, String value) => Padding(
        padding: EdgeInsets.only(bottom: 4.h),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(width: 80.w, child: Text(label, style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted))),
            Expanded(child: Text(value, style: TextStyle(fontSize: 12.sp, color: AppColors.textPrimary, fontWeight: FontWeight.w600))),
          ],
        ),
      );
}
