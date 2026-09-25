import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

final _bookingsProvider = FutureProvider<PagedResult<Booking>>((ref) => ref.watch(apiProvider).bookings());

class BookingsScreen extends ConsumerWidget {
  const BookingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final bookings = ref.watch(_bookingsProvider);
    return FixFlowScaffold(
      kind: WorkspaceKind.customer,
      tabIndex: 2,
      title: 'Bookings',
      description: 'Confirm a selected quote, then track the job until the work is finished.',
      body: AsyncBody(
        value: bookings,
        onRetry: () => ref.invalidate(_bookingsProvider),
        builder: (data) {
          if (data.items.isEmpty) {
            return const EmptyView(message: 'No bookings yet. Select a quote to start one.');
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
            children: data.items
                .map(
                  (item) => Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: Card(
                    child: ListTile(
                      title: Text(item.technicianDisplayName ?? 'Booking ${shortId(item.id)}'),
                      subtitle: Text(formatDate(item.confirmedAt)),
                      trailing: StatusChip(label: item.status, display: formatBookingStatus(item.status)),
                      onTap: () => Navigator.pushNamed(context, AppRoutes.bookingDetail, arguments: item.id),
                    ),
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
