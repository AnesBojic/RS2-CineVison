/// Client-side mirrors of `eCommerce.Model.Enums` — keep values in sync with the API.
library;

class MovieState {
  static const active = 'Active';
  static const draft = 'Draft';

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

class SeatTypes {
  static const regular = 0;
  static const vip = 1;
  static const couple = 2;
}

class NotificationTypes {
  static const email = 'Email';
  static const message = 'Message';
  static const reservation = 'Reservation';
  static const payment = 'Payment';
  static const cancellation = 'Cancellation';
  static const status = 'Status';
}

class UserRoles {
  static const admin = 'Admin';
  static const staff = 'Staff';
  static const customer = 'Customer';
}
