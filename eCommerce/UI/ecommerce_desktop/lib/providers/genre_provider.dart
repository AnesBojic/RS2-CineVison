import 'package:ecommerce_desktop/models/genre.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class GenreProvider extends BaseProvider<Genre> {
  GenreProvider() : super('Genres');

  @override
  Genre fromJson(data) => Genre.fromJson(data);
}
