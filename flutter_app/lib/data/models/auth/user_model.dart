/// User model for authentication
class UserModel {
  final int id;
  final String username;
  final String email;
  final String role;
  final DateTime createdAt;
  final String? picture; // Google profile picture URL

  UserModel({
    required this.id,
    required this.username,
    required this.email,
    required this.role,
    required this.createdAt,
    this.picture,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id'] as int,
      username: json['username'] as String,
      email: json['email'] as String,
      role: json['role'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      picture: json['picture'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'username': username,
      'email': email,
      'role': role,
      'createdAt': createdAt.toIso8601String(),
      'picture': picture,
    };
  }
}

