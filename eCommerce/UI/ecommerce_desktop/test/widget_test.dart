import 'package:ecommerce_desktop/main.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('Login screen renders', (WidgetTester tester) async {
    await tester.pumpWidget(const CineVisionApp());

    expect(find.text('CINEVISION'), findsOneWidget);
    expect(find.text('Sign In'), findsOneWidget);
  });
}
