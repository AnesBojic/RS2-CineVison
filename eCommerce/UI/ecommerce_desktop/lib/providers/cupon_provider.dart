import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

import '../models/cupon.dart';

class CuponProvider extends BaseProvider<Cupon> {
  CuponProvider() : super("Cupons");

  @override
  Cupon fromJson(data) {
    return Cupon.fromJson(data);
  }

  Future<void> toggleActivity(int id) async {
    var url = "${BaseProvider.baseUrl}$endpoint/$id/ToggleActivity";

    var uri = Uri.parse(url);

    var headers = createHeaders();

    http.Response response = await http.put(uri, headers: headers);

    validateResponse(response);
  }
}
