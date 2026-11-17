import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/analytics_repository.dart';
import '../models/analytics_summary.dart';

/// Revenue, expenses, dog utilization and handler performance at a glance —
/// mobile counterpart of the Portal's /Analytics page. Uses simple proportional
/// bars instead of a charting package, so nothing new had to be added to
/// pubspec.yaml.
class AnalyticsScreen extends StatefulWidget {
  const AnalyticsScreen({super.key});

  @override
  State<AnalyticsScreen> createState() => _AnalyticsScreenState();
}

class _AnalyticsScreenState extends State<AnalyticsScreen> {
  final _repo = AnalyticsRepository();
  Future<AnalyticsSummary>? _future;
  int _months = 6;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    final companyId = context.read<AuthProvider>().companyId;
    _future = _repo.getSummary(companyId, months: _months);
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Analytics'),
        actions: [
          PopupMenuButton<int>(
            initialValue: _months,
            onSelected: (v) => setState(() {
              _months = v;
              _load();
            }),
            itemBuilder: (_) => const [
              PopupMenuItem(value: 3, child: Text('Last 3 months')),
              PopupMenuItem(value: 6, child: Text('Last 6 months')),
              PopupMenuItem(value: 12, child: Text('Last 12 months')),
            ],
            child: Padding(
              padding: EdgeInsets.symmetric(horizontal: 12.w),
              child: Row(
                children: [
                  Text('$_months mo', style: TextStyle(fontSize: 13.sp)),
                  const Icon(Icons.arrow_drop_down),
                ],
              ),
            ),
          ),
        ],
      ),
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: AsyncView<AnalyticsSummary>(
            future: _future,
            onRetry: _refresh,
            builder: (context, data) => ListView(
              padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 32.h),
              physics: const AlwaysScrollableScrollPhysics(),
              children: [
                _kpiGrid(data),
                SizedBox(height: 20.h),
                _monthlyTrendCard(data),
                SizedBox(height: 16.h),
                if (data.expensesByCategory.isNotEmpty) ...[
                  _categoryCard(data),
                  SizedBox(height: 16.h),
                ],
                if (data.handlerActivity.isNotEmpty) _handlerActivityCard(data),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _kpiGrid(AnalyticsSummary d) {
    final tiles = [
      _Kpi('Revenue', Fmt.money(d.totalRevenue), Icons.payments_rounded, AppColors.success),
      _Kpi('Outstanding', Fmt.money(d.totalOutstanding), Icons.hourglass_bottom_rounded, AppColors.info),
      _Kpi('Expenses', Fmt.money(d.totalExpenses), Icons.receipt_long_rounded, AppColors.danger),
      _Kpi('Net Profit', Fmt.money(d.netProfit), Icons.trending_up_rounded, d.netProfit >= 0 ? AppColors.success : AppColors.danger),
      _Kpi('Total Dogs', '${d.totalDogs}', Icons.pets_rounded, AppColors.primary),
      _Kpi('Deployed', '${d.deployedDogs}', Icons.location_on_rounded, AppColors.primary),
      _Kpi('Vaccinated', '${d.vaccinatedDogs}', Icons.shield_rounded, AppColors.success),
      _Kpi('Active', '${d.activeDogs}', Icons.check_circle_rounded, AppColors.success),
    ];
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 10.h,
      crossAxisSpacing: 10.w,
      childAspectRatio: 2.1,
      children: [for (final k in tiles) _kpiTile(k)],
    );
  }

  Widget _kpiTile(_Kpi k) {
    return AppCard(
      padding: EdgeInsets.all(12.w),
      child: Row(
        children: [
          Container(
            width: 36.r,
            height: 36.r,
            decoration: BoxDecoration(color: k.color.withValues(alpha: .12), borderRadius: BorderRadius.circular(10.r)),
            child: Icon(k.icon, color: k.color, size: 18.r),
          ),
          SizedBox(width: 10.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(k.label, style: TextStyle(fontSize: 10.sp, color: AppColors.textMuted, fontWeight: FontWeight.w600)),
                SizedBox(height: 2.h),
                FittedBox(
                  fit: BoxFit.scaleDown,
                  alignment: Alignment.centerLeft,
                  child: Text(k.value, style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _monthlyTrendCard(AnalyticsSummary d) {
    final maxVal = [
      ...d.revenueByMonth.map((p) => p.amount),
      ...d.expensesByMonth.map((p) => p.amount),
    ].fold<double>(0, (m, v) => v > m ? v : m);

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionHeader(title: 'Revenue vs Expenses'),
          Row(
            children: [
              _legendDot(AppColors.primary, 'Revenue'),
              SizedBox(width: 14.w),
              _legendDot(AppColors.danger, 'Expenses'),
            ],
          ),
          SizedBox(height: 12.h),
          for (var i = 0; i < d.revenueByMonth.length; i++)
            Padding(
              padding: EdgeInsets.only(bottom: 10.h),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(d.revenueByMonth[i].label, style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted, fontWeight: FontWeight.w600)),
                  SizedBox(height: 4.h),
                  _dualBar(d.revenueByMonth[i].amount, i < d.expensesByMonth.length ? d.expensesByMonth[i].amount : 0, maxVal),
                ],
              ),
            ),
        ],
      ),
    );
  }

  Widget _dualBar(double revenue, double expense, double max) {
    final revPct = max <= 0 ? 0.0 : (revenue / max).clamp(0.0, 1.0);
    final expPct = max <= 0 ? 0.0 : (expense / max).clamp(0.0, 1.0);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _proportionalBar(revPct, AppColors.primary, Fmt.money(revenue)),
        SizedBox(height: 3.h),
        _proportionalBar(expPct, AppColors.danger, Fmt.money(expense)),
      ],
    );
  }

  Widget _proportionalBar(double pct, Color color, String label) {
    return LayoutBuilder(
      builder: (context, constraints) {
        return Stack(
          children: [
            Container(height: 16.h, decoration: BoxDecoration(color: AppColors.border.withValues(alpha: .5), borderRadius: BorderRadius.circular(4.r))),
            Container(
              height: 16.h,
              width: constraints.maxWidth * pct,
              decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(4.r)),
            ),
            Positioned.fill(
              child: Align(
                alignment: Alignment.centerRight,
                child: Padding(
                  padding: EdgeInsets.only(right: 6.w),
                  child: Text(label, style: TextStyle(fontSize: 9.sp, color: AppColors.textSecondary, fontWeight: FontWeight.w600)),
                ),
              ),
            ),
          ],
        );
      },
    );
  }

  Widget _legendDot(Color color, String label) {
    return Row(mainAxisSize: MainAxisSize.min, children: [
      Container(width: 8.r, height: 8.r, decoration: BoxDecoration(color: color, shape: BoxShape.circle)),
      SizedBox(width: 5.w),
      Text(label, style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary)),
    ]);
  }

  Widget _categoryCard(AnalyticsSummary d) {
    final maxVal = d.expensesByCategory.fold<double>(0, (m, c) => c.amount > m ? c.amount : m);
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionHeader(title: 'Expenses by Category'),
          for (final c in d.expensesByCategory)
            Padding(
              padding: EdgeInsets.only(bottom: 10.h),
              child: Row(
                children: [
                  SizedBox(width: 80.w, child: Text(c.category, style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w600))),
                  Expanded(child: _proportionalBar(maxVal <= 0 ? 0 : (c.amount / maxVal).clamp(0.0, 1.0), AppColors.primary, Fmt.money(c.amount))),
                ],
              ),
            ),
        ],
      ),
    );
  }

  Widget _handlerActivityCard(AnalyticsSummary d) {
    final maxCount = d.handlerActivity.fold<int>(0, (m, h) => h.activityCount > m ? h.activityCount : m);
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionHeader(title: 'Handler Activity (30 days)'),
          for (final h in d.handlerActivity)
            Padding(
              padding: EdgeInsets.only(bottom: 10.h),
              child: Row(
                children: [
                  SizedBox(width: 100.w, child: Text(h.handlerName, style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w600), maxLines: 1, overflow: TextOverflow.ellipsis)),
                  Expanded(child: _proportionalBar(maxCount <= 0 ? 0 : h.activityCount / maxCount, AppColors.success, '${h.activityCount}')),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _Kpi {
  final String label;
  final String value;
  final IconData icon;
  final Color color;
  _Kpi(this.label, this.value, this.icon, this.color);
}
