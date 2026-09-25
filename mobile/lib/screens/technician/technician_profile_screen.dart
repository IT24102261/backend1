import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
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
  bool _exists = false;
  bool _loading = true;
  bool _busy = false;
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
      _exists = true;
    } catch (_) {
      _exists = false;
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _save() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(apiProvider).saveTechnicianProfile(
            {
              'bio': _bio.text.trim(),
              'serviceArea': _area.text.trim(),
              'experienceSummary': _experience.text.trim(),
            },
            exists: _exists,
          );
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
      title: 'Profile',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                if (_error != null) ErrorView(message: _error!),
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
