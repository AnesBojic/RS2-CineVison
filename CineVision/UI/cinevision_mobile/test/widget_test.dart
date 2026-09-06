import 'package:cinevision_mobile/main.dart';
import 'package:cinevision_mobile/providers/auth_provider.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

void main() {
  testWidgets('Auth landing screen renders', (WidgetTester tester) async {
    await tester.pumpWidget(
      ChangeNotifierProvider(
        create: (_) => AuthProvider(),
        child: const CineVisionApp(),
      ),
    );

    expect(find.text('CINEVISION'), findsOneWidget);
    expect(find.text('Welcome to CineVision'), findsOneWidget);
    expect(find.text('Sign In'), findsOneWidget);
  });
}
