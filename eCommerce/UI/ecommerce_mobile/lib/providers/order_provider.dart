import 'dart:convert';

import 'package:ecommerce_mobile/models/order.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class OrderProvider extends BaseProvider<Order> {
  OrderProvider() : super('Orders');

  @override
  Order fromJson(data) => Order.fromJson(data as Map<String, dynamic>);

  Future<Order> checkout(
    List<Map<String, dynamic>> items, {
    String? paymentIntentId,
  }) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}Orders/Checkout');
    final headers = createHeaders();
    final body = jsonEncode({
      'items': items,
      'paymentIntentId': paymentIntentId,
    });
    final response = await http.post(uri, headers: headers, body: body);
    validateResponse(response);
    return Order.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<List<Order>> fetchMyOrders({int? status}) async {
    var filter = {'page': 1, 'pageSize': 100};

    if (status != null) {
      filter['status'] = status;
    }

    final result = await get(filter: filter);
    return result.items ?? [];
  }

  Future<Map<String, String>> createPaymentIntent(
    List<Map<String, dynamic>> items,
  ) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}Orders/CreatePaymentIntent');
    final headers = createHeaders();
    final body = jsonEncode({'items': items});
    final response = await http.post(uri, headers: headers, body: body);
    validateResponse(response);
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return {
      'clientSecret': data['clientSecret'] as String,
      'publishableKey': data['publishableKey'] as String,
    };
  }
}
