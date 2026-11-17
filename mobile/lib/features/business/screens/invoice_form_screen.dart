import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/cards.dart';
import '../../auth/providers/auth_provider.dart';
import '../../dogs/models/animal.dart';
import '../../dogs/screens/dog_picker_screen.dart';
import '../data/business_repository.dart';
import '../models/business_models.dart';
import 'customer_picker_screen.dart';

/// One line item being edited on the form — holds its own controllers so each
/// row can compute its own amount live as the user types.
class _LineItemEntry {
  final description = TextEditingController();
  final quantity = TextEditingController(text: '1');
  final unitPrice = TextEditingController(text: '0');
  final taxRate = TextEditingController(text: '0');
  final discountRate = TextEditingController(text: '0');

  double get _qty => double.tryParse(quantity.text) ?? 0;
  double get _price => double.tryParse(unitPrice.text) ?? 0;
  double get subtotal => _qty * _price;
  double get amount {
    final tax = double.tryParse(taxRate.text) ?? 0;
    final disc = double.tryParse(discountRate.text) ?? 0;
    return _qty * _price * (1 - disc / 100) * (1 + tax / 100);
  }

  void dispose() {
    description.dispose();
    quantity.dispose();
    unitPrice.dispose();
    taxRate.dispose();
    discountRate.dispose();
  }
}

/// Full-screen "New Invoice" form: pick a customer (searchable), add line
/// items (each optionally linked to one of our dogs via a searchable picker),
/// set tax/discount, notes and terms, then POST to the API — mirrors the
/// Portal's Create Invoice modal.
class InvoiceFormScreen extends StatefulWidget {
  const InvoiceFormScreen({super.key});

  @override
  State<InvoiceFormScreen> createState() => _InvoiceFormScreenState();
}

class _InvoiceFormScreenState extends State<InvoiceFormScreen> {
  final _repo = BusinessRepository();
  Customer? _customer;
  DateTime _invoiceDate = DateTime.now();
  DateTime _dueDate = DateTime.now().add(const Duration(days: 30));
  final _taxAmount = TextEditingController(text: '0');
  final _discountAmount = TextEditingController(text: '0');
  final _notes = TextEditingController();
  final _terms = TextEditingController();
  final List<_LineItemEntry> _items = [_LineItemEntry()];
  bool _saving = false;

  @override
  void dispose() {
    for (final item in _items) {
      item.dispose();
    }
    _taxAmount.dispose();
    _discountAmount.dispose();
    _notes.dispose();
    _terms.dispose();
    super.dispose();
  }

  double get _subtotal => _items.fold(0.0, (sum, it) => sum + it.subtotal);
  double get _total =>
      _items.fold(0.0, (sum, it) => sum + it.amount) -
      (double.tryParse(_discountAmount.text) ?? 0) +
      (double.tryParse(_taxAmount.text) ?? 0);

  Future<void> _pickCustomer() async {
    final c = await Navigator.of(context).push<Customer>(
      MaterialPageRoute(builder: (_) => const CustomerPickerScreen()),
    );
    if (c != null) setState(() => _customer = c);
  }

  Future<void> _pickDogForItem(_LineItemEntry item) async {
    final dog = await Navigator.of(context).push<Animal>(
      MaterialPageRoute(builder: (_) => const DogPickerScreen(title: 'Pick a dog')),
    );
    if (dog != null) setState(() => item.description.text = '${dog.name} (${dog.breed})');
  }

  Future<void> _pickDate(bool isInvoiceDate) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: isInvoiceDate ? _invoiceDate : _dueDate,
      firstDate: DateTime(2015),
      lastDate: DateTime(2035),
    );
    if (picked != null) {
      setState(() {
        if (isInvoiceDate) {
          _invoiceDate = picked;
        } else {
          _dueDate = picked;
        }
      });
    }
  }

  void _addItem() => setState(() => _items.add(_LineItemEntry()));

  void _removeItem(int i) {
    if (_items.length == 1) return;
    setState(() {
      _items[i].dispose();
      _items.removeAt(i);
    });
  }

  Future<void> _save() async {
    if (_customer == null) {
      _toast('Please pick a customer.');
      return;
    }
    final validItems = _items.where((it) => it.description.text.trim().isNotEmpty).toList();
    if (validItems.isEmpty) {
      _toast('Add at least one line item.');
      return;
    }
    setState(() => _saving = true);
    try {
      final companyId = context.read<AuthProvider>().companyId;
      await _repo.createInvoice({
        'companyId': companyId,
        'customerId': _customer!.id.toString(),
        'customerName': _customer!.displayName,
        'customerEmail': _customer!.email,
        'customerPhone': _customer!.cellphone,
        'invoiceDate': _invoiceDate.toIso8601String(),
        'dueDate': _dueDate.toIso8601String(),
        'taxAmount': double.tryParse(_taxAmount.text) ?? 0,
        'discountAmount': double.tryParse(_discountAmount.text) ?? 0,
        'notes': _notes.text.trim().isEmpty ? null : _notes.text.trim(),
        'terms': _terms.text.trim().isEmpty ? null : _terms.text.trim(),
        'items': validItems
            .map((it) => {
                  'description': it.description.text.trim(),
                  'quantity': int.tryParse(it.quantity.text) ?? 1,
                  'unitPrice': double.tryParse(it.unitPrice.text) ?? 0,
                  'taxRate': double.tryParse(it.taxRate.text) ?? 0,
                  'discountRate': double.tryParse(it.discountRate.text) ?? 0,
                })
            .toList(),
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
      appBar: AppBar(title: const Text('New Invoice')),
      body: SafeArea(
        top: false,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 32.h),
          children: [
            AppCard(
              onTap: _pickCustomer,
              child: Row(
                children: [
                  Icon(Icons.person_outline_rounded, color: AppColors.primary, size: 22.r),
                  SizedBox(width: 12.w),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Customer', style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
                        SizedBox(height: 2.h),
                        Text(
                          _customer?.displayName ?? 'Tap to select a customer',
                          style: TextStyle(
                            fontSize: 14.sp,
                            fontWeight: FontWeight.w700,
                            color: _customer == null ? AppColors.textMuted : AppColors.textPrimary,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 22.r),
                ],
              ),
            ),
            SizedBox(height: 12.h),
            Row(
              children: [
                Expanded(child: _dateField('Invoice Date', _invoiceDate, () => _pickDate(true))),
                SizedBox(width: 12.w),
                Expanded(child: _dateField('Due Date', _dueDate, () => _pickDate(false))),
              ],
            ),
            SizedBox(height: 20.h),
            SectionHeader(
              title: 'Line Items',
              trailing: TextButton.icon(
                onPressed: _addItem,
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Add'),
              ),
            ),
            for (var i = 0; i < _items.length; i++) _lineItemCard(i),
            SizedBox(height: 8.h),
            AppCard(
              child: Column(
                children: [
                  _totalsRow('Subtotal', _subtotal),
                  SizedBox(height: 10.h),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _taxAmount,
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          decoration: const InputDecoration(labelText: 'Tax Amount', border: OutlineInputBorder()),
                          onChanged: (_) => setState(() {}),
                        ),
                      ),
                      SizedBox(width: 12.w),
                      Expanded(
                        child: TextField(
                          controller: _discountAmount,
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          decoration: const InputDecoration(labelText: 'Discount Amount', border: OutlineInputBorder()),
                          onChanged: (_) => setState(() {}),
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 10.h),
                  const Divider(height: 1),
                  SizedBox(height: 10.h),
                  _totalsRow('TOTAL', _total, bold: true),
                ],
              ),
            ),
            SizedBox(height: 16.h),
            TextField(
              controller: _notes,
              maxLines: 2,
              decoration: const InputDecoration(labelText: 'Notes', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _terms,
              maxLines: 2,
              decoration: const InputDecoration(labelText: 'Terms', border: OutlineInputBorder()),
            ),
            SizedBox(height: 20.h),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _saving ? null : _save,
                child: _saving
                    ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Create Invoice'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _dateField(String label, DateTime value, VoidCallback onTap) {
    return InkWell(
      onTap: onTap,
      child: InputDecorator(
        decoration: InputDecoration(labelText: label, border: const OutlineInputBorder()),
        child: Text(Fmt.date(value)),
      ),
    );
  }

  Widget _lineItemCard(int i) {
    final item = _items[i];
    return Padding(
      padding: EdgeInsets.only(bottom: 10.h),
      child: AppCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: TextField(
                    controller: item.description,
                    decoration: const InputDecoration(labelText: 'Description', border: OutlineInputBorder()),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
                SizedBox(width: 6.w),
                IconButton(
                  tooltip: 'Pick a dog',
                  icon: Icon(Icons.pets_rounded, color: AppColors.primary, size: 20.r),
                  onPressed: () => _pickDogForItem(item),
                ),
                if (_items.length > 1)
                  IconButton(
                    tooltip: 'Remove line',
                    icon: Icon(Icons.close_rounded, color: AppColors.danger, size: 20.r),
                    onPressed: () => _removeItem(i),
                  ),
              ],
            ),
            SizedBox(height: 8.h),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: item.quantity,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(labelText: 'Qty', border: OutlineInputBorder()),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
                SizedBox(width: 8.w),
                Expanded(
                  child: TextField(
                    controller: item.unitPrice,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: const InputDecoration(labelText: 'Unit Price', border: OutlineInputBorder()),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
              ],
            ),
            SizedBox(height: 8.h),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: item.taxRate,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: const InputDecoration(labelText: 'Tax %', border: OutlineInputBorder()),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
                SizedBox(width: 8.w),
                Expanded(
                  child: TextField(
                    controller: item.discountRate,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: const InputDecoration(labelText: 'Disc %', border: OutlineInputBorder()),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
              ],
            ),
            SizedBox(height: 8.h),
            Align(
              alignment: Alignment.centerRight,
              child: Text('Amount: ${Fmt.money(item.amount)}',
                  style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13.sp)),
            ),
          ],
        ),
      ),
    );
  }

  Widget _totalsRow(String label, double value, {bool bold = false}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: TextStyle(fontWeight: bold ? FontWeight.w800 : FontWeight.w500, fontSize: bold ? 15.sp : 13.sp)),
        Text(Fmt.money(value), style: TextStyle(fontWeight: bold ? FontWeight.w800 : FontWeight.w600, fontSize: bold ? 15.sp : 13.sp)),
      ],
    );
  }
}
