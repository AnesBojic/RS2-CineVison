import 'package:ecommerce_mobile/models/order_item.dart';
import 'package:ecommerce_mobile/providers/product_provider.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../../core/components/asset_image.dart';
import '../../../../core/components/base64_image.dart';
import '../../../../models/product.dart';

class OrderDetailsProductTile extends StatefulWidget {
  final OrderItem orderItem;

  const OrderDetailsProductTile({super.key, required this.orderItem});

  @override
  State<OrderDetailsProductTile> createState() =>
      _OrderDetailsProductTileState();
}

class _OrderDetailsProductTileState extends State<OrderDetailsProductTile> {
  late ProductProvider _productProvider;
  bool isLoading = true;

  late Product product;

  @override
  void initState() {
    // TODO: implement initState
    super.initState();

    _productProvider = context.read<ProductProvider>();

    getProduct();

    
  }

  Future<void> getProduct() async{
      var productData = await _productProvider.getById(widget.orderItem.productId);

      setState(() {
        product = productData;
        isLoading = false;
      });
  }

  @override
  Widget build(BuildContext context) {
    return isLoading ? CircularProgressIndicator() : Row(
      children: [
        SizedBox(
          height: 80,
          child: AspectRatio(
            aspectRatio: 1 / 1,
            child: product.assets.isNotEmpty
                ? Base64ImageWithLoader(product.assets[0].base64Content ?? '')
                : AssetImageWithLoader('assets/images/product_placeholder.jpg'),
          ),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                product.name ?? '',
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                  // fontWeight: FontWeight.bold,
                  color: Colors.black,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                product.weight != null
                    ? product.weight!.toStringAsFixed(0)
                    : "No weight",
              ),
            ],
          ),
        ),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              '\$${product.price!.toStringAsFixed(0)}',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 8),
            Text('${widget.orderItem.quantity}x', style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      ],
    );
  }
}
