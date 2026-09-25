import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/services/device_location_service.dart';
import 'package:fixflow_mobile/services/geolocator_location_platform.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class CreateRequestScreen extends ConsumerStatefulWidget {
  const CreateRequestScreen({super.key});

  @override
  ConsumerState<CreateRequestScreen> createState() => _CreateRequestScreenState();
}

class _CreateRequestScreenState extends ConsumerState<CreateRequestScreen> {
  final _form = GlobalKey<FormState>();
  final _description = TextEditingController();
  final _address = TextEditingController();
  final _area = TextEditingController();
  final _budget = TextEditingController();
  DateTime? _date;
  TimeOfDay? _time;
  String? _categoryId;
  XFile? _image;
  double? _lat;
  double? _lng;
  String? _locationNote;
  List<Category> _categories = [];
  bool _busy = false;
  String? _error;
  final DeviceLocationService _location = DeviceLocationService(GeolocatorLocationPlatform());

  @override
  void initState() {
    super.initState();
    ref.read(apiProvider).categories().then((items) {
      if (mounted) setState(() => _categories = items);
    }).catchError((_) {});
  }

  @override
  void dispose() {
    _description.dispose();
    _address.dispose();
    _area.dispose();
    _budget.dispose();
    super.dispose();
  }

  Future<void> _pickImage() async {
    final file = await ImagePicker().pickImage(source: ImageSource.gallery, imageQuality: 80);
    if (file != null) setState(() => _image = file);
  }

  Future<void> _useGps() async {
    try {
      final result = await _location.tryGetCurrentLocation();
      if (!mounted) return;
      setState(() {
        _lat = result.latitude;
        _lng = result.longitude;
        _locationNote = result.message;
      });
      if (result.hasCoordinates) {
        try {
          final reverse = await ref.read(apiProvider).reverseGeocode(result.latitude!, result.longitude!);
          if (!mounted) return;
          if ((reverse['serviceArea'] as String?)?.isNotEmpty == true && _area.text.trim().isEmpty) {
            _area.text = reverse['serviceArea'] as String;
          }
          if ((reverse['displayName'] as String?)?.isNotEmpty == true && _address.text.trim().isEmpty) {
            _address.text = reverse['displayName'] as String;
          }
        } catch (_) {
          // Reverse geocode is optional. The request can continue with GPS or a manual area.
        }
      }
    } catch (_) {
      if (!mounted) return;
      setState(() => _locationNote = 'GPS is unavailable. Enter the address manually.');
    }
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    if (_date == null || _time == null) {
      setState(() => _error = 'Choose a preferred appointment date and time.');
      return;
    }
    if (_address.text.trim().isEmpty && (_lat == null || _lng == null)) {
      setState(() => _error = 'Allow location or enter a manual address.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final start = _date == null
          ? null
          : DateTime(_date!.year, _date!.month, _date!.day, _time?.hour ?? 9, _time?.minute ?? 0);
      final created = await ref.read(apiProvider).createRequest({
        if (_categoryId != null) 'categoryId': _categoryId,
        'description': _description.text.trim(),
        if (_budget.text.trim().isNotEmpty) 'budgetAmount': num.tryParse(_budget.text.trim()),
        'serviceArea': _area.text.trim(),
        'address': _address.text.trim(),
        if (_lat != null) 'latitude': _lat,
        if (_lng != null) 'longitude': _lng,
        if (start != null) 'preferredStart': start.toUtc().toIso8601String(),
        if (start != null) 'preferredEnd': start.add(const Duration(hours: 2)).toUtc().toIso8601String(),
      });
      if (_image != null) {
        await ref.read(apiProvider).addMedia(created.id, _image!.path, _image!.name);
      }
      await ref.read(apiProvider).submitRequest(created.id);
      if (!mounted) return;
      Navigator.pushReplacementNamed(context, AppRoutes.requestDetail, arguments: created.id);
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FixFlowScaffold(
      title: 'New request',
      description: 'Describe the work and invite eligible technicians.',
      body: Form(
        key: _form,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
            TextFormField(
              controller: _description,
              maxLines: 4,
              decoration: const InputDecoration(labelText: 'Problem description'),
              validator: (value) => value == null || value.trim().isEmpty ? 'Describe the problem' : null,
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              // ignore: deprecated_member_use
              value: _categoryId ?? '',
              decoration: const InputDecoration(labelText: 'Category (optional)'),
              items: [
                const DropdownMenuItem(value: '', child: Text('No category')),
                ..._categories.map((item) => DropdownMenuItem(value: item.id, child: Text(item.name))),
              ],
              onChanged: (value) => setState(() => _categoryId = value == null || value.isEmpty ? null : value),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _budget,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Budget (optional, stored with the request)'),
            ),
            const SizedBox(height: 12),
            ListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(_date == null ? 'Preferred date' : '${_date!.year}-${_date!.month}-${_date!.day}'),
              trailing: const Icon(Icons.calendar_today),
              onTap: () async {
                final picked = await showDatePicker(
                  context: context,
                  firstDate: DateTime.now(),
                  lastDate: DateTime.now().add(const Duration(days: 60)),
                  initialDate: DateTime.now(),
                );
                if (picked != null) setState(() => _date = picked);
              },
            ),
            ListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(_time == null ? 'Preferred time' : _time!.format(context)),
              trailing: const Icon(Icons.schedule),
              onTap: () async {
                final picked = await showTimePicker(context: context, initialTime: TimeOfDay.now());
                if (picked != null) setState(() => _time = picked);
              },
            ),
            TextFormField(
              controller: _area,
              decoration: const InputDecoration(labelText: 'Service area'),
              validator: (value) => value == null || value.trim().isEmpty ? 'Enter a service area' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(controller: _address, decoration: const InputDecoration(labelText: 'Manual address')),
            const SizedBox(height: 12),
            const Text('Location is optional. You can deny GPS and type a manual address and service area.'),
            const SizedBox(height: 8),
            OutlinedButton.icon(
              onPressed: _useGps,
              icon: const Icon(Icons.my_location),
              label: const Text('Use current location'),
            ),
            if (_locationNote != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_locationNote!)),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: _pickImage,
              icon: const Icon(Icons.photo),
              label: const Text('Attach problem image'),
            ),
            if (_image != null) ...[
              const SizedBox(height: 12),
              ClipRRect(
                borderRadius: BorderRadius.circular(12),
                child: Image.file(File(_image!.path), height: 180, fit: BoxFit.cover),
              ),
            ],
            const SizedBox(height: 20),
            FilledButton(onPressed: _busy ? null : _submit, child: Text(_busy ? 'Submitting…' : 'Submit request')),
          ],
        ),
      ),
    );
  }
}
