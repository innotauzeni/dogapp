import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../../dogs/data/dogs_repository.dart';
import '../../dogs/models/animal.dart';
import 'mating_form_screen.dart';
import 'litter_form_screen.dart';

class BreedingScreen extends StatefulWidget {
  /// 0 = Matings tab, 1 = Litters tab.
  final int initialTab;
  const BreedingScreen({super.key, this.initialTab = 0});

  @override
  State<BreedingScreen> createState() => _BreedingScreenState();
}

class _BreedingScreenState extends State<BreedingScreen> with SingleTickerProviderStateMixin {
  final _repo = DogsRepository();
  late final TabController _tab = TabController(length: 2, vsync: this, initialIndex: widget.initialTab);

  Future<List<MatingRecord>>? _matings;
  Future<List<Litter>>? _litters;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    final companyId = context.read<AuthProvider>().companyId;
    _matings = _repo.getMatingRecords(companyId);
    _litters = _repo.getLitters(companyId);
  }

  Future<void> _refresh() async {
    setState(_load);
    await Future.wait([_matings ?? Future.value(), _litters ?? Future.value()]);
  }

  @override
  void dispose() {
    _tab.dispose();
    super.dispose();
  }

  Future<void> _addMating() async {
    final ok = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const MatingFormScreen()),
    );
    if (ok == true) _refresh();
  }

  Future<void> _addLitter(MatingRecord mating) async {
    final ok = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => LitterFormScreen(matingRecordId: mating.id, title: '${mating.sireName} × ${mating.damName}')),
    );
    if (ok == true) _refresh();
  }

  /// Entry point for the Litters tab's FAB — a litter belongs to a mating
  /// record, so first ask which one, then reuse the same add-litter flow.
  Future<void> _addLitterFromPicker() async {
    final matings = await (_matings ?? Future.value(const <MatingRecord>[]));
    if (!mounted) return;
    if (matings.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Record a mating first, then add its litter.')),
      );
      return;
    }
    final selected = await showModalBottomSheet<MatingRecord>(
      context: context,
      builder: (_) => SafeArea(
        child: ListView(
          shrinkWrap: true,
          children: [
            Padding(
              padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 6.h),
              child: Text('Select a mating', style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w800)),
            ),
            for (final m in matings)
              ListTile(
                leading: const Icon(Icons.favorite_rounded, color: AppColors.danger),
                title: Text('${m.sireName ?? 'Sire'} × ${m.damName ?? 'Dam'}'),
                subtitle: Text(Fmt.date(m.matingDate)),
                onTap: () => Navigator.pop(context, m),
              ),
          ],
        ),
      ),
    );
    if (selected != null) _addLitter(selected);
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final canEdit = auth.can('pet.edit') || auth.isDogHandler;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Breeding'),
        bottom: TabBar(
          controller: _tab,
          labelColor: AppColors.primary,
          unselectedLabelColor: AppColors.textMuted,
          indicatorColor: AppColors.primary,
          labelStyle: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700),
          tabs: const [Tab(text: 'Matings'), Tab(text: 'Litters')],
        ),
      ),
      floatingActionButton: canEdit
          ? AnimatedBuilder(
              animation: _tab,
              builder: (context, _) => _tab.index == 1
                  ? FloatingActionButton.extended(
                      heroTag: 'fab-breeding-add-litter',
                      onPressed: _addLitterFromPicker,
                      icon: const Icon(Icons.add_rounded),
                      label: const Text('Litter'),
                    )
                  : FloatingActionButton.extended(
                      heroTag: 'fab-breeding-add-mating',
                      onPressed: _addMating,
                      icon: const Icon(Icons.add_rounded),
                      label: const Text('Mating'),
                    ),
            )
          : null,
      body: SafeArea(
        top: false,
        child: TabBarView(
          controller: _tab,
          children: [
            RefreshIndicator(
              onRefresh: _refresh,
              child: AsyncView<List<MatingRecord>>(
                future: _matings,
                onRetry: _refresh,
                builder: (context, items) => _MatingList(items: items, canEdit: canEdit, onAddLitter: _addLitter),
              ),
            ),
            RefreshIndicator(
              onRefresh: _refresh,
              child: AsyncView<List<Litter>>(
                future: _litters,
                onRetry: _refresh,
                builder: (context, items) => _LitterList(items: items),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MatingList extends StatelessWidget {
  final List<MatingRecord> items;
  final bool canEdit;
  final void Function(MatingRecord) onAddLitter;
  const _MatingList({required this.items, required this.canEdit, required this.onAddLitter});

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return ListView(children: [
        SizedBox(height: 80.h),
        StatusView.empty(title: 'No mating records', message: 'Record a mating to start tracking breeding.', icon: Icons.favorite_rounded),
      ]);
    }
    return ListView.separated(
      padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 96.h),
      physics: const AlwaysScrollableScrollPhysics(),
      itemCount: items.length,
      separatorBuilder: (context, index) => SizedBox(height: 10.h),
      itemBuilder: (context, i) {
        final m = items[i];
        return AppCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    width: 38.r,
                    height: 38.r,
                    decoration: BoxDecoration(color: AppColors.dangerLight, borderRadius: BorderRadius.circular(10.r)),
                    child: Icon(Icons.favorite_rounded, size: 18.r, color: AppColors.danger),
                  ),
                  SizedBox(width: 12.w),
                  Expanded(
                    child: Text('${m.sireName ?? 'Sire'} × ${m.damName ?? 'Dam'}',
                        maxLines: 1, overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w700)),
                  ),
                ],
              ),
              SizedBox(height: 10.h),
              InfoRow(label: 'Mating Date', value: Fmt.date(m.matingDate), icon: Icons.calendar_today_rounded),
              if ((m.location ?? '').isNotEmpty)
                InfoRow(label: 'Location', value: m.location!, icon: Icons.location_on_outlined),
              InfoRow(label: 'Litters', value: '${m.litters.length}', icon: Icons.child_care_rounded),
              if (canEdit) ...[
                SizedBox(height: 6.h),
                Align(
                  alignment: Alignment.centerRight,
                  child: OutlinedButton.icon(
                    onPressed: () => onAddLitter(m),
                    icon: Icon(Icons.add_rounded, size: 16.r),
                    label: const Text('Add Litter'),
                  ),
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}

class _LitterList extends StatefulWidget {
  final List<Litter> items;
  const _LitterList({required this.items});

  @override
  State<_LitterList> createState() => _LitterListState();
}

class _LitterListState extends State<_LitterList> {
  final Set<int> _expandedLitterIds = {};

  @override
  Widget build(BuildContext context) {
    if (widget.items.isEmpty) {
      return ListView(children: [
        SizedBox(height: 80.h),
        StatusView.empty(title: 'No litters', message: 'Litters appear here once recorded.', icon: Icons.child_care_rounded),
      ]);
    }
    return ListView.separated(
      padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 96.h),
      physics: const AlwaysScrollableScrollPhysics(),
      itemCount: widget.items.length,
      separatorBuilder: (context, index) => SizedBox(height: 10.h),
      itemBuilder: (context, i) {
        final l = widget.items[i];
        final isExpanded = _expandedLitterIds.contains(l.id);
        return AppCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              GestureDetector(
                onTap: () => setState(() => isExpanded ? _expandedLitterIds.remove(l.id) : _expandedLitterIds.add(l.id)),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('${l.sireName ?? 'Sire'} × ${l.damName ?? 'Dam'}',
                              maxLines: 1, overflow: TextOverflow.ellipsis,
                              style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w700)),
                          SizedBox(height: 4.h),
                          Text('Born ${Fmt.date(l.birthDate)}', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                        ],
                      ),
                    ),
                    Icon(isExpanded ? Icons.expand_less_rounded : Icons.expand_more_rounded, color: AppColors.textMuted),
                  ],
                ),
              ),
              SizedBox(height: 10.h),
              Row(
                children: [
                  _pill('Total', l.totalBorn, AppColors.primary),
                  SizedBox(width: 8.w),
                  _pill('Survived', l.survivedCount, AppColors.success),
                  SizedBox(width: 8.w),
                  _pill('Died', l.diedCount, AppColors.danger),
                ],
              ),
              if (isExpanded && l.puppies.isNotEmpty) ...[
                SizedBox(height: 12.h),
                Divider(height: 1, color: AppColors.border),
                SizedBox(height: 12.h),
                Text('Puppies', style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary)),
                SizedBox(height: 8.h),
                ListView.separated(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: l.puppies.length,
                  separatorBuilder: (context, index) => SizedBox(height: 8.h),
                  itemBuilder: (_, pi) {
                    final p = l.puppies[pi];
                    return _PuppyCard(puppy: p);
                  },
                ),
              ],
            ],
          ),
        );
      },
    );
  }

  Widget _pill(String label, int value, Color color) {
    return Expanded(
      child: Container(
        padding: EdgeInsets.symmetric(vertical: 8.h),
        decoration: BoxDecoration(color: color.withValues(alpha: 0.10), borderRadius: BorderRadius.circular(10.r)),
        child: Column(
          children: [
            Text('$value', style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800, color: color)),
            Text(label, style: TextStyle(fontSize: 10.sp, color: color)),
          ],
        ),
      ),
    );
  }
}

class _PuppyCard extends StatefulWidget {
  final Puppy puppy;
  const _PuppyCard({required this.puppy});

  @override
  State<_PuppyCard> createState() => _PuppyCardState();
}

class _PuppyCardState extends State<_PuppyCard> {
  final _repo = DogsRepository();
  Future<List<Vaccination>>? _vaccinations;
  bool _showVaccines = false;

  @override
  void initState() {
    super.initState();
    if (widget.puppy.vaccinations.isNotEmpty) {
      _vaccinations = Future.value(widget.puppy.vaccinations);
    }
  }

  Future<void> _recordVaccine() async {
    final result = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => _PuppyVaccineForm(puppyId: widget.puppy.id, puppyName: widget.puppy.name)),
    );
    if (result == true) {
      setState(() {
        _vaccinations = _repo.getPuppyVaccinations(widget.puppy.id);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final canEdit = auth.can('pet.edit') || auth.isDogHandler;
    return Container(
      decoration: BoxDecoration(color: AppColors.surfaceAlt, borderRadius: BorderRadius.circular(10.r)),
      padding: EdgeInsets.all(10.w),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(widget.puppy.name, style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
                    Text('${widget.puppy.color}${widget.puppy.gender != null ? " • ${widget.puppy.gender}" : ""}',
                        style: TextStyle(fontSize: 11.sp, color: AppColors.textSecondary)),
                  ],
                ),
              ),
              if (canEdit)
                SizedBox(
                  height: 32.h,
                  child: OutlinedButton.icon(
                    onPressed: _recordVaccine,
                    icon: Icon(Icons.add_rounded, size: 14.r),
                    label: Text('Vaccine', style: TextStyle(fontSize: 10.sp)),
                  ),
                ),
            ],
          ),
          if (_vaccinations != null)
            FutureBuilder<List<Vaccination>>(
              future: _vaccinations,
              builder: (context, snap) {
                if (!snap.hasData || snap.data!.isEmpty) return const SizedBox();
                final vaccs = snap.data!;
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SizedBox(height: 8.h),
                    GestureDetector(
                      onTap: () => setState(() => _showVaccines = !_showVaccines),
                      child: Row(
                        children: [
                          Icon(Icons.shield_rounded, size: 14.r, color: AppColors.success),
                          SizedBox(width: 4.w),
                          Text('${vaccs.length} vaccine(s)', style: TextStyle(fontSize: 10.sp, fontWeight: FontWeight.w600, color: AppColors.success)),
                          SizedBox(width: 4.w),
                          Icon(_showVaccines ? Icons.expand_less_rounded : Icons.expand_more_rounded, size: 14.r, color: AppColors.textMuted),
                        ],
                      ),
                    ),
                    if (_showVaccines) ...[
                      SizedBox(height: 6.h),
                      ...vaccs.map((v) => Padding(
                        padding: EdgeInsets.only(top: 6.h),
                        child: Text('${v.name ?? "Vaccine"} • ${Fmt.date(v.vaccinationDate)}',
                            style: TextStyle(fontSize: 9.sp, color: AppColors.textSecondary)),
                      )),
                    ],
                  ],
                );
              },
            ),
        ],
      ),
    );
  }
}

class _PuppyVaccineForm extends StatefulWidget {
  final int puppyId;
  final String puppyName;
  const _PuppyVaccineForm({required this.puppyId, required this.puppyName});

  @override
  State<_PuppyVaccineForm> createState() => _PuppyVaccineFormState();
}

class _PuppyVaccineFormState extends State<_PuppyVaccineForm> {
  final _repo = DogsRepository();
  final _formKey = GlobalKey<FormState>();
  late DateTime _vaccDate = DateTime.now();
  late DateTime? _nextDueDate;
  String? _name, _vaccType, _vetClinic, _notes;
  bool _loading = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Vaccinate ${widget.puppyName}')),
      body: SafeArea(
        child: Form(
          key: _formKey,
          child: SingleChildScrollView(
            padding: EdgeInsets.all(16.w),
            child: Column(
              children: [
                TextFormField(
                  decoration: InputDecoration(labelText: 'Vaccine Name *', hintText: 'e.g., Rabies, DHPP'),
                  validator: (v) => (v ?? '').isEmpty ? 'Required' : null,
                  onSaved: (v) => _name = v,
                ),
                SizedBox(height: 12.h),
                DropdownButtonFormField<String>(
                  decoration: InputDecoration(labelText: 'Vaccine Type'),
                  items: ['Core Vaccine', 'Non-Core Vaccine', 'Rabies', 'DHPP', 'Bordetella', 'Other']
                      .map((t) => DropdownMenuItem(value: t, child: Text(t)))
                      .toList(),
                  onChanged: (v) => _vaccType = v,
                ),
                SizedBox(height: 12.h),
                TextFormField(
                  decoration: InputDecoration(labelText: 'Vaccination Date *'),
                  readOnly: true,
                  onTap: () async {
                    final d = await showDatePicker(context: context, initialDate: _vaccDate, firstDate: DateTime(2000), lastDate: DateTime.now());
                    if (d != null) setState(() => _vaccDate = d);
                  },
                  controller: TextEditingController(text: Fmt.date(_vaccDate)),
                  validator: (v) => (v ?? '').isEmpty ? 'Required' : null,
                ),
                SizedBox(height: 12.h),
                TextFormField(
                  decoration: InputDecoration(labelText: 'Next Due Date'),
                  readOnly: true,
                  onTap: () async {
                    final d = await showDatePicker(context: context, initialDate: _vaccDate.add(Duration(days: 365)), firstDate: DateTime.now(), lastDate: DateTime(2099));
                    if (d != null) setState(() => _nextDueDate = d);
                  },
                  controller: TextEditingController(text: _nextDueDate != null ? Fmt.date(_nextDueDate!) : ''),
                ),
                SizedBox(height: 12.h),
                TextFormField(
                  decoration: InputDecoration(labelText: 'Veterinary Clinic', hintText: 'Clinic name'),
                  onSaved: (v) => _vetClinic = v,
                ),
                SizedBox(height: 12.h),
                TextFormField(
                  decoration: InputDecoration(labelText: 'Notes', hintText: 'Any additional info...'),
                  maxLines: 3,
                  onSaved: (v) => _notes = v,
                ),
                SizedBox(height: 24.h),
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton.icon(
                    onPressed: _loading ? null : _submit,
                    icon: const Icon(Icons.shield_rounded),
                    label: Text(_loading ? 'Recording...' : 'Record Vaccination'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    _formKey.currentState!.save();
    setState(() => _loading = true);
    try {
      await _repo.addVaccination({
        'puppyId': widget.puppyId,
        'name': _name,
        'vaccType': _vaccType,
        'vaccinationDate': _vaccDate.toIso8601String(),
        'nextVaccinationDate': _nextDueDate?.toIso8601String(),
        'vetclinicName': _vetClinic,
        'notes': _notes,
      });
      if (mounted) {
        Navigator.pop(context, true);
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Vaccination recorded!')));
      }
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }
}
