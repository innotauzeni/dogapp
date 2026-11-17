import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/branches_repository.dart';
import '../models/branch_models.dart';
import 'branch_form_screen.dart';

/// Head-office admins manage their branches here: view, add and delete.
class BranchesListScreen extends StatefulWidget {
  const BranchesListScreen({super.key});

  @override
  State<BranchesListScreen> createState() => _BranchesListScreenState();
}

class _BranchesListScreenState extends State<BranchesListScreen> {
  final _repo = BranchesRepository();
  Future<List<CompanyBranch>>? _future;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    final companyId = context.read<AuthProvider>().companyId;
    _future = _repo.getBranchesOf(companyId);
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  Future<void> _addBranch() async {
    final created = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const BranchFormScreen()),
    );
    if (created == true) _refresh();
  }

  void _openDetails(CompanyBranch branch, bool canDelete) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _BranchDetailSheet(
        branch: branch,
        canDelete: canDelete,
        onDelete: () {
          Navigator.of(context).maybePop();
          _delete(branch);
        },
      ),
    );
  }

  Future<void> _delete(CompanyBranch branch) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete branch?'),
        content: Text('Permanently remove ${branch.companyName}? This cannot be undone.'),
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
      await _repo.deleteBranch(branch.id);
      _refresh();
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final canCreate = auth.can('companies.create');
    final canDelete = auth.can('companies.delete');

    return Scaffold(
      appBar: AppBar(title: const Text('Branches')),
      floatingActionButton: canCreate
          ? FloatingActionButton.extended(
              heroTag: 'fab-branches-add',
              onPressed: _addBranch,
              icon: const Icon(Icons.add_business_rounded),
              label: const Text('Add Branch'),
            )
          : null,
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: AsyncView<List<CompanyBranch>>(
            future: _future,
            onRetry: _refresh,
            builder: (context, branches) {
              if (branches.isEmpty) {
                return ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: [
                    SizedBox(height: 60.h),
                    StatusView.empty(
                      title: 'No branches yet',
                      message: canCreate ? 'Add your first branch to get started.' : 'Branches will appear here.',
                    ),
                  ],
                );
              }
              return ListView.separated(
                padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 90.h),
                physics: const AlwaysScrollableScrollPhysics(),
                itemCount: branches.length,
                separatorBuilder: (_, _) => SizedBox(height: 10.h),
                itemBuilder: (context, i) => _BranchTile(
                  branch: branches[i],
                  canDelete: canDelete,
                  onDelete: () => _delete(branches[i]),
                  onTap: () => _openDetails(branches[i], canDelete),
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}

class _BranchTile extends StatelessWidget {
  final CompanyBranch branch;
  final bool canDelete;
  final VoidCallback onDelete;
  final VoidCallback onTap;
  const _BranchTile({required this.branch, required this.canDelete, required this.onDelete, required this.onTap});

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
            decoration: BoxDecoration(
              color: (branch.isActive ? AppColors.success : AppColors.textMuted).withValues(alpha: .12),
              borderRadius: BorderRadius.circular(12.r),
            ),
            child: Icon(Icons.store_rounded, color: branch.isActive ? AppColors.success : AppColors.textMuted, size: 22.r),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  branch.companyName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary),
                ),
                SizedBox(height: 2.h),
                Text(
                  [if (branch.companyCode?.isNotEmpty == true) branch.companyCode, if (branch.location?.isNotEmpty == true) branch.location]
                      .whereType<String>()
                      .join(' • '),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted),
                ),
                SizedBox(height: 4.h),
                Row(
                  children: [
                    Icon(Icons.pets_rounded, size: 13.r, color: AppColors.textMuted),
                    SizedBox(width: 3.w),
                    Text('${branch.userCount} users', style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                    if (!branch.isActive) ...[
                      SizedBox(width: 8.w),
                      Container(
                        padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 1.h),
                        decoration: BoxDecoration(color: AppColors.dangerLight, borderRadius: BorderRadius.circular(6.r)),
                        child: Text('Inactive', style: TextStyle(fontSize: 10.sp, color: AppColors.danger, fontWeight: FontWeight.w600)),
                      ),
                    ],
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

class _BranchDetailSheet extends StatelessWidget {
  final CompanyBranch branch;
  final bool canDelete;
  final VoidCallback onDelete;
  const _BranchDetailSheet({required this.branch, required this.canDelete, required this.onDelete});

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
                  decoration: BoxDecoration(
                    color: (branch.isActive ? AppColors.success : AppColors.textMuted).withValues(alpha: .12),
                    borderRadius: BorderRadius.circular(14.r),
                  ),
                  child: Icon(Icons.store_rounded, color: branch.isActive ? AppColors.success : AppColors.textMuted, size: 24.r),
                ),
                SizedBox(width: 12.w),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        branch.companyName,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary),
                      ),
                      SizedBox(height: 3.h),
                      Row(
                        children: [
                          _pill(branch.companyType, AppColors.primary),
                          SizedBox(width: 6.w),
                          _pill(branch.isActive ? 'Active' : 'Inactive', branch.isActive ? AppColors.success : AppColors.danger),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
            SizedBox(height: 20.h),
            Row(
              children: [
                Expanded(child: _statTile(Icons.account_tree_rounded, '${branch.branchCount}', 'Branches')),
                SizedBox(width: 10.w),
                Expanded(child: _statTile(Icons.people_alt_rounded, '${branch.userCount}', 'Users')),
              ],
            ),
            SizedBox(height: 20.h),
            if (branch.companyCode?.isNotEmpty == true) _infoRow(Icons.tag_rounded, 'Code', branch.companyCode!),
            if (branch.emailAddress?.isNotEmpty == true) _infoRow(Icons.email_outlined, 'Email', branch.emailAddress!),
            if (branch.phoneNumber?.isNotEmpty == true) _infoRow(Icons.phone_outlined, 'Phone', branch.phoneNumber!),
            if (branch.website?.isNotEmpty == true) _infoRow(Icons.language_rounded, 'Website', branch.website!),
            if (branch.address?.isNotEmpty == true) _infoRow(Icons.home_work_outlined, 'Address', branch.address!),
            if ((branch.location?.isNotEmpty == true) || branch.cityName != null || branch.countryName != null)
              _infoRow(
                Icons.location_on_outlined,
                'Location',
                [branch.location, branch.cityName, branch.provinceName, branch.countryName].whereType<String>().where((s) => s.isNotEmpty).join(', '),
              ),
            if (branch.currencyCode?.isNotEmpty == true) _infoRow(Icons.currency_exchange_rounded, 'Currency', branch.currencyCode!),
            if (branch.parentCompanyName?.isNotEmpty == true) _infoRow(Icons.corporate_fare_rounded, 'Parent', branch.parentCompanyName!),
            if (canDelete) ...[
              SizedBox(height: 12.h),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed: onDelete,
                  style: OutlinedButton.styleFrom(foregroundColor: AppColors.danger, side: const BorderSide(color: AppColors.danger)),
                  icon: const Icon(Icons.delete_outline_rounded, size: 18),
                  label: const Text('Delete Branch'),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _pill(String text, Color color) => Container(
        padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 2.h),
        decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20.r)),
        child: Text(text, style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w700, color: color)),
      );

  Widget _statTile(IconData icon, String value, String label) => Container(
        padding: EdgeInsets.symmetric(vertical: 12.h, horizontal: 10.w),
        decoration: BoxDecoration(color: AppColors.surfaceAlt, borderRadius: BorderRadius.circular(12.r)),
        child: Column(
          children: [
            Icon(icon, size: 18.r, color: AppColors.primary),
            SizedBox(height: 6.h),
            Text(value, style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
            Text(label, style: TextStyle(fontSize: 10.sp, color: AppColors.textMuted)),
          ],
        ),
      );

  Widget _infoRow(IconData icon, String label, String value) => Padding(
        padding: EdgeInsets.only(bottom: 12.h),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, size: 16.r, color: AppColors.textMuted),
            SizedBox(width: 10.w),
            SizedBox(width: 70.w, child: Text(label, style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted))),
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
