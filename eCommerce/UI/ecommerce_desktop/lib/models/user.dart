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
  final String? phoneNumber;
  final String? profileImageBase64;
  final DateTime? createdAt;
  final DateTime? lastLoginAt;
  final DateTime? updatedAt;

  User({
    this.id,
    this.firstName,
    this.lastName,
    this.email,
    this.username,
    this.role,
    this.isActive,
    this.phoneNumber,
    this.profileImageBase64,
    this.createdAt,
    this.lastLoginAt,
    this.updatedAt,
  });

  factory User.fromJson(Map<String, dynamic> json) => _$UserFromJson(json);

  Map<String, dynamic> toJson() => _$UserToJson(this);
}

const userRoles = ['Admin', 'Staff', 'Customer'];
