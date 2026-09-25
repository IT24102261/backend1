import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class UnauthorizedScreen extends ConsumerWidget {
  const UnauthorizedScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return FixFlowScaffold(
      title: 'Unauthorized',
      body: UnauthorizedView(
        onLogin: () async {
          await ref.read(authProvider.notifier).logout();
          if (context.mounted) {
            Navigator.pushNamedAndRemoveUntil(context, AppRoutes.login, (_) => false);
          }
        },
      ),
    );
  }
}
