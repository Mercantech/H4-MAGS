/// Quiz Summary Model
/// 
/// Repræsenterer en quiz summary (uden spørgsmål og svar).
class QuizSummaryModel {
  final int id;
  final String title;
  final String? description;
  final String pin;
  final String status;
  final DateTime createdAt;
  final int questionCount;

  QuizSummaryModel({
    required this.id,
    required this.title,
    this.description,
    required this.pin,
    required this.status,
    required this.createdAt,
    required this.questionCount,
  });

  factory QuizSummaryModel.fromJson(Map<String, dynamic> json) {
    return QuizSummaryModel(
      id: json['id'] as int,
      title: json['title'] as String,
      description: json['description'] as String?,
      pin: json['pin'] as String,
      status: json['status'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      questionCount: json['questionCount'] as int,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'title': title,
      if (description != null) 'description': description,
      'pin': pin,
      'status': status,
      'createdAt': createdAt.toIso8601String(),
      'questionCount': questionCount,
    };
  }
}

