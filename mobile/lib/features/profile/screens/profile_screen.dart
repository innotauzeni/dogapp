import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/config/app_config.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/photo_util.dart';
import '../../../shared/widgets/cards.dart';
import '../../auth/providers/auth_provider.dart';
import '../../settings/data/company_repository.dart';
import '../../settings/models/company_info.dart';
import '../../settings/screens/settings_screen.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  Future<void> _confirmLogout(BuildContext context) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sign out?'),
        content: const Text('You will need to sign in again to use the app.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text('Sign out'),
          ),
        ],
      ),
    );
    if (ok == true && context.mounted) {
      await context.read<AuthProvider>().logout();
    }
  }

  String _currentBranchName(AuthProvider auth) {
    if (auth.currentBranchId == null) return 'All Branches (Head Office)';
    final match = auth.branches.where((b) => b.id == auth.currentBranchId).toList();
    return match.isEmpty ? 'Branch ${auth.currentBranchId}' : (match.first.companyName ?? 'Branch');
  }

  Future<void> _switchBranch(BuildContext context, AuthProvider auth) async {
    await showModalBottomSheet<void>(
      context: context,
      builder: (_) => SafeArea(
        child: ListView(
          shrinkWrap: true,
          children: [
            Padding(
              padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 6.h),
              child: Text('Switch branch context',
                  style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800)),
            ),
            ListTile(
              leading: const Icon(Icons.apps_rounded),
              title: const Text('All Branches (Head Office)'),
              trailing: auth.currentBranchId == null ? const Icon(Icons.check, color: AppColors.primary) : null,
              onTap: () {
                auth.switchBranch(null);
                Navigator.pop(context);
              },
            ),
            const Divider(height: 1),
            ...auth.branches.where((b) => b.isActive).map((b) => ListTile(
                  leading: const Icon(Icons.store_mall_directory_outlined),
                  title: Text(b.companyName ?? 'Branch ${b.id}'),
                  subtitle: (b.location ?? '').isNotEmpty ? Text(b.location!) : null,
                  trailing: auth.currentBranchId == b.id ? const Icon(Icons.check, color: AppColors.primary) : null,
                  onTap: () {
                    auth.switchBranch(b.id);
                    Navigator.pop(context);
                  },
                )),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final user = auth.user;

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Profile'),
        elevation: 0,
        backgroundColor: Colors.transparent,
      ),
      body: SafeArea(
        top: false,
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            // Header with gradient background
            Container(
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  colors: [Color(0xFF667eea), Color(0xFF764ba2)],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
              ),
              padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 32.h),
              child: Column(
                children: [
                  CircleAvatar(
                    radius: 44.r,
                    backgroundColor: Colors.white.withValues(alpha: 0.3),
                    child: Text(
                      user?.initials ?? '?',
                      style: TextStyle(color: Colors.white, fontSize: 32.sp, fontWeight: FontWeight.w800),
                    ),
                  ),
                  SizedBox(height: 16.h),
                  Text(
                    user?.name ?? 'User',
                    style: TextStyle(fontSize: 22.sp, fontWeight: FontWeight.w800, color: Colors.white),
                  ),
                  SizedBox(height: 4.h),
                  Text(
                    user?.email ?? '',
                    style: TextStyle(fontSize: 13.sp, color: Colors.white.withValues(alpha: 0.8)),
                  ),
                  SizedBox(height: 12.h),
                  if ((user?.role ?? '').isNotEmpty)
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 6.h),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.2),
                        borderRadius: BorderRadius.circular(20.r),
                        border: Border.all(color: Colors.white.withValues(alpha: 0.3)),
                      ),
                      child: Text(
                        user!.role!,
                        style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w700, color: Colors.white),
                      ),
                    ),
                ],
              ),
            ),
            SizedBox(height: 20.h),
            Padding(
              padding: EdgeInsets.symmetric(horizontal: 16.w),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Company Details (always the head office, even for a branch user)
                  const _CompanyDetailsCard(),
                  SizedBox(height: 16.h),

                  // Branch Context (for head office users)
                  if (auth.isHeadOffice && auth.branches.isNotEmpty) ...[
                    AppCard(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const SectionHeader(title: 'Active Branch'),
                          ListTile(
                            contentPadding: EdgeInsets.zero,
                            leading: Container(
                              width: 40.r,
                              height: 40.r,
                              decoration: BoxDecoration(
                                color: AppColors.primaryLight,
                                borderRadius: BorderRadius.circular(10.r),
                              ),
                              child: const Icon(Icons.store_mall_directory_outlined, color: AppColors.primary),
                            ),
                            title: Text(_currentBranchName(auth),
                                style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                            subtitle: const Text('Tap to switch branch context'),
                            trailing: const Icon(Icons.swap_horiz_rounded),
                            onTap: () => _switchBranch(context, auth),
                          ),
                        ],
                      ),
                    ),
                    SizedBox(height: 16.h),
                  ],

                  // Settings & Actions
                  AppCard(
                    padding: EdgeInsets.zero,
                    child: Column(
                      children: [
                        _MenuTile(
                          icon: Icons.settings_outlined,
                          label: 'Settings',
                          onTap: () => Navigator.of(context).push(
                            MaterialPageRoute(builder: (_) => const SettingsScreen()),
                          ),
                          backgroundColor: AppColors.primaryLight,
                        ),
                        const Divider(height: 1),
                        _MenuTile(
                          icon: Icons.logout_rounded,
                          label: 'Sign out',
                          color: AppColors.danger,
                          backgroundColor: AppColors.danger.withValues(alpha: 0.1),
                          onTap: () => _confirmLogout(context),
                        ),
                      ],
                    ),
                  ),
                  SizedBox(height: 32.h),

                  // Footer with branding
                  Center(
                    child: Column(
                      children: [
                        Text('${AppConfig.appName} • v1.0.0',
                            style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary)),
                        SizedBox(height: 8.h),
                        Text(AppConfig.appTagline,
                            style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                      ],
                    ),
                  ),
                  SizedBox(height: 20.h),
                  const _DeveloperCard(),
                  SizedBox(height: 24.h),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Shows the head-office company's details, even when the signed-in user
/// belongs to a branch — a branch's own record is rarely what someone
/// looking at "my company" on their profile wants to see.
class _CompanyDetailsCard extends StatefulWidget {
  const _CompanyDetailsCard();

  @override
  State<_CompanyDetailsCard> createState() => _CompanyDetailsCardState();
}

class _CompanyDetailsCardState extends State<_CompanyDetailsCard> {
  final _repo = CompanyRepository();
  late final Future<CompanyInfo> _future = _repo.getRootCompany(context.read<AuthProvider>().companyId);

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<CompanyInfo>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return AppCard(
            child: SizedBox(
              height: 80.h,
              child: const Center(child: CircularProgressIndicator(strokeWidth: 2.4)),
            ),
          );
        }
        final company = snapshot.data;
        if (company == null) {
          return AppCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SectionHeader(title: 'Company'),
                Text('Company details are unavailable right now.',
                    style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted)),
              ],
            ),
          );
        }

        final logoUrl = PhotoUtil.resolveNetworkUrl(company.logo);
        return AppCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  if (logoUrl != null)
                    ClipRRect(
                      borderRadius: BorderRadius.circular(10.r),
                      child: CachedNetworkImage(
                        imageUrl: logoUrl,
                        width: 44.r,
                        height: 44.r,
                        fit: BoxFit.cover,
                        errorWidget: (_, _, _) => _companyIcon(),
                      ),
                    )
                  else
                    _companyIcon(),
                  SizedBox(width: 12.w),
                  Expanded(
                    child: Text(
                      company.companyName ?? 'Company',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary),
                    ),
                  ),
                  Container(
                    padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
                    decoration: BoxDecoration(
                      color: company.isActive ? AppColors.successLight : AppColors.dangerLight,
                      borderRadius: BorderRadius.circular(20.r),
                    ),
                    child: Text(
                      company.isActive ? 'Active' : 'Inactive',
                      style: TextStyle(
                        fontSize: 10.sp,
                        fontWeight: FontWeight.w700,
                        color: company.isActive ? AppColors.success : AppColors.danger,
                      ),
                    ),
                  ),
                ],
              ),
              SizedBox(height: 14.h),
              if ((company.companyCode ?? '').isNotEmpty)
                InfoRow(label: 'Code', value: company.companyCode!, icon: Icons.badge_outlined),
              if ((company.emailAddress ?? '').isNotEmpty)
                InfoRow(label: 'Email', value: company.emailAddress!, icon: Icons.email_outlined),
              if ((company.phoneNumber ?? '').isNotEmpty)
                InfoRow(label: 'Phone', value: company.phoneNumber!, icon: Icons.phone_outlined),
              if ((company.website ?? '').isNotEmpty)
                InfoRow(label: 'Website', value: company.website!, icon: Icons.language_rounded),
              if (company.fullLocation.isNotEmpty)
                InfoRow(label: 'Location', value: company.fullLocation, icon: Icons.location_on_outlined),
              if ((company.address ?? '').isNotEmpty)
                InfoRow(label: 'Address', value: company.address!, icon: Icons.home_outlined),
            ],
          ),
        );
      },
    );
  }

  Widget _companyIcon() => Container(
        width: 44.r,
        height: 44.r,
        decoration: BoxDecoration(color: AppColors.primaryLight, borderRadius: BorderRadius.circular(10.r)),
        child: Icon(Icons.business_rounded, color: AppColors.primary, size: 22.r),
      );
}

/// Credits the app's developer with links to their professional profiles.
class _DeveloperCard extends StatelessWidget {
  const _DeveloperCard();

  static const _linkedInUrl = 'https://www.linkedin.com/in/innocent-tauzeni-a5a18273/';
  static const _githubUrl = 'https://github.com/innotauzeni';
  static const _websiteUrl = 'https://daraerp.co.zw';

  Future<void> _open(BuildContext context, String url) async {
    final uri = Uri.parse(url);
    final launched = await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (!launched && context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Could not open $url')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Column(
        children: [
          Row(
            children: [
              Container(
                width: 44.r,
                height: 44.r,
                decoration: BoxDecoration(color: AppColors.primaryLight, borderRadius: BorderRadius.circular(12.r)),
                child: Icon(Icons.code_rounded, color: AppColors.primary, size: 22.r),
              ),
              SizedBox(width: 12.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Kerynet Solutions',
                        style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                    SizedBox(height: 2.h),
                    Text('.NET Developer • 10+ Years Experience',
                        style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary)),
                  ],
                ),
              ),
            ],
          ),
          SizedBox(height: 14.h),
          Row(
            children: [
              Expanded(
                child: _SocialButton(
                  icon: Icons.business_center_outlined,
                  label: 'LinkedIn',
                  onTap: () => _open(context, _linkedInUrl),
                ),
              ),
              SizedBox(width: 8.w),
              Expanded(
                child: _SocialButton(
                  icon: Icons.code_rounded,
                  label: 'GitHub',
                  onTap: () => _open(context, _githubUrl),
                ),
              ),
              SizedBox(width: 8.w),
              Expanded(
                child: _SocialButton(
                  icon: Icons.language_rounded,
                  label: 'Website',
                  onTap: () => _open(context, _websiteUrl),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _SocialButton extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  const _SocialButton({required this.icon, required this.label, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.surfaceAlt,
      borderRadius: BorderRadius.circular(10.r),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(10.r),
        child: Padding(
          padding: EdgeInsets.symmetric(vertical: 10.h),
          child: Column(
            children: [
              Icon(icon, size: 18.r, color: AppColors.primary),
              SizedBox(height: 4.h),
              Text(label, style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
            ],
          ),
        ),
      ),
    );
  }
}

class _MenuTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final Color? color;
  final Color? backgroundColor;
  const _MenuTile({
    required this.icon,
    required this.label,
    required this.onTap,
    this.color,
    this.backgroundColor,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      tileColor: backgroundColor,
      onTap: onTap,
      leading: Icon(icon, color: color ?? AppColors.textSecondary, size: 22.r),
      title: Text(label, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600, color: color ?? AppColors.textPrimary)),
      trailing: Icon(Icons.chevron_right_rounded, size: 20.r, color: AppColors.textMuted),
    );
  }
}
