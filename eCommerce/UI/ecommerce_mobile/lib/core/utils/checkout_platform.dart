import 'package:flutter/foundation.dart';

/// Stripe Payment Sheet is only supported on iOS/Android.
/// Desktop and web use the demo checkout flow from the mockups.
bool get supportsStripePaymentSheet {
  if (kIsWeb) return false;
  return defaultTargetPlatform == TargetPlatform.iOS ||
      defaultTargetPlatform == TargetPlatform.android;
}
