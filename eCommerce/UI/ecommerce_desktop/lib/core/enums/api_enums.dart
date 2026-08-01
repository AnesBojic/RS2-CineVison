/// Client-side mirrors of the API enums declared in `eCommerce.Model.Enums`.
/// Keep the values in sync with the backend.
library;

/// Mirrors `MovieLifecycleState`. The API serializes it by name.
class MovieState {
  static const active = 'Active';
  static const draft = 'Draft';

  /// The values the status filter and the edit dialog offer, in display order.
  static const all = [active, draft];

  static String displayLabel(String? movieState) {
    if (movieState == null || movieState.isEmpty) return draft;
    if (movieState.toLowerCase() == active.toLowerCase()) return active;
    if (movieState.toLowerCase() == draft.toLowerCase()) return draft;
    return movieState;
  }

  static bool isActive(String? movieState) =>
      movieState != null && movieState.toLowerCase() == active.toLowerCase();
}

/// Mirrors `ReservationStatus`. The API exposes it as its underlying int.
class ReservationStatus {
  static const pending = 0;
  static const confirmed = 1;
  static const paid = 2;
  static const cancelled = 3;
  static const completed = 4;
}

/// Mirrors `SeatType`. The API exposes it as its underlying int.
/// VIP is retired: the layout editor only offers Regular and Couple.
class SeatTypes {
  static const regular = 0;
  static const vip = 1;
  static const couple = 2;

  static const editableLabels = ['Regular', 'Couple'];
}

/// Mirrors `NotificationType`. The API serializes it by name.
class NotificationTypes {
  static const email = 'Email';
  static const message = 'Message';
  static const reservation = 'Reservation';
  static const payment = 'Payment';
  static const cancellation = 'Cancellation';
  static const status = 'Status';
}

/// Mirrors the backend `RoleNames` static class.
class UserRoles {
  static const admin = 'Admin';
  static const staff = 'Staff';
  static const customer = 'Customer';

  static const all = [admin, staff, customer];
}
