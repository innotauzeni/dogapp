import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/cards.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/dogs_repository.dart';
import '../models/animal.dart';

class CareRecordFormScreen extends StatefulWidget {
  final int dogId;

  /// Care events already on file for this dog — used to catch a duplicate
  /// feeding before it is sent. May be empty; the API enforces the rule too.
  final List<CareRecord> existingRecords;

  const CareRecordFormScreen({
    super.key,
    required this.dogId,
    this.existingRecords = const [],
  });

  @override
  State<CareRecordFormScreen> createState() => _CareRecordFormScreenState();
}

class _CareRecordFormScreenState extends State<CareRecordFormScreen> {
  final _repo = DogsRepository();
  final _formKey = GlobalKey<FormState>();

  String _eventType = 'Feeding';
  DateTime _occurredOn = DateTime.now();
  String _details = '';
  String _quantityOrOutcome = '';
  bool _requiresAttention = false;

  // Vaccination fields
  String _vaccineName = '';
  String _vaccineBatch = '';
  DateTime? _vaccineNextDue;
  String _vaccineNotes = '';

  // Mating fields
  String _matingPartner = '';
  String _matingDuration = '';

  // Injury fields
  String _injurySeverity = 'Minor';
  String _injuryDetails = '';
  bool _injuryTreated = false;

  // Bite fields
  String _biteSeverity = 'Minor';
  String _biteDetails = '';
  String _biteReportedTo = '';

  // Deworming fields
  String _dewormingProduct = '';
  DateTime? _dewormingNextDue;
  
final List<String> _eventTypes = [
  'Feeding',
  'Vaccinated',
  'Illness',
  'Injured',
  'Bite someone',
  'Heat',
  'Mating',
  'Birth',
  'Litter',
  'Deworming',
  'Other',
];

  /// A dog is fed once a day, so a second feeding on the selected day means a
  /// double entry rather than a real second meal.
  bool get _duplicateFeeding {
    if (_eventType != 'Feeding') return false;
    return widget.existingRecords.any((r) {
      final on = r.occurredOn;
      return on != null &&
          r.eventType.toLowerCase() == 'feeding' &&
          on.year == _occurredOn.year &&
          on.month == _occurredOn.month &&
          on.day == _occurredOn.day;
    });
  }

  bool get _showVaccineFields => _eventType == 'Vaccinated';
  bool get _showMatingFields => _eventType == 'Mating';
  bool get _showInjuryFields => _eventType == 'Injured';
  bool get _showBiteFields => _eventType == 'Bite someone';
  bool get _showDewormingFields => _eventType == 'Deworming';

  Future<void> _submitForm() async {
    if (!_formKey.currentState!.validate()) return;
    _formKey.currentState!.save();

    if (_duplicateFeeding) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'A feeding is already recorded for ${Fmt.date(_occurredOn)}. '
            'A dog is fed once a day.',
          ),
        ),
      );
      return;
    }

    final payload = {
      'dogId': widget.dogId,
      'companyId': context.read<AuthProvider>().user?.companyId ?? 0,
      'eventType': _eventType,
      'occurredOn': _occurredOn.toIso8601String(),
      'details': _details.isNotEmpty ? _details : null,
      'quantityOrOutcome': _quantityOrOutcome.isNotEmpty ? _quantityOrOutcome : null,
      'requiresAttention': _requiresAttention,
      if (_showVaccineFields) ...{
        'vaccineName': _vaccineName.isNotEmpty ? _vaccineName : null,
        'vaccineBatch': _vaccineBatch.isNotEmpty ? _vaccineBatch : null,
        'vaccineNextDue': _vaccineNextDue?.toIso8601String(),
        'vaccineNotes': _vaccineNotes.isNotEmpty ? _vaccineNotes : null,
      },
      if (_showMatingFields) ...{
        'matingPartnerName': _matingPartner.isNotEmpty ? _matingPartner : null,
        'matingDuration': _matingDuration.isNotEmpty ? _matingDuration : null,
      },
      if (_showInjuryFields) ...{
        'injurySeverity': _injurySeverity,
        'injuryDetails': _injuryDetails.isNotEmpty ? _injuryDetails : null,
        'injuryTreated': _injuryTreated,
      },
      if (_showBiteFields) ...{
        'biteSeverity': _biteSeverity,
        'biteDetails': _biteDetails.isNotEmpty ? _biteDetails : null,
        'biteReportedTo': _biteReportedTo.isNotEmpty ? _biteReportedTo : null,
      },
      if (_showDewormingFields) ...{
        'dewormingProduct': _dewormingProduct.isNotEmpty ? _dewormingProduct : null,
        'dewormingNextDue': _dewormingNextDue?.toIso8601String(),
      },
    };

    try {
      await _repo.addCareRecord(payload);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Care record saved')),
        );
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      if (mounted) {
        // The API's own wording (e.g. the once-a-day feeding rule) is the useful
        // part — a bare "Failed: <exception>" buries it.
        final message = e is ApiException ? e.message : 'Failed: $e';
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(message)),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Record Care Event')),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 120.h),
          children: [
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SectionHeader(title: 'Care Event Details'),
                  SizedBox(height: 12.h),
                  DropdownButtonFormField<String>(
                    value: _eventType,
                    decoration: InputDecoration(
                      labelText: 'Event Type *',
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                      isDense: true,
                      contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                    ),
                    items: _eventTypes
                        .map((e) => DropdownMenuItem(value: e, child: Text(e)))
                        .toList(),
                    onChanged: (value) {
                      if (value != null) {
                        setState(() {
                          _eventType = value;
                        });
                      }
                    },
                  ),
                  SizedBox(height: 8.h),
                  Text('Selected: $_eventType', style: TextStyle(fontSize: 12.sp, color: Colors.blue)),
                  SizedBox(height: 12.h),
                  TextFormField(
                    decoration: InputDecoration(
                      labelText: 'Date & Time *',
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                      isDense: true,
                      contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      suffixIcon: const Icon(Icons.calendar_today),
                    ),
                    readOnly: true,
                    onTap: () async {
                      final picked = await showDatePicker(
                        context: context,
                        initialDate: _occurredOn,
                        firstDate: DateTime(2000),
                        lastDate: DateTime.now(),
                      );
                      if (picked == null || !context.mounted) return;
                      // The field is labelled "Date & Time", so ask for the time too
                      // rather than silently carrying over whatever the clock read
                      // when the form opened.
                      final time = await showTimePicker(
                        context: context,
                        initialTime: TimeOfDay(hour: _occurredOn.hour, minute: _occurredOn.minute),
                      );
                      if (!mounted) return;
                      setState(() => _occurredOn = DateTime(
                            picked.year,
                            picked.month,
                            picked.day,
                            time?.hour ?? _occurredOn.hour,
                            time?.minute ?? _occurredOn.minute,
                          ));
                    },
                    controller: TextEditingController(text: Fmt.dateTime(_occurredOn)),
                  ),
                  if (_duplicateFeeding) ...[
                    SizedBox(height: 8.h),
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(Icons.error_outline, size: 16.sp, color: Colors.red.shade700),
                        SizedBox(width: 6.w),
                        Expanded(
                          child: Text(
                            'A feeding is already recorded for ${Fmt.date(_occurredOn)}. '
                            'A dog is fed once a day.',
                            style: TextStyle(fontSize: 12.sp, color: Colors.red.shade700),
                          ),
                        ),
                      ],
                    ),
                  ],
                  SizedBox(height: 12.h),
                  TextFormField(
                    maxLines: 3,
                    decoration: InputDecoration(
                      labelText: 'Details',
                      hintText: 'Food type, symptoms, mating details, or observations',
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                      isDense: true,
                      contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                    ),
                    onChanged: (value) => _details = value,
                  ),
                  SizedBox(height: 12.h),
                  TextFormField(
                    decoration: InputDecoration(
                      labelText: 'Quantity / Outcome',
                      hintText: 'e.g. 2 cups, medication given, 6 puppies',
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                      isDense: true,
                      contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                    ),
                    onChanged: (value) => _quantityOrOutcome = value,
                  ),
                  SizedBox(height: 12.h),
                  CheckboxListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Requires attention or follow-up'),
                    value: _requiresAttention,
                    onChanged: (value) => setState(() => _requiresAttention = value ?? false),
                  ),
                ],
              ),
            ),
            if (_showVaccineFields) ...[
              SizedBox(height: 16.h),
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SectionHeader(title: 'Vaccination Details'),
                    SizedBox(height: 12.h),
                    TextFormField(
                      decoration: InputDecoration(
                        labelText: 'Vaccine Name',
                        hintText: 'e.g. Rabies, DHPP',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      onChanged: (value) => _vaccineName = value,
                    ),
                    SizedBox(height: 12.h),
                    TextFormField(
                      decoration: InputDecoration(
                        labelText: 'Batch / Lot Number',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      onChanged: (value) => _vaccineBatch = value,
                    ),
                    SizedBox(height: 12.h),
                    TextFormField(
                      decoration: InputDecoration(
                        labelText: 'Next Due Date',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                        suffixIcon: const Icon(Icons.calendar_today),
                      ),
                      readOnly: true,
                      onTap: () async {
                        final picked = await showDatePicker(
                          context: context,
                          initialDate: _vaccineNextDue ?? DateTime.now().add(const Duration(days: 365)),
                          firstDate: DateTime.now(),
                          lastDate: DateTime(2030),
                        );
                        if (picked != null) setState(() => _vaccineNextDue = picked);
                      },
                      controller: TextEditingController(text: _vaccineNextDue != null ? Fmt.date(_vaccineNextDue!) : ''),
                    ),
                    SizedBox(height: 12.h),
                    TextFormField(
                      maxLines: 2,
                      decoration: InputDecoration(
                        labelText: 'Vaccine Notes',
                        hintText: 'Reaction, dosage, or route',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      onChanged: (value) => _vaccineNotes = value,
                    ),
                  ],
                ),
              ),
            ],
            if (_showDewormingFields) ...[
              SizedBox(height: 16.h),
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SectionHeader(title: 'Deworming Details'),
                    SizedBox(height: 12.h),
                    TextFormField(
                      decoration: InputDecoration(
                        labelText: 'Product / Medicine',
                        hintText: 'e.g. Drontal, Panacur',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      onChanged: (value) => _dewormingProduct = value,
                    ),
                    SizedBox(height: 12.h),
                    TextFormField(
                      decoration: InputDecoration(
                        labelText: 'Next Due Date',
                        hintText: 'When the next dose is due',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                        suffixIcon: const Icon(Icons.calendar_today),
                      ),
                      readOnly: true,
                      onTap: () async {
                        final picked = await showDatePicker(
                          context: context,
                          initialDate: _dewormingNextDue ?? DateTime.now().add(const Duration(days: 90)),
                          firstDate: DateTime.now(),
                          lastDate: DateTime(2030),
                        );
                        if (picked != null) setState(() => _dewormingNextDue = picked);
                      },
                      controller: TextEditingController(text: _dewormingNextDue != null ? Fmt.date(_dewormingNextDue!) : ''),
                    ),
                  ],
                ),
              ),
            ],
            if (_showMatingFields) ...[
              SizedBox(height: 16.h),
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SectionHeader(title: 'Mating Details'),
                    SizedBox(height: 12.h),
                    TextFormField(
                      decoration: InputDecoration(
                        labelText: 'Mating Partner Name',
                        hintText: 'Registered or outside dog',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      onChanged: (value) => _matingPartner = value,
                    ),
                    SizedBox(height: 12.h),
                    TextFormField(
                      decoration: InputDecoration(
                        labelText: 'Duration',
                        hintText: 'e.g. 5 minutes',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      onChanged: (value) => _matingDuration = value,
                    ),
                  ],
                ),
              ),
            ],
            if (_showInjuryFields) ...[
              SizedBox(height: 16.h),
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SectionHeader(title: 'Injury Details'),
                    SizedBox(height: 12.h),
                    DropdownButtonFormField<String>(
                      value: _injurySeverity,
                      decoration: InputDecoration(
                        labelText: 'Severity',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      items: ['Minor', 'Moderate', 'Severe']
                          .map((e) => DropdownMenuItem(value: e, child: Text(e)))
                          .toList(),
                      onChanged: (value) => setState(() => _injurySeverity = value ?? 'Minor'),
                    ),
                    SizedBox(height: 12.h),
                    TextFormField(
                      maxLines: 3,
                      decoration: InputDecoration(
                        labelText: 'Injury Details',
                        hintText: 'Describe injury and treatment',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      onChanged: (value) => _injuryDetails = value,
                    ),
                    SizedBox(height: 12.h),
                    CheckboxListTile(
                      contentPadding: EdgeInsets.zero,
                      title: const Text('Treated on-site'),
                      value: _injuryTreated,
                      onChanged: (value) => setState(() => _injuryTreated = value ?? false),
                    ),
                  ],
                ),
              ),
            ],
            if (_showBiteFields) ...[
              SizedBox(height: 16.h),
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SectionHeader(title: 'Bite Incident Details'),
                    SizedBox(height: 12.h),
                    DropdownButtonFormField<String>(
                      value: _biteSeverity,
                      decoration: InputDecoration(
                        labelText: 'Severity',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      items: ['Minor', 'Moderate', 'Severe']
                          .map((e) => DropdownMenuItem(value: e, child: Text(e)))
                          .toList(),
                      onChanged: (value) => setState(() => _biteSeverity = value ?? 'Minor'),
                    ),
                    SizedBox(height: 12.h),
                    TextFormField(
                      maxLines: 3,
                      decoration: InputDecoration(
                        labelText: 'Victim & Details',
                        hintText: 'Who was bitten and first aid given',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      onChanged: (value) => _biteDetails = value,
                    ),
                    SizedBox(height: 12.h),
                    TextFormField(
                      decoration: InputDecoration(
                        labelText: 'Reported To',
                        hintText: 'e.g. Owner, Clinic, Police',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8.r)),
                        isDense: true,
                        contentPadding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                      ),
                      onChanged: (value) => _biteReportedTo = value,
                    ),
                  ],
                ),
              ),
            ],
            SizedBox(height: 16.h),
            FilledButton(
              onPressed: _submitForm,
              child: const Text('Save Care Record'),
            ),
          ],
        ),
      ),
    );
  }
}
