import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/providers/age_rating_provider.dart';
import 'package:ecommerce_desktop/providers/analytics_provider.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/chatbot_provider.dart';
import 'package:ecommerce_desktop/providers/genre_provider.dart';
import 'package:ecommerce_desktop/providers/hall_provider.dart';
import 'package:ecommerce_desktop/providers/hall_status_provider.dart';
import 'package:ecommerce_desktop/providers/language_provider.dart';
import 'package:ecommerce_desktop/providers/movie_provider.dart';
import 'package:ecommerce_desktop/providers/news_provider.dart';
import 'package:ecommerce_desktop/providers/notification_provider.dart';
import 'package:ecommerce_desktop/providers/screen_type_provider.dart';
import 'package:ecommerce_desktop/providers/screening_provider.dart';
import 'package:ecommerce_desktop/providers/user_provider.dart';
import 'package:ecommerce_desktop/screens/login_screen.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

void main() {
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()),
        ChangeNotifierProvider(create: (_) => MovieProvider()),
        ChangeNotifierProvider(create: (_) => GenreProvider()),
        ChangeNotifierProvider(create: (_) => ScreenTypeProvider()),
        ChangeNotifierProvider(create: (_) => HallStatusProvider()),
        ChangeNotifierProvider(create: (_) => AgeRatingProvider()),
        ChangeNotifierProvider(create: (_) => LanguageProvider()),
        ChangeNotifierProvider(create: (_) => HallProvider()),
        ChangeNotifierProvider(create: (_) => ScreeningProvider()),
        ChangeNotifierProvider(create: (_) => NewsProvider()),
        ChangeNotifierProvider(create: (_) => UserProvider()),
        ChangeNotifierProvider(create: (_) => NotificationProvider()),
        ChangeNotifierProvider(create: (_) => AnalyticsProvider()),
        ChangeNotifierProvider(create: (_) => ChatBotProvider()),
      ],
      child: const CineVisionApp(),
    ),
  );
}

class CineVisionApp extends StatelessWidget {
  const CineVisionApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'CineVision',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.darkTheme,
      home: const LoginScreen(),
    );
  }
}
