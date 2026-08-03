import 'package:cinevision_mobile/models/genre.dart';
import 'package:cinevision_mobile/providers/base_provider.dart';

class GenreProvider extends BaseProvider<Genre> {
  GenreProvider() : super('Genres');

  @override
  Genre fromJson(data) => Genre.fromJson(data);
}
