import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/form_fields.dart';
import '../../../shared/widgets/inputs.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/providers/auth_provider.dart';
import '../../dogs/data/dogs_repository.dart';
import '../../dogs/models/animal.dart';

class MatingFormScreen extends StatefulWidget {
  const MatingFormScreen({super.key});

  @override
  State<MatingFormScreen> createState() => _MatingFormScreenState();
}

class _MatingFormScreenState extends State<MatingFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _repo = DogsRepository();
  final _location = TextEditingController();
  final _notes = TextEditingController();
  final _sireName = TextEditingController();
  final _damName = TextEditingController();

  Future<List<Animal>>? _pets;
  int? _sireId;
  int? _damId;
  bool _sireIsOurs = true;
  bool _damIsOurs = true;
  DateTime? _date;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final companyId = context.read<AuthProvider>().companyId;
    _pets = _repo.getPets(companyId);
  }

  @override
  void dispose() {
    _location.dispose();
    _notes.dispose();
    _sireName.dispose();
    _damName.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date ?? now,
      firstDate: DateTime(now.year - 5),
      lastDate: now,
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _save() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) return;

    // Validate sire selection
    if (_sireIsOurs && _sireId == null) {
      _toast('Select a sire from your dogs', error: true);
      return;
    }
    if (!_sireIsOurs && _sireName.text.trim().isEmpty) {
      _toast('Enter the sire name', error: true);
      return;
    }

    // Validate dam selection
    if (_damIsOurs && _damId == null) {
      _toast('Select a dam from your dogs', error: true);
      return;
    }
    if (!_damIsOurs && _damName.text.trim().isEmpty) {
      _toast('Enter the dam name', error: true);
      return;
    }

    // Can't mate same dog with itself
    if (_sireIsOurs && _damIsOurs && _sireId == _damId) {
      _toast('Sire and dam must be different dogs', error: true);
      return;
    }

    if (_date == null) {
      _toast('Select the mating date', error: true);
      return;
    }

    setState(() => _saving = true);
    final companyId = context.read<AuthProvider>().companyId;
    try {
      await _repo.addMatingRecord({
        'sireId': _sireIsOurs ? _sireId : null,
        'sireName': !_sireIsOurs ? _sireName.text.trim() : null,
        'damId': _damIsOurs ? _damId : null,
        'damName': !_damIsOurs ? _damName.text.trim() : null,
        'companyId': companyId,
        'matingDate': _date!.toUtc().toIso8601String(),
        'location': _location.text.trim().isEmpty ? null : _location.text.trim(),
        'notes': _notes.text.trim().isEmpty ? null : _notes.text.trim(),
      });
      if (!mounted) return;
      Navigator.of(context).pop(true);
    } on ApiException catch (e) {
      _toast(e.message, error: true);
    } catch (_) {
      _toast('Could not save mating record.', error: true);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _toast(String msg, {bool error = false}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(backgroundColor: error ? AppColors.danger : AppColors.success, content: Text(msg)),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Record Mating'),
        elevation: 0,
      ),
      body: SafeArea(
        child: FutureBuilder<List<Animal>>(
          future: _pets,
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const AppLoader(label: 'Loading dogs…');
            }
            final dogs = snapshot.data ?? const <Animal>[];
            final sireOptions = dogs.where((d) {
              final g = (d.gender ?? '').toLowerCase();
              return g.isEmpty || g.startsWith('m');
            }).toList();
            final damOptions = dogs.where((d) {
              final g = (d.gender ?? '').toLowerCase();
              return g.isEmpty || g.startsWith('f');
            }).toList();

            return Form(
              key: _formKey,
              child: ListView(
                padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
                children: [
                  // Header
                  Padding(
                    padding: EdgeInsets.only(bottom: 20.h),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Sire (Male Parent)', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                        SizedBox(height: 8.h),
                        _PartnerSelector(
                          isOurs: _sireIsOurs,
                          onToggle: () => setState(() => _sireIsOurs = !_sireIsOurs),
                        ),
                      ],
                    ),
                  ),

                  // Sire selection
                  if (_sireIsOurs)
                    AppDropdownField<int>(
                      label: 'Select sire',
                      value: _sireId,
                      icon: Icons.male_rounded,
                      hint: 'Choose from your dogs',
                      items: [for (final d in sireOptions) DropdownMenuItem(value: d.id, child: Text(d.name))],
                      onChanged: (v) => setState(() => _sireId = v),
                      validator: (v) => v == null ? 'Select a sire' : null,
                    )
                  else
                    AppTextField(
                      controller: _sireName,
                      label: 'Sire name',
                      icon: Icons.male_rounded,
                      hint: 'e.g., Champion Duke',
                      validator: (v) => (v ?? '').isEmpty ? 'Enter sire name' : null,
                    ),

                  SizedBox(height: 20.h),

                  // Dam header
                  Padding(
                    padding: EdgeInsets.only(bottom: 8.h),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Dam (Female Parent)', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                        SizedBox(height: 8.h),
                        _PartnerSelector(
                          isOurs: _damIsOurs,
                          onToggle: () => setState(() => _damIsOurs = !_damIsOurs),
                        ),
                      ],
                    ),
                  ),

                  // Dam selection
                  if (_damIsOurs)
                    AppDropdownField<int>(
                      label: 'Select dam',
                      value: _damId,
                      icon: Icons.female_rounded,
                      hint: 'Choose from your dogs',
                      items: [for (final d in damOptions) DropdownMenuItem(value: d.id, child: Text(d.name))],
                      onChanged: (v) => setState(() => _damId = v),
                      validator: (v) => v == null ? 'Select a dam' : null,
                    )
                  else
                    AppTextField(
                      controller: _damName,
                      label: 'Dam name',
                      icon: Icons.female_rounded,
                      hint: 'e.g., Lady Diamond',
                      validator: (v) => (v ?? '').isEmpty ? 'Enter dam name' : null,
                    ),

                  SizedBox(height: 20.h),

                  // Mating details
                  DateField(label: 'Mating Date', value: _date == null ? null : Fmt.date(_date), onTap: _pickDate),
                  SizedBox(height: 16.h),
                  AppTextField(controller: _location, label: 'Location (optional)', icon: Icons.location_on_outlined),
                  SizedBox(height: 16.h),
                  AppTextField(controller: _notes, label: 'Notes (optional)', icon: Icons.notes_rounded, maxLines: 3),

                  SizedBox(height: 24.h),

                  // Save button
                  SizedBox(
                    height: 48.h,
                    child: FilledButton.icon(
                      onPressed: _saving ? null : _save,
                      icon: _saving ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.favorite_rounded),
                      label: Text(_saving ? 'Saving…' : 'Record Mating', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600)),
                    ),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

class _PartnerSelector extends StatelessWidget {
  final bool isOurs;
  final VoidCallback onToggle;

  const _PartnerSelector({required this.isOurs, required this.onToggle});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: AppColors.border),
        borderRadius: BorderRadius.circular(10.r),
      ),
      child: Row(
        children: [
          Expanded(
            child: GestureDetector(
              onTap: isOurs ? null : onToggle,
              child: Container(
                padding: EdgeInsets.symmetric(vertical: 10.h),
                decoration: BoxDecoration(
                  color: isOurs ? AppColors.primary : Colors.transparent,
                  borderRadius: BorderRadius.only(
                    topLeft: Radius.circular(9.r),
                    bottomLeft: Radius.circular(9.r),
                  ),
                ),
                child: Center(
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.home_rounded, size: 16.r, color: isOurs ? AppColors.surface : AppColors.textMuted),
                      SizedBox(width: 4.w),
                      Text('Our Dog', style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w600, color: isOurs ? AppColors.surface : AppColors.textMuted)),
                    ],
                  ),
                ),
              ),
            ),
          ),
          const VerticalDivider(width: 1),
          Expanded(
            child: GestureDetector(
              onTap: !isOurs ? null : onToggle,
              child: Container(
                padding: EdgeInsets.symmetric(vertical: 10.h),
                decoration: BoxDecoration(
                  color: !isOurs ? AppColors.warning : Colors.transparent,
                  borderRadius: BorderRadius.only(
                    topRight: Radius.circular(9.r),
                    bottomRight: Radius.circular(9.r),
                  ),
                ),
                child: Center(
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.language_rounded, size: 16.r, color: !isOurs ? AppColors.surface : AppColors.textMuted),
                      SizedBox(width: 4.w),
                      Text('Outside', style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.w600, color: !isOurs ? AppColors.surface : AppColors.textMuted)),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
