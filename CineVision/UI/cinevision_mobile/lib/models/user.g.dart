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
  createdAt: const UtcDateTimeJsonConverter().fromJson(json['createdAt']),
  lastLoginAt: const UtcDateTimeJsonConverter().fromJson(json['lastLoginAt']),
  phoneNumber: json['phoneNumber'] as String?,
  updatedAt: const UtcDateTimeJsonConverter().fromJson(json['updatedAt']),
  profileImageBase64: json['profileImageBase64'] as String?,
);

Map<String, dynamic> _$UserToJson(User instance) => <String, dynamic>{
  'id': instance.id,
  'firstName': instance.firstName,
  'lastName': instance.lastName,
  'email': instance.email,
  'username': instance.username,
  'role': instance.role,
  'isActive': instance.isActive,
  'createdAt': const UtcDateTimeJsonConverter().toJson(instance.createdAt),
  'lastLoginAt': const UtcDateTimeJsonConverter().toJson(instance.lastLoginAt),
  'phoneNumber': instance.phoneNumber,
  'updatedAt': const UtcDateTimeJsonConverter().toJson(instance.updatedAt),
  'profileImageBase64': instance.profileImageBase64,
};
