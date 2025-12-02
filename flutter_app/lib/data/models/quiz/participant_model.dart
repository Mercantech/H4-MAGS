/// Participant Model
/// 
/// Repræsenterer en deltager i en quiz session.
class ParticipantModel {
  final int id;
  final String nickname;
  final int totalPoints;
  final DateTime joinedAt;
  final int quizSessionId;

  ParticipantModel({
    required this.id,
    required this.nickname,
    required this.totalPoints,
    required this.joinedAt,
    required this.quizSessionId,
  });

  factory ParticipantModel.fromJson(Map<String, dynamic> json) {
    return ParticipantModel(
      id: json['id'] as int,
      nickname: json['nickname'] as String,
      totalPoints: json['totalPoints'] as int,
      joinedAt: DateTime.parse(json['joinedAt'] as String),
      quizSessionId: json['quizSessionId'] as int,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'nickname': nickname,
      'totalPoints': totalPoints,
      'joinedAt': joinedAt.toIso8601String(),
      'quizSessionId': quizSessionId,
    };
  }
}

