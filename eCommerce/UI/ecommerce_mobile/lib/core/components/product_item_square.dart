import 'package:ecommerce_mobile/core/components/asset_image.dart';
import 'package:ecommerce_mobile/core/components/base64_image.dart';
import 'package:ecommerce_mobile/models/product.dart';
import 'package:flutter/material.dart';

import '../constants/constants.dart';
import '../routes/app_routes.dart';

class ProductItemSquare extends StatelessWidget {
  const ProductItemSquare({
    super.key,
    required this.product,
  });

  final Product product;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppDefaults.padding / 2),
      child: Material(
        borderRadius: AppDefaults.borderRadius,
        color: AppColors.scaffoldBackground,
        child: InkWell(
          borderRadius: AppDefaults.borderRadius,
          onTap: () => Navigator.pushNamed(context, AppRoutes.productDetails, arguments: product),
          child: Container(
            width: 176,
            height: 296,
            padding: const EdgeInsets.all(AppDefaults.padding),
            decoration: BoxDecoration(
              border: Border.all(width: 0.1, color: AppColors.placeholder),
              borderRadius: AppDefaults.borderRadius,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Padding(
                  padding: const EdgeInsets.all(AppDefaults.padding / 2),
                  child: AspectRatio(
                    aspectRatio: 1 / 1,
                    child: product.assets.isNotEmpty ? Base64ImageWithLoader(product.assets[0].base64Content ?? '') : 
                      AssetImageWithLoader('assets/images/product_placeholder.jpg')
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  product.name ?? '',
                  style: Theme.of(context)
                      .textTheme
                      .titleMedium
                      ?.copyWith(color: Colors.black),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const Spacer(),
                Text(
                  product.weight != null ?  product.weight!.toStringAsFixed(0) : "No weight" ,
                ),
                Row(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      '\$${product.price!.toStringAsFixed(0)}',
                      style: Theme.of(context)
                          .textTheme
                          .titleLarge
                          ?.copyWith(color: Colors.black),
                    ),
                    const SizedBox(
                      width: 4,
                    ),
                    Text(
                      '\$${product.price!.toStringAsFixed(0)}',
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            decoration: TextDecoration.lineThrough,
                          ),
                    ),
                  ],
                )
              ],
            ),
          ),
        ),
      ),
    );
  }
}
