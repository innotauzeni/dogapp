import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../core/responsive/responsive.dart';
import '../../core/theme/app_colors.dart';
import '../../shared/widgets/app_drawer.dart';
import '../auth/providers/auth_provider.dart';
import '../business/screens/business_screen.dart';
import '../breeding/screens/breeding_screen.dart';
import '../dashboard/screens/dashboard_screen.dart';
import '../dogs/screens/dogs_list_screen.dart';
import '../dogs/screens/handler_activities_screen.dart';
import '../dogs/screens/handler_dogs_screen.dart';
import '../profile/screens/profile_screen.dart';

/// Root authenticated shell. The visible tabs adapt to the signed-in user's
/// role and permissions, so a Dog Handler sees their assigned-dogs page and
/// only the sections they may use.
class HomeShell extends StatefulWidget {
  const HomeShell({super.key});

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int _index = 0;

  List<_TabDef> _tabsFor(AuthProvider auth) {
    // If permissions somehow failed to load, don't hide everything — show all.
    bool gate(String p) => auth.permissions.isEmpty || auth.can(p);

    final tabs = <_TabDef>[
      _TabDef('Home', Icons.dashboard_outlined, Icons.dashboard_rounded, const DashboardScreen()),
    ];
    if (auth.isDogHandler) {
      tabs.add(_TabDef('My Dogs', Icons.assignment_ind_outlined, Icons.assignment_ind_rounded,
          const HandlerDogsScreen()));
      tabs.add(_TabDef('Activities', Icons.history_rounded, Icons.history_rounded,
          const HandlerActivitiesScreen()));
    }
    if (gate('pet.view')) {
      tabs.add(_TabDef('Dogs', Icons.pets_outlined, Icons.pets_rounded, const DogsListScreen()));
      tabs.add(_TabDef('Breeding', Icons.favorite_outline_rounded, Icons.favorite_rounded,
          const BreedingScreen()));
    }
    if (gate('invoice.view') || gate('customer.view') || gate('expense.view')) {
      tabs.add(_TabDef('Business', Icons.receipt_long_outlined, Icons.receipt_long_rounded,
          const BusinessScreen()));
    }
    tabs.add(_TabDef('Profile', Icons.person_outline_rounded, Icons.person_rounded, const ProfileScreen()));
    return tabs;
  }

  void _onTap(int i) => setState(() => _index = i);

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final tabs = _tabsFor(auth);
    if (_index >= tabs.length) _index = tabs.length - 1;

    final useRail = context.isTablet;
    final body = IndexedStack(index: _index, children: [for (final t in tabs) t.page]);

    if (useRail) {
      return Scaffold(
        body: Row(
          children: [
            NavigationRail(
              selectedIndex: _index,
              onDestinationSelected: _onTap,
              labelType: NavigationRailLabelType.all,
              backgroundColor: AppColors.surface,
              destinations: [
                for (final t in tabs)
                  NavigationRailDestination(
                    icon: Icon(t.icon),
                    selectedIcon: Icon(t.activeIcon),
                    label: Text(t.label),
                  ),
              ],
            ),
            const VerticalDivider(width: 1),
            Expanded(child: body),
          ],
        ),
      );
    }

    return Scaffold(
      key: ShellDrawer.key,
      body: body,
      drawer: AppDrawer(
        currentIndex: _index,
        onItemSelected: _onTap,
        items: [
          for (final t in tabs)
            DrawerItem(
              label: t.label,
              icon: t.icon,
              activeIcon: t.activeIcon,
            ),
        ],
      ),
      bottomNavigationBar: Container(
        decoration: const BoxDecoration(
          border: Border(top: BorderSide(color: AppColors.border)),
        ),
        child: BottomNavigationBar(
          currentIndex: _index,
          onTap: _onTap,
          type: BottomNavigationBarType.fixed,
          selectedFontSize: 11.sp,
          unselectedFontSize: 11.sp,
          iconSize: 24.r,
          items: [
            for (final t in tabs)
              BottomNavigationBarItem(
                icon: Icon(t.icon),
                activeIcon: Icon(t.activeIcon),
                label: t.label,
              ),
          ],
        ),
      ),
    );
  }
}

class _TabDef {
  final String label;
  final IconData icon;
  final IconData activeIcon;
  final Widget page;
  const _TabDef(this.label, this.icon, this.activeIcon, this.page);
}
