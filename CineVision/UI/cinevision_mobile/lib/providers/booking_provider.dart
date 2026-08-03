import 'package:cinevision_mobile/models/movie.dart';
import 'package:cinevision_mobile/models/screening.dart';
import 'package:cinevision_mobile/models/screening_seat.dart';
import 'package:flutter/material.dart';

/// Holds in-progress booking state across showtime, seat, and checkout screens.
class BookingProvider with ChangeNotifier {
  Movie? movie;
  Screening? screening;
  final Set<int> selectedSeatIds = {};
  List<ScreeningSeat> seats = [];

  void startBooking(Movie selectedMovie) {
    movie = selectedMovie;
    screening = null;
    selectedSeatIds.clear();
    seats = [];
    notifyListeners();
  }

  void selectScreening(Screening? value) {
    screening = value;
    selectedSeatIds.clear();
    seats = [];
    notifyListeners();
  }

  void setSeats(List<ScreeningSeat> value) {
    seats = value;
    notifyListeners();
  }

  void toggleSeat(ScreeningSeat seat) {
    if (seat.isTaken) return;
    if (selectedSeatIds.contains(seat.seatId)) {
      selectedSeatIds.remove(seat.seatId);
    } else {
      selectedSeatIds.add(seat.seatId);
    }
    notifyListeners();
  }

  void clearSelection() {
    selectedSeatIds.clear();
    notifyListeners();
  }

  void reset() {
    movie = null;
    screening = null;
    selectedSeatIds.clear();
    seats = [];
    notifyListeners();
  }

  num get totalPrice {
    return seats
        .where((s) => selectedSeatIds.contains(s.seatId))
        .fold<num>(0, (sum, s) => sum + s.price);
  }

  int get selectedSeatCount {
    return seats
        .where((s) => selectedSeatIds.contains(s.seatId))
        .fold<int>(0, (sum, s) => sum + s.spotsOccupied);
  }

  List<ScreeningSeat> get selectedSeats =>
      seats.where((s) => selectedSeatIds.contains(s.seatId)).toList();

  bool isPartnerSlot(ScreeningSeat seat) {
    if (seat.isCouple) return false;
    return seats.any(
      (s) => s.isCouple && s.partnerSeatId == seat.seatId,
    );
  }
}
