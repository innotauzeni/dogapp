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
import '../models/animal.dart';
import '../widgets/photo_picker_field.dart';

/// Add or edit a dog. Pass [existing] to edit.
class DogFormScreen extends StatefulWidget {
  final Animal? existing;
  const DogFormScreen({super.key, this.existing});

  @override
  State<DogFormScreen> createState() => _DogFormScreenState();
}

class _DogFormScreenState extends State<DogFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _repo = DogsRepository();

  late final TextEditingController _name;
  late final TextEditingController _breed;
  late final TextEditingController _microchip;

  String? _gender;
  String? _color;
  String? _breedingType;
  DateTime? _dob;
  String? _photo; // data URI (new upload) or existing photo URL — optional
  int? _sireId;
  int? _damId;
  bool _active = true;
  bool _saving = false;

  /// Existing dogs, used to pick parents (sire/dam).
  List<Animal> _dogs = const [];
  bool _loadingDogs = true;

  bool get _isEdit => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final e = widget.existing;
    _name = TextEditingController(text: e?.name ?? '');
    _breed = TextEditingController(text: e?.breed ?? '');
    _microchip = TextEditingController(text: e?.microchipNumber ?? '');
    _color = e?.color;
    _breedingType = null; // Initialize to null
    _gender = e?.gender;
    _dob = e?.dateOfBirth;
    _photo = e?.photoUrl;
    _sireId = e?.sireId;
    _damId = e?.damId;
    _active = e?.isActive ?? true;
    _loadParents();
  }

  Future<void> _loadParents() async {
    try {
      final companyId = context.read<AuthProvider>().companyId;
      final dogs = await _repo.getPets(companyId);
      if (!mounted) return;
      setState(() {
        // Don't allow a dog to be its own parent when editing.
        _dogs = dogs.where((d) => d.id != widget.existing?.id).toList();
        _loadingDogs = false;
      });
    } catch (_) {
      if (mounted) setState(() => _loadingDogs = false);
    }
  }

  @override
  void dispose() {
    _name.dispose();
    _breed.dispose();
    _microchip.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) return;
    if (_dob == null) {
      _toast('Please select the date of birth', error: true);
      return;
    }
    setState(() => _saving = true);
    final companyId = context.read<AuthProvider>().companyId;
    if (_color == null || _color!.isEmpty) {
      _toast('Please select a color', error: true);
      return;
    }
    final body = {
      'name': _name.text.trim(),
      'breed': _breed.text.trim(),
      'color': _color,
      'dateOfBirth': _dob!.toUtc().toIso8601String(),
      'gender': _gender,
      'microchipNumber': _microchip.text.trim().isEmpty ? null : _microchip.text.trim(),
      // Photo is optional — null is fine. Currently a base64 data URI; will be
      // swapped for an R2 URL once the backend upload endpoint exists.
      'photoUrl': (_photo == null || _photo!.isEmpty) ? null : _photo,
      'sireId': _sireId,
      'damId': _damId,
      'companyId': companyId,
      'isActive': _active,
      // This form doesn't expose death/customer-assignment fields for editing —
      // carry the existing values through unchanged so a save doesn't silently
      // "revive" a deceased dog or clear its customer assignment (the update
      // handler overwrites these unconditionally from whatever the body sends).
      'dateOfDeath': widget.existing?.dateOfDeath?.toUtc().toIso8601String(),
      'customerId': widget.existing?.customerId,
    };
    try {
      if (_isEdit) {
        await _repo.updatePet(widget.existing!.id, {...body, 'id': widget.existing!.id});
      } else {
        await _repo.createPet(body);
      }
      if (!mounted) return;
      Navigator.of(context).pop(true);
    } on ApiException catch (e) {
      _toast(e.message, error: true);
    } catch (_) {
      _toast('Could not save. Please try again.', error: true);
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

  Future<void> _pickDob() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _dob ?? DateTime(now.year - 1),
      firstDate: DateTime(now.year - 30),
      lastDate: now,
    );
    if (picked != null) setState(() => _dob = picked);
  }

  /// Parent dropdown items — filtered by gender, with a "Not known" option.
  List<DropdownMenuItem<int?>> _parentItems(bool wantMale) {
    final matching = _dogs.where((d) {
      final g = (d.gender ?? '').toLowerCase();
      if (g.isEmpty) return true; // unknown gender → eligible for either
      return wantMale ? g.startsWith('m') : g.startsWith('f');
    }).toList();
    return [
      const DropdownMenuItem<int?>(value: null, child: Text('Not known')),
      for (final d in matching)
        DropdownMenuItem<int?>(value: d.id, child: Text(d.name, overflow: TextOverflow.ellipsis)),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEdit ? 'Edit Dog' : 'Add Dog')),
      body: SafeArea(
        child: Form(
          key: _formKey,
          child: ListView(
            padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 24.h),
            children: [
              PhotoPickerField(
                value: _photo,
                onChanged: (v) => setState(() => _photo = v),
              ),
              SizedBox(height: 24.h),
              AppTextField(
                controller: _name,
                label: 'Name',
                hint: 'e.g. Rex',
                icon: Icons.badge_outlined,
                validator: (v) => (v == null || v.trim().isEmpty) ? 'Name is required' : null,
              ),
              SizedBox(height: 16.h),
              AppDropdownField<String>(
                label: 'Breed',
                value: _breed.text.isEmpty ? null : _breed.text,
                icon: Icons.pets_outlined,
                hint: 'Select a breed',
                validator: (v) => (v == null || v.isEmpty) ? 'Breed is required' : null,
                // A dog's recorded breed can come from free-text entry (Portal, WhatsApp)
                // and not exactly match this curated list — DropdownButtonFormField
                // requires `value` to equal one of `items` or it breaks, so make sure
                // whatever breed this dog already has is always selectable.
                items: [
                  if (_breed.text.isNotEmpty && !_BreedDropdown.allBreeds.contains(_breed.text)) _breed.text,
                  ..._BreedDropdown.allBreeds,
                ].map((breed) => DropdownMenuItem(value: breed, child: Text(breed))).toList(),
                onChanged: (v) => setState(() {
                  _breed.text = v ?? '';
                }),
              ),
              SizedBox(height: 16.h),
              // Color Picker
              _ColorPickerField(
                label: 'Color',
                value: _color,
                onChanged: (v) => setState(() => _color = v),
              ),
              SizedBox(height: 16.h),
              // Breeding Type Dropdown
              AppDropdownField<String>(
                label: 'Breeding Type',
                value: _breedingType,
                icon: Icons.favorite_rounded,
                hint: 'Select breeding type',
                items: const [
                  DropdownMenuItem(value: 'Show Dog', child: Text('Show Dog')),
                  DropdownMenuItem(value: 'Working Dog', child: Text('Working Dog')),
                  DropdownMenuItem(value: 'Companion', child: Text('Companion')),
                  DropdownMenuItem(value: 'Guard Dog', child: Text('Guard Dog')),
                  DropdownMenuItem(value: 'Service Dog', child: Text('Service Dog')),
                  DropdownMenuItem(value: 'Breeder', child: Text('Breeder')),
                  DropdownMenuItem(value: 'Pet', child: Text('Pet')),
                ],
                onChanged: (v) => setState(() => _breedingType = v),
              ),
              SizedBox(height: 16.h),
              AppDropdownField<String>(
                label: 'Gender',
                value: _gender,
                icon: Icons.wc_rounded,
                // Same tolerance as Breed above — an existing dog's gender might not
                // be exactly 'Male'/'Female' (e.g. blank, or entered elsewhere).
                items: [
                  if (_gender != null && _gender!.isNotEmpty && _gender != 'Male' && _gender != 'Female')
                    DropdownMenuItem(value: _gender, child: Text(_gender!)),
                  const DropdownMenuItem(value: 'Male', child: Text('Male')),
                  const DropdownMenuItem(value: 'Female', child: Text('Female')),
                ],
                onChanged: (v) => setState(() => _gender = v),
              ),
              SizedBox(height: 16.h),
              DateField(
                label: 'Date of Birth',
                value: _dob == null ? null : Fmt.date(_dob),
                onTap: _pickDob,
              ),
              SizedBox(height: 16.h),
              // ── Parentage (family tree) ─────────────────────────────────────
              _ParentSection(
                loading: _loadingDogs,
                sireValue: _sireId,
                damValue: _damId,
                sireItems: _parentItems(true),
                damItems: _parentItems(false),
                onSire: (v) => setState(() => _sireId = v),
                onDam: (v) => setState(() => _damId = v),
              ),
              SizedBox(height: 16.h),
              AppTextField(
                controller: _microchip,
                label: 'Microchip Number (optional)',
                hint: 'e.g. 985112000000000',
                icon: Icons.qr_code_2_rounded,
              ),
              SizedBox(height: 16.h),
              SwitchListTile.adaptive(
                contentPadding: EdgeInsets.zero,
                value: _active,
                onChanged: (v) => setState(() => _active = v),
                title: Text('Active', style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600)),
                subtitle: Text('Inactive dogs are hidden from active operations',
                    style: TextStyle(fontSize: 12.sp, color: AppColors.textMuted)),
              ),
              SizedBox(height: 24.h),
              PrimaryButton(
                label: _isEdit ? 'Save Changes' : 'Create Dog',
                loading: _saving,
                onPressed: _save,
                icon: Icons.check_rounded,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Sire/Dam pickers with a small explanatory header.
class _ParentSection extends StatelessWidget {
  final bool loading;
  final int? sireValue;
  final int? damValue;
  final List<DropdownMenuItem<int?>> sireItems;
  final List<DropdownMenuItem<int?>> damItems;
  final ValueChanged<int?> onSire;
  final ValueChanged<int?> onDam;

  const _ParentSection({
    required this.loading,
    required this.sireValue,
    required this.damValue,
    required this.sireItems,
    required this.damItems,
    required this.onSire,
    required this.onDam,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(14.w),
      decoration: BoxDecoration(
        color: AppColors.surfaceAlt,
        borderRadius: BorderRadius.circular(14.r),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.account_tree_outlined, size: 16.r, color: AppColors.textSecondary),
              SizedBox(width: 6.w),
              Text('Parentage (for family tree)',
                  style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary)),
            ],
          ),
          SizedBox(height: 2.h),
          Text('Leave as "Not known" if the parent isn\'t recorded.',
              style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
          SizedBox(height: 12.h),
          if (loading)
            Padding(
              padding: EdgeInsets.symmetric(vertical: 8.h),
              child: Row(
                children: [
                  SizedBox(width: 16.r, height: 16.r, child: const CircularProgressIndicator(strokeWidth: 2)),
                  SizedBox(width: 10.w),
                  Text('Loading dogs…', style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
                ],
              ),
            )
          else ...[
            AppDropdownField<int?>(
              label: 'Sire (father)',
              value: sireValue,
              icon: Icons.male_rounded,
              items: sireItems,
              onChanged: onSire,
            ),
            SizedBox(height: 12.h),
            AppDropdownField<int?>(
              label: 'Dam (mother)',
              value: damValue,
              icon: Icons.female_rounded,
              items: damItems,
              onChanged: onDam,
            ),
          ],
        ],
      ),
    );
  }
}

class _ColorPickerField extends StatelessWidget {
  final String? value;
  final ValueChanged<String?> onChanged;
  final String label;

  const _ColorPickerField({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  static const _colorOptions = [
    ('Black', 'Black'),
    ('White', 'White'),
    ('Brown', 'Brown'),
    ('Red', 'Red'),
    ('Golden', 'Golden'),
    ('Yellow', 'Yellow'),
    ('Cream', 'Cream'),
    ('Grey', 'Grey'),
    ('Silver', 'Silver'),
    ('Sable', 'Sable'),
    ('Fawn', 'Fawn'),
    ('Brindle', 'Brindle'),
    ('Black & Tan', 'Black & Tan'),
    ('Black & White', 'Black & White'),
    ('Brown & White', 'Brown & White'),
    ('Red & White', 'Red & White'),
    ('Tri-color', 'Tri-color'),
  ];

  @override
  Widget build(BuildContext context) {
    final knownValues = _colorOptions.map((c) => c.$2).toSet();
    return AppDropdownField<String>(
      label: label,
      value: value,
      icon: Icons.palette_outlined,
      hint: 'Select a color',
      validator: (v) => (v == null || v.isEmpty) ? 'Color is required' : null,
      // Same tolerance as Breed/Gender — an existing dog's color might not be an
      // exact match for this fixed palette.
      items: [
        if (value != null && value!.isNotEmpty && !knownValues.contains(value)) DropdownMenuItem(value: value, child: Text(value!)),
        ..._colorOptions.map((color) => DropdownMenuItem(value: color.$2, child: Text(color.$1))),
      ],
      onChanged: onChanged,
    );
  }
}

class _BreedDropdown {
  static const List<String> sporting = [
    'American Cocker Spaniel',
    'American Water Spaniel',
    'Boykin Spaniel',
    'Brittany',
    'Chesapeake Bay Retriever',
    'Clumber Spaniel',
    'Cocker Spaniel',
    'Curly-Coated Retriever',
    'English Cocker Spaniel',
    'English Setter',
    'English Springer Spaniel',
    'Field Spaniel',
    'Flat-Coated Retriever',
    'German Shorthaired Pointer',
    'German Wirehaired Pointer',
    'Golden Retriever',
    'Gordon Setter',
    'Irish Red and White Setter',
    'Irish Setter',
    'Irish Water Spaniel',
    'Labrador Retriever',
    'Nova Scotia Duck Tolling Retriever',
    'Pointer',
    'Spinone Italiano',
    'Sussex Spaniel',
    'Vizsla',
    'Weimaraner',
    'Welsh Springer Spaniel',
    'Wirehaired Pointing Griffon',
    'Wirehaired Vizsla',
  ];

  static const List<String> hound = [
    'Afghan Hound',
    'American English Coonhound',
    'American Foxhound',
    'Basenji',
    'Basset Hound',
    'Beagle',
    'Black and Tan Coonhound',
    'Bloodhound',
    'Bluetick Coonhound',
    'Borzoi',
    "Cirneco dell'Etna",
    'Dachshund',
    'English Foxhound',
    'Greyhound',
    'Harrier',
    'Ibizan Hound',
    'Irish Wolfhound',
    'Norwegian Elkhound',
    'Otterhound',
    'Petit Basset Griffon Vendéen',
    'Pharaoh Hound',
    'Plott Hound',
    'Portuguese Podengo Pequeno',
    'Redbone Coonhound',
    'Rhodesian Ridgeback',
    'Saluki',
    'Scottish Deerhound',
    'Sloughi',
    'Treeing Walker Coonhound',
    'Whippet',
  ];

  static const List<String> working = [
    'Akita',
    'Alaskan Malamute',
    'Anatolian Shepherd Dog',
    'Bernese Mountain Dog',
    'Black Russian Terrier',
    'Boerboel',
    'Boxer',
    'Bullmastiff',
    'Cane Corso',
    'Chinook',
    'Doberman Pinscher',
    'Dogo Argentino',
    'Dogue de Bordeaux',
    'German Pinscher',
    'Giant Schnauzer',
    'Great Dane',
    'Great Pyrenees',
    'Greater Swiss Mountain Dog',
    'Komondor',
    'Kuvasz',
    'Leonberger',
    'Mastiff',
    'Neapolitan Mastiff',
    'Newfoundland',
    'Portuguese Water Dog',
    'Rottweiler',
    'Saint Bernard',
    'Samoyed',
    'Siberian Husky',
    'Standard Schnauzer',
    'Tibetan Mastiff',
  ];

  static const List<String> terrier = [
    'Airedale Terrier',
    'American Hairless Terrier',
    'American Staffordshire Terrier',
    'Australian Terrier',
    'Bedlington Terrier',
    'Border Terrier',
    'Bull Terrier',
    'Cairn Terrier',
    'Cesky Terrier',
    'Dandie Dinmont Terrier',
    'Glen of Imaal Terrier',
    'Irish Terrier',
    'Kerry Blue Terrier',
    'Lakeland Terrier',
    'Manchester Terrier',
    'Miniature Bull Terrier',
    'Miniature Schnauzer',
    'Norfolk Terrier',
    'Norwich Terrier',
    'Parson Russell Terrier',
    'Rat Terrier',
    'Russell Terrier',
    'Scottish Terrier',
    'Sealyham Terrier',
    'Skye Terrier',
    'Smooth Fox Terrier',
    'Soft Coated Wheaten Terrier',
    'Staffordshire Bull Terrier',
    'Welsh Terrier',
    'West Highland White Terrier',
    'Wire Fox Terrier',
  ];

  static const List<String> toy = [
    'Affenpinscher',
    'Brussels Griffon',
    'Cavalier King Charles Spaniel',
    'Chihuahua',
    'Chinese Crested',
    'English Toy Spaniel',
    'Havanese',
    'Italian Greyhound',
    'Japanese Chin',
    'Maltese',
    'Miniature Pinscher',
    'Papillon',
    'Pekingese',
    'Pomeranian',
    'Poodle (Toy)',
    'Pug',
    'Shih Tzu',
    'Silky Terrier',
    'Toy Fox Terrier',
    'Yorkshire Terrier',
  ];

  static const List<String> nonSporting = [
    'American Eskimo Dog',
    'Bichon Frise',
    'Boston Terrier',
    'Bulldog',
    'Chinese Shar-Pei',
    'Chow Chow',
    'Coton de Tulear',
    'Dalmatian',
    'Finnish Spitz',
    'French Bulldog',
    'Keeshond',
    'Lhasa Apso',
    'Löwchen',
    'Norwegian Lundehund',
    'Poodle (Miniature)',
    'Poodle (Standard)',
    'Schipperke',
    'Shiba Inu',
    'Tibetan Spaniel',
    'Tibetan Terrier',
    'Xoloitzcuintli',
  ];

  static const List<String> herding = [
    'Australian Cattle Dog',
    'Australian Shepherd',
    'Bearded Collie',
    'Beauceron',
    'Belgian Laekenois',
    'Belgian Malinois',
    'Belgian Sheepdog',
    'Belgian Tervuren',
    'Bergamasco Sheepdog',
    'Berger Picard',
    'Border Collie',
    'Bouvier des Flandres',
    'Briard',
    'Canaan Dog',
    'Cardigan Welsh Corgi',
    'Collie',
    'Entlebucher Mountain Dog',
    'Finnish Lapphund',
    'German Shepherd Dog',
    'Icelandic Sheepdog',
    'Miniature American Shepherd',
    'Norwegian Buhund',
    'Old English Sheepdog',
    'Pembroke Welsh Corgi',
    'Polish Lowland Sheepdog',
    'Puli',
    'Pumi',
    'Pyrenean Shepherd',
    'Shetland Sheepdog',
    'Spanish Water Dog',
    'Swedish Vallhund',
    'White Swiss Shepherd Dog',
  ];

  static const List<String> miscellaneous = [
    'Mixed Breed',
    'Other',
  ];

  static List<String> get allBreeds => [
        ...sporting,
        ...hound,
        ...working,
        ...terrier,
        ...toy,
        ...nonSporting,
        ...herding,
        ...miscellaneous,
      ];

  static Map<String, List<String>> get groupedBreeds => {
        'Sporting': sporting,
        'Hound': hound,
        'Working': working,
        'Terrier': terrier,
        'Toy': toy,
        'Non-Sporting': nonSporting,
        'Herding': herding,
        'Miscellaneous / Other': miscellaneous,
      };
}
