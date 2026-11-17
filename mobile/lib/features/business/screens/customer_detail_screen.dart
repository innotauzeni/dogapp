import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../auth/providers/auth_provider.dart';
import '../../dogs/data/dogs_repository.dart';
import '../../dogs/models/animal.dart';
import '../../dogs/screens/dog_picker_screen.dart';
import '../../dogs/screens/dog_profile_screen.dart';
import '../../dogs/widgets/dog_avatar.dart';
import '../data/business_repository.dart';
import '../models/business_models.dart';

/// Customer profile: contact details + the dogs they own, with the ability to
/// assign another dog or release one.
class CustomerDetailScreen extends StatefulWidget {
  final Customer customer;
  const CustomerDetailScreen({super.key, required this.customer});

  @override
  State<CustomerDetailScreen> createState() => _CustomerDetailScreenState();
}

class _CustomerDetailScreenState extends State<CustomerDetailScreen> {
  final _repo = DogsRepository();
  final _businessRepo = BusinessRepository();
  Future<List<Animal>>? _future;
  Future<List<CustomerSite>>? _sitesFuture;
  bool _working = false;

  Customer get _c => widget.customer;

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    final companyId = context.read<AuthProvider>().companyId;
    _future = _repo.getPets(companyId).then((all) => all.where((d) => d.customerId == _c.id).toList());
    _sitesFuture = _businessRepo.getCustomerSites(_c.id);
  }

  Future<void> _refresh() async {
    setState(_load);
    await _future;
    await _sitesFuture;
  }

  Future<void> _assignDog() async {
    final dog = await Navigator.of(context).push<Animal>(
      MaterialPageRoute(builder: (_) => DogPickerScreen(title: 'Assign Dog to ${_c.displayName}', excludeOwnerId: _c.id)),
    );
    if (dog == null) return;
    await _runAssign(dog.id, _c.id, '${dog.name} assigned to ${_c.displayName}.');
  }

  Future<void> _addSite() async {
    final saved = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(18.r))),
      builder: (_) => _SiteForm(customerId: _c.id, repo: _businessRepo),
    );
    if (saved == true && mounted) {
      _toast('Site added.', error: false);
      _refresh();
    }
  }

  Future<void> _assignDogToSite(CustomerSite site) async {
    final dog = await Navigator.of(context).push<Animal>(
      MaterialPageRoute(builder: (_) => DogPickerScreen(title: 'Assign Dog to ${site.siteName}')),
    );
    if (dog == null) return;

    if (!mounted) return;
    final companyId = context.read<AuthProvider>().companyId;
    setState(() => _working = true);
    try {
      await _repo.createDeployment({
        'dogId': dog.id,
        'companyId': companyId,
        'customerId': _c.id,
        'customerSiteId': site.id,
        'location': site.siteName,
        'companyDeployedTo': _c.displayName,
        'securityGuardAssigned': '',
        'startDate': DateTime.now().toIso8601String(),
        'isActive': true,
      });
      if (mounted) _toast('${dog.name} deployed to ${site.siteName}.', error: false);
      await _refresh();
    } on ApiException catch (e) {
      _toast(e.message, error: true);
    } catch (_) {
      _toast('Could not assign dog to this site.', error: true);
    } finally {
      if (mounted) setState(() => _working = false);
    }
  }

  Future<void> _unassign(Animal dog) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Release dog?'),
        content: Text('Remove ${dog.name} from ${_c.displayName}\'s ownership?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text('Release'),
          ),
        ],
      ),
    );
    if (ok == true) await _runAssign(dog.id, null, '${dog.name} released.');
  }

  Future<void> _runAssign(int dogId, int? customerId, String successMsg) async {
    setState(() => _working = true);
    try {
      await _repo.assignCustomer(dogId: dogId, customerId: customerId);
      if (mounted) _toast(successMsg, error: false);
      await _refresh();
    } on ApiException catch (e) {
      _toast(e.message, error: true);
    } catch (_) {
      _toast('Could not update assignment.', error: true);
    } finally {
      if (mounted) setState(() => _working = false);
    }
  }

  void _toast(String msg, {required bool error}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(backgroundColor: error ? AppColors.danger : AppColors.success, content: Text(msg)),
    );
  }

  @override
  Widget build(BuildContext context) {
    final name = _c.displayName.isEmpty ? 'Customer' : _c.displayName;
    final auth = context.watch<AuthProvider>();
    final canAssign = auth.can('pet.edit');
    final canEditSites = auth.can('customer.edit');
    final canDeploy = auth.can('deployment.create');
    return Scaffold(
      appBar: AppBar(title: Text(name)),
      floatingActionButton: canAssign
          ? FloatingActionButton.extended(
              heroTag: 'fab-customer-detail-assign-dog',
              onPressed: _working ? null : _assignDog,
              icon: const Icon(Icons.pets_rounded),
              label: const Text('Assign Dog'),
            )
          : null,
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          child: ListView(
            padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 96.h),
            physics: const AlwaysScrollableScrollPhysics(),
            children: [
              _infoCard(context, name),
              SizedBox(height: 16.h),
              SectionHeader(
                title: 'Sites / Premises',
                trailing: canEditSites
                    ? IconButton(
                        onPressed: _addSite,
                        icon: Icon(Icons.add_circle_outline_rounded, color: AppColors.primary, size: 22.r),
                        tooltip: 'Add site',
                      )
                    : null,
              ),
              AsyncView<List<CustomerSite>>(
                future: _sitesFuture,
                onRetry: _refresh,
                builder: (context, sites) {
                  if (sites.isEmpty) {
                    return AppCard(
                      child: Padding(
                        padding: EdgeInsets.symmetric(vertical: 12.h),
                        child: Row(
                          children: [
                            Icon(Icons.apartment_outlined, color: AppColors.textMuted, size: 20.r),
                            SizedBox(width: 10.w),
                            Expanded(
                              child: Text('No sites/premises recorded for this customer.',
                                  style: TextStyle(fontSize: 13.sp, color: AppColors.textSecondary)),
                            ),
                          ],
                        ),
                      ),
                    );
                  }
                  return Column(
                    children: [
                      for (final s in sites) ...[
                        _SiteRow(
                          site: s,
                          onAssign: canDeploy ? () => _assignDogToSite(s) : null,
                        ),
                        SizedBox(height: 10.h),
                      ],
                    ],
                  );
                },
              ),
              SizedBox(height: 16.h),
              SectionHeader(title: 'Owned Dogs', trailing: _working
                  ? SizedBox(width: 16.r, height: 16.r, child: const CircularProgressIndicator(strokeWidth: 2))
                  : null),
              AsyncView<List<Animal>>(
                future: _future,
                onRetry: _refresh,
                builder: (context, dogs) {
                  if (dogs.isEmpty) {
                    return AppCard(
                      child: Padding(
                        padding: EdgeInsets.symmetric(vertical: 12.h),
                        child: Row(
                          children: [
                            Icon(Icons.pets_outlined, color: AppColors.textMuted, size: 20.r),
                            SizedBox(width: 10.w),
                            Expanded(
                              child: Text('No dogs assigned yet. Tap "Assign Dog" to add one.',
                                  style: TextStyle(fontSize: 13.sp, color: AppColors.textSecondary)),
                            ),
                          ],
                        ),
                      ),
                    );
                  }
                  return Column(
                    children: [
                      for (final d in dogs) ...[
                        _DogRow(dog: d, onOpen: () => _open(d), onRelease: () => _unassign(d)),
                        SizedBox(height: 10.h),
                      ],
                    ],
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _open(Animal dog) async {
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => DogProfileScreen(dogId: dog.id, initial: dog)),
    );
    _refresh();
  }

  Widget _infoCard(BuildContext context, String name) {
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 26.r,
                backgroundColor: AppColors.primaryLight,
                child: Text(name.substring(0, 1).toUpperCase(),
                    style: TextStyle(color: AppColors.primary, fontWeight: FontWeight.w800, fontSize: 18.sp)),
              ),
              SizedBox(width: 14.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(name, style: TextStyle(fontSize: 17.sp, fontWeight: FontWeight.w800)),
                    if ((_c.gender ?? '').isNotEmpty)
                      Text(_c.gender!, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                  ],
                ),
              ),
            ],
          ),
          SizedBox(height: 12.h),
          const Divider(height: 1),
          SizedBox(height: 8.h),
          InfoRow(label: 'Phone', value: _c.cellphone ?? '—', icon: Icons.phone_outlined),
          InfoRow(label: 'Email', value: _c.email ?? '—', icon: Icons.email_outlined),
          InfoRow(label: 'ID Number', value: _c.idNumber ?? '—', icon: Icons.badge_outlined),
          InfoRow(label: 'Address', value: _c.address ?? '—', icon: Icons.home_outlined),
        ],
      ),
    );
  }
}

class _SiteRow extends StatelessWidget {
  final CustomerSite site;
  final VoidCallback? onAssign;
  const _SiteRow({required this.site, this.onAssign});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: EdgeInsets.all(12.w),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 36.r,
            height: 36.r,
            decoration: BoxDecoration(color: AppColors.infoLight, borderRadius: BorderRadius.circular(10.r)),
            child: Icon(Icons.apartment_rounded, size: 18.r, color: AppColors.info),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(site.siteName, maxLines: 1, overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                if ((site.address ?? '').isNotEmpty) ...[
                  SizedBox(height: 2.h),
                  Text(site.address!, style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                ],
                if ((site.contactPerson ?? '').isNotEmpty || (site.contactPhone ?? '').isNotEmpty) ...[
                  SizedBox(height: 4.h),
                  Text(
                    [site.contactPerson, site.contactPhone].where((s) => (s ?? '').isNotEmpty).join('  •  '),
                    style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted),
                  ),
                ],
              ],
            ),
          ),
          if (onAssign != null)
            IconButton(
              tooltip: 'Assign a dog here',
              icon: Icon(Icons.pets_rounded, color: AppColors.primary, size: 20.r),
              onPressed: onAssign,
            ),
        ],
      ),
    );
  }
}

class _SiteForm extends StatefulWidget {
  final int customerId;
  final BusinessRepository repo;
  const _SiteForm({required this.customerId, required this.repo});

  @override
  State<_SiteForm> createState() => _SiteFormState();
}

class _SiteFormState extends State<_SiteForm> {
  final _siteName = TextEditingController();
  final _address = TextEditingController();
  final _latitude = TextEditingController();
  final _longitude = TextEditingController();
  final _contactPerson = TextEditingController();
  final _contactPhone = TextEditingController();
  bool _saving = false;

  @override
  void dispose() {
    _siteName.dispose();
    _address.dispose();
    _latitude.dispose();
    _longitude.dispose();
    _contactPerson.dispose();
    _contactPhone.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final name = _siteName.text.trim();
    if (name.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Site name is required.')));
      return;
    }
    setState(() => _saving = true);
    try {
      await widget.repo.createCustomerSite({
        'customerId': widget.customerId,
        'siteName': name,
        'address': _address.text.trim().isEmpty ? null : _address.text.trim(),
        'latitude': double.tryParse(_latitude.text.trim()),
        'longitude': double.tryParse(_longitude.text.trim()),
        'contactPerson': _contactPerson.text.trim().isEmpty ? null : _contactPerson.text.trim(),
        'contactPhone': _contactPhone.text.trim().isEmpty ? null : _contactPhone.text.trim(),
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
            Text('Add Site / Premise', style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.w800)),
            SizedBox(height: 14.h),
            TextField(
              controller: _siteName,
              decoration: const InputDecoration(labelText: 'Site name', hintText: 'e.g. Warehouse B', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _address,
              maxLines: 2,
              decoration: const InputDecoration(labelText: 'Address', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _latitude,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true, signed: true),
                    decoration: const InputDecoration(labelText: 'Latitude', border: OutlineInputBorder()),
                  ),
                ),
                SizedBox(width: 12.w),
                Expanded(
                  child: TextField(
                    controller: _longitude,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true, signed: true),
                    decoration: const InputDecoration(labelText: 'Longitude', border: OutlineInputBorder()),
                  ),
                ),
              ],
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _contactPerson,
              decoration: const InputDecoration(labelText: 'Contact person', border: OutlineInputBorder()),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _contactPhone,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: 'Contact phone', border: OutlineInputBorder()),
            ),
            SizedBox(height: 16.h),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _saving ? null : _save,
                child: _saving
                    ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Save Site'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DogRow extends StatelessWidget {
  final Animal dog;
  final VoidCallback onOpen;
  final VoidCallback onRelease;
  const _DogRow({required this.dog, required this.onOpen, required this.onRelease});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      onTap: onOpen,
      padding: EdgeInsets.all(10.w),
      child: Row(
        children: [
          DogAvatar(photoUrl: dog.photoUrl, size: 44),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(dog.name, maxLines: 1, overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                Text(dog.breed, maxLines: 1, overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Release',
            icon: Icon(Icons.link_off_rounded, color: AppColors.danger, size: 20.r),
            onPressed: onRelease,
          ),
        ],
      ),
    );
  }
}
