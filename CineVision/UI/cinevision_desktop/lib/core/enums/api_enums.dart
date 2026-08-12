/// Client-side mirrors of `CineVision.Model.Enums` — keep values in sync with the API.
library;

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
