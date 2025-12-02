/// Session Model
/// 
/// Repræsenterer en quiz session med PIN kode.
class SessionModel {
  final int id;
  final String sessionPin;
  final String status;
  final DateTime createdAt;
  final DateTime? startedAt;
  final DateTime? completedAt;
  final int quizId;
  final String quizTitle;
  final int participantCount;

  SessionModel({
    required this.id,
    required this.sessionPin,
    required this.status,
    required this.createdAt,
    this.startedAt,
    this.completedAt,
    required this.quizId,
    required this.quizTitle,
    required this.participantCount,
  });

  factory SessionModel.fromJson(Map<String, dynamic> json) {
    return SessionModel(
      id: json['id'] as int,
      sessionPin: json['sessionPin'] as String,
      status: json['status'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      startedAt: json['startedAt'] != null
          ? DateTime.parse(json['startedAt'] as String)
          : null,
      completedAt: json['completedAt'] != null
          ? DateTime.parse(json['completedAt'] as String)
          : null,
      quizId: json['quizId'] as int,
      quizTitle: json['quizTitle'] as String,
      participantCount: json['participantCount'] as int,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'sessionPin': sessionPin,
      'status': status,
      'createdAt': createdAt.toIso8601String(),
      'startedAt': startedAt?.toIso8601String(),
      'completedAt': completedAt?.toIso8601String(),
      'quizId': quizId,
      'quizTitle': quizTitle,
      'participantCount': participantCount,
    };
  }
}

