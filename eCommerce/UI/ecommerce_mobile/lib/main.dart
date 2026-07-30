import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/providers/booking_provider.dart';
import 'package:ecommerce_mobile/providers/genre_provider.dart';
import 'package:ecommerce_mobile/providers/movie_provider.dart';
import 'package:ecommerce_mobile/providers/news_provider.dart';
import 'package:ecommerce_mobile/providers/notification_provider.dart';
import 'package:ecommerce_mobile/providers/reservation_provider.dart';
import 'package:ecommerce_mobile/providers/review_provider.dart';
import 'package:ecommerce_mobile/providers/screening_provider.dart';
import 'package:ecommerce_mobile/providers/user_provider.dart';
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
        ChangeNotifierProvider(create: (_) => ScreeningProvider()),
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

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'CineVision',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.defaultTheme,
      scrollBehavior: const AppScrollBehavior(),
      onGenerateRoute: RouteGenerator.onGenerate,
      initialRoute: AppRoutes.entryPoint,
    );
  }
}
