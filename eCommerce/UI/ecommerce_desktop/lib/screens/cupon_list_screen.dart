import 'package:ecommerce_desktop/layouts/master_screen.dart';
import 'package:ecommerce_desktop/screens/cupon_add_screen.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../models/cupon.dart';
import '../models/search_result.dart';
import '../providers/cupon_provider.dart';
import '../utils/utils_widgets.dart';

class CuponList extends StatefulWidget {
  const CuponList({super.key});

  @override
  State<CuponList> createState() => _CuponListState();
}

class _CuponListState extends State<CuponList> {
  late CuponProvider _cuponProvider;

  SearchResult<Cupon>? result;
  bool isLoading = true;

  TextEditingController _codeController = TextEditingController();

  @override
  void initState() {
    // TODO: implement initState
    super.initState();

    _cuponProvider = context.read<CuponProvider>();

    initTable();
  }

  Future<void> initTable() async {
    try {
      var data = await _cuponProvider.get(
        filter: {'code': _codeController.text},
      );

      setState(() {
        result = data;
        isLoading = false;
      });
    } on Exception catch (e) {
      alertBox(context, 'Error', e.toString());
    }
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Cupon List",
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            _buildSearch(),
            isLoading ? CircularProgressIndicator() : _buildTable(),
          ],
        ),
      ),
    );
  }

  Padding _buildSearch() {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: Row(
        children: [
          Expanded(
            child: Padding(
              padding: const EdgeInsets.all(8.0),
              child: TextField(
                controller: _codeController,
                decoration: InputDecoration(label: Text("Code")),
              ),
            ),
          ),
          SizedBox(width: 10),
          ElevatedButton(
            onPressed: () async {
              initTable();
            },
            child: Text("Search"),
          ),
          SizedBox(width: 10),
          ElevatedButton(
            onPressed: () async {
              var refresh = await Navigator.of(context).push(
                MaterialPageRoute(builder: (context) => CuponAddScreen()),
              );

              if (refresh == 'reload') await initTable();
            },
            child: Text("New"),
          ),
          SizedBox(width: 10),
        ],
      ),
    );
  }

  Expanded _buildTable() {
    return Expanded(
      child: SizedBox(
        width: double.infinity,
        child: SingleChildScrollView(
          child: DataTable(
            columns: [
              DataColumn(label: Text("Code")),
              DataColumn(label: Text("Discount Amount")),
              DataColumn(label: Text("Discount Type")),
              DataColumn(label: Text("Uses")),
              DataColumn(label: Text("Expiers At")),
              DataColumn(label: Text("Is Active")),
            ],
            rows:
                result?.items
                    ?.map(
                      (e) => DataRow(
                        cells: [
                          DataCell(Text(e.code)),
                          DataCell(Text(e.discountAmount.toStringAsFixed(0))),
                          DataCell(Text(e.discountType.name)),
                          DataCell(Text(e.uses.toString())),
                          DataCell(
                            Text(
                              "${e.expiresAt.year}-${e.expiresAt.month}-${e.expiresAt.day}",
                            ),
                          ),
                          DataCell(
                            Switch(
                              value: e.isActive,
                              onChanged: (value) async {
                                showDialog(
                                  context: context,
                                  builder: (context) => AlertDialog(
                                    title: Text("Change activity"),
                                    content: Text(
                                      "Are you sure that you want to change activity of this cupon?",
                                    ),
                                    actions: [
                                      TextButton(
                                        onPressed: () {
                                          Navigator.pop(context);
                                        },
                                        child: Text("Cancel"),
                                      ),
                                      ElevatedButton(
                                        onPressed: () async {
                                          try {
                                            await _cuponProvider.toggleActivity(
                                              e.id,
                                            );

                                            ScaffoldMessenger.of(
                                              context,
                                            ).showSnackBar(
                                              SnackBar(
                                                content: Text(
                                                  "Activity of cupon changed successfully",
                                                ),
                                              ),
                                            );

                                            Navigator.of(context).pop();

                                            setState(() {
                                              initTable();
                                            });
                                          } on Exception catch (e) {
                                            alertBoxMoveBack(
                                              context,
                                              "Error",
                                              e.toString(),
                                            );
                                          }
                                        },
                                        child: Text("Yes"),
                                      ),
                                    ],
                                  ),
                                );
                              },
                            ),
                          ),
                        ],
                      ),
                    )
                    .toList() ??
                List.empty(),
          ),
        ),
      ),
    );
  }
}
