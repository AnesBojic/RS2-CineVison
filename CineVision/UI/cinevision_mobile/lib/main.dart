import 'package:cinevision_mobile/providers/auth_provider.dart';
import 'package:cinevision_mobile/providers/booking_provider.dart';
import 'package:cinevision_mobile/providers/genre_provider.dart';
import 'package:cinevision_mobile/providers/movie_provider.dart';
import 'package:cinevision_mobile/providers/news_provider.dart';
import 'package:cinevision_mobile/providers/notification_provider.dart';
import 'package:cinevision_mobile/providers/reservation_provider.dart';
import 'package:cinevision_mobile/providers/review_provider.dart';
import 'package:cinevision_mobile/providers/projection_provider.dart';
import 'package:cinevision_mobile/providers/user_provider.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/routes/app_routes.dart';
import 'core/routes/on_generate_route.dart';
import 'core/themes/app_scroll_behavior.dart';
import 'core/themes/app_themes.dart';

void main() {
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()),
        ChangeNotifierProvider(create: (_) => MovieProvider()),
        ChangeNotifierProvider(create: (_) => GenreProvider()),
        ChangeNotifierProvider(create: (_) => ProjectionProvider()),
        ChangeNotifierProvider(create: (_) => ReservationProvider()),
        ChangeNotifierProvider(create: (_) => ReviewProvider()),
        ChangeNotifierProvider(create: (_) => BookingProvider()),
        ChangeNotifierProvider(create: (_) => UserProvider()),
        ChangeNotifierProvider(create: (_) => NotificationProvider()),
        ChangeNotifierProvider(create: (_) => NewsProvider()),
      ],
      child: const MyApp(),
    ),
  );
}

class MyApp extends StatefulWidget {
  const MyApp({super.key});

  @override
  State<MyApp> createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> {
  final _navigatorKey = GlobalKey<NavigatorState>();

  @override
  Widget build(BuildContext context) {
    return Consumer<AuthProvider>(
      builder: (context, auth, _) {
        if (auth.sessionExpired) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            if (!mounted) return;
            auth.acknowledgeSessionExpired();
            _navigatorKey.currentState?.pushNamedAndRemoveUntil(
              AppRoutes.authLanding,
              (_) => false,
            );
          });
        }

        return MaterialApp(
          navigatorKey: _navigatorKey,
          title: 'CineVision',
          debugShowCheckedModeBanner: false,
          theme: AppTheme.defaultTheme,
          scrollBehavior: const AppScrollBehavior(),
          onGenerateRoute: RouteGenerator.onGenerate,
          initialRoute: AppRoutes.entryPoint,
        );
      },
    );
  }
}
