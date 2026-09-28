import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/section_card.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

class CustomerHomeScreen extends ConsumerWidget {
  const CustomerHomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authProvider).value;
    if (user == null || !user.isCustomer) {
      return FixFlowScaffold(
        title: 'Customer',
        body: UnauthorizedView(
          onLogin: () => Navigator.pushNamedAndRemoveUntil(context, AppRoutes.login, (_) => false),
        ),
      );
    }

    final firstName = user.displayName.split(' ').first;
    return FixFlowScaffold(
      kind: WorkspaceKind.customer,
      tabIndex: 0,
      title: 'Dashboard',
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
        children: [
          WorkspaceBanner(
            kicker: 'Customer workspace',
            title: 'Hello, $firstName',
            description: 'Create a request, compare quotations, and follow the job until the work is finished.',
          ),
          const SizedBox(height: 16),
          const _DashboardLists(),
        ],
      ),
    );
  }
}

class _DashboardLists extends ConsumerWidget {
  const _DashboardLists();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return FutureBuilder(
      future: Future.wait([
        ref.read(apiProvider).requests(),
        ref.read(apiProvider).bookings(),
      ]),
      builder: (context, snapshot) {
        if (snapshot.hasError) return ErrorView(message: snapshot.error.toString());
        if (!snapshot.hasData) {
          return const Padding(padding: EdgeInsets.all(32), child: Center(child: CircularProgressIndicator()));
        }
        final requests = snapshot.data![0] as PagedResult<ServiceRequest>;
        final bookings = snapshot.data![1] as PagedResult<Booking>;
        final waiting = bookings.items.where((item) => item.status == 'PENDING_VALIDATION').length;

        return Column(
          children: [
            Row(
              children: [
                Expanded(child: StatTile(label: 'Requests', value: '${requests.totalCount}', hint: 'Submitted service requests', icon: Icons.assignment_outlined)),
                const SizedBox(width: 10),
                Expanded(child: StatTile(label: 'Bookings', value: '${bookings.totalCount}', hint: waiting > 0 ? '$waiting waiting confirmation' : 'Active and confirmed jobs', icon: Icons.work_outline, navy: true)),
              ],
            ),
            const SizedBox(height: 12),
            QuickActionTile(
              icon: Icons.add,
              title: 'New request',
              description: 'Describe the work and invite eligible technicians.',
              onTap: () => Navigator.pushNamed(context, AppRoutes.createRequest),
            ),
            QuickActionTile(
              icon: Icons.work_outline,
              title: 'Track a job',
              description: 'Follow status, confirm work, and open the map.',
              onTap: () => Navigator.pushNamed(context, AppRoutes.bookings),
            ),
            const SizedBox(height: 8),
            SectionCard(
              title: 'Recent requests',
              description: 'Your latest service requests.',
              action: TextButton(onPressed: () => Navigator.pushNamed(context, AppRoutes.requestHistory), child: const Text('View all')),
              child: requests.items.isEmpty
                  ? const Text('No requests yet. Start with a new service request.')
                  : Column(
                      children: requests.items.take(5).map((item) {
                        return Padding(
                          padding: const EdgeInsets.only(bottom: 12),
                          child: InkWell(
                            onTap: () => Navigator.pushNamed(context, AppRoutes.requestDetail, arguments: item.id),
                            child: Row(
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(item.categoryName ?? item.description, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontWeight: FontWeight.w600, color: AppColors.ink)),
                                      const SizedBox(height: 4),
                                      Text(
                                        item.status == 'CLARIFICATION_REQUIRED'
                                            ? 'How many need to be changed? Tap to answer.'
                                            : '${item.serviceArea ?? 'Area not set'} · ${formatDate(item.createdAt)}',
                                        style: const TextStyle(fontSize: 12, color: AppColors.faint),
                                      ),
                                    ],
                                  ),
                                ),
                                StatusChip(label: item.status),
                              ],
                            ),
                          ),
                        );
                      }).toList(),
                    ),
            ),
            SectionCard(
              title: 'Recent bookings',
              description: 'Jobs you have selected or confirmed.',
              action: TextButton(onPressed: () => Navigator.pushNamed(context, AppRoutes.bookings), child: const Text('Track jobs')),
              child: bookings.items.isEmpty
                  ? const Text('No bookings yet. Select a quotation from a request.')
                  : Column(
                      children: bookings.items.take(5).map((item) {
                        return Padding(
                          padding: const EdgeInsets.only(bottom: 12),
                          child: InkWell(
                            onTap: () => Navigator.pushNamed(context, AppRoutes.bookingDetail, arguments: item.id),
                            child: Row(
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(item.technicianDisplayName ?? 'Booking ${shortId(item.id)}', maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontWeight: FontWeight.w600, color: AppColors.ink)),
                                      const SizedBox(height: 4),
                                      Text('${item.categoryName ?? 'Service'} · ${formatBookingStatus(item.status)}', style: const TextStyle(fontSize: 12, color: AppColors.faint)),
                                    ],
                                  ),
                                ),
                                StatusChip(label: item.status, display: formatBookingStatus(item.status)),
                              ],
                            ),
                          ),
                        );
                      }).toList(),
                    ),
            ),
          ],
        );
      },
    );
  }
}
