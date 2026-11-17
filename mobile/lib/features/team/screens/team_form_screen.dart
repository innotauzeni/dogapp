import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../auth/models/auth_models.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/team_repository.dart';
import '../models/team_models.dart';

/// Creates a new user (handler, admin, etc.) under the current company or one
/// of its branches — or, when [existing] is passed, edits that user in place.
class TeamFormScreen extends StatefulWidget {
  final TeamUser? existing;
  const TeamFormScreen({super.key, this.existing});

  @override
  State<TeamFormScreen> createState() => _TeamFormScreenState();
}

class _TeamFormScreenState extends State<TeamFormScreen> {
  final _repo = TeamRepository();

  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _username = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  final _password = TextEditingController();
  bool _obscurePassword = true;

  Future<List<RoleOption>>? _rolesFuture;
  RoleOption? _role;
  late int _companyId;
  List<Branch> _companyOptions = const [];
  String _status = 'ACTIVE';

  bool _saving = false;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    _rolesFuture = _repo.getRoles();
    final auth = context.read<AuthProvider>();
    final existing = widget.existing;

    if (existing != null) {
      _firstName.text = existing.name ?? '';
      _lastName.text = existing.surname ?? '';
      _username.text = existing.username ?? '';
      _email.text = existing.email ?? '';
      _phone.text = existing.phoneNumber ?? '';
      _status = existing.isActive ? 'ACTIVE' : 'INACTIVE';
      _companyId = existing.companyId;
    } else {
      _companyId = auth.activeCompanyId;
    }

    if (auth.isHeadOffice) {
      _companyOptions = [
        Branch(id: auth.companyId, companyName: 'Head Office', isActive: true),
        ...auth.branches,
      ];
    }
  }

  @override
  void dispose() {
    _firstName.dispose();
    _lastName.dispose();
    _username.dispose();
    _email.dispose();
    _phone.dispose();
    _password.dispose();
    super.dispose();
  }

  void _toast(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  Future<void> _save() async {
    final firstName = _firstName.text.trim();
    final lastName = _lastName.text.trim();
    final username = _username.text.trim();
    final email = _email.text.trim();
    final password = _password.text;

    if (firstName.isEmpty || lastName.isEmpty) {
      _toast('First and last name are required.');
      return;
    }
    if (!_isEditing && (email.isEmpty || !email.contains('@'))) {
      _toast('Please enter a valid email — it doubles as the login.');
      return;
    }
    if (!_isEditing && password.length < 6) {
      _toast('Password must be at least 6 characters.');
      return;
    }
    if (_role == null) {
      _toast('Please select a role.');
      return;
    }

    setState(() => _saving = true);
    try {
      if (_isEditing) {
        await _repo.updateUser(widget.existing!.id, {
          'firstName': firstName,
          'lastName': lastName,
          'username': username.isEmpty ? null : username,
          'phoneNumber': _phone.text.trim().isEmpty ? null : _phone.text.trim(),
          'status': _status,
          'roleName': _role!.name,
          'companyId': _companyId,
        });
      } else {
        await _repo.createUser({
          'firstName': firstName,
          'lastName': lastName,
          'username': username.isEmpty ? null : username,
          'email': email,
          'password': password,
          'phoneNumber': _phone.text.trim().isEmpty ? null : _phone.text.trim(),
          'roleName': _role!.name,
          'companyId': _companyId,
        });
      }
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
    final auth = context.read<AuthProvider>();
    final myRole = (auth.user?.role ?? '').toLowerCase();
    final isSystemOwner = myRole == 'systemowner';

    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Edit User' : 'New User')),
      body: SafeArea(
        top: false,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 32.h),
          children: [
            TextField(
              controller: _firstName,
              decoration: const InputDecoration(labelText: 'First Name *', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _lastName,
              decoration: const InputDecoration(labelText: 'Last Name *', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _username,
              decoration: const InputDecoration(
                labelText: 'Username',
                hintText: 'Leave blank to use the email',
                border: OutlineInputBorder(),
              ),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _email,
              enabled: !_isEditing,
              keyboardType: TextInputType.emailAddress,
              decoration: InputDecoration(
                labelText: _isEditing ? 'Email (login) — not editable' : 'Email (login) *',
                border: const OutlineInputBorder(),
              ),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _phone,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: 'Phone', border: OutlineInputBorder()),
            ),
            if (!_isEditing) ...[
              SizedBox(height: 12.h),
              TextField(
                controller: _password,
                obscureText: _obscurePassword,
                decoration: InputDecoration(
                  labelText: 'Password *',
                  border: const OutlineInputBorder(),
                  suffixIcon: IconButton(
                    icon: Icon(_obscurePassword ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                    onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                  ),
                ),
              ),
            ],
            if (_isEditing) ...[
              SizedBox(height: 12.h),
              DropdownButtonFormField<String>(
                // ignore: deprecated_member_use
                value: _status,
                decoration: const InputDecoration(labelText: 'Status', border: OutlineInputBorder()),
                items: const [
                  DropdownMenuItem(value: 'ACTIVE', child: Text('Active')),
                  DropdownMenuItem(value: 'INACTIVE', child: Text('Inactive')),
                ],
                onChanged: (v) => setState(() => _status = v ?? _status),
              ),
            ],
            SizedBox(height: 12.h),
            FutureBuilder<List<RoleOption>>(
              future: _rolesFuture,
              builder: (context, snap) {
                final roles = (snap.data ?? const [])
                    .where((r) => isSystemOwner || (r.name != 'SystemOwner' && r.name != 'Admin'))
                    .toList();
                // Seed the current role once the roles list has loaded and nothing is
                // selected yet, so editing an existing user shows their role pre-picked.
                if (_isEditing && _role == null && roles.isNotEmpty) {
                  final current = widget.existing?.roleName;
                  final match = roles.where((r) => r.name == current).toList();
                  if (match.isNotEmpty) {
                    WidgetsBinding.instance.addPostFrameCallback((_) {
                      if (mounted) setState(() => _role = match.first);
                    });
                  }
                }
                return DropdownButtonFormField<RoleOption>(
                  // value (not initialValue) so the dropdown keeps reflecting _role after
                  // the user picks something — initialValue only seeds the first build.
                  // ignore: deprecated_member_use
                  value: _role,
                  decoration: const InputDecoration(labelText: 'Role *', border: OutlineInputBorder()),
                  items: roles.map((r) => DropdownMenuItem(value: r, child: Text(r.name))).toList(),
                  onChanged: snap.connectionState == ConnectionState.done ? (v) => setState(() => _role = v) : null,
                );
              },
            ),
            if (_companyOptions.isNotEmpty) ...[
              SizedBox(height: 12.h),
              DropdownButtonFormField<int>(
                // ignore: deprecated_member_use
                value: _companyId,
                decoration: const InputDecoration(labelText: 'Branch / Company', border: OutlineInputBorder()),
                items: _companyOptions.map((b) => DropdownMenuItem(value: b.id, child: Text(b.companyName ?? 'Company #${b.id}'))).toList(),
                onChanged: (v) => setState(() => _companyId = v ?? _companyId),
              ),
            ],
            SizedBox(height: 20.h),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _saving ? null : _save,
                child: _saving
                    ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : Text(_isEditing ? 'Save Changes' : 'Create User'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
