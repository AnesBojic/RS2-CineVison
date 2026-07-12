import 'package:ecommerce_desktop/models/hall.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class HallProvider extends BaseProvider<Hall> {
  HallProvider() : super('Halls');

  @override
  Hall fromJson(data) => Hall.fromJson(data);
}
