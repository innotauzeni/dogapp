import 'package:flutter/material.dart';
import '../../../shared/widgets/app_drawer.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/dogs_repository.dart';
import '../models/animal.dart';
import '../widgets/dog_avatar.dart';
import 'dog_profile_screen.dart';

/// The Dog Handler's home: the dogs assigned to them, with quick "Record Care".
class HandlerDogsScreen extends StatefulWidget {
  final Animal? initialDog;
  const HandlerDogsScreen({super.key, this.initialDog});

  @override
  State<HandlerDogsScreen> createState() => _HandlerDogsScreenState();
}

class _HandlerDogsScreenState extends State<HandlerDogsScreen> {
  final _repo = DogsRepository();
  Future<List<Animal>>? _future;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    final userId = context.read<AuthProvider>().user?.id ?? '';
    _future = _repo.getAssignedDogs(userId);
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  Future<void> _recordCare(Animal dog) async {
    final saved = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(18.r))),
      builder: (_) => _CareForm(dog: dog, repo: _repo),
    );
    if (saved == true && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Care recorded')));
      _refresh();
    }
  }

  @override
  Widget build(BuildContext context) {
    final canRecord = context.watch<AuthProvider>().can('pet.edit') ||
        context.watch<AuthProvider>().isDogHandler;
    return Scaffold(
      appBar: AppBar(leading: const MenuButton(), title: const Text('My Assigned Dogs')),
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: AsyncView<List<Animal>>(
            future: _future,
            onRetry: _refresh,
            builder: (context, dogs) {
              if (dogs.isEmpty) {
                return ListView(
                  children: [
                    SizedBox(height: 120.h),
                    StatusView.empty(
                      title: 'No assigned dogs',
                      message: 'Dogs assigned to you will appear here.',
                      icon: Icons.pets_rounded,
                    ),
                  ],
                );
              }
              return ListView.separated(
                padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
                physics: const AlwaysScrollableScrollPhysics(),
                itemCount: dogs.length,
                separatorBuilder: (_, _) => SizedBox(height: 10.h),
                itemBuilder: (context, i) {
                  final d = dogs[i];
                  return Container(
                    decoration: BoxDecoration(
                      color: Theme.of(context).cardColor,
                      borderRadius: BorderRadius.circular(14.r),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: Padding(
                      padding: EdgeInsets.all(12.w),
                      child: Row(
                        children: [
                          DogAvatar(photoUrl: d.photoUrl, size: 48),
                          SizedBox(width: 12.w),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(d.name, maxLines: 1, overflow: TextOverflow.ellipsis,
                                    style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800)),
                                Text('${d.breed} • ${d.color}',
                                    maxLines: 1, overflow: TextOverflow.ellipsis,
                                    style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                              ],
                            ),
                          ),
                          IconButton(
                            tooltip: 'View profile',
                            icon: const Icon(Icons.chevron_right_rounded),
                            onPressed: () => Navigator.of(context).push(
                              MaterialPageRoute(
                                builder: (_) => DogProfileScreen(dogId: d.id, initial: d),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ).withCareButton(
                    show: canRecord && !d.isDeceased,
                    onRecord: () => _recordCare(d),
                  );
                },
              );
            },
          ),
        ),
      ),
    );
  }
}

extension _CardWithButton on Widget {
  Widget withCareButton({required bool show, required VoidCallback onRecord}) {
    if (!show) return this;
    return Column(
      children: [
        this,
        Align(
          alignment: Alignment.centerRight,
          child: Padding(
            padding: EdgeInsets.only(top: 6.h, right: 4.w),
            child: TextButton.icon(
              onPressed: onRecord,
              icon: const Icon(Icons.assignment_outlined, size: 18),
              label: const Text('Record Care'),
            ),
          ),
        ),
      ],
    );
  }
}

class _CareForm extends StatefulWidget {
  final Animal dog;
  final DogsRepository repo;
  const _CareForm({required this.dog, required this.repo});

  @override
  State<_CareForm> createState() => _CareFormState();
}

class _CareFormState extends State<_CareForm> {
  static const _events = [
    'Feeding', 'Vaccinated', 'Illness', 'Injured', 'Bite someone', 'Heat', 'Mating', 'Birth', 'Other',
  ];
  String _event = 'Feeding';
  DateTime _when = DateTime.now();
  final _details = TextEditingController();
  final _outcome = TextEditingController();
  final _location = TextEditingController(); // Kennel/location
  bool _attention = false;
  bool _saving = false;
  List<Animal> _allDogs = [];

  // Vaccination fields
  final _vaccineName = TextEditingController();
  final _vaccineBatch = TextEditingController();
  DateTime? _vaccineNextDue;
  final _vaccineNotes = TextEditingController();

  // Mating fields
  int? _selectedPartnerId;
  final _matingPartnerName = TextEditingController(); // For "Other" option
  final _matingDuration = TextEditingController();

  // Injury fields
  String _injurySeverity = 'Minor';
  final _injuryDetails = TextEditingController();
  bool _injuryTreated = false;

  // Bite fields
  String _biteSeverity = 'Minor';
  final _biteDetails = TextEditingController();
  final _biteReportedTo = TextEditingController();

  bool get _showVaccineFields => _event == 'Vaccinated';
  bool get _showMatingFields => _event == 'Mating';
  bool get _showInjuryFields => _event == 'Injured';
  bool get _showBiteFields => _event == 'Bite someone';
  bool get _isMatingOther => _selectedPartnerId == -1;

  @override
  void initState() {
    super.initState();
    _loadDogs();
  }

  Future<void> _loadDogs() async {
    try {
      final dogs = await widget.repo.getAssignedDogs('');
      setState(() => _allDogs = dogs.where((d) => d.id != widget.dog.id).toList());
    } catch (e) {
      debugPrint('Failed to load dogs: $e');
    }
  }

  @override
  void dispose() {
    _details.dispose();
    _outcome.dispose();
    _location.dispose();
    _vaccineName.dispose();
    _vaccineBatch.dispose();
    _vaccineNotes.dispose();
    _matingPartnerName.dispose();
    _matingDuration.dispose();
    _injuryDetails.dispose();
    _biteDetails.dispose();
    _biteReportedTo.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    try {
      final payload = {
        'dogId': widget.dog.id,
        'eventType': _event,
        'occurredOn': _when.toIso8601String(),
        'details': _details.text.trim().isEmpty ? null : _details.text.trim(),
        'quantityOrOutcome': _outcome.text.trim().isEmpty ? null : _outcome.text.trim(),
        'location': _location.text.trim().isEmpty ? null : _location.text.trim(),
        'requiresAttention': _attention,
      };

      if (_showVaccineFields) {
        payload['vaccineName'] = _vaccineName.text.trim().isEmpty ? null : _vaccineName.text.trim();
        payload['vaccineBatch'] = _vaccineBatch.text.trim().isEmpty ? null : _vaccineBatch.text.trim();
        payload['vaccineNextDue'] = _vaccineNextDue?.toIso8601String();
        payload['vaccineNotes'] = _vaccineNotes.text.trim().isEmpty ? null : _vaccineNotes.text.trim();
      }

      if (_showMatingFields) {
        if (_isMatingOther) {
          payload['matingPartnerName'] = _matingPartnerName.text.trim().isEmpty ? null : _matingPartnerName.text.trim();
        } else if (_selectedPartnerId != null && _selectedPartnerId! > 0) {
          payload['matingPartnerName'] = _allDogs.firstWhere((d) => d.id == _selectedPartnerId).name;
        }
        payload['matingDuration'] = _matingDuration.text.trim().isEmpty ? null : _matingDuration.text.trim();
      }

      if (_showInjuryFields) {
        payload['injurySeverity'] = _injurySeverity;
        payload['injuryDetails'] = _injuryDetails.text.trim().isEmpty ? null : _injuryDetails.text.trim();
        payload['injuryTreated'] = _injuryTreated;
      }

      if (_showBiteFields) {
        payload['biteSeverity'] = _biteSeverity;
        payload['biteDetails'] = _biteDetails.text.trim().isEmpty ? null : _biteDetails.text.trim();
        payload['biteReportedTo'] = _biteReportedTo.text.trim().isEmpty ? null : _biteReportedTo.text.trim();
      }

      await widget.repo.addCareRecord(payload);
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) {
        setState(() => _saving = false);
        // The API's own wording (e.g. the once-a-day feeding rule) is the useful
        // part — a bare "Failed: <exception>" buries it.
        final message = e is ApiException ? e.message : 'Failed: $e';
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final bottom = MediaQuery.of(context).viewInsets.bottom;
    return Padding(
      padding: EdgeInsets.fromLTRB(18.w, 16.h, 18.w, 16.h + bottom),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Record Care — ${widget.dog.name}',
                style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800)),
            SizedBox(height: 14.h),
            DropdownButtonFormField<String>(
              // ignore: deprecated_member_use
              value: _event,
              decoration: const InputDecoration(labelText: 'Event type', border: OutlineInputBorder()),
              items: _events.map((e) => DropdownMenuItem(value: e, child: Text(e))).toList(),
              onChanged: (v) => setState(() => _event = v ?? 'Feeding'),
            ),
            SizedBox(height: 12.h),
            InkWell(
              onTap: () async {
                final d = await showDatePicker(
                  context: context, initialDate: _when,
                  firstDate: DateTime(2015), lastDate: DateTime.now().add(const Duration(days: 1)),
                );
                if (d != null) setState(() => _when = DateTime(d.year, d.month, d.day, _when.hour, _when.minute));
              },
              child: InputDecorator(
                decoration: const InputDecoration(labelText: 'Date', border: OutlineInputBorder()),
                child: Text('${_when.year}-${_when.month.toString().padLeft(2, '0')}-${_when.day.toString().padLeft(2, '0')}'),
              ),
            ),
            SizedBox(height: 12.h),
            if (_event == 'Feeding')
              TextField(
                controller: _location,
                decoration: const InputDecoration(labelText: 'Kennel / Location', hintText: 'Where was the dog fed?', border: OutlineInputBorder()),
              ),
            if (_event == 'Feeding') SizedBox(height: 12.h),
            TextField(
              controller: _details,
              maxLines: 3,
              decoration: const InputDecoration(labelText: 'Details / notes', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _outcome,
              decoration: const InputDecoration(labelText: 'Quantity / outcome', border: OutlineInputBorder()),
            ),
            SizedBox(height: 6.h),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Requires attention / follow-up'),
              value: _attention,
              onChanged: (v) => setState(() => _attention = v),
            ),
            if (_showVaccineFields) ...[
              SizedBox(height: 16.h),
              Text('Vaccination Details', style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
              SizedBox(height: 12.h),
              TextField(controller: _vaccineName, decoration: const InputDecoration(labelText: 'Vaccine Name', hintText: 'e.g. Rabies, DHPP', border: OutlineInputBorder())),
              SizedBox(height: 12.h),
              TextField(controller: _vaccineBatch, decoration: const InputDecoration(labelText: 'Batch / Lot Number', border: OutlineInputBorder())),
              SizedBox(height: 12.h),
              InkWell(
                onTap: () async {
                  final picked = await showDatePicker(context: context, initialDate: _vaccineNextDue ?? DateTime.now().add(const Duration(days: 365)), firstDate: DateTime.now(), lastDate: DateTime(2030));
                  if (picked != null) setState(() => _vaccineNextDue = picked);
                },
                child: InputDecorator(decoration: const InputDecoration(labelText: 'Next Due Date', border: OutlineInputBorder()), child: Text(_vaccineNextDue == null ? 'Select date' : '${_vaccineNextDue!.year}-${_vaccineNextDue!.month.toString().padLeft(2, '0')}-${_vaccineNextDue!.day.toString().padLeft(2, '0')}')),
              ),
              SizedBox(height: 12.h),
              TextField(controller: _vaccineNotes, maxLines: 2, decoration: const InputDecoration(labelText: 'Vaccine Notes', hintText: 'Reaction, dosage, or route', border: OutlineInputBorder())),
            ],
            if (_showMatingFields) ...[
              SizedBox(height: 16.h),
              Text('Mating Details', style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
              SizedBox(height: 12.h),
              DropdownButtonFormField<int?>(
                // ignore: deprecated_member_use
                value: _selectedPartnerId,
                decoration: const InputDecoration(labelText: 'Mating Partner', hintText: 'Select a dog or choose Other', border: OutlineInputBorder()),
                items: [
                  ..._allDogs.map((d) => DropdownMenuItem(value: d.id, child: Text(d.name))),
                  const DropdownMenuItem(value: -1, child: Text('Other (outside dog)')),
                ],
                onChanged: (v) => setState(() => _selectedPartnerId = v),
              ),
              if (_isMatingOther) ...[
                SizedBox(height: 12.h),
                TextField(controller: _matingPartnerName, decoration: const InputDecoration(labelText: 'Partner Dog Name', border: OutlineInputBorder())),
              ],
              SizedBox(height: 12.h),
              TextField(controller: _matingDuration, decoration: const InputDecoration(labelText: 'Duration', hintText: 'e.g. 5 minutes', border: OutlineInputBorder())),
            ],
            if (_showInjuryFields) ...[
              SizedBox(height: 16.h),
              Text('Injury Details', style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
              SizedBox(height: 12.h),
              // ignore: deprecated_member_use
              DropdownButtonFormField<String>(value: _injurySeverity, decoration: const InputDecoration(labelText: 'Severity', border: OutlineInputBorder()), items: ['Minor', 'Moderate', 'Severe'].map((e) => DropdownMenuItem(value: e, child: Text(e))).toList(), onChanged: (v) => setState(() => _injurySeverity = v ?? 'Minor')),
              SizedBox(height: 12.h),
              TextField(controller: _injuryDetails, maxLines: 3, decoration: const InputDecoration(labelText: 'Injury Details', hintText: 'Describe injury and treatment', border: OutlineInputBorder())),
              SizedBox(height: 6.h),
              CheckboxListTile(contentPadding: EdgeInsets.zero, title: const Text('Treated on-site'), value: _injuryTreated, onChanged: (v) => setState(() => _injuryTreated = v ?? false)),
            ],
            if (_showBiteFields) ...[
              SizedBox(height: 16.h),
              Text('Bite Incident Details', style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700)),
              SizedBox(height: 12.h),
              // ignore: deprecated_member_use
              DropdownButtonFormField<String>(value: _biteSeverity, decoration: const InputDecoration(labelText: 'Severity', border: OutlineInputBorder()), items: ['Minor', 'Moderate', 'Severe'].map((e) => DropdownMenuItem(value: e, child: Text(e))).toList(), onChanged: (v) => setState(() => _biteSeverity = v ?? 'Minor')),
              SizedBox(height: 12.h),
              TextField(controller: _biteDetails, maxLines: 3, decoration: const InputDecoration(labelText: 'Victim & Details', hintText: 'Who was bitten and first aid given', border: OutlineInputBorder())),
              SizedBox(height: 12.h),
              TextField(controller: _biteReportedTo, decoration: const InputDecoration(labelText: 'Reported To', hintText: 'e.g. Owner, Clinic, Police', border: OutlineInputBorder())),
            ],
            SizedBox(height: 8.h),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _saving ? null : _save,
                child: _saving
                    ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Save Record'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
