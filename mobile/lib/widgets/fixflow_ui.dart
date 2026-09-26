import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';

enum WorkspaceKind { customer, technician }

class FixFlowScaffold extends ConsumerWidget {
  const FixFlowScaffold({
    super.key,
    required this.title,
    required this.body,
    this.kind,
    this.tabIndex,
    this.description,
    this.actions,
    this.floatingActionButton,
  });

  final String title;
  final String? description;
  final Widget body;
  final WorkspaceKind? kind;
  final int? tabIndex;
  final List<Widget>? actions;
  final Widget? floatingActionButton;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authProvider).value;
    final initials = (user?.displayName ?? 'U')
        .split(' ')
        .where((part) => part.isNotEmpty)
        .take(2)
        .map((part) => part[0].toUpperCase())
        .join();

    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        titleSpacing: 16,
        title: Row(
          children: [
            InkWell(
              onTap: () {
                if (ModalRoute.of(context)?.settings.name == AppRoutes.home) return;
                Navigator.pushNamedAndRemoveUntil(context, AppRoutes.home, (_) => false);
              },
              borderRadius: BorderRadius.circular(10),
              child: Container(
                width: 32,
                height: 32,
                decoration: BoxDecoration(color: AppColors.ink, borderRadius: BorderRadius.circular(10)),
                child: const Icon(Icons.home_outlined, size: 16, color: Colors.white),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('FixFlow AI', style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.ink)),
                  Text(title, style: const TextStyle(fontSize: 12, color: AppColors.muted, fontWeight: FontWeight.w500)),
                ],
              ),
            ),
          ],
        ),
        actions: [
          if (user != null)
            Padding(
              padding: const EdgeInsets.only(right: 4),
              child: CircleAvatar(
                radius: 14,
                backgroundColor: AppColors.ink,
                child: Text(initials.isEmpty ? 'U' : initials, style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w700)),
              ),
            ),
          ...?actions,
          if (user != null)
            TextButton(
              onPressed: () async {
                await ref.read(authProvider.notifier).logout();
                if (context.mounted) {
                  Navigator.pushNamedAndRemoveUntil(context, AppRoutes.login, (_) => false);
                }
              },
              child: const Text('Logout'),
            ),
        ],
      ),
      floatingActionButton: floatingActionButton,
      bottomNavigationBar: kind == null || tabIndex == null ? null : _Nav(kind: kind!, index: tabIndex!),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (description != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 4, 20, 0),
              child: Text(description!, style: const TextStyle(color: AppColors.muted, height: 1.5)),
            ),
          Expanded(child: body),
        ],
      ),
    );
  }
}

class _Nav extends StatelessWidget {
  const _Nav({required this.kind, required this.index});

  final WorkspaceKind kind;
  final int index;

  @override
  Widget build(BuildContext context) {
    final items = kind == WorkspaceKind.customer
        ? const [
            (Icons.dashboard_outlined, 'Dashboard', AppRoutes.customerHome),
            (Icons.assignment_outlined, 'Requests', AppRoutes.requestHistory),
            (Icons.work_outline, 'Bookings', AppRoutes.bookings),
            (Icons.notifications_none, 'Alerts', AppRoutes.notifications),
          ]
        : const [
            (Icons.dashboard_outlined, 'Dashboard', AppRoutes.technicianHome),
            (Icons.inbox_outlined, 'Invites', AppRoutes.technicianInvitations),
            (Icons.handyman_outlined, 'Jobs', AppRoutes.technicianJobs),
            (Icons.verified_outlined, 'Verify', AppRoutes.technicianVerification),
          ];

    return Container(
      decoration: const BoxDecoration(color: AppColors.navy),
      child: SafeArea(
        child: BottomNavigationBar(
          currentIndex: index.clamp(0, items.length - 1),
          onTap: (value) {
            final route = items[value].$3;
            if (value == index) return;
            if (value == 0) {
              Navigator.pushNamedAndRemoveUntil(context, route, (item) => false);
              return;
            }
            Navigator.pushNamed(context, route);
          },
          items: [
            for (final item in items)
              BottomNavigationBarItem(icon: Icon(item.$1, size: 22), label: item.$2),
          ],
        ),
      ),
    );
  }
}

class AuthShell extends StatelessWidget {
  const AuthShell({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 480),
            child: child,
          ),
        ),
      ),
    );
  }
}

class AuthCard extends StatelessWidget {
  const AuthCard({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.all(20),
      padding: const EdgeInsets.fromLTRB(24, 28, 24, 28),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(28),
        border: Border.all(color: AppColors.cardBorder),
      ),
      child: child,
    );
  }
}

class BrandMark extends StatelessWidget {
  const BrandMark({super.key, this.size = 44});

  final double size;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(color: AppColors.cream, borderRadius: BorderRadius.circular(16)),
      child: Icon(Icons.home_outlined, color: AppColors.ink, size: size * 0.42),
    );
  }
}

class WorkspaceBanner extends StatelessWidget {
  const WorkspaceBanner({super.key, required this.kicker, required this.title, required this.description});

  final String kicker;
  final String title;
  final String description;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(20, 24, 20, 24),
      decoration: BoxDecoration(
        color: AppColors.navy,
        borderRadius: BorderRadius.circular(28),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
            decoration: BoxDecoration(
              color: Colors.white10,
              borderRadius: BorderRadius.circular(999),
              border: Border.all(color: Colors.white12),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(width: 6, height: 6, decoration: const BoxDecoration(color: AppColors.gold, shape: BoxShape.circle)),
                const SizedBox(width: 8),
                Text(kicker.toUpperCase(), style: const TextStyle(color: AppColors.goldSoft, fontSize: 10, letterSpacing: 1.4, fontWeight: FontWeight.w600)),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Text(title, style: const TextStyle(color: Colors.white, fontSize: 28, fontWeight: FontWeight.w600, letterSpacing: -0.4)),
          const SizedBox(height: 8),
          Text(description, style: const TextStyle(color: Colors.white70, height: 1.5)),
        ],
      ),
    );
  }
}

class StatTile extends StatelessWidget {
  const StatTile({super.key, required this.label, required this.value, required this.hint, required this.icon, this.navy = false});

  final String label;
  final String value;
  final String hint;
  final IconData icon;
  final bool navy;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: AppColors.cardBorder),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(child: Text(label.toUpperCase(), style: const TextStyle(fontSize: 10, letterSpacing: 1.2, color: AppColors.faint, fontWeight: FontWeight.w600))),
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: navy ? AppColors.navy : AppColors.cream,
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Icon(icon, size: 18, color: navy ? AppColors.goldSoft : AppColors.gold),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Text(value, style: const TextStyle(fontSize: 26, fontWeight: FontWeight.w600, color: AppColors.ink)),
          const SizedBox(height: 4),
          Text(hint, style: const TextStyle(fontSize: 12, color: AppColors.muted, height: 1.4)),
        ],
      ),
    );
  }
}

class QuickActionTile extends StatelessWidget {
  const QuickActionTile({super.key, required this.icon, required this.title, required this.description, required this.onTap});

  final IconData icon;
  final String title;
  final String description;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.canvas,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(color: AppColors.navy, borderRadius: BorderRadius.circular(16)),
                child: Icon(icon, color: AppColors.goldSoft, size: 18),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: const TextStyle(fontWeight: FontWeight.w600, color: AppColors.ink)),
                    const SizedBox(height: 4),
                    Text(description, style: const TextStyle(fontSize: 13, color: AppColors.muted, height: 1.4)),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class PageIntro extends StatelessWidget {
  const PageIntro({super.key, required this.title, this.description});

  final String title;
  final String? description;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(4, 8, 4, 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: Theme.of(context).textTheme.headlineSmall),
          if (description != null) ...[
            const SizedBox(height: 6),
            Text(description!, style: const TextStyle(color: AppColors.muted, height: 1.5)),
          ],
        ],
      ),
    );
  }
}
