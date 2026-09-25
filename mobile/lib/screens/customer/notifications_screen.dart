import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authProvider).value;
    return FixFlowScaffold(
      kind: user?.isTechnician == true ? WorkspaceKind.technician : WorkspaceKind.customer,
      tabIndex: user?.isTechnician == true ? 0 : 3,
      title: 'Notifications',
      body: FutureBuilder(
        future: user?.isTechnician == true
            ? Future.wait<Object>([
                ref.read(apiProvider).notifications(),
                ref.read(apiProvider).invitations(),
                ref.read(apiProvider).bookings(),
              ])
            : Future.wait<Object>([
                ref.read(apiProvider).notifications(),
                ref.read(apiProvider).requests(),
                ref.read(apiProvider).bookings(),
              ]),
        builder: (context, snapshot) {
          if (snapshot.hasError) return ErrorView(message: snapshot.error.toString());
          if (!snapshot.hasData) return const Center(child: CircularProgressIndicator());
          final items = <(String, String, DateTime?, String)>[];
          final alerts = snapshot.data![0] as List<AppNotification>;
          items.addAll(alerts.map((item) => (item.title, item.message, item.createdAt, item.isRead ? 'READ' : 'NEW')));
          if (user?.isTechnician == true) {
            final invitations = snapshot.data![1] as List<Invitation>;
            final bookings = snapshot.data![2] as PagedResult<Booking>;
            items.addAll(invitations.map((item) => ('Invitation ${formatStatus(item.status)}', item.description, item.sentAt, item.status)));
            items.addAll(bookings.items.map((item) => ('Job ${formatStatus(item.status)}', item.id, item.confirmedAt, item.status)));
          } else {
            final requests = snapshot.data![1] as PagedResult<ServiceRequest>;
            final bookings = snapshot.data![2] as PagedResult<Booking>;
            items.addAll(requests.items.map((item) => ('Request ${formatStatus(item.status)}', item.description, item.createdAt, item.status)));
            items.addAll(bookings.items.map((item) => ('Booking ${formatStatus(item.status)}', item.id, item.confirmedAt, item.status)));
          }
          if (items.isEmpty) {
            return const EmptyView(message: 'No request or booking updates yet.');
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
            children: items
                .map(
                  (item) => Card(
                    child: ListTile(
                      title: Text(item.$1),
                      subtitle: Text('${item.$2}\n${formatDate(item.$3)}'),
                      isThreeLine: true,
                      trailing: StatusChip(label: item.$4),
                    ),
                  ),
                )
                .toList(),
          );
        },
      ),
    );
  }
}
