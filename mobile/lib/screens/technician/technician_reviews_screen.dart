import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

class TechnicianReviewsScreen extends ConsumerWidget {
  const TechnicianReviewsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authProvider).value;
    return FixFlowScaffold(
      title: 'Reviews',
      body: user?.technicianProfileId == null
          ? const EmptyView(message: 'Create a profile first to receive reviews.')
          : FutureBuilder(
              future: ref.read(apiProvider).technicianReviews(user!.technicianProfileId!),
              builder: (context, snapshot) {
                if (snapshot.hasError) return ErrorView(message: snapshot.error.toString());
                if (!snapshot.hasData) return const Center(child: CircularProgressIndicator());
                if (snapshot.data!.isEmpty) {
                  return const EmptyView(message: 'No reviews yet.');
                }
                return ListView(
                  padding: const EdgeInsets.all(16),
                  children: snapshot.data!
                      .map(
                        (item) => Card(
                          child: ListTile(
                            title: Text('${item.rating} / 5'),
                            subtitle: Text([
                              'Booking ${shortId(item.bookingId)}${item.customerDisplayName?.isNotEmpty == true ? ' · ${item.customerDisplayName}' : ''}',
                              item.body ?? 'No comment',
                              item.adminReply?.isNotEmpty == true
                                  ? 'Admin reply: ${item.adminReply}'
                                  : 'Technicians cannot reply. An admin can respond if needed.',
                            ].join('\n')),
                            isThreeLine: true,
                            trailing: StatusChip(label: item.status),
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
