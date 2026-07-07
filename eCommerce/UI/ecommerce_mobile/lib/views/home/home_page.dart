import 'package:flutter/material.dart';
import 'package:flutter_svg/svg.dart';
import 'package:provider/provider.dart';
import '../../core/components/product_item_square.dart';
import '../../core/constants/app_icons.dart';

import '../../core/constants/app_defaults.dart';
import '../../core/routes/app_routes.dart';
import '../../models/product.dart';
import '../../models/search_result.dart';
//import '../../providers/cart_provider.dart';
import '../../providers/product_provider.dart';
import '../../utils/utils_widgets.dart';
import 'components/ad_space.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  late ProductProvider _productProvider;
  //late CartProvider _cartProvider;

  SearchResult<Product>? productResult;

  bool isLoading = true;
  bool hasMore = true;
  int page = 1;

  final TextEditingController _searchController = TextEditingController();

  @override
  void initState() {
    // TODO: implement initState
    super.initState();

    _productProvider = context.read<ProductProvider>();
    //_cartProvider = context.read<CartProvider>();

    initData();

    _scrollController.addListener(() {
      if (_scrollController.position.pixels >=
              _scrollController.position.maxScrollExtent - 200 &&
          !isLoading &&
          hasMore) {
        addNextPageData();
      }
    });
  }

  Future<void> addNextPageData() async {
    try {
      page++;

      var data = await _productProvider.get(
        filter: {
          'page': page,
          'name': _searchController.text,
          'includeAssets': true,
        },
      );

      if (data.items!.isEmpty) {
        hasMore = false;
        return;
      }

      setState(() {
        productResult!.items!.addAll(data.items!);
        isLoading = false;
      });
    } on Exception catch (e) {
      alertBox(context, 'Error', e.toString());
    }
  }

  final ScrollController _scrollController = ScrollController();

  Future<void> initData() async {
    try {
      var data = await _productProvider.get(
        filter: {'name': _searchController.text, 'includeAssets': true},
      );

      //print(data.items?[0].assets[0].base64Content);
      setState(() {
        productResult = data;
        isLoading = false;
      });
    } on Exception catch (e) {
      alertBox(context, 'Error', e.toString());
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: CustomScrollView(
          controller: _scrollController,
          slivers: [
            SliverAppBar(
              leading: Padding(
                padding: const EdgeInsets.only(left: 8),
                child: ElevatedButton(
                  onPressed: () {
                    Navigator.pushNamed(context, AppRoutes.drawerPage);
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFFF2F6F3),
                    shape: const CircleBorder(),
                  ),
                  child: SvgPicture.asset(AppIcons.sidebarIcon),
                ),
              ),
              floating: true,
              title: SvgPicture.asset("assets/images/app_logo.svg", height: 32),
              actions: [
                Padding(
                  padding: const EdgeInsets.only(right: 8, top: 4, bottom: 4),
                  child: ElevatedButton(
                    onPressed: () {
                      Navigator.pushNamed(context, AppRoutes.search);
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFFF2F6F3),
                      shape: const CircleBorder(),
                    ),
                    child: SvgPicture.asset(AppIcons.search),
                  ),
                ),
              ],
            ),
            const SliverToBoxAdapter(child: AdSpace()),
            SliverPadding(
              padding: EdgeInsets.symmetric(vertical: AppDefaults.padding),
              sliver: SliverGrid.builder(
                itemCount: productResult?.items?.length ?? 0,
                gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  crossAxisSpacing: 5,
                  childAspectRatio: 0.78,
                ),
                itemBuilder: (_, int index) {
                  var product = productResult!.items![index];

                  return Padding(
                    padding: const EdgeInsets.only(right: AppDefaults.padding),
                    child: ProductItemSquare(product: product),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
