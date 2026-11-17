import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/dogs_repository.dart';
import '../models/animal.dart';
import '../widgets/dog_avatar.dart';

/// Full-screen searchable dog picker. Returns the chosen [Animal] via pop.
class DogPickerScreen extends StatefulWidget {
  final String title;

  /// Optionally hide dogs already owned by this customer id.
  final int? excludeOwnerId;

  const DogPickerScreen({super.key, this.title = 'Select Dog', this.excludeOwnerId});

  @override
  State<DogPickerScreen> createState() => _DogPickerScreenState();
}

class _DogPickerScreenState extends State<DogPickerScreen> {
  final _repo = DogsRepository();
  Future<List<Animal>>? _future;
  String _query = '';

  @override
  void initState() {
    super.initState();
    final companyId = context.read<AuthProvider>().companyId;
    _future = _repo.getPets(companyId);
  }

  List<Animal> _filter(List<Animal> dogs) {
    var list = dogs;
    if (widget.excludeOwnerId != null) {
      list = list.where((d) => d.customerId != widget.excludeOwnerId).toList();
    }
    if (_query.trim().isEmpty) return list;
    final q = _query.toLowerCase();
    return list
        .where((d) => d.name.toLowerCase().contains(q) || d.breed.toLowerCase().contains(q))
        .toList();
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
                  hintText: 'Search dogs',
                  prefixIcon: Icon(Icons.search_rounded, size: 20.r, color: AppColors.textMuted),
                ),
              ),
            ),
            Expanded(
              child: AsyncView<List<Animal>>(
                future: _future,
                builder: (context, dogs) {
                  final filtered = _filter(dogs);
                  if (filtered.isEmpty) {
                    return StatusView.empty(title: 'No dogs available', icon: Icons.pets_rounded);
                  }
                  return ListView.separated(
                    padding: EdgeInsets.fromLTRB(16.w, 4.h, 16.w, 24.h),
                    itemCount: filtered.length,
                    separatorBuilder: (context, index) => Divider(height: 1, color: AppColors.border),
                    itemBuilder: (context, i) {
                      final d = filtered[i];
                      return ListTile(
                        contentPadding: EdgeInsets.symmetric(horizontal: 4.w, vertical: 4.h),
                        leading: DogAvatar(photoUrl: d.photoUrl, size: 44),
                        title: Text(d.name, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                        subtitle: Text(
                          d.customerName != null && d.customerName!.isNotEmpty
                              ? '${d.breed} • Owner: ${d.customerName}'
                              : d.breed,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary),
                        ),
                        trailing: Icon(Icons.add_circle_outline_rounded, color: AppColors.primary, size: 22.r),
                        onTap: () => Navigator.of(context).pop(d),
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
