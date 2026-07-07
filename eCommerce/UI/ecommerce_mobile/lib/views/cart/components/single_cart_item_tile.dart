import 'package:ecommerce_mobile/core/components/asset_image.dart';
import 'package:ecommerce_mobile/core/components/base64_image.dart';
import 'package:ecommerce_mobile/models/cart.dart';
import 'package:ecommerce_mobile/providers/cart_provider.dart';
import 'package:flutter/material.dart';
import 'package:flutter_svg/svg.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/constants.dart';

class SingleCartItemTile extends StatefulWidget {
  const SingleCartItemTile({super.key, required this.cartItem});

  final CartItem cartItem;

  @override
  State<SingleCartItemTile> createState() => _SingleCartItemTileState();
}

class _SingleCartItemTileState extends State<SingleCartItemTile> {
  late CartProvider _cartProvider;

  @override
  void initState() {
    // TODO: implement initState
    super.initState();

    _cartProvider = context.read<CartProvider>();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppDefaults.padding,
        vertical: AppDefaults.padding / 2,
      ),
      child: Column(
        children: [
          Row(
            children: [
              /// Thumbnail
              SizedBox(
                width: 70,
                child: AspectRatio(
                  aspectRatio: 1 / 1,
                  child: widget.cartItem.product.assets.isNotEmpty
                      ? Base64ImageWithLoader(
                          widget.cartItem.product.assets[0].base64Content!,
                          fit: BoxFit.contain,
                        )
                      : AssetImageWithLoader(
                          'assets/images/product_placeholder.jpg',
                          fit: BoxFit.contain,
                        ),
                ),
              ),
              const SizedBox(width: 16),

              /// Quantity and Name
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Padding(
                    padding: const EdgeInsets.only(left: 8),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          widget.cartItem.product.name ?? '',
                          style: Theme.of(
                            context,
                          ).textTheme.bodyLarge?.copyWith(color: Colors.black),
                        ),
                        Text(
                          widget.cartItem.product.weight == null
                              ? 'No wight speciifed'
                              : 'Weight: ${(widget.cartItem.product.weight! * widget.cartItem.quantity).toStringAsFixed(0)}',
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      ],
                    ),
                  ),
                  Row(
                    children: [
                      IconButton(
                        onPressed: () {
                          setState(() {
                            _cartProvider.incrementQuantity(widget.cartItem.product);
                          });
                        },
                        icon: SvgPicture.asset(AppIcons.addQuantity),
                        constraints: const BoxConstraints(),
                      ),
                      Padding(
                        padding: const EdgeInsets.all(8.0),
                        child: Text(
                          widget.cartItem.quantity.toString(),
                          style: Theme.of(context).textTheme.bodyLarge
                              ?.copyWith(
                                fontWeight: FontWeight.bold,
                                color: Colors.black,
                              ),
                        ),
                      ),
                      IconButton(
                        onPressed: () {
                          setState(() {
                            _cartProvider.decrementQuantity(widget.cartItem.product);
                          });
                        },
                        icon: SvgPicture.asset(AppIcons.removeQuantity),
                        constraints: const BoxConstraints(),
                      ),
                    ],
                  ),
                ],
              ),
              const Spacer(),

              /// Price and Delete labelLarge
              Column(
                children: [
                  IconButton(
                    constraints: const BoxConstraints(),
                    onPressed: () {
                      _cartProvider.removeFromCart(widget.cartItem.product);
                      setState(() {});
                    },
                    icon: SvgPicture.asset(AppIcons.delete),
                  ),
                  const SizedBox(height: 16),
                  Text('\$${(widget.cartItem.product.price! * widget.cartItem.quantity).toStringAsFixed(2)}'),
                ],
              ),
            ],
          ),
          const Divider(thickness: 0.1),
        ],
      ),
    );
  }
}
