// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

User _$UserFromJson(Map<String, dynamic> json) => User(
  id: (json['id'] as num?)?.toInt(),
  firstName: json['firstName'] as String?,
  lastName: json['lastName'] as String?,
  email: json['email'] as String?,
  username: json['username'] as String?,
  role: json['role'] as String?,
  isActive: json['isActive'] as bool?,
  phoneNumber: json['phoneNumber'] as String?,
  profileImageBase64: json['profileImageBase64'] as String?,
  createdAt: const UtcDateTimeJsonConverter().fromJson(json['createdAt']),
  lastLoginAt: const UtcDateTimeJsonConverter().fromJson(json['lastLoginAt']),
  updatedAt: const UtcDateTimeJsonConverter().fromJson(json['updatedAt']),
);

Map<String, dynamic> _$UserToJson(User instance) => <String, dynamic>{
  'id': instance.id,
  'firstName': instance.firstName,
  'lastName': instance.lastName,
  'email': instance.email,
  'username': instance.username,
  'role': instance.role,
  'isActive': instance.isActive,
  'phoneNumber': instance.phoneNumber,
  'profileImageBase64': instance.profileImageBase64,
  'createdAt': const UtcDateTimeJsonConverter().toJson(instance.createdAt),
  'lastLoginAt': const UtcDateTimeJsonConverter().toJson(instance.lastLoginAt),
  'updatedAt': const UtcDateTimeJsonConverter().toJson(instance.updatedAt),
};
