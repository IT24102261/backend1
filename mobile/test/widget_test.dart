import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/main.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';

void main() {
  testWidgets('renders FixFlow splash branding', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authProvider.overrideWith(() => _SignedOutAuth()),
        ],
        child: const FixFlowApp(),
      ),
    );
    await tester.pump();

    expect(find.text('FixFlow AI'), findsOneWidget);
  });
}

class _SignedOutAuth extends AuthNotifier {
  @override
  Future<UserProfile?> build() async => null;
}
