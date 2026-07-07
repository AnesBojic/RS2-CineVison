import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../../core/enums/dummy_order_status.dart';
import '../../../../core/routes/app_routes.dart';
import '../../../../models/order.dart';
import '../../../../providers/order_provider.dart';
import '../../../../utils/utils_widgets.dart';
import 'order_preview_tile.dart';

class CompletedTab extends StatefulWidget {
  const CompletedTab({super.key});

  @override
  State<CompletedTab> createState() => _CompletedTabState();
}

class _CompletedTabState extends State<CompletedTab> {
  List<Order>? _orders;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final list = await context.read<OrderProvider>().fetchMyOrders(status: 3);
      setState(() {
        _orders = list;
        _loading = false;
      });
    } on Exception catch (e) {
      setState(() => _loading = false);
      if (mounted) {
        alertBox(context, 'Error', e.toString());
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return _loading ? CircularProgressIndicator() : ListView.builder(
      padding: const EdgeInsets.only(top: 8),
      itemCount: _orders?.length ?? 0,
      itemBuilder: (context, index) {
        var order = _orders![index];
        return OrderPreviewTile(
          orderID: order.orderNumber,
          date: '${order.orderDate.day} ${getShortMonthName(order.orderDate)}',
          status: OrderStatus.delivery,
          onTap: () => Navigator.pushNamed(context, AppRoutes.orderDetails, arguments: order),
        );
      },
    );
  }
}
