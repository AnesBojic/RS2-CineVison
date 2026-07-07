import 'package:ecommerce_desktop/models/discount_type_enum.dart';
import 'package:json_annotation/json_annotation.dart';

part 'cupon.g.dart';

@JsonSerializable()
class Cupon {
  final int id;
  final String code;
  final double discountAmount;
  final DiscountType discountType;
  final int uses;
  final DateTime expiresAt;
  final bool isActive;
  final DateTime createdAt;
  final DateTime? updatedAt;

  Cupon({
    required this.id,
    required this.code,
    required this.discountAmount,
    required this.discountType,
    required this.uses,
    required this.expiresAt,
    required this.isActive,
    required this.createdAt,
    this.updatedAt,
  });

  factory Cupon.fromJson(Map<String, dynamic> json) => _$CuponFromJson(json);

  Map<String, dynamic> toJson() => _$CuponToJson(this);
}
