import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../auth/providers/auth_provider.dart';
import '../../business/data/business_repository.dart';
import '../../business/screens/customer_detail_screen.dart';
import '../../business/screens/invoice_detail_screen.dart';
import '../../dogs/screens/dog_profile_screen.dart';
import '../data/search_repository.dart';
import '../models/search_result.dart';

/// Cross-entity search — dogs, customers, and invoices in one box.
class GlobalSearchScreen extends StatefulWidget {
  const GlobalSearchScreen({super.key});

  @override
  State<GlobalSearchScreen> createState() => _GlobalSearchScreenState();
}

class _GlobalSearchScreenState extends State<GlobalSearchScreen> {
  final _repo = SearchRepository();
  final _businessRepo = BusinessRepository();
  final _controller = TextEditingController();
  Timer? _debounce;
  SearchResults? _results;
  bool _loading = false;
  bool _resolving = false;

  @override
  void dispose() {
    _debounce?.cancel();
    _controller.dispose();
    super.dispose();
  }

  void _onChanged(String value) {
    _debounce?.cancel();
    final q = value.trim();
    if (q.length < 2) {
      setState(() => _results = null);
      return;
    }
    _debounce = Timer(const Duration(milliseconds: 350), () => _search(q));
  }

  Future<void> _search(String q) async {
    setState(() => _loading = true);
    try {
      final companyId = context.read<AuthProvider>().companyId;
      final results = await _repo.search(companyId, q);
      if (mounted) setState(() => _results = results);
    } catch (_) {
      if (mounted) setState(() => _results = null);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _openDog(SearchResultItem item) async {
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => DogProfileScreen(dogId: item.id)),
    );
  }

  Future<void> _openCustomer(SearchResultItem item) async {
    setState(() => _resolving = true);
    try {
      final companyId = context.read<AuthProvider>().companyId;
      final customers = await _businessRepo.getCustomers(companyId);
      final matches = customers.where((c) => c.id == item.id);
      final match = matches.isEmpty ? null : matches.first;
      if (match != null && mounted) {
        await Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => CustomerDetailScreen(customer: match)),
        );
      }
    } finally {
      if (mounted) setState(() => _resolving = false);
    }
  }

  Future<void> _openInvoice(SearchResultItem item) async {
    setState(() => _resolving = true);
    try {
      final companyId = context.read<AuthProvider>().companyId;
      final invoices = await _businessRepo.getInvoices(companyId);
      final matches = invoices.where((i) => i.id == item.id);
      final match = matches.isEmpty ? null : matches.first;
      if (match != null && mounted) {
        await Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => InvoiceDetailScreen(invoice: match)),
        );
      }
    } finally {
      if (mounted) setState(() => _resolving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: TextField(
          controller: _controller,
          autofocus: true,
          onChanged: _onChanged,
          decoration: const InputDecoration(
            border: InputBorder.none,
            hintText: 'Search dogs, customers, invoices...',
          ),
        ),
      ),
      body: SafeArea(
        top: false,
        child: Stack(
          children: [
            _buildBody(),
            if (_resolving)
              Container(
                color: Colors.black26,
                child: const Center(child: CircularProgressIndicator()),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    final results = _results;
    if (results == null) {
      return Center(
        child: Text('Type at least 2 characters to search', style: TextStyle(color: AppColors.textMuted, fontSize: 13.sp)),
      );
    }
    if (results.isEmpty) {
      return Center(
        child: Text('No matches found', style: TextStyle(color: AppColors.textMuted, fontSize: 13.sp)),
      );
    }
    return ListView(
      padding: EdgeInsets.symmetric(vertical: 8.h),
      children: [
        _group('Dogs', Icons.pets_rounded, results.dogs, _openDog),
        _group('Customers', Icons.person_rounded, results.customers, _openCustomer),
        _group('Invoices', Icons.receipt_long_rounded, results.invoices, _openInvoice),
      ],
    );
  }

  Widget _group(String label, IconData icon, List<SearchResultItem> items, void Function(SearchResultItem) onTap) {
    if (items.isEmpty) return const SizedBox.shrink();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 4.h),
          child: Text(label.toUpperCase(),
              style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.w700, color: AppColors.textMuted, letterSpacing: .5)),
        ),
        for (final item in items)
          ListTile(
            leading: Icon(icon, color: AppColors.primary, size: 20.r),
            title: Text(item.title, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600)),
            subtitle: item.subtitle != null ? Text(item.subtitle!, style: TextStyle(fontSize: 12.sp)) : null,
            trailing: Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 20.r),
            onTap: () => onTap(item),
          ),
      ],
    );
  }
}
