import 'package:collection/collection.dart';
import 'package:ecommerce_mobile/models/cart.dart';
import 'package:flutter/material.dart';

import '../models/product.dart';

class CartProvider extends ChangeNotifier {
  Cart cart = Cart();

  addToCart(Product product, {int quantity = 1}) {
    print("adding to cart: ${product.name}");

    CartItem? cartItem = findCartItem(product);
    if (cartItem != null) {
      cartItem.quantity++;
    } else {
      cart.items.add(
        CartItem()
          ..product = product
          ..quantity = quantity,
      );
    }

    notifyListeners();
  }

  CartItem? findCartItem(Product product) {
    return cart.items.firstWhereOrNull((item) => item.product.id == product.id);
  }

  void incrementQuantity(Product product) {
    final cartItem = findCartItem(product);
    if (cartItem != null) {
      cartItem.quantity++;
      notifyListeners();
    }
  }

  void decrementQuantity(Product product) {
    final cartItem = findCartItem(product);
    if (cartItem != null && cartItem.quantity > 1) {
      cartItem.quantity--;
      notifyListeners();
    }
  }

  removeFromCart(Product product) {
    CartItem? cartItem = findCartItem(product);
    if (cartItem != null) {
      cart.items.remove(cartItem);
      notifyListeners();
    }
  }

  void clearCart() {
    cart.items.clear();
    notifyListeners();
  }
}
