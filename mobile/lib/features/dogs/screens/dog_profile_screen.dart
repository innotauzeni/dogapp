import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/status_chip.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/dogs_repository.dart';
import '../models/animal.dart';
import '../services/dog_certificate.dart';
import '../widgets/dog_avatar.dart';
import 'care_record_form_screen.dart';
import 'dog_form_screen.dart';
import 'family_tree_screen.dart';
import 'vaccination_form_screen.dart';

class DogProfileScreen extends StatefulWidget {
  final int dogId;
  final Animal? initial;
  const DogProfileScreen({super.key, required this.dogId, this.initial});

  @override
  State<DogProfileScreen> createState() => _DogProfileScreenState();
}

class _DogProfileScreenState extends State<DogProfileScreen> {
  final _repo = DogsRepository();
  Future<DogProfile>? _future;
  bool _dirty = false; // whether to tell the list to refresh on pop

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    _future = _repo.getProfile(widget.dogId);
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  Future<void> _edit(Animal dog) async {
    final changed = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => DogFormScreen(existing: dog)),
    );
    if (changed == true) {
      _dirty = true;
      _refresh();
    }
  }

  Future<void> _addVaccination() async {
    final added = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => VaccinationFormScreen(dogId: widget.dogId)),
    );
    if (added == true) {
      _dirty = true;
      _refresh();
    }
  }

  Future<void> _addCareRecord() async {
    // The already-recorded events go with us so the form can flag a duplicate
    // feeding before the handler fills the whole thing in. A failed profile load
    // only costs the local check — the server enforces the rule either way.
    List<CareRecord> existing = const [];
    try {
      existing = (await _future)?.careRecords ?? const [];
    } catch (_) {}
    if (!mounted) return;
    final added = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => CareRecordFormScreen(
          dogId: widget.dogId,
          existingRecords: existing,
        ),
      ),
    );
    if (added == true) {
      _dirty = true;
      _refresh();
    }
  }

  Future<void> _certificate() async {
    final data = await _future;
    if (data == null || !mounted) return;
    try {
      await DogCertificate.generate(data.dog, companyName: data.dog.companyName);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Could not generate certificate: $e')),
        );
      }
    }
  }

  Future<void> _markDeceased(Animal dog) async {
    var deathDate = DateTime.now();
    String? photoPath;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => AlertDialog(
          title: const Text('Mark as Deceased'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.event_outlined),
                title: const Text('Date of death'),
                subtitle: Text(Fmt.date(deathDate)),
                onTap: () async {
                  final picked = await showDatePicker(
                    context: ctx,
                    initialDate: deathDate,
                    firstDate: DateTime(2000),
                    lastDate: DateTime.now(),
                  );
                  if (picked != null) setLocal(() => deathDate = picked);
                },
              ),
              TextButton.icon(
                icon: const Icon(Icons.photo_camera_outlined),
                label: Text(photoPath == null ? 'Add photo (optional)' : 'Photo selected ✓'),
                onPressed: () async {
                  final picked = await ImagePicker().pickImage(source: ImageSource.gallery, maxWidth: 1400);
                  if (picked != null) setLocal(() => photoPath = picked.path);
                },
              ),
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
            FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Confirm')),
          ],
        ),
      ),
    );
    if (confirmed != true) return;

    try {
      var photoUrl = dog.photoUrl;
      if (photoPath != null) photoUrl = await _repo.uploadPhoto(photoPath!);
      await _repo.updatePet(dog.id, {
        'id': dog.id,
        'name': dog.name,
        'breed': dog.breed,
        'color': dog.color,
        'dateOfBirth': dog.dateOfBirth?.toIso8601String(),
        'dateOfDeath': deathDate.toIso8601String(),
        'gender': dog.gender,
        'microchipNumber': dog.microchipNumber,
        'photoUrl': photoUrl,
        'sireId': dog.sireId,
        'damId': dog.damId,
        'companyId': dog.companyId,
        'isActive': false,
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Marked as deceased')));
        _dirty = true;
        _refresh();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e')));
      }
    }
  }

  Future<void> _returnDog(int deploymentId) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Return dog?'),
        content: const Text('This recalls the dog back to its branch/head office and ends the current deployment.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Return')),
        ],
      ),
    );
    if (confirmed != true) return;

    try {
      await _repo.returnDeployment(deploymentId);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Dog returned to branch.')));
        _dirty = true;
        _refresh();
      }
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) Navigator.of(context).pop(_dirty);
      },
      child: Scaffold(
        appBar: AppBar(
          title: Text(widget.initial?.name ?? 'Dog Profile'),
          actions: [
            if (!auth.isDogHandler)
              IconButton(
                icon: const Icon(Icons.workspace_premium_outlined),
                tooltip: 'Certificate',
                onPressed: _certificate,
              ),
            if (!auth.isDogHandler)
              IconButton(
                icon: const Icon(Icons.account_tree_outlined),
                tooltip: 'Family tree',
                onPressed: () => Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => FamilyTreeScreen(dogId: widget.dogId, dogName: widget.initial?.name),
                  ),
                ),
              ),
            if (auth.can('pet.edit'))
              IconButton(
                icon: const Icon(Icons.healing_outlined),
                tooltip: 'Record Care',
                onPressed: _addCareRecord,
              ),
            if (auth.can('pet.edit'))
              IconButton(
                icon: const Icon(Icons.edit_outlined),
                onPressed: () async {
                  final data = await _future;
                  if (data != null && mounted) _edit(data.dog);
                },
              ),
            if (auth.can('pet.edit'))
              IconButton(
                icon: const Icon(Icons.heart_broken_outlined),
                tooltip: 'Mark as deceased',
                onPressed: () async {
                  final messenger = ScaffoldMessenger.of(context);
                  final data = await _future;
                  if (data == null || !mounted) return;
                  if (data.dog.isDeceased) {
                    messenger.showSnackBar(
                      const SnackBar(content: Text('This dog is already marked as deceased.')),
                    );
                    return;
                  }
                  _markDeceased(data.dog);
                },
              ),
          ],
        ),
        floatingActionButton: auth.can('pet.edit')
            ? FloatingActionButton.extended(
                heroTag: 'fab-dog-profile-add-vaccination',
                onPressed: _addVaccination,
                icon: const Icon(Icons.vaccines_outlined),
                label: const Text('Vaccination'),
              )
            : null,
        body: SafeArea(
          top: false,
          child: RefreshIndicator(
            onRefresh: _refresh,
            child: AsyncView<DogProfile>(
              future: _future,
              onRetry: _refresh,
              builder: (context, profile) {
                final auth = context.watch<AuthProvider>();
                return _ProfileBody(
                  profile: profile,
                  isDogHandler: auth.isDogHandler,
                  canManageDeployment: auth.can('deployment.edit'),
                  onCertificate: _certificate,
                  onReturnDog: _returnDog,
                  onChanged: () {
                    _dirty = true;
                    _refresh();
                  },
                );
              },
            ),
          ),
        ),
      ),
    );
  }
}

class _ProfileBody extends StatelessWidget {
  final DogProfile profile;
  final bool isDogHandler;
  final bool canManageDeployment;
  final VoidCallback onChanged;
  final VoidCallback onCertificate;
  final ValueChanged<int> onReturnDog;
  const _ProfileBody({
    required this.profile,
    required this.isDogHandler,
    required this.canManageDeployment,
    required this.onChanged,
    required this.onCertificate,
    required this.onReturnDog,
  });

  @override
  Widget build(BuildContext context) {
    final dog = profile.dog;
    return ListView(
      padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 96.h),
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        _Header(dog: dog),
        SizedBox(height: 12.h),
        if (!isDogHandler)
          Column(
            children: [
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed: onCertificate,
                  icon: const Icon(Icons.workspace_premium_outlined),
                  label: const Text('Generate Certificate'),
                ),
              ),
              SizedBox(height: 12.h),
            ],
          ),
        AppCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SectionHeader(title: 'Details'),
              InfoRow(label: 'Breed', value: dog.breed, icon: Icons.pets_outlined),
              InfoRow(label: 'Color', value: dog.color, icon: Icons.palette_outlined),
              InfoRow(label: 'Gender', value: dog.gender ?? '—', icon: Icons.wc_rounded),
              InfoRow(label: 'Date of Birth', value: Fmt.date(dog.dateOfBirth), icon: Icons.cake_outlined),
              InfoRow(label: 'Age', value: Fmt.age(dog.dateOfBirth, until: dog.dateOfDeath), icon: Icons.timelapse_rounded),
              InfoRow(label: 'Microchip', value: dog.microchipNumber ?? '—', icon: Icons.qr_code_2_rounded),
              InfoRow(label: 'Sire', value: dog.sireName ?? '—', icon: Icons.male_rounded),
              InfoRow(label: 'Dam', value: dog.damName ?? '—', icon: Icons.female_rounded),
              InfoRow(label: 'Owner', value: dog.customerName ?? 'Unassigned', icon: Icons.person_outline_rounded),
            ],
          ),
        ),
        if (dog.isDeployed) ...[
          SizedBox(height: 16.h),
          AppCard(
            color: AppColors.infoLight,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Icon(Icons.location_on_rounded, color: AppColors.info, size: 18.r),
                    SizedBox(width: 8.w),
                    Text('Currently Deployed',
                        style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700, color: AppColors.info)),
                  ],
                ),
                SizedBox(height: 8.h),
                InfoRow(label: 'Location', value: profile.currentDeployment?.location ?? dog.deployedLocation ?? '—'),
                InfoRow(label: 'Deployed To', value: profile.currentDeployment?.companyDeployedTo ?? dog.deployedTo ?? '—'),
                if ((profile.currentDeployment?.customerName ?? '').isNotEmpty)
                  InfoRow(label: 'Customer', value: profile.currentDeployment!.customerName!, icon: Icons.person_outline_rounded),
                if ((profile.currentDeployment?.customerSiteName ?? '').isNotEmpty)
                  InfoRow(label: 'Site / Premise', value: profile.currentDeployment!.customerSiteName!, icon: Icons.apartment_rounded),
                if ((profile.currentDeployment?.customerSiteAddress ?? '').isNotEmpty)
                  InfoRow(label: 'Site Address', value: profile.currentDeployment!.customerSiteAddress!, icon: Icons.map_outlined),
                if (canManageDeployment && profile.currentDeployment != null) ...[
                  SizedBox(height: 10.h),
                  SizedBox(
                    width: double.infinity,
                    child: OutlinedButton.icon(
                      onPressed: () => onReturnDog(profile.currentDeployment!.id),
                      icon: const Icon(Icons.arrow_back_rounded, size: 16),
                      label: const Text('Return to Branch / Head Office'),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
        SizedBox(height: 16.h),
        _CareTimeline(items: profile.careRecords),
        SizedBox(height: 16.h),
        _Vaccinations(items: profile.vaccinations),
        SizedBox(height: 16.h),
        _CertificationsSection(dogId: dog.id, canEdit: context.watch<AuthProvider>().can('pet.edit')),
        SizedBox(height: 16.h),
        _DocumentsSection(dogId: dog.id, canEdit: context.watch<AuthProvider>().can('pet.edit')),
        SizedBox(height: 16.h),
        _MatingRecords(items: profile.matingRecords),
      ],
    );
  }
}

class _CertificationsSection extends StatefulWidget {
  final int dogId;
  final bool canEdit;
  const _CertificationsSection({required this.dogId, required this.canEdit});

  @override
  State<_CertificationsSection> createState() => _CertificationsSectionState();
}

class _CertificationsSectionState extends State<_CertificationsSection> {
  final _repo = DogsRepository();
  Future<List<DogCertification>>? _future;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() => _future = _repo.getCertifications(widget.dogId);

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  Future<void> _addCertification() async {
    final saved = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(18.r))),
      builder: (_) => _CertificationForm(dogId: widget.dogId, repo: _repo),
    );
    if (saved == true) _refresh();
  }

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionHeader(
            title: 'Certifications',
            trailing: widget.canEdit
                ? IconButton(
                    onPressed: _addCertification,
                    icon: Icon(Icons.add_circle_outline_rounded, color: AppColors.primary, size: 22.r),
                    tooltip: 'Add certification',
                  )
                : null,
          ),
          FutureBuilder<List<DogCertification>>(
            future: _future,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return Padding(
                  padding: EdgeInsets.symmetric(vertical: 12.h),
                  child: const Center(child: SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))),
                );
              }
              final certs = snapshot.data ?? [];
              if (certs.isEmpty) return _emptyLine('No certifications recorded');
              return Column(
                children: certs.map((c) => Padding(
                      padding: EdgeInsets.only(bottom: 10.h),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            width: 36.r,
                            height: 36.r,
                            decoration: BoxDecoration(color: const Color(0xFFFEF3C7), borderRadius: BorderRadius.circular(10.r)),
                            child: Icon(Icons.workspace_premium_rounded, size: 18.r, color: const Color(0xFFB45309)),
                          ),
                          SizedBox(width: 12.w),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(c.certificationName, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600)),
                                if ((c.issuingBody ?? '').isNotEmpty)
                                  Text(c.issuingBody!, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                                if (c.expiryDate != null)
                                  Text(
                                    c.isExpired ? 'Expired ${Fmt.date(c.expiryDate)}' : 'Valid until ${Fmt.date(c.expiryDate)}',
                                    style: TextStyle(fontSize: 11.sp, color: c.isExpired ? AppColors.danger : AppColors.success, fontWeight: FontWeight.w600),
                                  ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    )).toList(),
              );
            },
          ),
        ],
      ),
    );
  }
}

class _CertificationForm extends StatefulWidget {
  final int dogId;
  final DogsRepository repo;
  const _CertificationForm({required this.dogId, required this.repo});

  @override
  State<_CertificationForm> createState() => _CertificationFormState();
}

class _CertificationFormState extends State<_CertificationForm> {
  final _name = TextEditingController();
  final _issuer = TextEditingController();
  final _trainer = TextEditingController();
  DateTime _issueDate = DateTime.now();
  DateTime? _expiryDate;
  final _notes = TextEditingController();
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _issuer.dispose();
    _trainer.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final name = _name.text.trim();
    if (name.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Certification name is required.')));
      return;
    }
    setState(() => _saving = true);
    try {
      await widget.repo.createCertification({
        'dogId': widget.dogId,
        'certificationName': name,
        'issuingBody': _issuer.text.trim().isEmpty ? null : _issuer.text.trim(),
        'trainerName': _trainer.text.trim().isEmpty ? null : _trainer.text.trim(),
        'issueDate': _issueDate.toIso8601String(),
        'expiryDate': _expiryDate?.toIso8601String(),
        'notes': _notes.text.trim().isEmpty ? null : _notes.text.trim(),
      });
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) {
        setState(() => _saving = false);
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e')));
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
            Text('Add Certification', style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800)),
            SizedBox(height: 14.h),
            TextField(
              controller: _name,
              decoration: const InputDecoration(labelText: 'Certification name', hintText: 'e.g. Obedience Level 2', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _issuer,
              decoration: const InputDecoration(labelText: 'Issuing body', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _trainer,
              decoration: const InputDecoration(labelText: 'Trainer', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            Row(
              children: [
                Expanded(
                  child: InkWell(
                    onTap: () async {
                      final picked = await showDatePicker(context: context, initialDate: _issueDate, firstDate: DateTime(2000), lastDate: DateTime.now());
                      if (picked != null) setState(() => _issueDate = picked);
                    },
                    child: InputDecorator(
                      decoration: const InputDecoration(labelText: 'Issue date', border: OutlineInputBorder()),
                      child: Text(Fmt.date(_issueDate)),
                    ),
                  ),
                ),
                SizedBox(width: 12.w),
                Expanded(
                  child: InkWell(
                    onTap: () async {
                      final picked = await showDatePicker(
                        context: context,
                        initialDate: _expiryDate ?? DateTime.now().add(const Duration(days: 365)),
                        firstDate: DateTime(2000),
                        lastDate: DateTime(2100),
                      );
                      if (picked != null) setState(() => _expiryDate = picked);
                    },
                    child: InputDecorator(
                      decoration: const InputDecoration(labelText: 'Expiry (optional)', border: OutlineInputBorder()),
                      child: Text(_expiryDate == null ? 'None' : Fmt.date(_expiryDate)),
                    ),
                  ),
                ),
              ],
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _notes,
              maxLines: 2,
              decoration: const InputDecoration(labelText: 'Notes', border: OutlineInputBorder()),
            ),
            SizedBox(height: 16.h),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _saving ? null : _save,
                child: _saving
                    ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Save Certification'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  final Animal dog;
  const _Header({required this.dog});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Row(
        children: [
          DogAvatar(
            photoUrl: dog.photoUrl, 
            size: 72,
            enableFullScreenView: true,
            heroTag: 'dog-profile-${dog.id}',
          ),
          SizedBox(width: 16.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(dog.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontSize: 20.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                SizedBox(height: 4.h),
                Text(dog.companyName ?? dog.breed,
                    maxLines: 1, overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontSize: 13.sp, color: AppColors.textSecondary)),
                SizedBox(height: 10.h),
                Wrap(
                  spacing: 8.w,
                  runSpacing: 6.h,
                  children: [
                    if (dog.isDeceased)
                      StatusChip.forStatus('DECEASED')
                    else
                      StatusChip.boolStatus(dog.isActive),
                    if (dog.isDeployed) StatusChip.forStatus('DEPLOYED'),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _CareTimeline extends StatelessWidget {
  final List<CareRecord> items;
  const _CareTimeline({required this.items});

  static const Map<String, (String icon, int color)> _eventTypes = {
    'Feeding': ('🍗', 0xFF10B981),
    'Vaccinated': ('💉', 0xFF0369A1),
    'Illness': ('🤒', 0xFFB45309),
    'Injured': ('🩹', 0xFFDC2626),
    'Bite someone': ('⚠️', 0xFF92400E),
    'Heat': ('🔥', 0xFFE11D48),
    'Mating': ('❤️', 0xFFBE185D),
    'Birth': ('✨', 0xFF7C3AED),
    'Other': ('•••', 0xFF475569),
  };

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionHeader(title: 'Care Timeline (${items.length})'),
          if (items.isEmpty)
            _emptyLine('No care activity recorded yet')
          else
            ...items.take(5).map((r) {
              final (emoji, _) = _eventTypes[r.eventType] ?? ('•••', 0xFF475569);
              return Padding(
                padding: EdgeInsets.only(bottom: 12.h),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Text(emoji, style: TextStyle(fontSize: 18.sp)),
                        SizedBox(width: 8.w),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(r.eventType,
                                  style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600)),
                              SizedBox(height: 2.h),
                              Text(
                                Fmt.eventWhen(r.occurredOn ?? DateTime.now()),
                                style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary),
                              ),
                            ],
                          ),
                        ),
                        if (r.requiresAttention)
                          Container(
                            padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h),
                            decoration: BoxDecoration(
                              color: const Color(0xFFFEE2E2),
                              borderRadius: BorderRadius.circular(4.r),
                            ),
                            child: Text('Attention', style: TextStyle(fontSize: 11.sp, color: const Color(0xFFDC2626), fontWeight: FontWeight.w600)),
                          ),
                      ],
                    ),
                    if ((r.details ?? '').isNotEmpty) ...[
                      SizedBox(height: 6.h),
                      Text(r.details!, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary, height: 1.4)),
                    ],
                    if ((r.quantityOrOutcome ?? '').isNotEmpty) ...[
                      SizedBox(height: 4.h),
                      Text('✓ ${r.quantityOrOutcome}', style: TextStyle(fontSize: 12.sp, color: AppColors.info, fontWeight: FontWeight.w500)),
                    ],
                    if ((r.recordedByName ?? '').isNotEmpty) ...[
                      SizedBox(height: 4.h),
                      Text(r.recordedByName!, style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                    ],
                  ],
                ),
              );
            }),
          if (items.length > 5)
            Padding(
              padding: EdgeInsets.only(top: 8.h),
              child: Text('+ ${items.length - 5} more records', style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted, fontStyle: FontStyle.italic)),
            ),
        ],
      ),
    );
  }
}

class _Vaccinations extends StatelessWidget {
  final List<Vaccination> items;
  const _Vaccinations({required this.items});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionHeader(title: 'Vaccinations (${items.length})'),
          if (items.isEmpty)
            _emptyLine('No vaccination records')
          else
            ...items.map((v) => Padding(
                  padding: EdgeInsets.only(bottom: 10.h),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Container(
                        width: 36.r,
                        height: 36.r,
                        decoration: BoxDecoration(color: AppColors.infoLight, borderRadius: BorderRadius.circular(10.r)),
                        child: Icon(Icons.vaccines_outlined, size: 18.r, color: AppColors.info),
                      ),
                      SizedBox(width: 12.w),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(v.name ?? v.vaccType ?? 'Vaccination',
                                style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600)),
                            SizedBox(height: 2.h),
                            Text(
                              '${Fmt.date(v.vaccinationDate)}'
                              '${v.nextVaccinationDate != null ? '  •  Next: ${Fmt.date(v.nextVaccinationDate)}' : ''}',
                              style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary),
                            ),
                            if ((v.vetclinicName ?? '').isNotEmpty)
                              Text(v.vetclinicName!, style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted)),
                          ],
                        ),
                      ),
                    ],
                  ),
                )),
        ],
      ),
    );
  }
}

class _DocumentsSection extends StatefulWidget {
  final int dogId;
  final bool canEdit;
  const _DocumentsSection({required this.dogId, required this.canEdit});

  @override
  State<_DocumentsSection> createState() => _DocumentsSectionState();
}

class _DocumentsSectionState extends State<_DocumentsSection> {
  final _repo = DogsRepository();
  Future<List<DocumentAttachment>>? _future;
  bool _uploading = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() => _future = _repo.getDocuments('Dog', widget.dogId);

  Future<void> _refresh() async {
    setState(_load);
    await _future;
  }

  Future<void> _pickAndUpload() async {
    final companyId = context.read<AuthProvider>().companyId;
    final picked = await ImagePicker().pickImage(source: ImageSource.gallery, maxWidth: 1600);
    if (picked == null) return;
    setState(() => _uploading = true);
    try {
      await _repo.uploadDocument(filePath: picked.path, companyId: companyId, entityType: 'Dog', entityId: widget.dogId);
      await _refresh();
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Upload failed: $e')));
    } finally {
      if (mounted) setState(() => _uploading = false);
    }
  }

  Future<void> _delete(DocumentAttachment doc) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete document?'),
        content: Text('Remove ${doc.fileName}?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await _repo.deleteDocument(doc.id);
      _refresh();
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionHeader(
            title: 'Documents',
            trailing: widget.canEdit
                ? (_uploading
                    ? SizedBox(width: 18.r, height: 18.r, child: const CircularProgressIndicator(strokeWidth: 2))
                    : IconButton(
                        onPressed: _pickAndUpload,
                        icon: Icon(Icons.upload_file_rounded, color: AppColors.primary, size: 22.r),
                        tooltip: 'Upload a document',
                      ))
                : null,
          ),
          FutureBuilder<List<DocumentAttachment>>(
            future: _future,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return Padding(
                  padding: EdgeInsets.symmetric(vertical: 12.h),
                  child: const Center(child: SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))),
                );
              }
              final docs = snapshot.data ?? [];
              if (docs.isEmpty) return _emptyLine('No documents attached — tap upload to add a photo of a certificate, insurance doc, etc.');
              return Column(
                children: docs.map((d) => Padding(
                      padding: EdgeInsets.only(bottom: 8.h),
                      child: Row(
                        children: [
                          Container(
                            width: 36.r,
                            height: 36.r,
                            decoration: BoxDecoration(color: AppColors.infoLight, borderRadius: BorderRadius.circular(10.r)),
                            child: Icon(Icons.insert_drive_file_rounded, size: 18.r, color: AppColors.info),
                          ),
                          SizedBox(width: 12.w),
                          Expanded(
                            child: Text(d.fileName, maxLines: 1, overflow: TextOverflow.ellipsis,
                                style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w600)),
                          ),
                          IconButton(
                            tooltip: 'Delete',
                            icon: Icon(Icons.delete_outline_rounded, color: AppColors.danger, size: 20.r),
                            onPressed: () => _delete(d),
                          ),
                        ],
                      ),
                    )).toList(),
              );
            },
          ),
        ],
      ),
    );
  }
}

class _MatingRecords extends StatelessWidget {
  final List<MatingRecord> items;
  const _MatingRecords({required this.items});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionHeader(title: 'Breeding Records (${items.length})'),
          if (items.isEmpty)
            _emptyLine('No breeding records')
          else
            ...items.map((m) => Padding(
                  padding: EdgeInsets.only(bottom: 10.h),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('${m.sireName ?? 'Sire'} × ${m.damName ?? 'Dam'}',
                          style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600)),
                      SizedBox(height: 2.h),
                      Text(
                        '${Fmt.date(m.matingDate)}'
                        '${(m.location ?? '').isNotEmpty ? '  •  ${m.location}' : ''}'
                        '  •  ${m.litters.length} litter(s)',
                        style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary),
                      ),
                    ],
                  ),
                )),
        ],
      ),
    );
  }
}

Widget _emptyLine(String text) => Padding(
      padding: EdgeInsets.symmetric(vertical: 6.h),
      child: Text(text, style: TextStyle(fontSize: 13.sp, color: AppColors.textMuted)),
    );
