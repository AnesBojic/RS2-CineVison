import 'package:ecommerce_desktop/models/screening.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class ScreeningProvider extends BaseProvider<Screening> {
  ScreeningProvider() : super('Screenings');

  @override
  Screening fromJson(data) => Screening.fromJson(data);
}
