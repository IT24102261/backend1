import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

class AdminVerificationsScreen extends ConsumerStatefulWidget {
  const AdminVerificationsScreen({super.key});

  @override
  ConsumerState<AdminVerificationsScreen> createState() => _AdminVerificationsScreenState();
}

class _AdminVerificationsScreenState extends ConsumerState<AdminVerificationsScreen> {
  late Future<PagedResult<TechnicianApplication>> _future;

  @override
  void initState() {
    super.initState();
    _future = ref.read(apiProvider).adminApplications();
  }

  @override
  Widget build(BuildContext context) {
    return FixFlowScaffold(
      title: 'Verifications',
      description: 'Open an application to inspect NIC, profile photo, and certificate before you approve.',
      body: FutureBuilder(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.hasError) return ErrorView(message: snapshot.error.toString());
          if (!snapshot.hasData) {
            return const Center(child: CircularProgressIndicator());
          }
          final items = snapshot.data!.items;
          if (items.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(16),
              child: Text('No technician applications yet.', style: TextStyle(color: AppColors.muted)),
            );
          }
          return ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: items.length,
            itemBuilder: (context, index) {
              final row = items[index];
              return Card(
                margin: const EdgeInsets.only(bottom: 12),
                child: ListTile(
                  leading: CircleAvatar(
                    backgroundImage: mediaUrl(row.profilePhotoUrl) == null ? null : NetworkImage(mediaUrl(row.profilePhotoUrl)!),
                    child: mediaUrl(row.profilePhotoUrl) == null
                        ? Text((row.technicianDisplayName ?? 'T').isEmpty ? 'T' : (row.technicianDisplayName ?? 'T')[0].toUpperCase())
                        : null,
                  ),
                  title: Text(row.technicianDisplayName ?? 'Technician'),
                  subtitle: Text('${row.categoryName ?? 'Trade'} · ${row.evidenceCount} file(s)'),
                  trailing: StatusChip(label: row.status),
                  onTap: () => Navigator.pushNamed(context, AppRoutes.adminVerificationDetail, arguments: row.id),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
