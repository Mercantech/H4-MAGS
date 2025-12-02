import 'question_model.dart';

/// Quiz Model
/// 
/// Repræsenterer en quiz med spørgsmål og svar.
class QuizModel {
  final int id;
  final String title;
  final String? description;
  final String pin;
  final String status;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final DateTime? startedAt;
  final DateTime? finishedAt;
  final int userId;
  final List<QuestionModel> questions;

  QuizModel({
    required this.id,
    required this.title,
    this.description,
    required this.pin,
    required this.status,
    required this.createdAt,
    this.updatedAt,
    this.startedAt,
    this.finishedAt,
    required this.userId,
    required this.questions,
  });

  factory QuizModel.fromJson(Map<String, dynamic> json) {
    return QuizModel(
      id: json['id'] as int,
      title: json['title'] as String,
      description: json['description'] as String?,
      pin: json['pin'] as String,
      status: json['status'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: json['updatedAt'] != null
          ? DateTime.parse(json['updatedAt'] as String)
          : null,
      startedAt: json['startedAt'] != null
          ? DateTime.parse(json['startedAt'] as String)
          : null,
      finishedAt: json['finishedAt'] != null
          ? DateTime.parse(json['finishedAt'] as String)
          : null,
      userId: json['userId'] as int,
      questions: (json['questions'] as List<dynamic>?)
              ?.map((e) => QuestionModel.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
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
      if (updatedAt != null) 'updatedAt': updatedAt!.toIso8601String(),
      if (startedAt != null) 'startedAt': startedAt!.toIso8601String(),
      if (finishedAt != null) 'finishedAt': finishedAt!.toIso8601String(),
      'userId': userId,
      'questions': questions.map((q) => q.toJson()).toList(),
    };
  }
}

