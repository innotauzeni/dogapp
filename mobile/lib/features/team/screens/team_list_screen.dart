import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/team_repository.dart';
import '../models/team_models.dart';
import 'team_form_screen.dart';

/// Team management: view every user across the company (and its branches, for
/// head-office admins), add new ones, and remove ones that shouldn't have access
/// anymore.
class TeamListScreen extends StatefulWidget {
  const TeamListScreen({super.key});

  @override
  State<TeamListScreen> createState() => _TeamListScreenState();
}

class _TeamListScreenState extends State<TeamListScreen> {
  final _repo = TeamRepository();
  Future<List<TeamUser>>? _future;
  String _query = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    final auth = context.read<AuthProvider>();
    final companyIds = <int>{
      auth.companyId,
      if (auth.isHeadOffice) ...auth.branches.map((b) => b.id),
    };
    _future = _loadAll(companyIds, auth.user?.id);
  }

  Future<List<TeamUser>> _loadAll(Set<int> companyIds, String? selfId) async {
    final lists = await Future.wait(companyIds.map((id) => _repo.getUsers(companyId: id)));
    final byId = <String, TeamUser>{};
    for (final list in lists) {
      for (final u in list) {
        if (u.id == selfId) continue;
        byId[u.id] = u;
      }
    }
    final result = byId.values.toList()..sort((a, b) => a.fullName.toLowerCase().compareTo(b.fullName.toLowerCase()));
    return result;
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  List<TeamUser> _filter(List<TeamUser> users) {
    if (_query.trim().isEmpty) return users;
    final q = _query.toLowerCase();
    return users
        .where((u) =>
            u.fullName.toLowerCase().contains(q) ||
            (u.email ?? '').toLowerCase().contains(q) ||
            (u.roleName ?? '').toLowerCase().contains(q))
        .toList();
  }

  Future<void> _addUser() async {
    final created = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const TeamFormScreen()),
    );
    if (created == true) _refresh();
  }

  Future<void> _editUser(TeamUser user) async {
    final saved = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => TeamFormScreen(existing: user)),
    );
    if (saved == true) _refresh();
  }

  void _openDetails(TeamUser user, {required bool canEdit, required bool canDelete}) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _UserDetailSheet(
        user: user,
        canEdit: canEdit,
        canDelete: canDelete,
        onEdit: () {
          Navigator.of(context).maybePop();
          _editUser(user);
        },
        onDelete: () {
          Navigator.of(context).maybePop();
          _delete(user);
        },
      ),
    );
  }

  Future<void> _delete(TeamUser user) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete user?'),
        content: Text('Permanently remove ${user.fullName}? They will lose access immediately.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await _repo.deleteUser(user.id);
      _refresh();
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final canCreate = auth.can('users.create');
    final canEdit = auth.can('users.edit');
    final canDelete = auth.can('users.delete');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Team'),
        bottom: PreferredSize(
          preferredSize: Size.fromHeight(56.h),
          child: Padding(
            padding: EdgeInsets.fromLTRB(16.w, 0, 16.w, 12.h),
            child: TextField(
              onChanged: (v) => setState(() => _query = v),
              decoration: InputDecoration(
                hintText: 'Search by name, email or role',
                prefixIcon: const Icon(Icons.search_rounded),
                filled: true,
                fillColor: Theme.of(context).cardColor,
                contentPadding: EdgeInsets.symmetric(vertical: 10.h),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12.r), borderSide: BorderSide.none),
              ),
            ),
          ),
        ),
      ),
      floatingActionButton: canCreate
          ? FloatingActionButton.extended(
              heroTag: 'fab-team-add-user',
              onPressed: _addUser,
              icon: const Icon(Icons.person_add_alt_1_rounded),
              label: const Text('Add User'),
            )
          : null,
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: AsyncView<List<TeamUser>>(
            future: _future,
            onRetry: _refresh,
            builder: (context, all) {
              final users = _filter(all);
              if (users.isEmpty) {
                return ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: [
                    SizedBox(height: 60.h),
                    StatusView.empty(
                      title: all.isEmpty ? 'No team members yet' : 'No matches',
                      message: all.isEmpty ? 'Add your first user to get started.' : 'Try a different search.',
                    ),
                  ],
                );
              }
              return ListView.separated(
                padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 90.h),
                physics: const AlwaysScrollableScrollPhysics(),
                itemCount: users.length,
                separatorBuilder: (_, _) => SizedBox(height: 10.h),
                itemBuilder: (context, i) => _UserTile(
                  user: users[i],
                  canDelete: canDelete,
                  onDelete: () => _delete(users[i]),
                  onTap: () => _openDetails(users[i], canEdit: canEdit, canDelete: canDelete),
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}

class _UserTile extends StatelessWidget {
  final TeamUser user;
  final bool canDelete;
  final VoidCallback onDelete;
  final VoidCallback onTap;
  const _UserTile({required this.user, required this.canDelete, required this.onDelete, required this.onTap});

  String get _initials {
    final n = user.fullName.trim();
    if (n.isEmpty) return '?';
    final parts = n.split(RegExp(r'\s+'));
    return parts.length == 1 ? parts.first.substring(0, 1).toUpperCase() : (parts.first[0] + parts.last[0]).toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    return AppCard(
      onTap: onTap,
      padding: EdgeInsets.all(14.w),
      child: Row(
        children: [
          Container(
            width: 42.r,
            height: 42.r,
            decoration: BoxDecoration(color: AppColors.primaryLight, shape: BoxShape.circle),
            child: Center(
              child: Text(_initials, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800, color: AppColors.primary)),
            ),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(user.fullName.isEmpty ? (user.email ?? 'Unnamed') : user.fullName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary)),
                SizedBox(height: 2.h),
                Text(user.email ?? '—', maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted)),
                SizedBox(height: 6.h),
                Row(
                  children: [
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 2.h),
                      decoration: BoxDecoration(color: AppColors.primaryLight, borderRadius: BorderRadius.circular(20.r)),
                      child: Text(user.roleName ?? 'No role', style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w700, color: AppColors.primary)),
                    ),
                    SizedBox(width: 6.w),
                    if (!user.isActive)
                      Container(
                        padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 2.h),
                        decoration: BoxDecoration(color: AppColors.dangerLight, borderRadius: BorderRadius.circular(20.r)),
                        child: Text(user.status ?? 'Inactive', style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w700, color: AppColors.danger)),
                      ),
                  ],
                ),
              ],
            ),
          ),
          if (canDelete)
            IconButton(
              onPressed: onDelete,
              icon: Icon(Icons.delete_outline_rounded, color: AppColors.danger, size: 20.r),
            ),
          Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 20.r),
        ],
      ),
    );
  }
}

class _UserDetailSheet extends StatelessWidget {
  final TeamUser user;
  final bool canEdit;
  final bool canDelete;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  const _UserDetailSheet({
    required this.user,
    required this.canEdit,
    required this.canDelete,
    required this.onEdit,
    required this.onDelete,
  });

  String get _initials {
    final n = user.fullName.trim();
    if (n.isEmpty) return '?';
    final parts = n.split(RegExp(r'\s+'));
    return parts.length == 1 ? parts.first.substring(0, 1).toUpperCase() : (parts.first[0] + parts.last[0]).toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.6,
      maxChildSize: 0.9,
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
                  width: 46.r,
                  height: 46.r,
                  decoration: BoxDecoration(color: AppColors.primaryLight, shape: BoxShape.circle),
                  child: Center(
                    child: Text(_initials, style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800, color: AppColors.primary)),
                  ),
                ),
                SizedBox(width: 12.w),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        user.fullName.isEmpty ? (user.email ?? 'Unnamed') : user.fullName,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary),
                      ),
                      SizedBox(height: 3.h),
                      Row(
                        children: [
                          _pill(user.roleName ?? 'No role', AppColors.primary),
                          SizedBox(width: 6.w),
                          _pill(user.isActive ? 'Active' : (user.status ?? 'Inactive'), user.isActive ? AppColors.success : AppColors.danger),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
            SizedBox(height: 20.h),
            if (user.username?.isNotEmpty == true) _infoRow(Icons.badge_outlined, 'Username', user.username!),
            if (user.email?.isNotEmpty == true) _infoRow(Icons.email_outlined, 'Email', user.email!),
            if (user.phoneNumber?.isNotEmpty == true) _infoRow(Icons.phone_outlined, 'Phone', user.phoneNumber!),
            _infoRow(Icons.calendar_today_outlined, 'Joined', _formatDate(user.createdAt)),
            if (user.lastLoginDate != null) _infoRow(Icons.login_rounded, 'Last Login', _formatDate(user.lastLoginDate!)),
            if (canEdit || canDelete) ...[
              SizedBox(height: 12.h),
              Row(
                children: [
                  if (canEdit)
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: onEdit,
                        icon: const Icon(Icons.edit_outlined, size: 18),
                        label: const Text('Edit User'),
                      ),
                    ),
                  if (canEdit && canDelete) SizedBox(width: 10.w),
                  if (canDelete)
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: onDelete,
                        style: OutlinedButton.styleFrom(foregroundColor: AppColors.danger, side: const BorderSide(color: AppColors.danger)),
                        icon: const Icon(Icons.delete_outline_rounded, size: 18),
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

  String _formatDate(DateTime d) => '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  Widget _pill(String text, Color color) => Container(
        padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 2.h),
        decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20.r)),
        child: Text(text, style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w700, color: color)),
      );

  Widget _infoRow(IconData icon, String label, String value) => Padding(
        padding: EdgeInsets.only(bottom: 12.h),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, size: 16.r, color: AppColors.textMuted),
            SizedBox(width: 10.w),
            SizedBox(width: 90.w, child: Text(label, style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted))),
            Expanded(
              child: Text(
                value,
                style: TextStyle(fontSize: 12.5.sp, color: AppColors.textPrimary, fontWeight: FontWeight.w600),
                overflow: TextOverflow.ellipsis,
                maxLines: 3,
              ),
            ),
          ],
        ),
      );
}
