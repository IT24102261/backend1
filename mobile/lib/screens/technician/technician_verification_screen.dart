import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

class TechnicianVerificationScreen extends ConsumerStatefulWidget {
  const TechnicianVerificationScreen({super.key});

  @override
  ConsumerState<TechnicianVerificationScreen> createState() => _TechnicianVerificationScreenState();
}

class _TechnicianVerificationScreenState extends ConsumerState<TechnicianVerificationScreen> {
  List<Category> _categories = [];
  List<TechnicianApplication> _applications = [];
  String? _categoryId;
  String? _applicationId;
  String _evidenceType = 'LICENSE';
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final api = ref.read(apiProvider);
      final categories = await api.categories();
      final applications = await api.myApplications();
      setState(() {
        _categories = categories;
        _applications = applications;
        _categoryId ??= categories.isEmpty ? null : categories.first.id;
        _applicationId ??= applications.isEmpty ? null : applications.first.id;
      });
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _apply() async {
    if (_categoryId == null) return;
    try {
      await ref.read(apiProvider).applyCategory(_categoryId!);
      await _load();
    } catch (error) {
      setState(() => _error = error.toString());
    }
  }

  Future<void> _upload() async {
    if (_applicationId == null) return;
    final file = await ImagePicker().pickImage(source: ImageSource.gallery);
    if (file == null) return;
    try {
      await ref.read(apiProvider).uploadDocument(_applicationId!, file.path, file.name, _evidenceType);
      await _load();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Document uploaded')));
      }
    } catch (error) {
      setState(() => _error = error.toString());
    }
  }

  @override
  Widget build(BuildContext context) {
    return FixFlowScaffold(
      kind: WorkspaceKind.technician,
      tabIndex: 3,
      title: 'Verification',
      description: 'Apply per category. Approval for one trade never unlocks another.',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                if (_error != null) ErrorView(message: _error!, onRetry: _load),
                if (_categories.isNotEmpty)
                  DropdownButtonFormField<String>(
                    // ignore: deprecated_member_use
                    value: _categoryId,
                    decoration: const InputDecoration(labelText: 'Category'),
                    items: _categories.map((item) => DropdownMenuItem(value: item.id, child: Text(item.name))).toList(),
                    onChanged: (value) => setState(() => _categoryId = value),
                  ),
                const SizedBox(height: 12),
                FilledButton(onPressed: _apply, child: const Text('Apply for category')),
                const SizedBox(height: 16),
                const Text('Application status'),
                ..._applications.map(
                  (item) => ListTile(
                    title: Text(item.categoryName ?? item.categoryId),
                    subtitle: Text(formatDate(item.submittedAt)),
                    trailing: StatusChip(label: item.status),
                  ),
                ),
                ..._applications.expand((item) => item.documents.map((doc) => ListTile(
                      title: Text(doc.evidenceType == 'IDENTITY'
                          ? 'NIC / Identity'
                          : doc.evidenceType == 'CERTIFICATE'
                              ? 'Studied certificate'
                              : doc.evidenceType),
                      subtitle: Text(item.categoryName ?? 'Uploaded document'),
                    ))),
                const SizedBox(height: 16),
                if (_applications.isNotEmpty)
                  DropdownButtonFormField<String>(
                    // ignore: deprecated_member_use
                    value: _applicationId,
                    decoration: const InputDecoration(labelText: 'Application for document'),
                    items: _applications
                        .map((item) => DropdownMenuItem(value: item.id, child: Text(item.categoryName ?? item.id)))
                        .toList(),
                    onChanged: (value) => setState(() => _applicationId = value),
                  ),
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  // ignore: deprecated_member_use
                  value: _evidenceType,
                  decoration: const InputDecoration(labelText: 'Evidence type'),
                  items: const [
                    DropdownMenuItem(value: 'IDENTITY', child: Text('NIC / Identity')),
                    DropdownMenuItem(value: 'LICENSE', child: Text('LICENSE')),
                    DropdownMenuItem(value: 'INSURANCE', child: Text('INSURANCE')),
                    DropdownMenuItem(value: 'CERTIFICATE', child: Text('Studied certificate')),
                    DropdownMenuItem(value: 'WORK_SAMPLE', child: Text('WORK_SAMPLE')),
                    DropdownMenuItem(value: 'OTHER', child: Text('OTHER')),
                  ],
                  onChanged: (value) => setState(() => _evidenceType = value ?? 'LICENSE'),
                ),
                const SizedBox(height: 12),
                OutlinedButton(onPressed: _upload, child: const Text('Upload verification document')),
              ],
            ),
    );
  }
}
