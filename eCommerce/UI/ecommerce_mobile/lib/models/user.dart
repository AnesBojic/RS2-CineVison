import 'package:ecommerce_mobile/core/utils/utc_datetime.dart';
import 'package:json_annotation/json_annotation.dart';

part 'user.g.dart';

@JsonSerializable()
class User {
  final int? id;
  final String? firstName;
  final String? lastName;
  final String? email;
  final String? username;
  final String? role;
  final bool? isActive;
  @UtcDateTimeJsonConverter()
  final DateTime? createdAt;
  @UtcDateTimeJsonConverter()
  final DateTime? lastLoginAt;
  final String? phoneNumber;
  @UtcDateTimeJsonConverter()
  final DateTime? updatedAt;
  final String? profileImageBase64;

  User({
    this.id,
    this.firstName,
    this.lastName,
    this.email,
    this.username,
    this.role,
    this.isActive,
    this.createdAt,
    this.lastLoginAt,
    this.phoneNumber,
    this.updatedAt,
    this.profileImageBase64
  });

  factory User.fromJson(Map<String, dynamic> json) => _$UserFromJson(json);

  Map<String, dynamic> toJson() => _$UserToJson(this);
}
