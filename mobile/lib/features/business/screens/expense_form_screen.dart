import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../auth/providers/auth_provider.dart';
import '../../dogs/data/dogs_repository.dart';
import '../../dogs/models/animal.dart';
import '../data/business_repository.dart';

/// Record an operating cost (food, vet, fuel, salaries...), optionally tied to a dog.
class ExpenseFormScreen extends StatefulWidget {
  const ExpenseFormScreen({super.key});

  @override
  State<ExpenseFormScreen> createState() => _ExpenseFormScreenState();
}

class _ExpenseFormScreenState extends State<ExpenseFormScreen> {
  static const _categories = ['Food', 'Vet', 'Fuel', 'Salaries', 'Equipment', 'Training', 'Other'];

  final _repo = BusinessRepository();
  final _dogsRepo = DogsRepository();
  String _category = 'Food';
  final _description = TextEditingController();
  final _amount = TextEditingController();
  DateTime _expenseDate = DateTime.now();
  final _vendor = TextEditingController();
  String? _paymentMethod;
  final _notes = TextEditingController();
  Animal? _dog;
  bool _saving = false;

  @override
  void dispose() {
    _description.dispose();
    _amount.dispose();
    _vendor.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _expenseDate,
      firstDate: DateTime(2015),
      lastDate: DateTime.now().add(const Duration(days: 1)),
    );
    if (picked != null) setState(() => _expenseDate = picked);
  }

  Future<void> _pickDog() async {
    final companyId = context.read<AuthProvider>().companyId;
    final dogs = await _dogsRepo.getPets(companyId);
    if (!mounted) return;
    final chosen = await showModalBottomSheet<Animal?>(
      context: context,
      isScrollControlled: true,
      builder: (_) => _DogPickerSheet(dogs: dogs),
    );
    setState(() => _dog = chosen);
  }

  Future<void> _save() async {
    final description = _description.text.trim();
    final amount = double.tryParse(_amount.text.trim());
    if (description.isEmpty) {
      _toast('Description is required.');
      return;
    }
    if (amount == null || amount <= 0) {
      _toast('Please enter a valid amount.');
      return;
    }

    setState(() => _saving = true);
    try {
      final companyId = context.read<AuthProvider>().companyId;
      await _repo.createExpense({
        'companyId': companyId,
        'category': _category,
        'description': description,
        'amount': amount,
        'expenseDate': _expenseDate.toIso8601String(),
        'dogId': _dog?.id,
        'vendor': _vendor.text.trim().isEmpty ? null : _vendor.text.trim(),
        'paymentMethod': _paymentMethod,
        'notes': _notes.text.trim().isEmpty ? null : _notes.text.trim(),
      });
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) {
        setState(() => _saving = false);
        _toast('Failed: $e');
      }
    }
  }

  void _toast(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('New Expense')),
      body: SafeArea(
        top: false,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 32.h),
          children: [
            DropdownButtonFormField<String>(
              // ignore: deprecated_member_use
              value: _category,
              decoration: const InputDecoration(labelText: 'Category', border: OutlineInputBorder()),
              items: _categories.map((c) => DropdownMenuItem(value: c, child: Text(c))).toList(),
              onChanged: (v) => setState(() => _category = v ?? 'Food'),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _description,
              decoration: const InputDecoration(labelText: 'Description', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _amount,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: const InputDecoration(labelText: 'Amount', border: OutlineInputBorder()),
                  ),
                ),
                SizedBox(width: 12.w),
                Expanded(
                  child: InkWell(
                    onTap: _pickDate,
                    child: InputDecorator(
                      decoration: const InputDecoration(labelText: 'Date', border: OutlineInputBorder()),
                      child: Text(Fmt.date(_expenseDate)),
                    ),
                  ),
                ),
              ],
            ),
            SizedBox(height: 12.h),
            InkWell(
              onTap: _pickDog,
              child: InputDecorator(
                decoration: const InputDecoration(labelText: 'Dog (optional)', border: OutlineInputBorder()),
                child: Text(_dog?.name ?? 'Not dog-specific',
                    style: TextStyle(color: _dog == null ? AppColors.textMuted : AppColors.textPrimary)),
              ),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _vendor,
              decoration: const InputDecoration(labelText: 'Vendor', hintText: 'Paid to', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            DropdownButtonFormField<String>(
              // ignore: deprecated_member_use
              value: _paymentMethod,
              decoration: const InputDecoration(labelText: 'Payment Method', border: OutlineInputBorder()),
              items: const ['Cash', 'Bank Transfer', 'EcoCash', 'Card', 'Other']
                  .map((m) => DropdownMenuItem(value: m, child: Text(m)))
                  .toList(),
              onChanged: (v) => setState(() => _paymentMethod = v),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _notes,
              maxLines: 2,
              decoration: const InputDecoration(labelText: 'Notes', border: OutlineInputBorder()),
            ),
            SizedBox(height: 20.h),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _saving ? null : _save,
                child: _saving
                    ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Save Expense'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DogPickerSheet extends StatelessWidget {
  final List<Animal> dogs;
  const _DogPickerSheet({required this.dogs});

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: ListView(
        shrinkWrap: true,
        children: [
          ListTile(
            title: const Text('Not dog-specific'),
            onTap: () => Navigator.of(context).pop(),
          ),
          const Divider(height: 1),
          for (final d in dogs)
            ListTile(
              title: Text(d.name),
              subtitle: Text(d.breed),
              onTap: () => Navigator.of(context).pop(d),
            ),
        ],
      ),
    );
  }
}
