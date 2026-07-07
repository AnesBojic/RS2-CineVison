import 'package:ecommerce_desktop/models/discount_type_enum.dart';
import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../providers/cupon_provider.dart';
import '../utils/utils_widgets.dart';

class CuponAddScreen extends StatefulWidget {
  const CuponAddScreen({super.key});

  @override
  State<CuponAddScreen> createState() => _CuponAddScreenState();
}

class _CuponAddScreenState extends State<CuponAddScreen> {
  final _formKey = GlobalKey<FormBuilderState>();

  late CuponProvider _cuponProvider;

  @override
  void initState() {
    // TODO: implement initState
    super.initState();

    _cuponProvider = context.read<CuponProvider>();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text("Add Cupon"), centerTitle: true),
      body: SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(children: [_buildForm(), _saveButton(context)]),
        ),
      ),
    );
  }

  Padding _saveButton(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(32.0),
      child: ElevatedButton(
        onPressed: () async {
          _formKey.currentState?.save();

          try {
            if (_formKey.currentState!.validate()) {
              Map<String, dynamic> request = Map.of(
                _formKey.currentState!.value,
              );

              var discountAmount = double.parse(
                _formKey.currentState!.value['discountAmount'],
              );

              request['discountAmount'] = discountAmount;

              request['discountType'] = int.tryParse(request['discountType']);

              request['expiresAt'] = dateEncode(
                _formKey.currentState?.value['expiresAt'],
              );

              await _cuponProvider.insert(request);

              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(content: Text("Cupon successfully added")),
              );

              Navigator.pop(context, 'reload');
            }
          } on Exception catch (e) {
            alertBox(context, "Error", e.toString());
          }
        },
        child: Text("Save", style: const TextStyle(fontSize: 15)),
      ),
    );
  }

  FormBuilder _buildForm() {
    return FormBuilder(
      key: _formKey,
      child: Column(
        children: [
          Row(
            children: [
              Expanded(
                child: FormBuilderTextField(
                  name: 'code',
                  validator: (value) {
                    if (value == null) {
                      return mField;
                    } else {
                      return null;
                    }
                  },
                  decoration: InputDecoration(label: Text("Name")),
                ),
              ),
              const SizedBox(width: 20),
              Expanded(
                child: FormBuilderTextField(
                  name: 'discountAmount',
                  validator: (value) {
                    if (value == null) {
                      return mField;
                    } else if (double.tryParse(value) == null) {
                      return numericField;
                    } else {
                      return null;
                    }
                  },
                  decoration: InputDecoration(label: Text("Discount Amount")),
                ),
              ),
            ],
          ),
          SizedBox(height: 10),
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Expanded(
                child: FormBuilderDropdown<String>(
                  name: 'discountType',
                  validator: (value) {
                    if (value == null) {
                      return mField;
                    } else {
                      return null;
                    }
                  },
                  decoration: InputDecoration(labelText: "Dicount Type"),
                  items: DiscountType.values.map((DiscountType dicountType) {
                    return DropdownMenuItem<String>(
                      value: dicountType.index.toString(),
                      child: Text(dicountType.toString().split('.').last),
                    );
                  }).toList(),
                ),
              ),
              const SizedBox(width: 20),
              Expanded(
                child: FormBuilderDateTimePicker(
                  name: 'expiresAt',
                  firstDate: DateTime.now(),
                  validator: (value) {
                    if (value == null) {
                      return mField;
                    } else {
                      return null;
                    }
                  },
                  decoration: InputDecoration(label: Text("Expiration Date")),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
