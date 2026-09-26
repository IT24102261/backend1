import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

class TechnicianInvitationsScreen extends ConsumerStatefulWidget {
  const TechnicianInvitationsScreen({super.key});

  @override
  ConsumerState<TechnicianInvitationsScreen> createState() => _TechnicianInvitationsScreenState();
}

class _TechnicianInvitationsScreenState extends ConsumerState<TechnicianInvitationsScreen> {
  List<Invitation> _items = [];
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final items = await ref.read(apiProvider).invitations();
      setState(() => _items = items);
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FixFlowScaffold(
      kind: WorkspaceKind.technician,
      tabIndex: 1,
      title: 'Invitations',
      description: 'See new requests in your approved trade.',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? ErrorView(message: _error!, onRetry: _load)
              : _items.isEmpty
                  ? const EmptyView(message: 'No invitations yet.')
                  : ListView(
                      padding: const EdgeInsets.all(16),
                      children: _items
                          .map(
                            (item) => Card(
                              child: Padding(
                                padding: const EdgeInsets.all(16),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      children: [
                                        Expanded(child: Text(item.categoryName ?? 'Invitation')),
                                        StatusChip(label: item.status),
                                      ],
                                    ),
                                    Text(item.description),
                                    Text('Area: ${item.serviceArea ?? 'approximate only'}'),
                                    Text(formatDate(item.sentAt)),
                                    if (!item.canQuote)
                                      Padding(
                                        padding: const EdgeInsets.only(top: 8),
                                        child: Text(
                                          'This job has already been accepted by another technician.',
                                          style: Theme.of(context).textTheme.bodySmall?.copyWith(color: const Color(0xFF9A968E)),
                                        ),
                                      )
                                    else if (item.status == 'SENT')
                                      Row(
                                        children: [
                                          TextButton(
                                            onPressed: () async {
                                              await ref.read(apiProvider).acceptInvitation(item.id);
                                              await _load();
                                            },
                                            child: const Text('Accept'),
                                          ),
                                          TextButton(
                                            onPressed: () async {
                                              await ref.read(apiProvider).declineInvitation(item.id);
                                              await _load();
                                            },
                                            child: const Text('Decline'),
                                          ),
                                        ],
                                      )
                                    else if (item.status == 'ACCEPTED')
                                      FilledButton(
                                        onPressed: () => Navigator.pushNamed(context, AppRoutes.technicianQuote, arguments: item),
                                        child: const Text('Create quotation'),
                                      ),
                                  ],
                                ),
                              ),
                            ),
                          )
                          .toList(),
                    ),
    );
  }
}
