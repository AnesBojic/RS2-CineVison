import 'package:cinevision_mobile/models/movie.dart';
import 'package:cinevision_mobile/models/projection.dart';
import 'package:cinevision_mobile/models/projection_seat.dart';
import 'package:flutter/material.dart';

/// Holds in-progress booking state across showtime, seat, and checkout screens.
class BookingProvider with ChangeNotifier {
  Movie? movie;
  Projection? projection;
  final Set<int> selectedSeatIds = {};
  List<ProjectionSeat> seats = [];

  void startBooking(Movie selectedMovie) {
    movie = selectedMovie;
    projection = null;
    selectedSeatIds.clear();
    seats = [];
    notifyListeners();
  }

  void selectProjection(Projection? value) {
    projection = value;
    selectedSeatIds.clear();
    seats = [];
    notifyListeners();
  }

  void setSeats(List<ProjectionSeat> value) {
    seats = value;
    notifyListeners();
  }

  void toggleSeat(ProjectionSeat seat) {
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
    projection = null;
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

  List<ProjectionSeat> get selectedSeats =>
      seats.where((s) => selectedSeatIds.contains(s.seatId)).toList();

  bool isPartnerSlot(ProjectionSeat seat) {
    if (seat.isCouple) return false;
    return seats.any(
      (s) => s.isCouple && s.partnerSeatId == seat.seatId,
    );
  }
}
