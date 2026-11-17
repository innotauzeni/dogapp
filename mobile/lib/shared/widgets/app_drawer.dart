import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../core/theme/app_colors.dart';
import '../../features/auth/providers/auth_provider.dart';

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

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final user = auth.user;
    // Falls back to the default charcoal grey when the company hasn't set a
    // custom brand color, so the drawer always inherits the same accent as
    // the rest of the app (AppBar, buttons, dashboard hero) instead of a
    // color that's stuck regardless of what's configured.
    final brand = auth.brandColor ?? AppColors.primary;
    final brandDark = Color.lerp(brand, Colors.black, 0.35)!;

    return Drawer(
      child: SafeArea(
        child: Column(
          children: [
            // Header with user info
            Container(
              padding: EdgeInsets.all(16.w),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [brand, brandDark],
                ),
              ),
              child: Row(
                children: [
                  Container(
                    width: 48.r,
                    height: 48.r,
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(10.r),
                    ),
                    child: Center(
                      child: Text(
                        (user?.name ?? 'U')[0].toUpperCase(),
                        style: TextStyle(fontSize: 20.sp, fontWeight: FontWeight.w800, color: brand),
                      ),
                    ),
                  ),
                  SizedBox(width: 12.w),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          user?.name ?? 'User',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800, color: AppColors.surface),
                        ),
                        if (auth.isDogHandler)
                          Text(
                            '🐾 Dog Handler',
                            style: TextStyle(fontSize: 11.sp, color: AppColors.primaryLight),
                          ),
                      ],
                    ),
                  ),
                ],
              ),
            ),

            SizedBox(height: 8.h),

            // Menu items
            Expanded(
              child: ListView.builder(
                itemCount: items.length,
                itemBuilder: (context, i) {
                  final item = items[i];
                  final isSelected = currentIndex == i;
                  return Padding(
                    padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
                    child: ListTile(
                      leading: Icon(
                        isSelected ? item.activeIcon : item.icon,
                        color: isSelected ? brand : AppColors.textMuted,
                      ),
                      title: Text(
                        item.label,
                        style: TextStyle(
                          fontSize: 13.sp,
                          fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                          color: isSelected ? brand : AppColors.textPrimary,
                        ),
                      ),
                      selected: isSelected,
                      selectedTileColor: brand.withValues(alpha: 0.08),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10.r)),
                      onTap: () {
                        onItemSelected(i);
                        Navigator.pop(context);
                      },
                    ),
                  );
                },
              ),
            ),

            // Footer
            Padding(
              padding: EdgeInsets.all(12.w),
              child: Column(
                children: [
                  Divider(color: AppColors.border),
                  SizedBox(height: 8.h),
                  ListTile(
                    leading: Icon(Icons.logout_rounded, color: AppColors.danger, size: 20.r),
                    title: Text(
                      'Sign Out',
                      style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600, color: AppColors.danger),
                    ),
                    onTap: () {
                      Navigator.pop(context);
                      auth.logout();
                    },
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

class DrawerItem {
  final String label;
  final IconData icon;
  final IconData activeIcon;

  DrawerItem({required this.label, required this.icon, required this.activeIcon});
}
