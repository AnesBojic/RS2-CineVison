import 'package:cinevision_desktop/main.dart';
import 'package:cinevision_desktop/providers/auth_provider.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

void main() {
  testWidgets('Login screen renders', (WidgetTester tester) async {
    await tester.pumpWidget(
      ChangeNotifierProvider(
        create: (_) => AuthProvider(),
        child: const CineVisionApp(),
      ),
    );

    expect(find.text('CINEVISION'), findsOneWidget);
    expect(find.text('Sign In'), findsOneWidget);
  });
}
