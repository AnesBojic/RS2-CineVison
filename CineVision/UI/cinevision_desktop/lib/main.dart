import 'package:cinevision_desktop/core/theme/app_theme.dart';
import 'package:cinevision_desktop/providers/age_rating_provider.dart';
import 'package:cinevision_desktop/providers/analytics_provider.dart';
import 'package:cinevision_desktop/providers/auth_provider.dart';
import 'package:cinevision_desktop/providers/chatbot_provider.dart';
import 'package:cinevision_desktop/providers/genre_provider.dart';
import 'package:cinevision_desktop/providers/hall_provider.dart';
import 'package:cinevision_desktop/providers/hall_status_provider.dart';
import 'package:cinevision_desktop/providers/language_provider.dart';
import 'package:cinevision_desktop/providers/movie_provider.dart';
import 'package:cinevision_desktop/providers/news_provider.dart';
import 'package:cinevision_desktop/providers/notification_provider.dart';
import 'package:cinevision_desktop/providers/role_provider.dart';
import 'package:cinevision_desktop/providers/screen_type_provider.dart';
import 'package:cinevision_desktop/providers/projection_provider.dart';
import 'package:cinevision_desktop/providers/user_provider.dart';
import 'package:cinevision_desktop/screens/login_screen.dart';
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
        ChangeNotifierProvider(create: (_) => ProjectionProvider()),
        ChangeNotifierProvider(create: (_) => NewsProvider()),
        ChangeNotifierProvider(create: (_) => UserProvider()),
        ChangeNotifierProvider(create: (_) => RoleProvider()),
        ChangeNotifierProvider(create: (_) => NotificationProvider()),
        ChangeNotifierProvider(create: (_) => AnalyticsProvider()),
        ChangeNotifierProvider(create: (_) => ChatBotProvider()),
      ],
      child: const CineVisionApp(),
    ),
  );
}

class CineVisionApp extends StatefulWidget {
  const CineVisionApp({super.key});

  @override
  State<CineVisionApp> createState() => _CineVisionAppState();
}

class _CineVisionAppState extends State<CineVisionApp> {
  final _navigatorKey = GlobalKey<NavigatorState>();

  @override
  Widget build(BuildContext context) {
    return Consumer<AuthProvider>(
      builder: (context, auth, _) {
        if (auth.sessionExpired) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            if (!mounted) return;
            auth.acknowledgeSessionExpired();
            _navigatorKey.currentState?.pushAndRemoveUntil(
              MaterialPageRoute(builder: (_) => const LoginScreen()),
              (_) => false,
            );
          });
        }

        return MaterialApp(
          navigatorKey: _navigatorKey,
          title: 'CineVision',
          debugShowCheckedModeBanner: false,
          theme: AppTheme.darkTheme,
          home: const LoginScreen(),
        );
      },
    );
  }
}
