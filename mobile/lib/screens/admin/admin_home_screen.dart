import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class AdminHomeScreen extends ConsumerWidget {
  const AdminHomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return FixFlowScaffold(
      title: 'Admin',
      description: 'Review technician documents and manage the photos customers see.',
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          ListTile(
            tileColor: AppColors.cream,
            leading: const Icon(Icons.verified_outlined),
            title: const Text('Verifications'),
            subtitle: const Text('Review NIC, profile photo, and certificates.'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => Navigator.pushNamed(context, AppRoutes.adminVerifications),
          ),
          const SizedBox(height: 8),
          ListTile(
            tileColor: AppColors.cream,
            leading: const Icon(Icons.badge_outlined),
            title: const Text('Technician photos'),
            subtitle: const Text('Upload a display picture for each technician.'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => Navigator.pushNamed(context, AppRoutes.adminTechnicians),
          ),
        ],
      ),
    );
  }
}
