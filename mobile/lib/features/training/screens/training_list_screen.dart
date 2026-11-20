import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/status_chip.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/training_menus.dart';
import '../data/training_repository.dart';
import 'training_report_form_screen.dart';

/// One Dog Training menu as a searchable list; tap a row for its full details.
/// The server only returns what the signed-in user may see (handlers get just their own dogs).
class TrainingListScreen extends StatefulWidget {
  final TrainingMenu config;
  const TrainingListScreen({super.key, required this.config});

  @override
  State<TrainingListScreen> createState() => _TrainingListScreenState();
}

class _TrainingListScreenState extends State<TrainingListScreen> {
  final _repo = TrainingRepository();
  final _search = TextEditingController();
  late Future<List<Map<String, dynamic>>> _future;

  @override
  void initState() {
    super.initState();
    _future = _repo.rawList(widget.config.path);
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _refresh() async {
    setState(() => _future = _repo.rawList(widget.config.path));
    await _future.catchError((_) => <Map<String, dynamic>>[]);
  }

  void _openDetails(TRow r) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      showDragHandle: true,
      builder: (_) => DraggableScrollableSheet(
        expand: false,
        initialChildSize: 0.75,
        maxChildSize: 0.95,
        builder: (context, controller) => ListView(
          controller: controller,
          padding: EdgeInsets.fromLTRB(20.w, 4.h, 20.w, 28.h),
          children: [
            Row(
              children: [
                Expanded(child: Text(r.title, style: TextStyle(fontSize: 17.sp, fontWeight: FontWeight.w800))),
                if (r.badge.isNotEmpty) StatusChip.forStatus(r.badge),
              ],
            ),
            SizedBox(height: 12.h),
            for (final e in r.details)
              Padding(
                padding: EdgeInsets.symmetric(vertical: 6.h),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(e.key.toUpperCase(), style: TextStyle(fontSize: 10.sp, letterSpacing: 0.8, fontWeight: FontWeight.w700, color: AppColors.textMuted)),
                    SizedBox(height: 2.h),
                    Text(e.value, style: TextStyle(fontSize: 14.sp, color: AppColors.textPrimary)),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final cfg = widget.config;
    final canCreate = cfg.createPermission != null && auth.can(cfg.createPermission!);

    return Scaffold(
      appBar: AppBar(title: Text(cfg.title)),
      floatingActionButton: canCreate
          ? FloatingActionButton.extended(
              heroTag: 'fab-training-report',
              icon: const Icon(Icons.edit_note_rounded),
              label: const Text('New report'),
              onPressed: () async {
                final done = await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => const TrainingReportFormScreen()));
                if (done == true) _refresh();
              },
            )
          : null,
      body: SafeArea(
        top: false,
        child: Column(
          children: [
            Padding(
              padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 4.h),
              child: TextField(
                controller: _search,
                onChanged: (_) => setState(() {}),
                decoration: const InputDecoration(prefixIcon: Icon(Icons.search_rounded), hintText: 'Search'),
              ),
            ),
            Expanded(
              child: RefreshIndicator(
                onRefresh: _refresh,
                child: AsyncView<List<Map<String, dynamic>>>(
                  future: _future,
                  onRetry: _refresh,
                  builder: (context, data) {
                    final q = _search.text.trim().toLowerCase();
                    final rows = data.map(cfg.row).where((r) => q.isEmpty || '${r.title} ${r.subtitle} ${r.badge}'.toLowerCase().contains(q)).toList();
                    if (rows.isEmpty) {
                      return ListView(children: [
                        SizedBox(height: 80.h),
                        Center(child: Text(cfg.empty, style: TextStyle(fontSize: 14.sp, color: AppColors.textMuted))),
                      ]);
                    }
                    return ListView.separated(
                      physics: const AlwaysScrollableScrollPhysics(),
                      padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 96.h),
                      itemCount: rows.length,
                      separatorBuilder: (_, __) => SizedBox(height: 10.h),
                      itemBuilder: (_, i) {
                        final r = rows[i];
                        return AppCard(
                          onTap: () => _openDetails(r),
                          child: Row(
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(r.title, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                                    if (r.subtitle.isNotEmpty)
                                      Padding(
                                        padding: EdgeInsets.only(top: 2.h),
                                        child: Text(r.subtitle, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                                      ),
                                    if (r.trailing.isNotEmpty)
                                      Padding(
                                        padding: EdgeInsets.only(top: 4.h),
                                        child: Text(r.trailing, style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w700, color: AppColors.primary)),
                                      ),
                                  ],
                                ),
                              ),
                              if (r.badge.isNotEmpty) StatusChip.forStatus(r.badge),
                            ],
                          ),
                        );
                      },
                    );
                  },
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
