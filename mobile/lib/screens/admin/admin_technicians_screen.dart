import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class AdminTechniciansScreen extends ConsumerStatefulWidget {
  const AdminTechniciansScreen({super.key});

  @override
  ConsumerState<AdminTechniciansScreen> createState() => _AdminTechniciansScreenState();
}

class _AdminTechniciansScreenState extends ConsumerState<AdminTechniciansScreen> {
  late Future<PagedResult<TechnicianReport>> _future;
  String? _uploadingId;
  String? _error;

  @override
  void initState() {
    super.initState();
    _future = ref.read(apiProvider).technicianReports();
  }

  Future<void> _reload() async {
    setState(() {
      _error = null;
      _future = ref.read(apiProvider).technicianReports();
    });
  }

  Future<void> _pickPhoto(TechnicianReport technician) async {
    final file = await ImagePicker().pickImage(source: ImageSource.gallery, imageQuality: 85);
    if (file == null) return;
    setState(() {
      _uploadingId = technician.id;
      _error = null;
    });
    try {
      await ref.read(apiProvider).setTechnicianPhoto(technician.id, file.path, file.name);
      if (!mounted) return;
      await _reload();
    } catch (error) {
      if (!mounted) return;
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _uploadingId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FixFlowScaffold(
      title: 'Technician photos',
      description: 'JPEG, PNG or WebP up to 5 MB. Customers see this picture when they confirm a booking.',
      body: FutureBuilder(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.hasError) return ErrorView(message: snapshot.error.toString());
          if (!snapshot.hasData) {
            return const Center(child: CircularProgressIndicator());
          }
          final items = snapshot.data!.items;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              if (_error != null) ErrorView(message: _error!),
              if (items.isEmpty)
                const Text('No technicians yet.', style: TextStyle(color: AppColors.muted)),
              ...items.map(
                (row) => Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  child: ListTile(
                    leading: CircleAvatar(
                      backgroundImage: mediaUrl(row.profilePhotoUrl) == null
                          ? null
                          : NetworkImage(mediaUrl(row.profilePhotoUrl)!),
                      child: mediaUrl(row.profilePhotoUrl) == null ? Text(row.displayName.isEmpty ? 'T' : row.displayName[0]) : null,
                    ),
                    title: Text(row.displayName),
                    subtitle: Text(row.isSuspended ? 'Suspended' : 'Active'),
                    trailing: TextButton(
                      onPressed: _uploadingId == row.id ? null : () => _pickPhoto(row),
                      child: Text(_uploadingId == row.id ? 'Saving…' : 'Set photo'),
                    ),
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}
