import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../auth/providers/auth_provider.dart';
import '../data/branches_repository.dart';
import '../models/branch_models.dart';

/// Registers a new branch under the current (head-office) company.
class BranchFormScreen extends StatefulWidget {
  const BranchFormScreen({super.key});

  @override
  State<BranchFormScreen> createState() => _BranchFormScreenState();
}

class _BranchFormScreenState extends State<BranchFormScreen> {
  final _repo = BranchesRepository();

  final _name = TextEditingController();
  final _code = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  final _address = TextEditingController();
  final _location = TextEditingController();

  Future<List<LookupItem>>? _countriesFuture;
  Future<List<CurrencyOption>>? _currenciesFuture;
  List<LookupItem> _provinces = [];
  List<LookupItem> _cities = [];

  LookupItem? _country;
  LookupItem? _province;
  LookupItem? _city;
  CurrencyOption? _currency;

  bool _loadingProvinces = false;
  bool _loadingCities = false;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _countriesFuture = _repo.getCountries();
    _currenciesFuture = _repo.getCurrencies();
  }

  @override
  void dispose() {
    _name.dispose();
    _code.dispose();
    _email.dispose();
    _phone.dispose();
    _address.dispose();
    _location.dispose();
    super.dispose();
  }

  Future<void> _onCountryChanged(LookupItem? country) async {
    setState(() {
      _country = country;
      _province = null;
      _city = null;
      _provinces = [];
      _cities = [];
    });
    if (country == null) return;
    setState(() => _loadingProvinces = true);
    try {
      final provinces = await _repo.getProvinces(country.id);
      if (mounted) setState(() => _provinces = provinces);
    } catch (_) {
      // Leave the province list empty; the user can retry by reselecting the country.
    } finally {
      if (mounted) setState(() => _loadingProvinces = false);
    }
  }

  Future<void> _onProvinceChanged(LookupItem? province) async {
    setState(() {
      _province = province;
      _city = null;
      _cities = [];
    });
    if (province == null) return;
    setState(() => _loadingCities = true);
    try {
      final cities = await _repo.getCities(province.id);
      if (mounted) setState(() => _cities = cities);
    } catch (_) {
      // Leave the city list empty; the user can retry by reselecting the province.
    } finally {
      if (mounted) setState(() => _loadingCities = false);
    }
  }

  void _toast(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  Future<void> _save() async {
    final name = _name.text.trim();
    final code = _code.text.trim();
    if (name.isEmpty || code.isEmpty) {
      _toast('Branch name and code are required.');
      return;
    }
    if (_country == null || _province == null || _city == null || _currency == null) {
      _toast('Please select country, province, city and currency.');
      return;
    }

    setState(() => _saving = true);
    try {
      final headOfficeId = context.read<AuthProvider>().companyId;
      await _repo.createBranch({
        'companyName': name,
        'companyCode': code,
        'emailAddress': _email.text.trim().isEmpty ? null : _email.text.trim(),
        'phoneNumber': _phone.text.trim().isEmpty ? null : _phone.text.trim(),
        'address': _address.text.trim().isEmpty ? null : _address.text.trim(),
        'location': _location.text.trim().isEmpty ? null : _location.text.trim(),
        'countryId': _country!.id,
        'provinceId': _province!.id,
        'cityId': _city!.id,
        'defaultCurrencyId': _currency!.id,
        'parentCompanyId': headOfficeId,
        'isActive': true,
      });
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) {
        setState(() => _saving = false);
        _toast('Failed: $e');
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('New Branch')),
      body: SafeArea(
        top: false,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 32.h),
          children: [
            TextField(
              controller: _name,
              decoration: const InputDecoration(labelText: 'Branch Name *', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _code,
              textCapitalization: TextCapitalization.characters,
              decoration: const InputDecoration(labelText: 'Branch Code *', hintText: 'e.g. NB2', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _email,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(labelText: 'Email', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _phone,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: 'Phone', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _address,
              decoration: const InputDecoration(labelText: 'Address', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _location,
              decoration: const InputDecoration(labelText: 'Location', hintText: 'Suburb / area', border: OutlineInputBorder()),
            ),
            SizedBox(height: 16.h),
            FutureBuilder<List<LookupItem>>(
              future: _countriesFuture,
              builder: (context, snap) {
                final countries = snap.data ?? const [];
                return DropdownButtonFormField<LookupItem>(
                  // value (not initialValue) so the field reflects the cascading
                  // country/province/city selection after the first build.
                  // ignore: deprecated_member_use
                  value: _country,
                  decoration: const InputDecoration(labelText: 'Country *', border: OutlineInputBorder()),
                  items: countries.map((c) => DropdownMenuItem(value: c, child: Text(c.name))).toList(),
                  onChanged: snap.connectionState == ConnectionState.done ? _onCountryChanged : null,
                );
              },
            ),
            SizedBox(height: 12.h),
            DropdownButtonFormField<LookupItem>(
              // ignore: deprecated_member_use
              value: _province,
              decoration: InputDecoration(
                labelText: 'Province *',
                border: const OutlineInputBorder(),
                suffixIcon: _loadingProvinces ? const Padding(padding: EdgeInsets.all(12), child: CircularProgressIndicator(strokeWidth: 2)) : null,
              ),
              items: _provinces.map((p) => DropdownMenuItem(value: p, child: Text(p.name))).toList(),
              onChanged: _country == null ? null : _onProvinceChanged,
            ),
            SizedBox(height: 12.h),
            DropdownButtonFormField<LookupItem>(
              // ignore: deprecated_member_use
              value: _city,
              decoration: InputDecoration(
                labelText: 'City *',
                border: const OutlineInputBorder(),
                suffixIcon: _loadingCities ? const Padding(padding: EdgeInsets.all(12), child: CircularProgressIndicator(strokeWidth: 2)) : null,
              ),
              items: _cities.map((c) => DropdownMenuItem(value: c, child: Text(c.name))).toList(),
              onChanged: _province == null ? null : (v) => setState(() => _city = v),
            ),
            SizedBox(height: 12.h),
            FutureBuilder<List<CurrencyOption>>(
              future: _currenciesFuture,
              builder: (context, snap) {
                final currencies = snap.data ?? const [];
                return DropdownButtonFormField<CurrencyOption>(
                  // ignore: deprecated_member_use
                  value: _currency,
                  decoration: const InputDecoration(labelText: 'Currency *', border: OutlineInputBorder()),
                  items: currencies.map((c) => DropdownMenuItem(value: c, child: Text('${c.code} — ${c.name}'))).toList(),
                  onChanged: snap.connectionState == ConnectionState.done ? (v) => setState(() => _currency = v) : null,
                );
              },
            ),
            SizedBox(height: 20.h),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _saving ? null : _save,
                child: _saving
                    ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Create Branch'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
