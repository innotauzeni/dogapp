import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/cards.dart';
import '../../auth/providers/auth_provider.dart';
import '../../branches/screens/branches_list_screen.dart';
import '../../systemlogs/screens/audit_trail_screen.dart';
import '../../systemlogs/screens/system_logs_screen.dart';
import '../../team/screens/team_list_screen.dart';
import 'privacy_policy_screen.dart';

class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  static const _supportEmail = 'support@daraerp.co.zw';

  Future<void> _contactSupport(BuildContext context) async {
    final uri = Uri(scheme: 'mailto', path: _supportEmail, queryParameters: {'subject': 'Support request'});
    final launched = await launchUrl(uri);
    if (!launched && context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not open your mail app. Email us at $_supportEmail.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(title: const Text('Settings')),
      body: SafeArea(
        top: false,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 32.h),
          children: [
            if (auth.canManageBranding) ...[
              const _BrandColorSection(),
              SizedBox(height: 16.h),
            ],
            if (auth.can('companies.view') || auth.can('users.view')) ...[
              AppCard(
                padding: EdgeInsets.zero,
                child: Column(
                  children: [
                    if (auth.can('companies.view'))
                      _SettingsTile(
                        icon: Icons.store_rounded,
                        label: 'Branches',
                        onTap: () => Navigator.of(context).push(
                          MaterialPageRoute(builder: (_) => const BranchesListScreen()),
                        ),
                      ),
                    if (auth.can('companies.view') && auth.can('users.view')) const Divider(height: 1),
                    if (auth.can('users.view'))
                      _SettingsTile(
                        icon: Icons.people_alt_rounded,
                        label: 'Team',
                        onTap: () => Navigator.of(context).push(
                          MaterialPageRoute(builder: (_) => const TeamListScreen()),
                        ),
                      ),
                  ],
                ),
              ),
              SizedBox(height: 16.h),
            ],
            if (auth.isSystemOwner && (auth.can('systemworker.view') || auth.can('systemworker.audit'))) ...[
              AppCard(
                padding: EdgeInsets.zero,
                child: Column(
                  children: [
                    if (auth.can('systemworker.view'))
                      _SettingsTile(
                        icon: Icons.fact_check_outlined,
                        label: 'System Logs',
                        onTap: () => Navigator.of(context).push(
                          MaterialPageRoute(builder: (_) => const SystemLogsScreen()),
                        ),
                      ),
                    if (auth.can('systemworker.view') && auth.can('systemworker.audit')) const Divider(height: 1),
                    if (auth.can('systemworker.audit'))
                      _SettingsTile(
                        icon: Icons.history_rounded,
                        label: 'Audit Trail',
                        onTap: () => Navigator.of(context).push(
                          MaterialPageRoute(builder: (_) => const AuditTrailScreen()),
                        ),
                      ),
                  ],
                ),
              ),
              SizedBox(height: 16.h),
            ],
            AppCard(
              padding: EdgeInsets.zero,
              child: Column(
                children: [
                  _SettingsTile(
                    icon: Icons.lock_outline_rounded,
                    label: 'Privacy & Security',
                    onTap: () => Navigator.of(context).push(
                      MaterialPageRoute(builder: (_) => const PrivacyPolicyScreen()),
                    ),
                  ),
                  const Divider(height: 1),
                  _SettingsTile(
                    icon: Icons.help_outline_rounded,
                    label: 'Help & Support',
                    onTap: () => _contactSupport(context),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SettingsTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  const _SettingsTile({required this.icon, required this.label, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return ListTile(
      onTap: onTap,
      leading: Container(
        width: 38.r,
        height: 38.r,
        decoration: BoxDecoration(color: AppColors.primaryLight, borderRadius: BorderRadius.circular(10.r)),
        child: Icon(icon, color: AppColors.primary, size: 20.r),
      ),
      title: Text(label, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600, color: AppColors.textPrimary)),
      trailing: Icon(Icons.chevron_right_rounded, size: 20.r, color: AppColors.textMuted),
    );
  }
}

/// Admin-only: pick the color used to brand the app's welcome screen.
class _BrandColorSection extends StatefulWidget {
  const _BrandColorSection();

  @override
  State<_BrandColorSection> createState() => _BrandColorSectionState();
}

class _BrandColorSectionState extends State<_BrandColorSection> {
  late Color? _selected = context.read<AuthProvider>().brandColor;
  bool _saving = false;

  Future<void> _save() async {
    setState(() => _saving = true);
    try {
      final hex = _selected == null ? null : AppColors.toHex(_selected!);
      await context.read<AuthProvider>().updateBrandColor(hex);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Welcome screen color updated.')),
        );
      }
    } on ApiException catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Could not save the color. Please try again.')),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final preview = _selected ?? AppColors.primary;
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionHeader(title: 'Company Branding'),
          Text(
            'Choose the color shown on the app\'s welcome screen for everyone at your company.',
            style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted),
          ),
          SizedBox(height: 14.h),
          Container(
            height: 64.h,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(12.r),
              gradient: LinearGradient(
                colors: [preview, Color.lerp(preview, Colors.black, 0.35)!],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
            ),
            alignment: Alignment.center,
            child: Text(
              'Welcome screen preview',
              style: TextStyle(color: Colors.white, fontSize: 12.sp, fontWeight: FontWeight.w700),
            ),
          ),
          SizedBox(height: 16.h),
          Wrap(
            spacing: 12.w,
            runSpacing: 12.h,
            children: AppColors.brandPresets.map((c) {
              final isSelected = _selected?.toARGB32() == c.toARGB32();
              return GestureDetector(
                onTap: () => setState(() => _selected = c),
                child: Container(
                  width: 38.r,
                  height: 38.r,
                  decoration: BoxDecoration(
                    color: c,
                    shape: BoxShape.circle,
                    border: Border.all(
                      color: isSelected ? AppColors.textPrimary : Colors.transparent,
                      width: 2.5,
                    ),
                  ),
                  child: isSelected ? const Icon(Icons.check_rounded, color: Colors.white, size: 18) : null,
                ),
              );
            }).toList(),
          ),
          SizedBox(height: 16.h),
          Row(
            children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: _saving ? null : () => setState(() => _selected = null),
                  child: const Text('Reset to default'),
                ),
              ),
              SizedBox(width: 12.w),
              Expanded(
                child: ElevatedButton(
                  onPressed: _saving ? null : _save,
                  child: _saving
                      ? SizedBox(
                          width: 18.r,
                          height: 18.r,
                          child: const CircularProgressIndicator(strokeWidth: 2.2, color: Colors.white),
                        )
                      : const Text('Save'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
