import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/section_card.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

class RequestDetailScreen extends ConsumerStatefulWidget {
  const RequestDetailScreen({super.key, required this.requestId});

  final String requestId;

  @override
  ConsumerState<RequestDetailScreen> createState() => _RequestDetailScreenState();
}

class _RequestDetailScreenState extends ConsumerState<RequestDetailScreen> {
  ServiceRequest? _request;
  List<RequestHistory> _history = [];
  String? _error;
  bool _loading = true;
  final _clarification = TextEditingController();

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _clarification.dispose();
    super.dispose();
  }

  bool get _canCancel {
    final status = _request?.status;
    return status != null &&
        !const {'BOOKED', 'COMPLETED', 'CANCELLED', 'FAILED'}.contains(status);
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final api = ref.read(apiProvider);
      final request = await api.getRequest(widget.requestId);
      final history = await api.requestHistory(widget.requestId);
      setState(() {
        _request = request;
        _history = history;
      });
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _cancel() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancel this request?'),
        content: const Text(
          'Technicians who received an invitation or sent a quotation will be told you cancelled this request.',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep request')),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Cancel request')),
        ],
      ),
    );
    if (confirmed != true) return;
    try {
      await ref.read(apiProvider).cancelRequest(widget.requestId);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Request cancelled. Technicians were notified.')),
        );
      }
      await _load();
    } catch (error) {
      setState(() => _error = error.toString());
    }
  }

  Future<void> _sendClarification() async {
    if (_clarification.text.trim().isEmpty) {
      setState(() => _error = 'Type your answer first, then tap Submit clarification.');
      return;
    }
    setState(() => _error = null);
    try {
      await ref.read(apiProvider).addClarification(widget.requestId, _clarification.text.trim());
      _clarification.clear();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Answer sent. Matching will continue.')),
        );
      }
      await _load();
    } catch (error) {
      setState(() => _error = error.toString());
    }
  }

  @override
  Widget build(BuildContext context) {
    final request = _request;
    return FixFlowScaffold(
      title: 'Request details',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && request == null
              ? ErrorView(message: _error!, onRetry: _load)
              : request == null
                  ? const EmptyView(message: 'Request not found.')
                  : ListView(
                      padding: const EdgeInsets.all(16),
                      children: [
                        if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                        Row(
                          children: [
                            Expanded(child: Text(request.categoryName ?? 'Service request', style: Theme.of(context).textTheme.titleLarge)),
                            StatusChip(label: request.status),
                          ],
                        ),
                        const SizedBox(height: 8),
                        Text(request.description),
                        const SizedBox(height: 8),
                        Text('Created ${formatDate(request.createdAt)}'),
                        const SizedBox(height: 16),
                        SectionCard(
                          title: 'Status timeline',
                          child: _history.isEmpty
                              ? const Text('No history yet.')
                              : Column(
                                  children: _history
                                      .map((item) => ListTile(
                                            contentPadding: EdgeInsets.zero,
                                            title: Text('${formatStatus(item.fromStatus)} → ${formatStatus(item.toStatus)}'),
                                            subtitle: Text('${formatDate(item.timestamp)}\n${item.note ?? ''}'),
                                            isThreeLine: true,
                                          ))
                                      .toList(),
                                ),
                        ),
                        if (request.status == 'CLARIFICATION_REQUIRED') ...[
                          const SizedBox(height: 12),
                          SectionCard(
                            title: 'We need a little more information',
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  _history
                                          .where((item) => item.toStatus == 'CLARIFICATION_REQUIRED')
                                          .map((item) => item.note)
                                          .whereType<String>()
                                          .where((note) => note.trim().isNotEmpty)
                                          .lastOrNull ??
                                      'Please add more detail so matching can continue.',
                                ),
                                const SizedBox(height: 12),
                                TextField(
                                  controller: _clarification,
                                  decoration: const InputDecoration(
                                    labelText: 'Your answer',
                                    hintText: 'Example: 2 plug switches need replacement',
                                  ),
                                  maxLines: 3,
                                ),
                                const SizedBox(height: 8),
                                FilledButton(onPressed: _sendClarification, child: const Text('Submit clarification')),
                              ],
                            ),
                          ),
                        ],
                        const SizedBox(height: 12),
                        if (_canCancel)
                          OutlinedButton(
                            onPressed: _cancel,
                            child: const Text('Cancel request'),
                          ),
                        if (_canCancel) const SizedBox(height: 8),
                        FilledButton(
                          onPressed: request.status == 'CANCELLED'
                              ? null
                              : () => Navigator.pushNamed(context, AppRoutes.quotes, arguments: request),
                          child: const Text('Compare quotations'),
                        ),
                      ],
                    ),
    );
  }
}
