import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/dogs_repository.dart';
import '../models/animal.dart';
import '../widgets/dog_avatar.dart';

/// Ancestry (up to 3 generations) + offspring for a dog. The whole graph is
/// derived from the company's pets list, so no extra endpoints are needed.
class FamilyTreeScreen extends StatefulWidget {
  final int dogId;
  final String? dogName;
  const FamilyTreeScreen({super.key, required this.dogId, this.dogName});

  @override
  State<FamilyTreeScreen> createState() => _FamilyTreeScreenState();
}

class _FamilyTreeScreenState extends State<FamilyTreeScreen> {
  final _repo = DogsRepository();
  Future<Map<int, Animal>>? _future;

  static const _maxDepth = 3; // parents, grandparents, great-grandparents

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    final companyId = context.read<AuthProvider>().companyId;
    _future = _repo.getPets(companyId).then((dogs) => {for (final d in dogs) d.id: d});
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  void _recenter(int id, String name) {
    Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => FamilyTreeScreen(dogId: id, dogName: name)),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.dogName == null ? 'Family Tree' : '${widget.dogName} — Family Tree')),
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: AsyncView<Map<int, Animal>>(
            future: _future,
            onRetry: _refresh,
            builder: (context, byId) {
              final focal = byId[widget.dogId];
              if (focal == null) {
                return ListView(children: [
                  SizedBox(height: 80.h),
                  StatusView.empty(title: 'Dog not found', message: 'This dog is no longer in the list.', icon: Icons.account_tree_rounded),
                ]);
              }
              return _TreeBody(focal: focal, byId: byId, maxDepth: _maxDepth, onTapDog: _recenter);
            },
          ),
        ),
      ),
    );
  }
}

class _TreeBody extends StatelessWidget {
  final Animal focal;
  final Map<int, Animal> byId;
  final int maxDepth;
  final void Function(int id, String name) onTapDog;

  const _TreeBody({required this.focal, required this.byId, required this.maxDepth, required this.onTapDog});

  /// A resolved ancestor: either a full Animal or just a recorded name.
  List<Widget> _ancestors() {
    final rows = <Widget>[];

    void walk(int? id, String? fallbackName, _Relation relation, int depth) {
      if (depth > maxDepth) return;
      if (id == null && (fallbackName == null || fallbackName.isEmpty)) return;
      final animal = id != null ? byId[id] : null;
      final name = animal?.name ?? fallbackName ?? 'Unknown';
      rows.add(_AncestorRow(
        depth: depth,
        relation: relation,
        name: name,
        breed: animal?.breed,
        photoUrl: animal?.photoUrl,
        onTap: animal == null ? null : () => onTapDog(animal.id, animal.name),
      ));
      if (animal != null && depth < maxDepth) {
        walk(animal.sireId, animal.sireName, _Relation.sire, depth + 1);
        walk(animal.damId, animal.damName, _Relation.dam, depth + 1);
      }
    }

    walk(focal.sireId, focal.sireName, _Relation.sire, 1);
    walk(focal.damId, focal.damName, _Relation.dam, 1);
    return rows;
  }

  List<Animal> _offspring() => byId.values
      .where((d) => d.sireId == focal.id || d.damId == focal.id)
      .toList()
    ..sort((a, b) => (b.dateOfBirth ?? DateTime(0)).compareTo(a.dateOfBirth ?? DateTime(0)));

  @override
  Widget build(BuildContext context) {
    final ancestors = _ancestors();
    final offspring = _offspring();
    final hasParents = focal.sireId != null || focal.damId != null ||
        (focal.sireName ?? '').isNotEmpty || (focal.damName ?? '').isNotEmpty;

    return ListView(
      padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        // Focal dog
        AppCard(
          color: AppColors.primaryLight,
          child: Row(
            children: [
              DogAvatar(photoUrl: focal.photoUrl, size: 60),
              SizedBox(width: 14.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(focal.name,
                        maxLines: 1, overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontSize: 18.sp, fontWeight: FontWeight.w800, color: AppColors.primaryDark)),
                    SizedBox(height: 2.h),
                    Text('${focal.breed} • ${Fmt.age(focal.dateOfBirth)}',
                        maxLines: 1, overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontSize: 12.sp, color: AppColors.primary)),
                  ],
                ),
              ),
            ],
          ),
        ),
        SizedBox(height: 20.h),
        const SectionHeader(title: 'Ancestry'),
        if (!hasParents)
          _emptyNote('No parents recorded for this dog yet.')
        else
          ...ancestors,
        SizedBox(height: 20.h),
        SectionHeader(title: 'Offspring (${offspring.length})'),
        if (offspring.isEmpty)
          _emptyNote('No offspring recorded.')
        else
          ...offspring.map((d) => Padding(
                padding: EdgeInsets.only(bottom: 8.h),
                child: _OffspringRow(dog: d, onTap: () => onTapDog(d.id, d.name)),
              )),
      ],
    );
  }

  Widget _emptyNote(String text) => Padding(
        padding: EdgeInsets.symmetric(vertical: 8.h),
        child: Text(text, style: TextStyle(fontSize: 13.sp, color: AppColors.textMuted)),
      );
}

enum _Relation { sire, dam }

class _AncestorRow extends StatelessWidget {
  final int depth; // 1 = parent, 2 = grandparent, ...
  final _Relation relation;
  final String name;
  final String? breed;
  final String? photoUrl;
  final VoidCallback? onTap;

  const _AncestorRow({
    required this.depth,
    required this.relation,
    required this.name,
    this.breed,
    this.photoUrl,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isSire = relation == _Relation.sire;
    final color = isSire ? AppColors.info : AppColors.danger;
    // Indent each generation; cap so deep trees still fit narrow screens.
    final indent = ((depth - 1).clamp(0, 3)) * 22.w;

    return Padding(
      padding: EdgeInsets.only(left: indent, bottom: 8.h),
      child: Row(
        children: [
          if (depth > 1)
            Padding(
              padding: EdgeInsets.only(right: 6.w),
              child: Icon(Icons.subdirectory_arrow_right_rounded, size: 16.r, color: AppColors.textMuted),
            ),
          Expanded(
            child: AppCard(
              onTap: onTap,
              padding: EdgeInsets.all(10.w),
              child: Row(
                children: [
                  DogAvatar(photoUrl: photoUrl, size: 36),
                  SizedBox(width: 10.w),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Icon(isSire ? Icons.male_rounded : Icons.female_rounded, size: 13.r, color: color),
                            SizedBox(width: 3.w),
                            Text(isSire ? 'Sire' : 'Dam',
                                style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w700, color: color)),
                          ],
                        ),
                        SizedBox(height: 1.h),
                        Text(name,
                            maxLines: 1, overflow: TextOverflow.ellipsis,
                            style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary)),
                        if ((breed ?? '').isNotEmpty)
                          Text(breed!,
                              maxLines: 1, overflow: TextOverflow.ellipsis,
                              style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                      ],
                    ),
                  ),
                  if (onTap != null)
                    Icon(Icons.account_tree_outlined, size: 16.r, color: AppColors.textMuted),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _OffspringRow extends StatelessWidget {
  final Animal dog;
  final VoidCallback onTap;
  const _OffspringRow({required this.dog, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      onTap: onTap,
      padding: EdgeInsets.all(10.w),
      child: Row(
        children: [
          DogAvatar(photoUrl: dog.photoUrl, size: 40),
          SizedBox(width: 10.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(dog.name,
                    maxLines: 1, overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
                Text('${dog.breed} • ${Fmt.age(dog.dateOfBirth)}',
                    maxLines: 1, overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
              ],
            ),
          ),
          Icon(Icons.account_tree_outlined, size: 16.r, color: AppColors.textMuted),
        ],
      ),
    );
  }
}
