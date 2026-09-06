import 'package:cinevision_desktop/models/reservation.dart';
import 'package:cinevision_desktop/providers/auth_provider.dart';
import 'package:cinevision_desktop/providers/base_provider.dart';
import 'package:signalr_netcore/signalr_client.dart';

class ReservationProvider extends BaseProvider<Reservation> {
  ReservationProvider() : super('Reservations');

  HubConnection? _hubConnection;
  bool _connecting = false;
  bool _liveConnected = false;
  int _liveTick = 0;

  bool get isLiveConnected => _liveConnected;
  int get liveTick => _liveTick;

  @override
  Reservation fromJson(data) =>
      Reservation.fromJson(Map<String, dynamic>.from(data as Map));

  Future<void> connectRealtime() async {
    if (_connecting || _liveConnected) return;
    if (AuthProvider.accesstoken == null || AuthProvider.accesstoken!.isEmpty) {
      return;
    }

    _connecting = true;
    try {
      final hubUrl = '${Uri.parse(BaseProvider.baseUrl ?? 'http://localhost:5126/').origin}/hubs/bookings';
      _hubConnection = HubConnectionBuilder()
          .withUrl(
            hubUrl,
            options: HttpConnectionOptions(
              accessTokenFactory: () async => AuthProvider.accesstoken ?? '',
            ),
          )
          .withAutomaticReconnect()
          .build();

      _hubConnection!.on('BookingsUpdated', (_) {
        _liveTick++;
        notifyListeners();
      });
      _hubConnection!.onclose(({error}) {
        _liveConnected = false;
        notifyListeners();
      });
      _hubConnection!.onreconnected(({connectionId}) {
        _liveConnected = true;
        _liveTick++;
        notifyListeners();
      });

      await _hubConnection!.start();
      _liveConnected = true;
    } catch (_) {
      _liveConnected = false;
    } finally {
      _connecting = false;
      notifyListeners();
    }
  }

  Future<void> disconnectRealtime() async {
    final hub = _hubConnection;
    _hubConnection = null;
    _liveConnected = false;
    if (hub != null) {
      try {
        await hub.stop();
      } catch (_) {}
    }
    notifyListeners();
  }
}
