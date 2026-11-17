import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/form_fields.dart';
import '../../../shared/widgets/inputs.dart';
import '../../auth/providers/auth_provider.dart';
import '../../dogs/data/dogs_repository.dart';

class LitterFormScreen extends StatefulWidget {
  final int matingRecordId;
  final String? title;
  const LitterFormScreen({super.key, required this.matingRecordId, this.title});

  @override
  State<LitterFormScreen> createState() => _LitterFormScreenState();
}

class _LitterFormScreenState extends State<LitterFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _repo = DogsRepository();
  final _total = TextEditingController();
  final _survived = TextEditingController();
  final _died = TextEditingController();
  final _notes = TextEditingController();
  DateTime? _birthDate;
  bool _saving = false;

  @override
  void dispose() {
    _total.dispose();
    _survived.dispose();
    _died.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _birthDate ?? now,
      firstDate: DateTime(now.year - 5),
      lastDate: now,
    );
    if (picked != null) setState(() => _birthDate = picked);
  }

  Future<void> _save() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) return;
    if (_birthDate == null) {
      _toast('Select the birth date', error: true);
      return;
    }
    setState(() => _saving = true);
    final companyId = context.read<AuthProvider>().companyId;
    try {
      await _repo.addLitter({
        'matingRecordId': widget.matingRecordId,
        'companyId': companyId,
        'birthDate': _birthDate!.toUtc().toIso8601String(),
        'totalBorn': int.tryParse(_total.text.trim()) ?? 0,
        'survivedCount': int.tryParse(_survived.text.trim()) ?? 0,
        'diedCount': int.tryParse(_died.text.trim()) ?? 0,
        'notes': _notes.text.trim().isEmpty ? null : _notes.text.trim(),
      });
      if (!mounted) return;
      Navigator.of(context).pop(true);
    } on ApiException catch (e) {
      _toast(e.message, error: true);
    } catch (_) {
      _toast('Could not save litter.', error: true);
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
    final onlyDigits = [FilteringTextInputFormatter.digitsOnly];
    return Scaffold(
      appBar: AppBar(title: const Text('Add Litter')),
      body: SafeArea(
        child: Form(
          key: _formKey,
          child: ListView(
            padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
            children: [
              if ((widget.title ?? '').isNotEmpty) ...[
                Text(widget.title!, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary)),
                SizedBox(height: 16.h),
              ],
              DateField(label: 'Birth Date', value: _birthDate == null ? null : Fmt.date(_birthDate), onTap: _pickDate),
              SizedBox(height: 16.h),
              AppTextField(
                controller: _total,
                label: 'Total Born',
                keyboardType: TextInputType.number,
                formatters: onlyDigits,
                icon: Icons.numbers_rounded,
                validator: (v) => (v == null || v.trim().isEmpty) ? 'Required' : null,
              ),
              SizedBox(height: 16.h),
              AppTextField(
                controller: _survived,
                label: 'Survived',
                keyboardType: TextInputType.number,
                formatters: onlyDigits,
                icon: Icons.favorite_rounded,
              ),
              SizedBox(height: 16.h),
              AppTextField(
                controller: _died,
                label: 'Died',
                keyboardType: TextInputType.number,
                formatters: onlyDigits,
                icon: Icons.heart_broken_outlined,
              ),
              SizedBox(height: 16.h),
              AppTextField(controller: _notes, label: 'Notes (optional)', icon: Icons.notes_rounded, maxLines: 3),
              SizedBox(height: 24.h),
              PrimaryButton(label: 'Save Litter', loading: _saving, onPressed: _save, icon: Icons.check_rounded),
            ],
          ),
        ),
      ),
    );
  }
}
