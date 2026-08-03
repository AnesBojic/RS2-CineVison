import 'package:cinevision_desktop/models/genre.dart';
import 'package:cinevision_desktop/providers/base_provider.dart';

class GenreProvider extends BaseProvider<Genre> {
  GenreProvider() : super('Genres');

  @override
  Genre fromJson(data) => Genre.fromJson(data);
}
