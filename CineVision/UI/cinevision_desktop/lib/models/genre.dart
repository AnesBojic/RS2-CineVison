class Genre {
  final int? id;
  final String? name;
  final String? description;

  Genre({this.id, this.name, this.description});

  factory Genre.fromJson(Map<String, dynamic> json) {
    return Genre(
      id: json['id'] as int?,
      name: json['name'] as String?,
      description: json['description'] as String?,
    );
  }

  Map<String, dynamic> toInsertJson() => {
        'name': name,
        'description': description ?? '',
      };

  Map<String, dynamic> toUpdateJson() => toInsertJson();
}
