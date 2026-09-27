import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class SplashScreen extends ConsumerWidget {
  const SplashScreen({super.key});

  String _home(UserProfile? user) {
    if (user == null) return AppRoutes.home;
    if (user.isTechnician) return AppRoutes.technicianHome;
    if (user.isCustomer) return AppRoutes.customerHome;
    if (user.isAdmin) return AppRoutes.adminHome;
    return AppRoutes.unauthorized;
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.listen(authProvider, (previous, next) {
      next.whenData((user) {
        Navigator.of(context).pushReplacementNamed(_home(user));
      });
    });

    final auth = ref.watch(authProvider);
    return Scaffold(
      backgroundColor: AppColors.canvas,
      body: Center(
        child: auth.when(
          data: (user) => _Ready(home: _home(user)),
          loading: () => const _Brand(),
          error: (error, _) => Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const _Brand(),
              const SizedBox(height: 16),
              Text(error.toString(), style: const TextStyle(color: AppColors.muted)),
              TextButton(
                onPressed: () => Navigator.pushReplacementNamed(context, AppRoutes.login),
                child: const Text('Continue to login'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Brand extends StatelessWidget {
  const _Brand();

  @override
  Widget build(BuildContext context) {
    return const Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        BrandMark(size: 56),
        SizedBox(height: 16),
        Text('FixFlow AI', style: TextStyle(fontSize: 28, fontWeight: FontWeight.w600, color: AppColors.ink)),
        SizedBox(height: 16),
        CircularProgressIndicator(),
      ],
    );
  }
}

class _Ready extends StatelessWidget {
  const _Ready({required this.home});

  final String home;

  @override
  Widget build(BuildContext context) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      Navigator.of(context).pushReplacementNamed(home);
    });
    return const _Brand();
  }
}
