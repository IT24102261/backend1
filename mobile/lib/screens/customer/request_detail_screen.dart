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

  Future<void> _sendClarification() async {
    if (_clarification.text.trim().isEmpty) return;
    try {
      await ref.read(apiProvider).addClarification(widget.requestId, _clarification.text.trim());
      _clarification.clear();
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
                            title: 'AI clarification',
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                ..._history
                                    .where((item) => item.toStatus == 'CLARIFICATION_REQUIRED' || (item.note ?? '').isNotEmpty)
                                    .map((item) => Padding(
                                          padding: const EdgeInsets.only(bottom: 8),
                                          child: Text(item.note ?? 'Please add more detail so matching can continue.'),
                                        )),
                                TextField(
                                  controller: _clarification,
                                  decoration: const InputDecoration(labelText: 'Your answer'),
                                  maxLines: 3,
                                ),
                                const SizedBox(height: 8),
                                FilledButton(onPressed: _sendClarification, child: const Text('Submit clarification')),
                              ],
                            ),
                          ),
                        ],
                        const SizedBox(height: 12),
                        FilledButton(
                          onPressed: () => Navigator.pushNamed(context, AppRoutes.quotes, arguments: request),
                          child: const Text('Compare quotations'),
                        ),
                      ],
                    ),
    );
  }
}
