/// Answer Model (for quiz questions)
class AnswerModel {
  final int id;
  final String text;
  final bool isCorrect;
  final int orderIndex;
  final DateTime createdAt;

  AnswerModel({
    required this.id,
    required this.text,
    required this.isCorrect,
    required this.orderIndex,
    required this.createdAt,
  });

  factory AnswerModel.fromJson(Map<String, dynamic> json) {
    return AnswerModel(
      id: json['id'] as int,
      text: json['text'] as String,
      isCorrect: json['isCorrect'] as bool,
      orderIndex: json['orderIndex'] as int,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'text': text,
      'isCorrect': isCorrect,
      'orderIndex': orderIndex,
      'createdAt': createdAt.toIso8601String(),
    };
  }
}

/// Question Model (for quiz questions)
class QuestionModel {
  final int id;
  final String text;
  final int timeLimitSeconds;
  final int points;
  final int orderIndex;
  final List<AnswerModel> answers;

  QuestionModel({
    required this.id,
    required this.text,
    required this.timeLimitSeconds,
    required this.points,
    required this.orderIndex,
    required this.answers,
  });

  factory QuestionModel.fromJson(Map<String, dynamic> json) {
    return QuestionModel(
      id: json['id'] as int,
      text: json['text'] as String,
      timeLimitSeconds: json['timeLimitSeconds'] as int,
      points: json['points'] as int,
      orderIndex: json['orderIndex'] as int,
      answers: (json['answers'] as List<dynamic>?)
              ?.map((e) => AnswerModel.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'text': text,
      'timeLimitSeconds': timeLimitSeconds,
      'points': points,
      'orderIndex': orderIndex,
      'answers': answers.map((a) => a.toJson()).toList(),
    };
  }
}

