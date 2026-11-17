import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/business_repository.dart';
import '../models/business_models.dart';

/// Full-screen searchable customer picker. Returns the chosen [Customer] via pop.
class CustomerPickerScreen extends StatefulWidget {
  final String title;
  const CustomerPickerScreen({super.key, this.title = 'Select Customer'});

  @override
  State<CustomerPickerScreen> createState() => _CustomerPickerScreenState();
}

class _CustomerPickerScreenState extends State<CustomerPickerScreen> {
  final _repo = BusinessRepository();
  Future<List<Customer>>? _future;
  String _query = '';

  @override
  void initState() {
    super.initState();
    final companyId = context.read<AuthProvider>().companyId;
    _future = _repo.getCustomers(companyId);
  }

  List<Customer> _filter(List<Customer> customers) {
    if (_query.trim().isEmpty) return customers;
    final q = _query.toLowerCase();
    return customers.where((c) =>
        c.displayName.toLowerCase().contains(q) ||
        (c.email ?? '').toLowerCase().contains(q) ||
        (c.cellphone ?? '').toLowerCase().contains(q)).toList();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.title)),
      body: SafeArea(
        top: false,
        child: Column(
          children: [
            Padding(
              padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 8.h),
              child: TextField(
                onChanged: (v) => setState(() => _query = v),
                decoration: InputDecoration(
                  hintText: 'Search by name, email or phone',
                  prefixIcon: Icon(Icons.search_rounded, size: 20.r, color: AppColors.textMuted),
                ),
              ),
            ),
            Expanded(
              child: AsyncView<List<Customer>>(
                future: _future,
                builder: (context, customers) {
                  final filtered = _filter(customers);
                  if (filtered.isEmpty) {
                    return StatusView.empty(title: 'No customers found', icon: Icons.people_alt_rounded);
                  }
                  return ListView.separated(
                    padding: EdgeInsets.fromLTRB(16.w, 4.h, 16.w, 24.h),
                    itemCount: filtered.length,
                    separatorBuilder: (context, index) => Divider(height: 1, color: AppColors.border),
                    itemBuilder: (context, i) {
                      final c = filtered[i];
                      final name = c.displayName.isEmpty ? 'Unnamed' : c.displayName;
                      return ListTile(
                        contentPadding: EdgeInsets.symmetric(horizontal: 4.w, vertical: 4.h),
                        leading: CircleAvatar(
                          radius: 20.r,
                          backgroundColor: AppColors.primaryLight,
                          child: Text(name.substring(0, 1).toUpperCase(),
                              style: TextStyle(color: AppColors.primary, fontWeight: FontWeight.w800, fontSize: 14.sp)),
                        ),
                        title: Text(name, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                        subtitle: Text(
                          [c.email, c.cellphone].where((s) => (s ?? '').isNotEmpty).join('  •  '),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary),
                        ),
                        trailing: Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 22.r),
                        onTap: () => Navigator.of(context).pop(c),
                      );
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
