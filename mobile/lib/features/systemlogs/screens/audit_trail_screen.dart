import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/state_views.dart';
import '../data/system_logs_repository.dart';
import '../models/system_log_models.dart';

/// Who changed what, when — a read-only feed of every INSERT/UPDATE/DELETE
/// recorded by the backend's audit interceptor.
class AuditTrailScreen extends StatefulWidget {
  const AuditTrailScreen({super.key});

  @override
  State<AuditTrailScreen> createState() => _AuditTrailScreenState();
}

class _AuditTrailScreenState extends State<AuditTrailScreen> {
  final _repo = SystemLogsRepository();
  Future<AuditPage>? _future;
  String? _entity;
  String? _action;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    _future = _repo.getAuditEntries(entity: _entity, action: _action, pageSize: 100);
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  void _openDetails(AuditItem entry) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _AuditDetailSheet(repo: _repo, summary: entry),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Audit Trail')),
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: AsyncView<AuditPage>(
            future: _future,
            onRetry: _refresh,
            builder: (context, page) {
              return ListView(
                padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 32.h),
                physics: const AlwaysScrollableScrollPhysics(),
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: DropdownButtonFormField<String?>(
                          // ignore: deprecated_member_use
                          value: _entity,
                          isExpanded: true,
                          decoration: InputDecoration(
                            labelText: 'Entity',
                            isDense: true,
                            contentPadding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 8.h),
                            border: OutlineInputBorder(borderRadius: BorderRadius.circular(9.r)),
                          ),
                          items: [
                            const DropdownMenuItem(value: null, child: Text('All')),
                            for (final e in page.entities) DropdownMenuItem(value: e, child: Text(e, overflow: TextOverflow.ellipsis)),
                          ],
                          onChanged: (v) => setState(() {
                            _entity = v;
                            _load();
                          }),
                        ),
                      ),
                      SizedBox(width: 10.w),
                      Expanded(
                        child: DropdownButtonFormField<String?>(
                          // ignore: deprecated_member_use
                          value: _action,
                          isExpanded: true,
                          decoration: InputDecoration(
                            labelText: 'Action',
                            isDense: true,
                            contentPadding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 8.h),
                            border: OutlineInputBorder(borderRadius: BorderRadius.circular(9.r)),
                          ),
                          items: const [
                            DropdownMenuItem(value: null, child: Text('All')),
                            DropdownMenuItem(value: 'INSERT', child: Text('Insert')),
                            DropdownMenuItem(value: 'UPDATE', child: Text('Update')),
                            DropdownMenuItem(value: 'DELETE', child: Text('Delete')),
                          ],
                          onChanged: (v) => setState(() {
                            _action = v;
                            _load();
                          }),
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 8.h),
                  Text('${page.total} entries', style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted, fontWeight: FontWeight.w600)),
                  SizedBox(height: 12.h),
                  if (page.data.isEmpty)
                    Padding(
                      padding: EdgeInsets.only(top: 40.h),
                      child: StatusView.empty(icon: Icons.history_rounded, title: 'No activity found', message: 'Try a different filter.'),
                    )
                  else
                    for (final entry in page.data) ...[
                      _AuditCard(entry: entry, onTap: () => _openDetails(entry)),
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

(IconData, Color) _actionStyle(String? action) {
  switch ((action ?? '').toUpperCase()) {
    case 'INSERT':
      return (Icons.add_circle_rounded, AppColors.success);
    case 'DELETE':
      return (Icons.remove_circle_rounded, AppColors.danger);
    default:
      return (Icons.edit_rounded, AppColors.info);
  }
}

class _AuditCard extends StatelessWidget {
  final AuditItem entry;
  final VoidCallback onTap;
  const _AuditCard({required this.entry, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final (icon, color) = _actionStyle(entry.actionType);
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
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        '${entry.entityName ?? 'Unknown'}${entry.entityId != null ? ' #${entry.entityId}' : ''}',
                        style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                    SizedBox(width: 6.w),
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
                      decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20.r)),
                      child: Text(entry.actionType ?? '—', style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w700, color: color)),
                    ),
                  ],
                ),
                SizedBox(height: 7.h),
                // Username and timestamp each get their own row (rather than sharing
                // one Row with no Flexible) so a long name/GUID can never push the
                // timestamp off the edge of the screen on a narrow device.
                Row(
                  children: [
                    Icon(Icons.person_outline_rounded, size: 13.r, color: AppColors.textMuted),
                    SizedBox(width: 4.w),
                    Expanded(
                      child: Text(
                        entry.username ?? 'System',
                        style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
                SizedBox(height: 3.h),
                Row(
                  children: [
                    Icon(Icons.schedule_rounded, size: 13.r, color: AppColors.textMuted),
                    SizedBox(width: 4.w),
                    Text(Fmt.dateTime(entry.timeStamp), style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary)),
                  ],
                ),
              ],
            ),
          ),
          SizedBox(width: 4.w),
          Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 20.r),
        ],
      ),
    );
  }
}

class _AuditDetailSheet extends StatefulWidget {
  final SystemLogsRepository repo;
  final AuditItem summary;
  const _AuditDetailSheet({required this.repo, required this.summary});

  @override
  State<_AuditDetailSheet> createState() => _AuditDetailSheetState();
}

class _AuditDetailSheetState extends State<_AuditDetailSheet> {
  Future<AuditItem>? _future;

  @override
  void initState() {
    super.initState();
    _future = widget.repo.getAuditEntry(widget.summary.id);
  }

  @override
  Widget build(BuildContext context) {
    final (icon, color) = _actionStyle(widget.summary.actionType);
    return DraggableScrollableSheet(
      initialChildSize: 0.65,
      maxChildSize: 0.92,
      minChildSize: 0.35,
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
                      Text(
                        '${widget.summary.entityName ?? 'Unknown'}${widget.summary.entityId != null ? ' #${widget.summary.entityId}' : ''}',
                        style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                      Text(widget.summary.actionType ?? '—', style: TextStyle(fontSize: 11.sp, color: color, fontWeight: FontWeight.w700)),
                    ],
                  ),
                ),
              ],
            ),
            SizedBox(height: 18.h),
            _infoRow('Changed by', widget.summary.username ?? 'System'),
            _infoRow('When', Fmt.dateTime(widget.summary.timeStamp)),
            if (widget.summary.entityId != null) _infoRow('Record ID', widget.summary.entityId!),
            SizedBox(height: 18.h),
            Text('Field Changes', style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: AppColors.textMuted, letterSpacing: .4)),
            SizedBox(height: 8.h),
            FutureBuilder<AuditItem>(
              future: _future,
              builder: (context, snap) {
                if (snap.connectionState != ConnectionState.done) {
                  return Padding(
                    padding: EdgeInsets.symmetric(vertical: 24.h),
                    child: const Center(child: SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.2))),
                  );
                }
                if (snap.hasError) {
                  return Padding(
                    padding: EdgeInsets.symmetric(vertical: 12.h),
                    child: Text('Could not load field changes.', style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted)),
                  );
                }
                final changes = snap.data?.changes;
                if (changes == null || changes.isEmpty) {
                  return Padding(
                    padding: EdgeInsets.symmetric(vertical: 12.h),
                    child: Text('No field-level detail recorded for this entry.', style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted)),
                  );
                }
                final keys = changes.keys.toList()..sort();
                return Container(
                  width: double.infinity,
                  padding: EdgeInsets.symmetric(horizontal: 4.w),
                  decoration: BoxDecoration(color: AppColors.surfaceAlt, borderRadius: BorderRadius.circular(12.r)),
                  child: Column(
                    children: [
                      for (var i = 0; i < keys.length; i++)
                        Padding(
                          padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 8.h),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(keys[i], style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: AppColors.textSecondary)),
                              SizedBox(height: 2.h),
                              Text(
                                _formatValue(changes[keys[i]]),
                                style: TextStyle(fontSize: 12.5.sp, color: AppColors.textPrimary),
                              ),
                              if (i < keys.length - 1) Padding(padding: EdgeInsets.only(top: 8.h), child: Divider(height: 1, color: AppColors.border)),
                            ],
                          ),
                        ),
                    ],
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  String _formatValue(dynamic v) {
    if (v == null) return '—';
    if (v is bool) return v ? 'Yes' : 'No';
    final s = v.toString();
    return s.isEmpty ? '—' : s;
  }

  Widget _infoRow(String label, String value) => Padding(
        padding: EdgeInsets.only(bottom: 6.h),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(width: 90.w, child: Text(label, style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted))),
            Expanded(
              child: Text(
                value,
                style: TextStyle(fontSize: 12.sp, color: AppColors.textPrimary, fontWeight: FontWeight.w600),
                overflow: TextOverflow.ellipsis,
                maxLines: 3,
              ),
            ),
          ],
        ),
      );
}
