import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';

String s(dynamic v) => v?.toString() ?? '';
int n(dynamic v) => v is num ? v.toInt() : int.tryParse(s(v)) ?? 0;
double dbl(dynamic v) => v is num ? v.toDouble() : double.tryParse(s(v)) ?? 0;
String tDate(dynamic v) {
  final d = DateTime.tryParse(s(v));
  return d == null ? '—' : Fmt.date(d);
}

String tMoney(dynamic v) => Fmt.money(dbl(v));
String errText(Object e) => e is ApiException ? e.message : e.toString().replaceFirst('Exception: ', '');

/// Status → (label, text colour, background) exactly as the web portal words and colours them.
(String, Color, Color) trStatus(String status) {
  const amber = (Color(0xFF92400E), Color(0xFFFEF3C7));
  const green = (Color(0xFF065F46), Color(0xFFD1FAE5));
  const red = (Color(0xFF991B1B), Color(0xFFFEE2E2));
  const blue = (Color(0xFF1D4ED8), Color(0xFFDBEAFE));
  const purple = (Color(0xFF6D28D9), Color(0xFFEDE9FE));
  const gray = (Color(0xFF475569), Color(0xFFE2E8F0));
  const indigo = (Color(0xFF4338CA), Color(0xFFE0E7FF));
  (String, (Color, Color)) r;
  switch (status) {
    case 'AwaitingHealthAssessment': r = ('Awaiting health check', amber);
    case 'HealthFailed': r = ('Health check failed', red);
    case 'HealthPassed': r = ('Health passed – invoice due', green);
    case 'AwaitingPayment': r = ('Awaiting payment', amber);
    case 'AwaitingHandler': r = ('Paid – awaiting handler', purple);
    case 'Ready': r = ('Ready to start', blue);
    case 'InTraining': r = ('In training', green);
    case 'AwaitingFinalAssessment': r = ('Awaiting final assessment', purple);
    case 'AwaitingApproval': r = ('Awaiting approval', indigo);
    case 'Completed': r = ('Completed', green);
    case 'Cancelled': r = ('Cancelled', gray);
    case 'Passed': r = ('Passed', green);
    case 'Failed': r = ('Failed', red);
    case 'Pending': r = ('Pending', amber);
    case 'RequiresFurtherExamination': r = ('Further examination', amber);
    case 'RequiresAdditionalTraining': r = ('More training needed', amber);
    case 'Approved': r = ('Approved', green);
    case 'Submitted': r = ('Submitted', blue);
    case 'Draft': r = ('Draft', gray);
    case 'Returned': r = ('Returned', amber);
    case 'CONFIRMED': r = ('Confirmed', green);
    case 'PENDING': r = ('Pending', amber);
    case 'REJECTED': r = ('Rejected', red);
    case 'PAID': r = ('Paid', green);
    case 'PARTIAL': r = ('Part paid', blue);
    case 'SENT': r = ('Unpaid', amber);
    case 'CANCELLED': r = ('Cancelled', gray);
    default: r = (status, gray);
  }
  return (r.$1, r.$2.$1, r.$2.$2);
}

class TrPill extends StatelessWidget {
  final String status;
  const TrPill(this.status, {super.key});

  @override
  Widget build(BuildContext context) {
    final (label, fg, bg) = trStatus(status);
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(20.r)),
      child: Text(label, style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: fg)),
    );
  }
}

class DogAvatar extends StatelessWidget {
  final String? url;
  final String name;
  final double size;
  const DogAvatar({super.key, this.url, required this.name, this.size = 48});

  @override
  Widget build(BuildContext context) {
    final r = size.r;
    final placeholder = Container(
      width: r,
      height: r,
      decoration: BoxDecoration(color: AppColors.surfaceAlt, borderRadius: BorderRadius.circular(r * 0.28)),
      child: Icon(Icons.pets_rounded, size: r * 0.5, color: AppColors.textSecondary),
    );
    if (url == null || url!.isEmpty) return placeholder;
    return ClipRRect(
      borderRadius: BorderRadius.circular(r * 0.28),
      child: CachedNetworkImage(imageUrl: url!, width: r, height: r, fit: BoxFit.cover, errorWidget: (_, __, ___) => placeholder, placeholder: (_, __) => placeholder),
    );
  }
}

/// White rounded card used across the training screens.
class TrCard extends StatelessWidget {
  final Widget child;
  final VoidCallback? onTap;
  final EdgeInsetsGeometry? padding;
  final Color? border;
  const TrCard({super.key, required this.child, this.onTap, this.padding, this.border});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(16.r),
      child: InkWell(
        borderRadius: BorderRadius.circular(16.r),
        onTap: onTap,
        child: Container(
          padding: padding ?? EdgeInsets.all(14.w),
          decoration: BoxDecoration(borderRadius: BorderRadius.circular(16.r), border: Border.all(color: border ?? AppColors.border)),
          child: child,
        ),
      ),
    );
  }
}

/// Label above a value, like the web's key/value tiles.
class KV extends StatelessWidget {
  final String k;
  final String v;
  const KV(this.k, this.v, {super.key});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 6.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(k.toUpperCase(), style: TextStyle(fontSize: 10.sp, letterSpacing: 0.8, fontWeight: FontWeight.w700, color: AppColors.textMuted)),
          SizedBox(height: 2.h),
          Text(v.trim().isEmpty ? '—' : v, style: TextStyle(fontSize: 14.sp, color: AppColors.textPrimary)),
        ],
      ),
    );
  }
}

class TrEmpty extends StatelessWidget {
  final IconData icon;
  final String text;
  const TrEmpty(this.icon, this.text, {super.key});

  @override
  Widget build(BuildContext context) => Padding(
        padding: EdgeInsets.symmetric(vertical: 36.h, horizontal: 24.w),
        child: Column(children: [
          Icon(icon, size: 40.r, color: AppColors.textMuted),
          SizedBox(height: 10.h),
          Text(text, textAlign: TextAlign.center, style: TextStyle(fontSize: 13.sp, color: AppColors.textMuted)),
        ]),
      );
}

void trToast(BuildContext context, String msg, {bool error = false}) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(msg), backgroundColor: error ? AppColors.danger : null, behavior: SnackBarBehavior.floating));
}

/// Bottom sheet with a title, a body and a primary action - used for the small forms (invoice, extend, cancel ...).
Future<T?> trSheet<T>(BuildContext context, {required String title, required Widget Function(BuildContext) body}) {
  return showModalBottomSheet<T>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    showDragHandle: true,
    builder: (ctx) => Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.of(ctx).viewInsets.bottom),
      child: SingleChildScrollView(
        padding: EdgeInsets.fromLTRB(20.w, 0, 20.w, 20.h),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: TextStyle(fontSize: 17.sp, fontWeight: FontWeight.w800)),
            SizedBox(height: 12.h),
            body(ctx),
          ],
        ),
      ),
    ),
  );
}

class TrProgress extends StatelessWidget {
  final double value;
  final Color color;
  const TrProgress(this.value, {super.key, this.color = const Color(0xFF059669)});

  @override
  Widget build(BuildContext context) => ClipRRect(
        borderRadius: BorderRadius.circular(6.r),
        child: LinearProgressIndicator(value: value.clamp(0, 1), minHeight: 7.h, backgroundColor: AppColors.surfaceAlt, color: color),
      );
}
