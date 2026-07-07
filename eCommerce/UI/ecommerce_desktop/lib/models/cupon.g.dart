// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'cupon.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Cupon _$CuponFromJson(Map<String, dynamic> json) => Cupon(
  id: (json['id'] as num).toInt(),
  code: json['code'] as String,
  discountAmount: (json['discountAmount'] as num).toDouble(),
  discountType: $enumDecode(_$DiscountTypeEnumMap, json['discountType']),
  uses: (json['uses'] as num).toInt(),
  expiresAt: DateTime.parse(json['expiresAt'] as String),
  isActive: json['isActive'] as bool,
  createdAt: DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
);

Map<String, dynamic> _$CuponToJson(Cupon instance) => <String, dynamic>{
  'id': instance.id,
  'code': instance.code,
  'discountAmount': instance.discountAmount,
  'discountType': _$DiscountTypeEnumMap[instance.discountType]!,
  'uses': instance.uses,
  'expiresAt': instance.expiresAt.toIso8601String(),
  'isActive': instance.isActive,
  'createdAt': instance.createdAt.toIso8601String(),
  'updatedAt': instance.updatedAt?.toIso8601String(),
};

const _$DiscountTypeEnumMap = {
  DiscountType.percentage: 0,
  DiscountType.fixedAmount: 1,
};
