class Genre {
  final int? id;
  final String? name;
  final String? description;
  final bool? isActive;

  Genre({this.id, this.name, this.description, this.isActive});

  factory Genre.fromJson(Map<String, dynamic> json) {
    return Genre(
      id: json['id'] as int?,
      name: json['name'] as String?,
      description: json['description'] as String?,
      isActive: json['isActive'] as bool?,
    );
  }
}
