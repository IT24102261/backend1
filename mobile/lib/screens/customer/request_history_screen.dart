import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

final _requestsProvider = FutureProvider<PagedResult<ServiceRequest>>((ref) {
  return ref.watch(apiProvider).requests();
});

class RequestHistoryScreen extends ConsumerWidget {
  const RequestHistoryScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final requests = ref.watch(_requestsProvider);
    return FixFlowScaffold(
      kind: WorkspaceKind.customer,
      tabIndex: 1,
      title: 'Requests',
      description: 'Create a request, then compare AI-recommended quotations.',
      body: AsyncBody(
        value: requests,
        onRetry: () => ref.invalidate(_requestsProvider),
        builder: (data) {
          if (data.items.isEmpty) {
            return const EmptyView(message: 'You have not created any requests yet.');
          }
          return ListView.separated(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
            itemCount: data.items.length,
            separatorBuilder: (_, _) => const SizedBox(height: 8),
            itemBuilder: (context, index) {
              final item = data.items[index];
              return Card(
                child: ListTile(
                  title: Text(item.categoryName ?? 'Service request'),
                  subtitle: Text('${item.description}\n${formatDate(item.createdAt)}'),
                  isThreeLine: true,
                  trailing: StatusChip(label: item.status),
                  onTap: () => Navigator.pushNamed(context, AppRoutes.requestDetail, arguments: item.id),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
