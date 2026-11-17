import 'package:intl/intl.dart';

/// Shared date / number formatting helpers.
class Fmt {
  Fmt._();

  static final _date = DateFormat('dd MMM yyyy');
  static final _dateTime = DateFormat('dd MMM yyyy, HH:mm');
  static final _time = DateFormat('HH:mm');

  static String date(DateTime? d) => d == null ? '—' : _date.format(d.toLocal());
  static String dateTime(DateTime? d) => d == null ? '—' : _dateTime.format(d.toLocal());

  /// An event timestamp — date plus the clock time, but only when a time was
  /// actually captured. Records from date-only sources (back-dated entries,
  /// imports) sit on midnight, and a literal "00:00" beside them reads as
  /// "recorded at midnight" rather than "time unknown".
  ///
  /// Mirrors `EventTime.When` on the server so the care timeline reads the same
  /// here, in the portal, and in the WhatsApp bot's confirmations.
  static String eventWhen(DateTime? d) {
    if (d == null) return '—';
    final local = d.toLocal();
    final onlyDate = local.hour == 0 && local.minute == 0 && local.second == 0;
    return onlyDate ? _date.format(local) : '${_date.format(local)} · ${_time.format(local)}';
  }

  static String money(num? value, {String symbol = '\$'}) {
    final f = NumberFormat.currency(symbol: symbol, decimalDigits: 2);
    return f.format(value ?? 0);
  }

  /// Human age, e.g. "2 yr 3 mo" or "5 mo" or "12 d".
  static String age(DateTime? dob, {DateTime? until}) {
    if (dob == null) return '—';
    final end = until ?? DateTime.now();
    if (end.isBefore(dob)) return '—';
    var years = end.year - dob.year;
    var months = end.month - dob.month;
    var days = end.day - dob.day;
    if (days < 0) months -= 1;
    if (months < 0) {
      years -= 1;
      months += 12;
    }
    if (years > 0) return months > 0 ? '$years yr $months mo' : '$years yr';
    if (months > 0) return '$months mo';
    final totalDays = end.difference(dob).inDays;
    return '$totalDays d';
  }
}
