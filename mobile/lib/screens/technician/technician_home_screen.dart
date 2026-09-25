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

class TechnicianHomeScreen extends ConsumerWidget {
  const TechnicianHomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authProvider).value;
    if (user == null || !user.isTechnician) {
      return FixFlowScaffold(
        title: 'Technician',
        body: UnauthorizedView(
          onLogin: () => Navigator.pushNamedAndRemoveUntil(context, AppRoutes.login, (_) => false),
        ),
      );
    }

    final firstName = user.displayName.split(' ').first;
    return FixFlowScaffold(
      kind: WorkspaceKind.technician,
      tabIndex: 0,
      title: 'Dashboard',
      body: FutureBuilder(
        future: Future.wait<Object?>([
          () async {
            try {
              return await ref.read(apiProvider).technicianProfile();
            } catch (_) {
              return null;
            }
          }(),
          () async {
            try {
              return await ref.read(apiProvider).myApplications();
            } catch (_) {
              return <TechnicianApplication>[];
            }
          }(),
          () async {
            try {
              return await ref.read(apiProvider).invitations();
            } catch (_) {
              return <Invitation>[];
            }
          }(),
          ref.read(apiProvider).bookings(),
        ]),
        builder: (context, snapshot) {
          if (snapshot.hasError) {
            return ErrorView(message: snapshot.error.toString());
          }
          if (!snapshot.hasData) {
            return const Center(child: CircularProgressIndicator());
          }
          final profile = snapshot.data![0] as TechnicianProfile?;
          final applications = snapshot.data![1] as List<TechnicianApplication>;
          final invitations = snapshot.data![2] as List<Invitation>;
          final jobs = snapshot.data![3] as PagedResult<Booking>;
          final openInvites = invitations.where((item) => item.status == 'SENT').length;
          final approved = profile?.approvedCategories.length ?? user.approvedCategories.length;

          return ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
            children: [
              WorkspaceBanner(
                kicker: 'Technician workspace',
                title: 'Welcome back, $firstName',
                description: 'Review invitations, send priced quotes, and keep booked jobs moving until the customer confirms.',
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(child: StatTile(label: 'Approved trades', value: '$approved', hint: approved > 0 ? 'You can quote in these categories' : 'Apply from Verification', icon: Icons.verified_outlined)),
                  const SizedBox(width: 10),
                  Expanded(child: StatTile(label: 'Open invites', value: '$openInvites', hint: 'Requests waiting for your quote', icon: Icons.inbox_outlined, navy: true)),
                ],
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(child: StatTile(label: 'Jobs', value: '${jobs.totalCount}', hint: 'Bookings assigned to you', icon: Icons.handyman_outlined)),
                  const SizedBox(width: 10),
                  Expanded(child: StatTile(label: 'Rating', value: profile == null ? '—' : profile.averageRating.toStringAsFixed(1), hint: '${profile?.reviewCount ?? 0} published reviews', icon: Icons.star_outline)),
                ],
              ),
              const SizedBox(height: 12),
              QuickActionTile(icon: Icons.inbox_outlined, title: 'Invitations', description: 'See new requests in your approved trade.', onTap: () => Navigator.pushNamed(context, AppRoutes.technicianInvitations)),
              QuickActionTile(icon: Icons.request_quote_outlined, title: 'Send a quote', description: 'Price labour, materials, and travel.', onTap: () => Navigator.pushNamed(context, AppRoutes.technicianInvitations)),
              QuickActionTile(icon: Icons.work_outline, title: 'Active jobs', description: 'Update status and open the customer map.', onTap: () => Navigator.pushNamed(context, AppRoutes.technicianJobs)),
              const SizedBox(height: 8),
              SectionCard(
                title: 'Applications',
                description: 'Category verification status.',
                action: TextButton(onPressed: () => Navigator.pushNamed(context, AppRoutes.technicianVerification), child: const Text('Open')),
                child: applications.isEmpty
                    ? const Text('No applications yet.')
                    : Column(
                        children: applications.take(4).map((item) {
                          return Padding(
                            padding: const EdgeInsets.only(bottom: 10),
                            child: Row(
                              children: [
                                Expanded(child: Text(item.categoryName ?? 'Application', style: const TextStyle(fontWeight: FontWeight.w600, color: AppColors.ink))),
                                StatusChip(label: item.status),
                              ],
                            ),
                          );
                        }).toList(),
                      ),
              ),
              SectionCard(
                title: 'Recent jobs',
                action: TextButton(onPressed: () => Navigator.pushNamed(context, AppRoutes.technicianJobs), child: const Text('View all')),
                child: jobs.items.isEmpty
                    ? const Text('No jobs yet.')
                    : Column(
                        children: jobs.items.take(4).map((item) {
                          return Padding(
                            padding: const EdgeInsets.only(bottom: 10),
                            child: Row(
                              children: [
                                Expanded(
                                  child: Text(
                                    item.customerDisplayName?.isNotEmpty == true ? item.customerDisplayName! : 'Job ${shortId(item.id)}',
                                    style: const TextStyle(fontWeight: FontWeight.w600, color: AppColors.ink),
                                  ),
                                ),
                                StatusChip(label: item.status, display: formatBookingStatus(item.status)),
                              ],
                            ),
                          );
                        }).toList(),
                      ),
              ),
            ],
          );
        },
      ),
    );
  }
}
