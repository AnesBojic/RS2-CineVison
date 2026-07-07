import 'package:ecommerce_mobile/core/components/dotted_divider.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/views/cart/components/item_row.dart';
import 'package:ecommerce_mobile/views/cart/empty_cart_page.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_stripe/flutter_stripe.dart';

import '../../core/constants/app_defaults.dart';
import '../../core/routes/app_routes.dart';
import '../../providers/auth_provider.dart';
import '../../providers/cart_provider.dart';
import '../../providers/order_provider.dart';
import '../../utils/api_client_exception.dart';
import 'components/coupon_code_field.dart';
import 'components/single_cart_item_tile.dart';

class CartPage extends StatefulWidget {
  final VoidCallback? onGoToHome;

  const CartPage({super.key, this.onGoToHome});

  @override
  State<CartPage> createState() => _CartPageState();
}

class _CartPageState extends State<CartPage> {
  late CartProvider _cartProvider;
  bool _checkoutBusy = false;

  @override
  void initState() {
    super.initState();
    _cartProvider = context.read<CartProvider>();
  }

  double _subtotal(CartProvider cart) {
    return cart.cart.items.fold<double>(
      0,
      (sum, item) => sum + (item.product.price ?? 0) * item.quantity,
    );
  }

   int _totalItems(CartProvider cart) {
    return cart.cart.items.fold<int>(
      0,
      (sum, item) => sum + item.quantity,
    );
  }

  Future<void> _checkout() async {
    if (AuthProvider.accesstoken == null || AuthProvider.accesstoken!.isEmpty) {
      alertBox(context, 'Login required', 'Please log in to place an order.');
      return;
    }

    if (_cartProvider.cart.items.isEmpty) {
      return;
    }

    final invalid = _cartProvider.cart.items
        .where((e) => e.product.id == null)
        .toList();
    if (invalid.isNotEmpty) {
      alertBox(context, 'Cart error', 'Some items are missing a product id.');
      return;
    }

    setState(() => _checkoutBusy = true);
    try {
      final payload = _cartProvider.cart.items
          .map(
            (e) => <String, dynamic>{
              'productId': e.product.id,
              'quantity': e.quantity,
            },
          )
          .toList();

      final orderProvider = context.read<OrderProvider>();
      final intentData = await orderProvider.createPaymentIntent(payload);

      Stripe.publishableKey = intentData['publishableKey']!;

      await Stripe.instance.initPaymentSheet(
        paymentSheetParameters: SetupPaymentSheetParameters(
          paymentIntentClientSecret: intentData['clientSecret']!,
          merchantDisplayName: 'eCommerce Shop',
        ),
      );

      await Stripe.instance.presentPaymentSheet();

      final paymentIntentId = intentData['clientSecret']!
          .split('_secret_')
          .first;
      await orderProvider.checkout(
        payload,
        paymentIntentId: paymentIntentId,
      );

      _cartProvider.clearCart();

      if (!mounted) return;
      await Navigator.pushNamed(context, AppRoutes.orderSuccessfull);
    } on StripeException catch (e) {
      if (mounted) {
        print(e);
        final msg =
            e.error.localizedMessage ?? e.error.message ?? 'Payment cancelled.';
        alertBox(context, 'Payment', msg);
      }
    } on ApiClientException catch (e) {
      if (mounted) {
        alertBox(context, 'Order could not be placed', e.message);
      }
    } on Exception catch (e) {
      if (mounted) {
        alertBox(context, 'Checkout', e.toString());
      }
    } finally {
      if (mounted) {
        setState(() => _checkoutBusy = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final cartProvider = context.watch<CartProvider>();
    final cartItems = cartProvider.cart.items;

    return Scaffold(
      body: SafeArea(
        child: _cartProvider.cart.items.isEmpty
            ? EmptyCartPage()
            : SingleChildScrollView(
                child: Column(
                  children: [
                    for (final item in cartItems)
                      SingleCartItemTile(cartItem: item),
                    const CouponCodeField(),
                    _itemTotalsAndPrice(cartProvider),
                    SizedBox(
                      width: double.infinity,
                      child: Padding(
                        padding: const EdgeInsets.all(AppDefaults.padding),
                        child: ElevatedButton(
                          onPressed: _checkout,
                          child: const Text('Checkout'),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
      ),
    );
  }

  Padding _itemTotalsAndPrice(CartProvider cartProvider) {
    return Padding(
      padding: EdgeInsets.all(AppDefaults.padding),
      child: Column(
        children: [
          ItemRow(title: 'Total Item', value: '${_totalItems(cartProvider)}'),
          ItemRow(title: 'Price', value: '\$ ${_subtotal(cartProvider).toStringAsFixed(2)}'),
          ItemRow(title: 'Discount', value: '\$ 0.00'),
          DottedDivider(),
          ItemRow(title: 'Total Price', value: '\$ ${_subtotal(cartProvider).toStringAsFixed(2)}'),
        ],
      ),
    );
  }
}
