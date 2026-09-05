/// Client-side mirrors of `CineVision.Model.Enums` — keep values in sync with the API.
library;

class ReservationStatus {
  static const pending = 0;
  static const confirmed = 1;
  static const paid = 2;
  static const cancelled = 3;
  static const completed = 4;
}

class RefundStatus {
  static const none = 0;
  static const pending = 1;
  static const refunded = 2;
  static const failed = 3;
}

class SeatTypes {
  static const regular = 0;
  static const vip = 1;
  static const couple = 2;

  static int spotsOccupied(int? seatType) => seatType == couple ? 2 : 1;
}

class NotificationTypes {
  static const email = 'Email';
  static const message = 'Message';
  static const reservation = 'Reservation';
  static const payment = 'Payment';
  static const cancellation = 'Cancellation';
  static const status = 'Status';
}
