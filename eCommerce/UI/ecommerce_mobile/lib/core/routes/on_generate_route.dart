import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

import '../../models/movie.dart';
import '../../models/reservation.dart';
import '../../views/auth/auth_landing_page.dart';
import '../../views/auth/forget_password_page.dart';
import '../../views/auth/login_page.dart';
import '../../views/auth/password_reset_page.dart';
import '../../views/auth/sign_up_page.dart';
import '../../views/booking/booking_confirmed_page.dart';
import '../../views/booking/booking_page.dart';
import '../../views/bookings/my_bookings_page.dart';
import '../../views/checkout/checkout_page.dart';
import '../../views/entrypoint/entrypoint_ui.dart';
import '../../views/news/news_page.dart';
import '../../views/profile/cine_profile_page.dart';
import '../../views/profile/notification_page.dart';
import '../../views/review/movie_review_page.dart';
import 'app_routes.dart';
import 'unknown_page.dart';

class RouteGenerator {
  static Route? onGenerate(RouteSettings settings) {
    final route = settings.name;

    switch (route) {
      case AppRoutes.entryPoint:
        return MaterialPageRoute(builder: (_) => const EntryPointUI());

      case AppRoutes.booking:
        final movie = settings.arguments as Movie;
        return MaterialPageRoute(
          builder: (_) => BookingPage(movie: movie),
        );

      case AppRoutes.checkout:
        return MaterialPageRoute(builder: (_) => const CheckoutPage());

      case AppRoutes.bookingConfirmed:
        final args = settings.arguments;
        if (args is Reservation) {
          return MaterialPageRoute(
            builder: (_) => BookingConfirmedPage(reservation: args),
          );
        }
        final map = args as Map<String, dynamic>;
        return MaterialPageRoute(
          builder: (_) => BookingConfirmedPage(
            reservation: map['reservation'] as Reservation,
            genreLine: map['genreLine'] as String?,
          ),
        );

      case AppRoutes.myBookings:
        return MaterialPageRoute(builder: (_) => const MyBookingsPage());

      case AppRoutes.news:
        return MaterialPageRoute(builder: (_) => const NewsPage());

      case AppRoutes.notifications:
        return CupertinoPageRoute(builder: (_) => const NotificationPage());

      case AppRoutes.myProfile:
        return MaterialPageRoute(builder: (_) => const CineProfilePage());

      case AppRoutes.authLanding:
        return MaterialPageRoute(builder: (_) => const AuthLandingPage());

      case AppRoutes.login:
        return MaterialPageRoute(builder: (_) => const LoginPage());

      case AppRoutes.signup:
        return MaterialPageRoute(builder: (_) => const SignUpPage());

      case AppRoutes.forgotPassword:
        return CupertinoPageRoute(builder: (_) => const ForgetPasswordPage());

      case AppRoutes.passwordReset:
        final account =
            settings.arguments is String ? settings.arguments as String : null;
        return CupertinoPageRoute(
          builder: (_) => PasswordResetPage(emailOrUsername: account),
        );

      case AppRoutes.submitReview:
        final args = settings.arguments as Map<String, dynamic>;
        return MaterialPageRoute(
          builder: (_) => MovieReviewPage(
            movieId: args['movieId'] as int,
            movieTitle: args['movieTitle'] as String? ?? '',
            reviewId: args['reviewId'] as int?,
            initialRating: args['initialRating'] as int?,
            initialComment: args['initialComment'] as String?,
          ),
        );

      default:
        return errorRoute();
    }
  }

  static Route? errorRoute() =>
      CupertinoPageRoute(builder: (_) => const UnknownPage());
}
