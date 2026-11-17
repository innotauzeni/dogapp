import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/dogs_repository.dart';
import '../models/animal.dart';
import '../widgets/dog_list_tile.dart';
import 'dog_form_screen.dart';
import 'dog_profile_screen.dart';

class DogsListScreen extends StatefulWidget {
  final String? filterType; // 'vaccinated', 'active', 'deployed', 'total'
  const DogsListScreen({super.key, this.filterType});

  @override
  State<DogsListScreen> createState() => _DogsListScreenState();
}

class _DogsListScreenState extends State<DogsListScreen> {
  final _repo = DogsRepository();
  Future<List<Animal>>? _future;
  String _query = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  int _loadedCompany = -1;

  void _load() {
    final companyId = context.read<AuthProvider>().activeCompanyId;
    _loadedCompany = companyId;
    _future = _loadDogs(companyId);
  }

  Future<List<Animal>> _loadDogs(int companyId) async {
    final pets = await _repo.getPets(companyId);
    if (widget.filterType == 'vaccinated') {
      final vaccinatedIds = await _repo.getVaccinatedDogIds(companyId);
      return pets.where((d) => vaccinatedIds.contains(d.id)).toList();
    }
    return pets;
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  List<Animal> _filter(List<Animal> dogs) {
    var filtered = dogs;

    // Apply status filter based on dashboard selection (vaccinated dogs are
    // already filtered server-side in _loadDogs).
    if (widget.filterType == 'active') {
      filtered = filtered.where((d) => d.isActive).toList();
    } else if (widget.filterType == 'deployed') {
      filtered = filtered.where((d) => d.isDeployed).toList();
    }

    // Apply search query
    if (_query.trim().isEmpty) return filtered;
    final q = _query.toLowerCase();
    return filtered
        .where((d) =>
            d.name.toLowerCase().contains(q) ||
            d.breed.toLowerCase().contains(q) ||
            (d.microchipNumber ?? '').toLowerCase().contains(q))
        .toList();
  }

  Future<void> _openProfile(Animal dog) async {
    final changed = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => DogProfileScreen(dogId: dog.id, initial: dog)),
    );
    if (changed == true) _refresh();
  }

  Future<void> _addDog() async {
    final created = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const DogFormScreen()),
    );
    if (created == true) _refresh();
  }

  String _getTitle() {
    return switch (widget.filterType) {
      'active' => 'Active Dogs',
      'deployed' => 'Deployed Dogs',
      'vaccinated' => 'Vaccinated Dogs',
      _ => 'Dogs',
    };
  }

  (String, String) _emptyState() {
    return switch (widget.filterType) {
      'vaccinated' => ('No vaccinated dogs', 'Dogs will appear here once a vaccination is recorded for them.'),
      'active' => ('No active dogs', 'Active dogs will appear here.'),
      'deployed' => ('No deployed dogs', 'Deployed dogs will appear here.'),
      _ => ('No dogs yet', 'Add your first dog to get started.'),
    };
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final canCreate = auth.can('pet.create');
    if (auth.activeCompanyId != _loadedCompany) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) setState(_load);
      });
    }
    return Scaffold(
      appBar: AppBar(title: Text(_getTitle())),
      floatingActionButton: canCreate
          ? FloatingActionButton.extended(
              heroTag: 'fab-dogs-list-add',
              onPressed: _addDog,
              icon: const Icon(Icons.add_rounded),
              label: const Text('Add Dog'),
            )
          : null,
      body: SafeArea(
        top: false,
        child: Column(
          children: [
            Padding(
              padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 8.h),
              child: TextField(
                onChanged: (v) => setState(() => _query = v),
                decoration: InputDecoration(
                  hintText: 'Search by name, breed or microchip',
                  prefixIcon: Icon(Icons.search_rounded, size: 20.r, color: AppColors.textMuted),
                ),
              ),
            ),
            Expanded(
              child: RefreshIndicator(
                onRefresh: _refresh,
                child: AsyncView<List<Animal>>(
                  future: _future,
                  onRetry: _refresh,
                  builder: (context, dogs) {
                    final filtered = _filter(dogs);
                    if (filtered.isEmpty) {
                      final (emptyTitle, emptyMessage) = _emptyState();
                      return ListView(
                        children: [
                          SizedBox(height: 80.h),
                          StatusView.empty(
                            title: dogs.isEmpty ? emptyTitle : 'No matches',
                            message: dogs.isEmpty ? emptyMessage : 'Try a different search term.',
                            icon: Icons.pets_rounded,
                          ),
                        ],
                      );
                    }
                    return ListView.separated(
                      padding: EdgeInsets.fromLTRB(16.w, 4.h, 16.w, 96.h),
                      physics: const AlwaysScrollableScrollPhysics(),
                      itemCount: filtered.length,
                      separatorBuilder: (context, index) => SizedBox(height: 10.h),
                      itemBuilder: (_, i) => DogListTile(dog: filtered[i], onTap: () => _openProfile(filtered[i])),
                    );
                  },
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
