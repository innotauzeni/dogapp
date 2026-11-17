import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/form_fields.dart';
import '../../../shared/widgets/inputs.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/dogs_repository.dart';

class VaccinationFormScreen extends StatefulWidget {
  final int dogId;
  const VaccinationFormScreen({super.key, required this.dogId});

  @override
  State<VaccinationFormScreen> createState() => _VaccinationFormScreenState();
}

class _VaccinationFormScreenState extends State<VaccinationFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _repo = DogsRepository();

  final _name = TextEditingController();
  final _type = TextEditingController();
  final _batch = TextEditingController();
  final _clinic = TextEditingController();
  final _notes = TextEditingController();
  DateTime? _date;
  DateTime? _nextDate;
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _type.dispose();
    _batch.dispose();
    _clinic.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _pick({required bool next}) async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: next ? (now) : (_date ?? now),
      firstDate: DateTime(now.year - 20),
      lastDate: DateTime(now.year + 10),
    );
    if (picked != null) setState(() => next ? _nextDate = picked : _date = picked);
  }

  Future<void> _save() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) return;
    if (_date == null) {
      _toast('Select the vaccination date', error: true);
      return;
    }
    setState(() => _saving = true);
    final companyId = context.read<AuthProvider>().companyId;
    try {
      await _repo.addVaccination({
        'dogId': widget.dogId,
        'companyId': companyId,
        'name': _name.text.trim(),
        'vaccType': _type.text.trim().isEmpty ? null : _type.text.trim(),
        'batchNumber': _batch.text.trim().isEmpty ? null : _batch.text.trim(),
        'vetclinicName': _clinic.text.trim().isEmpty ? null : _clinic.text.trim(),
        'vaccinationDate': _date!.toUtc().toIso8601String(),
        'nextVaccinationDate': _nextDate?.toUtc().toIso8601String(),
        'notes': _notes.text.trim().isEmpty ? null : _notes.text.trim(),
      });
      if (!mounted) return;
      Navigator.of(context).pop(true);
    } on ApiException catch (e) {
      _toast(e.message, error: true);
    } catch (_) {
      _toast('Could not save vaccination.', error: true);
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
      appBar: AppBar(title: const Text('Add Vaccination')),
      body: SafeArea(
        child: Form(
          key: _formKey,
          child: ListView(
            padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
            children: [
              AppTextField(
                controller: _name,
                label: 'Vaccine Name',
                hint: 'e.g. Rabies',
                icon: Icons.vaccines_outlined,
                validator: (v) => (v == null || v.trim().isEmpty) ? 'Name is required' : null,
              ),
              SizedBox(height: 16.h),
              AppTextField(controller: _type, label: 'Type (optional)', hint: 'e.g. Core', icon: Icons.category_outlined),
              SizedBox(height: 16.h),
              AppTextField(controller: _batch, label: 'Batch Number (optional)', icon: Icons.tag_rounded),
              SizedBox(height: 16.h),
              AppTextField(controller: _clinic, label: 'Vet / Clinic (optional)', icon: Icons.local_hospital_outlined),
              SizedBox(height: 16.h),
              DateField(label: 'Vaccination Date', value: _date == null ? null : Fmt.date(_date), onTap: () => _pick(next: false)),
              SizedBox(height: 16.h),
              DateField(label: 'Next Due (optional)', value: _nextDate == null ? null : Fmt.date(_nextDate), onTap: () => _pick(next: true)),
              SizedBox(height: 16.h),
              AppTextField(controller: _notes, label: 'Notes (optional)', icon: Icons.notes_rounded, maxLines: 3),
              SizedBox(height: 24.h),
              PrimaryButton(label: 'Save Vaccination', loading: _saving, onPressed: _save, icon: Icons.check_rounded),
            ],
          ),
        ),
      ),
    );
  }
}
