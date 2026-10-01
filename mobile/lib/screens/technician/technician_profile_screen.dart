import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class TechnicianProfileScreen extends ConsumerStatefulWidget {
  const TechnicianProfileScreen({super.key});

  @override
  ConsumerState<TechnicianProfileScreen> createState() => _TechnicianProfileScreenState();
}

class _TechnicianProfileScreenState extends ConsumerState<TechnicianProfileScreen> {
  final _bio = TextEditingController();
  final _area = TextEditingController();
  final _experience = TextEditingController();
  TechnicianProfile? _profile;
  bool _exists = false;
  bool _loading = true;
  bool _busy = false;
  bool _savingPhoto = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _bio.dispose();
    _area.dispose();
    _experience.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final profile = await ref.read(apiProvider).technicianProfile();
      _bio.text = profile.bio ?? '';
      _area.text = profile.serviceArea ?? '';
      _experience.text = profile.experienceSummary ?? '';
      _profile = profile;
      _exists = true;
    } catch (_) {
      _exists = false;
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _changePhoto() async {
    if (!_exists) {
      setState(() => _error = 'Save your profile first, then you can add a photo.');
      return;
    }
    final file = await ImagePicker().pickImage(source: ImageSource.gallery, imageQuality: 85);
    if (file == null) return;
    setState(() {
      _savingPhoto = true;
      _error = null;
    });
    try {
      final saved = await ref.read(apiProvider).setOwnProfilePhoto(file.path, file.name);
      if (!mounted) return;
      setState(() => _profile = saved);
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Profile photo updated')));
    } catch (error) {
      if (mounted) setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _savingPhoto = false);
    }
  }

  Future<void> _save() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final saved = await ref.read(apiProvider).saveTechnicianProfile(
            {
              'bio': _bio.text.trim(),
              'serviceArea': _area.text.trim(),
              'experienceSummary': _experience.text.trim(),
            },
            exists: _exists,
          );
      _profile = saved;
      _exists = true;
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Profile saved')));
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FixFlowScaffold(
      kind: WorkspaceKind.technician,
      tabIndex: 0,
      title: 'Profile',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                if (_error != null) ErrorView(message: _error!),
                Row(
                  children: [
                    CircleAvatar(
                      radius: 40,
                      backgroundImage: mediaUrl(_profile?.profilePhotoUrl) == null
                          ? null
                          : NetworkImage(mediaUrl(_profile!.profilePhotoUrl)!),
                      child: mediaUrl(_profile?.profilePhotoUrl) == null
                          ? Text(
                              ((_profile?.displayName ?? 'T').trim().isEmpty ? 'T' : _profile!.displayName.trim()[0]).toUpperCase(),
                            )
                          : null,
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Profile photo'),
                          const SizedBox(height: 4),
                          const Text(
                            'Customers see this picture. A photo set by an admin appears here, and you can replace it.',
                            style: TextStyle(fontSize: 13),
                          ),
                          TextButton(
                            onPressed: _savingPhoto ? null : _changePhoto,
                            child: Text(_savingPhoto ? 'Saving…' : (_profile?.profilePhotoUrl == null ? 'Add photo' : 'Change photo')),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                if (_profile != null) ...[
                  Text(
                    _profile!.reviewCount == 0
                        ? 'No reviews yet'
                        : '${_profile!.averageRating.toStringAsFixed(1)} average from ${_profile!.reviewCount} review${_profile!.reviewCount == 1 ? '' : 's'}',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  Row(
                    children: List.generate(
                      5,
                      (index) => Icon(
                        index < _profile!.averageRating.round() ? Icons.star : Icons.star_border,
                        color: const Color(0xFFC4A574),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                ],
                TextField(controller: _bio, maxLines: 3, decoration: const InputDecoration(labelText: 'Bio')),
                const SizedBox(height: 12),
                TextField(controller: _area, decoration: const InputDecoration(labelText: 'Service area')),
                const SizedBox(height: 12),
                TextField(controller: _experience, maxLines: 3, decoration: const InputDecoration(labelText: 'Experience')),
                const SizedBox(height: 16),
                FilledButton(onPressed: _busy ? null : _save, child: Text(_exists ? 'Update profile' : 'Create profile')),
              ],
            ),
    );
  }
}
