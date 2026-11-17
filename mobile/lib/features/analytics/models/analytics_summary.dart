double _asDouble(dynamic v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
int _asInt(dynamic v) => v is int ? v : int.tryParse('$v') ?? 0;

class MonthlyPoint {
  final String label;
  final double amount;
  MonthlyPoint({required this.label, required this.amount});

  factory MonthlyPoint.fromJson(Map<String, dynamic> j) => MonthlyPoint(
        label: (j['label'] ?? '').toString(),
        amount: _asDouble(j['amount']),
      );
}

class CategoryBreakdown {
  final String category;
  final double amount;
  CategoryBreakdown({required this.category, required this.amount});

  factory CategoryBreakdown.fromJson(Map<String, dynamic> j) => CategoryBreakdown(
        category: (j['category'] ?? '').toString(),
        amount: _asDouble(j['amount']),
      );
}

class HandlerActivityStat {
  final String handlerName;
  final int activityCount;
  HandlerActivityStat({required this.handlerName, required this.activityCount});

  factory HandlerActivityStat.fromJson(Map<String, dynamic> j) => HandlerActivityStat(
        handlerName: (j['handlerName'] ?? '').toString(),
        activityCount: _asInt(j['activityCount']),
      );
}

class AnalyticsSummary {
  final double totalRevenue;
  final double totalOutstanding;
  final double totalExpenses;
  final double netProfit;
  final int totalDogs;
  final int activeDogs;
  final int deployedDogs;
  final int vaccinatedDogs;
  final List<MonthlyPoint> revenueByMonth;
  final List<MonthlyPoint> expensesByMonth;
  final List<CategoryBreakdown> expensesByCategory;
  final List<HandlerActivityStat> handlerActivity;

  AnalyticsSummary({
    required this.totalRevenue,
    required this.totalOutstanding,
    required this.totalExpenses,
    required this.netProfit,
    required this.totalDogs,
    required this.activeDogs,
    required this.deployedDogs,
    required this.vaccinatedDogs,
    this.revenueByMonth = const [],
    this.expensesByMonth = const [],
    this.expensesByCategory = const [],
    this.handlerActivity = const [],
  });

  factory AnalyticsSummary.fromJson(Map<String, dynamic> j) => AnalyticsSummary(
        totalRevenue: _asDouble(j['totalRevenue']),
        totalOutstanding: _asDouble(j['totalOutstanding']),
        totalExpenses: _asDouble(j['totalExpenses']),
        netProfit: _asDouble(j['netProfit']),
        totalDogs: _asInt(j['totalDogs']),
        activeDogs: _asInt(j['activeDogs']),
        deployedDogs: _asInt(j['deployedDogs']),
        vaccinatedDogs: _asInt(j['vaccinatedDogs']),
        revenueByMonth: ((j['revenueByMonth'] as List?) ?? const []).map((e) => MonthlyPoint.fromJson((e as Map).cast<String, dynamic>())).toList(),
        expensesByMonth: ((j['expensesByMonth'] as List?) ?? const []).map((e) => MonthlyPoint.fromJson((e as Map).cast<String, dynamic>())).toList(),
        expensesByCategory: ((j['expensesByCategory'] as List?) ?? const []).map((e) => CategoryBreakdown.fromJson((e as Map).cast<String, dynamic>())).toList(),
        handlerActivity: ((j['handlerActivity'] as List?) ?? const []).map((e) => HandlerActivityStat.fromJson((e as Map).cast<String, dynamic>())).toList(),
      );
}
