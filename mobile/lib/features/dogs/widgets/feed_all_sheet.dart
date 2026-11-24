import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';

/// "Feed all dogs": one tap records today's feeding for every dog in the kennel (standard portion unless changed),
/// whoever the dogs are allocated to. The server skips dogs already fed today and sends the WhatsApp notices.
Future<void> showFeedAllSheet(BuildContext context) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    showDragHandle: true,
    builder: (_) => const _FeedAllSheet(),
  );
}

class _FeedAllSheet extends StatefulWidget {
  const _FeedAllSheet();

  @override
  State<_FeedAllSheet> createState() => _FeedAllSheetState();
}

class _FeedAllSheetState extends State<_FeedAllSheet> {
  static const _portions = ['Standard portion', 'Double portion', 'Half portion'];
  final _portion = TextEditingController(text: 'Standard portion');
  final _note = TextEditingController();
  bool _includeDeployed = false;
  bool _busy = false;
  String? _error;
  Map<String, dynamic>? _result;

  @override
  void dispose() {
    _portion.dispose();
    _note.dispose();
    super.dispose();
  }

  Future<void> _go() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final data = await ApiClient.instance.post<Map<String, dynamic>>(
        'Animal/FeedAllDogs',
        body: {
          'portion': _portion.text.trim().isEmpty ? 'Standard portion' : _portion.text.trim(),
          'details': _note.text.trim().isEmpty ? null : _note.text.trim(),
          'includeDeployed': _includeDeployed,
          'occurredOn': DateTime.now().toIso8601String(),
        },
        parser: (d) => ((d as Map?) ?? const {}).cast<String, dynamic>(),
      );
      if (mounted) setState(() => _result = data);
    } catch (e) {
      if (mounted) setState(() => _error = e is ApiException ? e.message : 'Could not record the feeding. Check your connection and try again.');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  List<String> _names(String k) => ((_result?[k] as List?) ?? const []).map((e) => '$e').toList();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
      child: SingleChildScrollView(
        padding: EdgeInsets.fromLTRB(20.w, 0, 20.w, 20.h),
        child: _result != null ? _done(_result!) : _form(),
      ),
    );
  }

  Widget _form() => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          const Icon(Icons.restaurant_rounded, color: Color(0xFFD97706)),
          SizedBox(width: 8.w),
          Text('Feed all dogs', style: TextStyle(fontSize: 18.sp, fontWeight: FontWeight.w800)),
        ]),
        SizedBox(height: 6.h),
        Text(
          "Records today's feeding for every dog in the kennel in one go. Dogs that already have today's feeding are left out, and a WhatsApp notice goes out for each dog.",
          style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary),
        ),
        SizedBox(height: 14.h),
        Wrap(spacing: 8.w, runSpacing: 4.h, children: [
          for (final p in _portions) ChoiceChip(label: Text(p), selected: _portion.text == p, onSelected: (_) => setState(() => _portion.text = p)),
        ]),
        SizedBox(height: 10.h),
        TextField(controller: _portion, onChanged: (_) => setState(() {}), decoration: const InputDecoration(labelText: 'Portion (e.g. Standard portion, 2 cups)')),
        SizedBox(height: 10.h),
        TextField(controller: _note, decoration: const InputDecoration(labelText: 'Note (optional)')),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          value: _includeDeployed,
          onChanged: (v) => setState(() => _includeDeployed = v),
          title: const Text('Include dogs out on deployment'),
        ),
        if (_error != null)
          Padding(padding: EdgeInsets.only(bottom: 8.h), child: Text(_error!, style: TextStyle(color: AppColors.danger, fontSize: 12.sp, fontWeight: FontWeight.w600))),
        SizedBox(
          width: double.infinity,
          child: FilledButton.icon(
            style: FilledButton.styleFrom(backgroundColor: const Color(0xFFD97706), minimumSize: Size.fromHeight(48.h)),
            onPressed: _busy ? null : _go,
            icon: _busy ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white)) : const Icon(Icons.check_circle_rounded),
            label: Text(_busy ? 'Recording…' : 'Feed all dogs'),
          ),
        ),
      ]);

  Widget _done(Map<String, dynamic> r) {
    final already = _names('alreadyFed');
    final deployed = _names('deployed');
    final fed = r['fed'] ?? 0;
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Row(children: [
        const Icon(Icons.check_circle_rounded, color: AppColors.success),
        SizedBox(width: 8.w),
        Expanded(child: Text('$fed dog(s) recorded as fed', style: TextStyle(fontSize: 17.sp, fontWeight: FontWeight.w800, color: AppColors.success))),
      ]),
      SizedBox(height: 8.h),
      Text(
        '${r['portion'] ?? 'Standard portion'} · ${already.length} already fed today${deployed.isEmpty ? '' : ' · ${deployed.length} on deployment (not included)'}',
        style: TextStyle(fontSize: 13.sp),
      ),
      if (already.isNotEmpty)
        Padding(padding: EdgeInsets.only(top: 6.h), child: Text('Already fed: ${already.join(', ')}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary))),
      if (r['notifyingWhatsApp'] == true)
        Padding(
          padding: EdgeInsets.only(top: 8.h),
          child: Text('WhatsApp notifications are being sent for each dog — it takes a few minutes.', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
        ),
      SizedBox(height: 14.h),
      SizedBox(width: double.infinity, child: FilledButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Done'))),
    ]);
  }
}
