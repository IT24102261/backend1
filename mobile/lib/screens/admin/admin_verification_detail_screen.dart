import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/authenticated_media.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

class AdminVerificationDetailScreen extends ConsumerStatefulWidget {
  const AdminVerificationDetailScreen({super.key, required this.applicationId});

  final String applicationId;

  @override
  ConsumerState<AdminVerificationDetailScreen> createState() => _AdminVerificationDetailScreenState();
}

class _AdminVerificationDetailScreenState extends ConsumerState<AdminVerificationDetailScreen> {
  TechnicianApplication? _item;
  final _notes = TextEditingController();
  String? _error;
  bool _loading = true;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _notes.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final item = await ref.read(apiProvider).adminApplication(widget.applicationId);
      setState(() {
        _item = item;
        _notes.text = item.decisionNotes ?? '';
        _loading = false;
      });
    } catch (error) {
      setState(() {
        _error = error.toString();
        _loading = false;
      });
    }
  }

  Future<void> _decide(String action) async {
    setState(() => _busy = true);
    try {
      final updated = await ref.read(apiProvider).decideApplication(widget.applicationId, action, _notes.text);
      if (!mounted) return;
      setState(() => _item = updated);
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Application ${formatStatus(updated.status)}')));
    } catch (error) {
      if (!mounted) return;
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  String _label(String type) {
    switch (type.toUpperCase()) {
      case 'IDENTITY':
        return 'NIC / Identity';
      case 'CERTIFICATE':
        return 'Studied certificate';
      case 'LICENSE':
        return 'License';
      default:
        return formatStatus(type);
    }
  }

  @override
  Widget build(BuildContext context) {
    final item = _item;
    return FixFlowScaffold(
      title: 'Review application',
      description: 'Look at the profile photo, NIC, and certificate before you approve this trade.',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && item == null
              ? ErrorView(message: _error!)
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    if (_error != null) ErrorView(message: _error!),
                    if (item != null) ...[
                      Row(
                        children: [
                          CircleAvatar(
                            radius: 36,
                            backgroundImage: mediaUrl(item.profilePhotoUrl) == null
                                ? null
                                : NetworkImage(mediaUrl(item.profilePhotoUrl)!),
                            child: mediaUrl(item.profilePhotoUrl) == null ? const Text('T') : null,
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(item.technicianDisplayName ?? 'Technician', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                                Text(item.technicianEmail ?? '', style: const TextStyle(color: AppColors.muted)),
                                Text(item.categoryName ?? '', style: const TextStyle(color: AppColors.muted)),
                              ],
                            ),
                          ),
                          StatusChip(label: item.status),
                        ],
                      ),
                      const SizedBox(height: 20),
                      const Text('Uploaded documents', style: TextStyle(fontWeight: FontWeight.w700)),
                      const SizedBox(height: 8),
                      if (item.documents.isEmpty)
                        const Text('No NIC or certificate files were uploaded.', style: TextStyle(color: AppColors.muted)),
                      ...item.documents.map(
                        (doc) => Padding(
                          padding: const EdgeInsets.only(bottom: 16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(_label(doc.evidenceType), style: const TextStyle(fontWeight: FontWeight.w600)),
                              const SizedBox(height: 8),
                              AuthenticatedMedia(path: doc.url, label: _label(doc.evidenceType), mimeType: doc.mimeType),
                            ],
                          ),
                        ),
                      ),
                      TextField(
                        controller: _notes,
                        maxLines: 3,
                        decoration: const InputDecoration(labelText: 'Decision notes'),
                      ),
                      const SizedBox(height: 12),
                      FilledButton(onPressed: _busy ? null : () => _decide('approve'), child: const Text('Approve')),
                      const SizedBox(height: 8),
                      OutlinedButton(onPressed: _busy ? null : () => _decide('request-info'), child: const Text('Request more information')),
                      const SizedBox(height: 8),
                      OutlinedButton(onPressed: _busy ? null : () => _decide('reject'), child: const Text('Reject')),
                    ],
                  ],
                ),
    );
  }
}
