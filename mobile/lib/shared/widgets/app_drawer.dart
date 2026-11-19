import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../core/responsive/responsive.dart';
import '../../core/theme/app_colors.dart';
import '../../features/auth/providers/auth_provider.dart';
import '../../features/settings/screens/settings_screen.dart';
import '../../features/training/screens/training_dogs_screen.dart';

/// Lets any tab screen open the shell's navigation drawer.
class ShellDrawer {
  ShellDrawer._();
  static final GlobalKey<ScaffoldState> key = GlobalKey<ScaffoldState>();

  /// The drawer exists on phones; tablets use the navigation rail instead.
  static bool availableFor(BuildContext context) => !context.isTablet;
  static void open() => key.currentState?.openDrawer();
}

/// App-bar leading button: a back arrow on pushed screens, the menu icon on the main tabs
/// (and nothing on tablets, where the navigation rail is always visible).
class MenuButton extends StatelessWidget {
  const MenuButton({super.key});

  @override
  Widget build(BuildContext context) {
    if (Navigator.of(context).canPop()) return const BackButton();
    if (!ShellDrawer.availableFor(context)) return const SizedBox.shrink();
    return IconButton(
      icon: const Icon(Icons.menu_rounded),
      tooltip: 'Menu',
      onPressed: ShellDrawer.open,
    );
  }
}

class AppDrawer extends StatelessWidget {
  final int currentIndex;
  final Function(int) onItemSelected;
  final List<DrawerItem> items;

  const AppDrawer({
    super.key,
    required this.currentIndex,
    required this.onItemSelected,
    required this.items,
  });

  void _push(BuildContext context, Widget page) {
    Navigator.pop(context);
    Navigator.of(context).push(MaterialPageRoute(builder: (_) => page));
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final user = auth.user;
    // Uses the company's brand colour when set, otherwise the default charcoal grey.
    final brand = auth.brandColor ?? AppColors.primary;
    final brandDark = Color.lerp(brand, Colors.black, 0.45)!;
    final name = (user?.name ?? 'User').trim();
    final role = auth.isDogHandler ? 'Dog Handler' : (user?.role ?? '');
    final canTraining = auth.isDogHandler || auth.permissions.isEmpty || auth.can('training.log.view') || auth.can('training.dashboard.view');

    return Drawer(
      width: 304.w.clamp(280, 340).toDouble(),
      backgroundColor: AppColors.surface,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.horizontal(right: Radius.circular(24))),
      child: Column(
        children: [
          // ── Header ──
          Container(
            width: double.infinity,
            padding: EdgeInsets.fromLTRB(20.w, MediaQuery.of(context).padding.top + 22.h, 20.w, 20.h),
            decoration: BoxDecoration(
              gradient: LinearGradient(begin: Alignment.topLeft, end: Alignment.bottomRight, colors: [brandDark, brand]),
              borderRadius: const BorderRadius.only(bottomRight: Radius.circular(28)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      width: 52.r,
                      height: 52.r,
                      padding: EdgeInsets.all(5.r),
                      decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(14.r)),
                      child: Image.asset('assets/logo_mark.png', fit: BoxFit.contain),
                    ),
                    SizedBox(width: 12.w),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Black Eagle', style: TextStyle(fontSize: 17.sp, fontWeight: FontWeight.w800, color: Colors.white)),
                          Text('Dog Management', style: TextStyle(fontSize: 11.sp, color: Colors.white70)),
                        ],
                      ),
                    ),
                  ],
                ),
                SizedBox(height: 18.h),
                Row(
                  children: [
                    CircleAvatar(
                      radius: 20.r,
                      backgroundColor: Colors.white24,
                      child: Text(name.isEmpty ? 'U' : name[0].toUpperCase(),
                          style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800, color: Colors.white)),
                    ),
                    SizedBox(width: 10.w),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(name, maxLines: 1, overflow: TextOverflow.ellipsis,
                              style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700, color: Colors.white)),
                          if ((user?.email ?? '').isNotEmpty)
                            Text(user!.email, maxLines: 1, overflow: TextOverflow.ellipsis,
                                style: TextStyle(fontSize: 11.sp, color: Colors.white70)),
                        ],
                      ),
                    ),
                  ],
                ),
                if (role.isNotEmpty) ...[
                  SizedBox(height: 10.h),
                  Container(
                    padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 3.h),
                    decoration: BoxDecoration(color: Colors.white24, borderRadius: BorderRadius.circular(20.r)),
                    child: Text(role, style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: Colors.white)),
                  ),
                ],
              ],
            ),
          ),

          // ── Menu ──
          Expanded(
            child: ListView(
              padding: EdgeInsets.fromLTRB(12.w, 14.h, 12.w, 8.h),
              children: [
                _section('MENU'),
                for (var i = 0; i < items.length; i++)
                  _tile(
                    icon: currentIndex == i ? items[i].activeIcon : items[i].icon,
                    label: items[i].label,
                    selected: currentIndex == i,
                    brand: brand,
                    onTap: () {
                      onItemSelected(i);
                      Navigator.pop(context);
                    },
                  ),
                SizedBox(height: 8.h),
                _section('MORE'),
                if (canTraining)
                  _tile(
                    icon: Icons.school_outlined,
                    label: 'Dog Training',
                    selected: false,
                    brand: brand,
                    onTap: () => _push(context, const TrainingDogsScreen()),
                  ),
                _tile(
                  icon: Icons.settings_outlined,
                  label: 'Settings & About',
                  selected: false,
                  brand: brand,
                  onTap: () => _push(context, const SettingsScreen()),
                ),
              ],
            ),
          ),

          // ── Footer ──
          Container(
            padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 12.h + MediaQuery.of(context).padding.bottom),
            decoration: const BoxDecoration(border: Border(top: BorderSide(color: AppColors.border))),
            child: Row(
              children: [
                Expanded(
                  child: TextButton.icon(
                    style: TextButton.styleFrom(
                      foregroundColor: AppColors.danger,
                      backgroundColor: AppColors.dangerLight,
                      padding: EdgeInsets.symmetric(vertical: 12.h),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                    ),
                    icon: Icon(Icons.logout_rounded, size: 18.r),
                    label: Text('Sign out', style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
                    onPressed: () {
                      Navigator.pop(context);
                      auth.logout();
                    },
                  ),
                ),
                SizedBox(width: 12.w),
                Text('v1.1.0', style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _section(String text) => Padding(
        padding: EdgeInsets.fromLTRB(10.w, 4.h, 10.w, 6.h),
        child: Text(text, style: TextStyle(fontSize: 10.sp, letterSpacing: 1.1, fontWeight: FontWeight.w800, color: AppColors.textMuted)),
      );

  Widget _tile({required IconData icon, required String label, required bool selected, required Color brand, required VoidCallback onTap}) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 2.h),
      child: Material(
        color: selected ? brand.withValues(alpha: 0.10) : Colors.transparent,
        borderRadius: BorderRadius.circular(12.r),
        child: InkWell(
          borderRadius: BorderRadius.circular(12.r),
          onTap: onTap,
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
            child: Row(
              children: [
                Icon(icon, size: 22.r, color: selected ? brand : AppColors.textSecondary),
                SizedBox(width: 14.w),
                Expanded(
                  child: Text(label,
                      style: TextStyle(
                        fontSize: 14.sp,
                        fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
                        color: selected ? brand : AppColors.textPrimary,
                      )),
                ),
                if (selected) Container(width: 6.r, height: 6.r, decoration: BoxDecoration(color: brand, shape: BoxShape.circle)),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class DrawerItem {
  final String label;
  final IconData icon;
  final IconData activeIcon;

  DrawerItem({required this.label, required this.icon, required this.activeIcon});
}
