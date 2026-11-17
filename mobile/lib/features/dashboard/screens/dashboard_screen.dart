import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/responsive/responsive.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/cards.dart';
import '../../auth/providers/auth_provider.dart';
import '../../dogs/data/dogs_repository.dart';
import '../../dogs/models/animal.dart';
import '../../dogs/screens/dog_profile_screen.dart';
import '../../dogs/screens/dogs_list_screen.dart';
import '../../dogs/screens/handler_dashboard_screen.dart';
import '../../breeding/screens/breeding_screen.dart';
import '../../analytics/screens/analytics_screen.dart';
import '../../search/screens/global_search_screen.dart';

class _DashboardData {
  final List<Animal> pets;
  final int vaccinated;
  final int litters;
  final int mating;
  final List<VaccineDue> vaccineDue;
  _DashboardData(this.pets, this.vaccinated, this.litters, this.mating, this.vaccineDue);

  int get total => pets.length;
  int get deployed => pets.where((p) => p.isDeployed).length;
  int get active => pets.where((p) => p.isActive).length;
}

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  final _repo = DogsRepository();
  Future<_DashboardData>? _future;
  int _loadedCompany = -1;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    final companyId = context.read<AuthProvider>().activeCompanyId;
    _loadedCompany = companyId;
    _future = _fetch(companyId);
  }

  Future<_DashboardData> _fetch(int companyId) async {
    // Counts are best-effort; if a report endpoint is unavailable we still show
    // the headline figures derived from the pets list.
    final pets = await _repo.getPets(companyId);
    Future<int> safe(Future<int> f) => f.catchError((_) => 0);
    final results = await Future.wait([
      safe(_repo.vaccinatedCount(companyId)),
      safe(_repo.littersCount(companyId)),
      safe(_repo.matingCount(companyId)),
    ]);
    final vaccineDue = await _repo.getVaccineDue(companyId).catchError((_) => <VaccineDue>[]);
    return _DashboardData(pets, results[0], results[1], results[2], vaccineDue);
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    // Show handler dashboard if user is a dog handler
    if (auth.isDogHandler) {
      return const HandlerDashboardScreen();
    }

    // Reload when the head-office user switches branch context.
    if (auth.activeCompanyId != _loadedCompany) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) setState(_load);
      });
    }
    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: FutureBuilder<_DashboardData>(
            future: _future,
            builder: (context, snapshot) {
              final loading = snapshot.connectionState == ConnectionState.waiting;
              final data = snapshot.data;
              return CustomScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                slivers: [
                  SliverToBoxAdapter(child: _Greeting(name: auth.user?.name ?? 'there')),
                  SliverPadding(
                    padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 8.h),
                    sliver: SliverToBoxAdapter(
                      child: _Stats(loading: loading, data: data),
                    ),
                  ),
                  if (data != null && data.vaccineDue.isNotEmpty)
                    SliverPadding(
                      padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 24.h),
                      sliver: SliverToBoxAdapter(
                        child: _VaccineDueSection(items: data.vaccineDue),
                      ),
                    ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}

class _Greeting extends StatelessWidget {
  final String name;
  const _Greeting({required this.name});

  String get _salutation {
    final h = DateTime.now().hour;
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  @override
  Widget build(BuildContext context) {
    // Falls back to the default charcoal-grey gradient when the company hasn't
    // set a custom brand color — never left uncolored.
    final brand = context.watch<AuthProvider>().brandColor;
    final gradientColors = brand == null
        ? AppColors.brandGradient
        : [
            Color.lerp(brand, Colors.black, 0.35)!,
            brand,
            Color.lerp(brand, Colors.white, 0.25)!,
          ];

    return Container(
      width: double.infinity,
      padding: EdgeInsets.fromLTRB(20.w, 20.h, 20.w, 28.h),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: gradientColors,
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: const BorderRadius.vertical(bottom: Radius.circular(24)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(_salutation, style: TextStyle(color: Colors.white70, fontSize: 13.sp)),
                    SizedBox(height: 4.h),
                    Text(
                      name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: Colors.white, fontSize: 24.sp, fontWeight: FontWeight.w800),
                    ),
                  ],
                ),
              ),
              InkWell(
                borderRadius: BorderRadius.circular(12.r),
                onTap: () => Navigator.of(context).push(
                  MaterialPageRoute(builder: (_) => const AnalyticsScreen()),
                ),
                child: Container(
                  padding: EdgeInsets.all(10.w),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.18),
                    borderRadius: BorderRadius.circular(12.r),
                  ),
                  child: Icon(Icons.insights_rounded, color: Colors.white, size: 20.r),
                ),
              ),
              SizedBox(width: 10.w),
              InkWell(
                borderRadius: BorderRadius.circular(12.r),
                onTap: () => Navigator.of(context).push(
                  MaterialPageRoute(builder: (_) => const GlobalSearchScreen()),
                ),
                child: Container(
                  padding: EdgeInsets.all(10.w),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.18),
                    borderRadius: BorderRadius.circular(12.r),
                  ),
                  child: Icon(Icons.search_rounded, color: Colors.white, size: 20.r),
                ),
              ),
            ],
          ),
          SizedBox(height: 14.h),
          Container(
            padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 8.h),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.18),
              borderRadius: BorderRadius.circular(12.r),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.shield_outlined, color: Colors.white, size: 16.r),
                SizedBox(width: 8.w),
                Text('Kennel & Operations Overview',
                    style: TextStyle(color: Colors.white, fontSize: 12.sp, fontWeight: FontWeight.w600)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Stats extends StatelessWidget {
  final bool loading;
  final _DashboardData? data;
  const _Stats({required this.loading, required this.data});

  @override
  Widget build(BuildContext context) {
    final columns = context.gridColumns(minTileWidth: 150, max: 4);
    final tiles = <Widget>[
      _tile(context, 'Total Dogs', data?.total, Icons.pets_rounded, AppColors.primary),
      _tile(context, 'Active', data?.active, Icons.check_circle_outline_rounded, AppColors.success),
      _tile(context, 'Deployed', data?.deployed, Icons.location_on_outlined, AppColors.info),
      _tile(context, 'Vaccinated', data?.vaccinated, Icons.vaccines_outlined, AppColors.accent),
      _tile(context, 'Litters', data?.litters, Icons.child_care_rounded, AppColors.warning),
      _tile(context, 'Matings', data?.mating, Icons.favorite_rounded, AppColors.danger),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SectionHeader(title: 'At a glance'),
        GridView.count(
          crossAxisCount: columns,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 12.h,
          crossAxisSpacing: 12.w,
          childAspectRatio: 1.05,
          children: tiles,
        ),
      ],
    );
  }

  Widget _tile(BuildContext context, String label, int? value, IconData icon, Color color) {
    return GestureDetector(
      onTap: () => _navigate(context, label),
      child: StatCard(
        label: label,
        value: loading || value == null ? '—' : '$value',
        icon: icon,
        color: color,
      ),
    );
  }

  void _navigate(BuildContext context, String label) {
    late Widget screen;

    switch (label) {
      case 'Total Dogs':
        screen = const DogsListScreen();
      case 'Active':
        screen = const DogsListScreen(filterType: 'active');
      case 'Deployed':
        screen = const DogsListScreen(filterType: 'deployed');
      case 'Vaccinated':
        screen = const DogsListScreen(filterType: 'vaccinated');
      case 'Litters':
        screen = const BreedingScreen(initialTab: 1);
      case 'Matings':
        screen = const BreedingScreen(initialTab: 0);
      default:
        screen = const DogsListScreen();
    }

    Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen));
  }
}

class _VaccineDueSection extends StatelessWidget {
  final List<VaccineDue> items;
  const _VaccineDueSection({required this.items});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).cardColor,
        borderRadius: BorderRadius.circular(14.r),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: EdgeInsets.fromLTRB(14.w, 12.h, 14.w, 8.h),
            child: Row(
              children: [
                Icon(Icons.vaccines_rounded, size: 18.sp, color: AppColors.primary),
                SizedBox(width: 8.w),
                Text('Vaccinations Due',
                    style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800)),
                const Spacer(),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 2.h),
                  decoration: BoxDecoration(
                    color: AppColors.danger.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(20.r),
                  ),
                  child: Text('${items.length}',
                      style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: AppColors.danger)),
                ),
              ],
            ),
          ),
          ...items.take(8).map((v) {
            final overdue = v.isOverdue;
            final when = overdue ? 'Overdue ${v.daysUntilDue.abs()}d' : 'Due in ${v.daysUntilDue}d';
            final color = overdue ? AppColors.danger : Colors.orange.shade700;
            return InkWell(
              onTap: () => Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => DogProfileScreen(dogId: v.dogId)),
              ),
              child: Padding(
                padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 9.h),
                child: Row(
                  children: [
                    Container(
                      width: 34.w, height: 34.w,
                      decoration: BoxDecoration(
                        color: AppColors.primary.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(9.r),
                      ),
                      child: Icon(Icons.shield_moon_outlined, size: 17.sp, color: AppColors.primary),
                    ),
                    SizedBox(width: 10.w),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(v.dogName, maxLines: 1, overflow: TextOverflow.ellipsis,
                              style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
                          Text(v.vaccineName, maxLines: 1, overflow: TextOverflow.ellipsis,
                              style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                        ],
                      ),
                    ),
                    Text(when,
                        style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: color)),
                  ],
                ),
              ),
            );
          }),
          SizedBox(height: 6.h),
        ],
      ),
    );
  }
}
