import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/form_fields.dart';
import '../../../shared/widgets/inputs.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/business_repository.dart';

class CustomerFormScreen extends StatefulWidget {
  const CustomerFormScreen({super.key});

  @override
  State<CustomerFormScreen> createState() => _CustomerFormScreenState();
}

class _CustomerFormScreenState extends State<CustomerFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _repo = BusinessRepository();

  final _first = TextEditingController();
  final _last = TextEditingController();
  final _phone = TextEditingController();
  final _email = TextEditingController();
  final _idNumber = TextEditingController();
  final _address = TextEditingController();
  String? _gender;
  bool _saving = false;

  @override
  void dispose() {
    _first.dispose();
    _last.dispose();
    _phone.dispose();
    _email.dispose();
    _idNumber.dispose();
    _address.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    final companyId = context.read<AuthProvider>().companyId;
    final fullName = '${_first.text.trim()} ${_last.text.trim()}'.trim();
    try {
      await _repo.createCustomer({
        'companyId': companyId,
        'firstName': _first.text.trim(),
        'lastName': _last.text.trim(),
        'fullName': fullName,
        'gender': _gender,
        'cellphone': _phone.text.trim().isEmpty ? null : _phone.text.trim(),
        'email': _email.text.trim().isEmpty ? null : _email.text.trim(),
        'iDnumber': _idNumber.text.trim().isEmpty ? null : _idNumber.text.trim(),
        'address': _address.text.trim().isEmpty ? null : _address.text.trim(),
        'isActive': true,
      });
      if (!mounted) return;
      Navigator.of(context).pop(true);
    } on ApiException catch (e) {
      _toast(e.message, error: true);
    } catch (_) {
      _toast('Could not save customer.', error: true);
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
      appBar: AppBar(title: const Text('Add Customer')),
      body: SafeArea(
        child: Form(
          key: _formKey,
          child: ListView(
            padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
            children: [
              AppTextField(
                controller: _first,
                label: 'First Name',
                icon: Icons.person_outline_rounded,
                validator: (v) => (v == null || v.trim().isEmpty) ? 'First name is required' : null,
              ),
              SizedBox(height: 16.h),
              AppTextField(
                controller: _last,
                label: 'Last Name',
                icon: Icons.person_outline_rounded,
                validator: (v) => (v == null || v.trim().isEmpty) ? 'Last name is required' : null,
              ),
              SizedBox(height: 16.h),
              AppDropdownField<String>(
                label: 'Gender (optional)',
                value: _gender,
                icon: Icons.wc_rounded,
                hint: 'Select',
                items: const [
                  DropdownMenuItem(value: 'Male', child: Text('Male')),
                  DropdownMenuItem(value: 'Female', child: Text('Female')),
                ],
                onChanged: (v) => setState(() => _gender = v),
              ),
              SizedBox(height: 16.h),
              AppTextField(
                controller: _phone,
                label: 'Cellphone',
                icon: Icons.phone_outlined,
                keyboardType: TextInputType.phone,
              ),
              SizedBox(height: 16.h),
              AppTextField(
                controller: _email,
                label: 'Email (optional)',
                icon: Icons.email_outlined,
                keyboardType: TextInputType.emailAddress,
              ),
              SizedBox(height: 16.h),
              AppTextField(controller: _idNumber, label: 'ID Number (optional)', icon: Icons.badge_outlined),
              SizedBox(height: 16.h),
              AppTextField(controller: _address, label: 'Address (optional)', icon: Icons.home_outlined, maxLines: 2),
              SizedBox(height: 24.h),
              PrimaryButton(label: 'Save Customer', loading: _saving, onPressed: _save, icon: Icons.check_rounded),
            ],
          ),
        ),
      ),
    );
  }
}
