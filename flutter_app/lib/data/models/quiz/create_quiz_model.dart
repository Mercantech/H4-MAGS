/// Models for quiz creation
class CreateAnswerModel {
  final String text;
  final bool isCorrect;
  final int orderIndex;

  CreateAnswerModel({
    required this.text,
    required this.isCorrect,
    required this.orderIndex,
  });

  Map<String, dynamic> toJson() {
    return {
      'text': text,
      'isCorrect': isCorrect,
      'orderIndex': orderIndex,
    };
  }
}

class CreateQuestionModel {
  final String text;
  final int timeLimitSeconds;
  final int points;
  final int orderIndex;
  final List<CreateAnswerModel> answers;

  CreateQuestionModel({
    required this.text,
    this.timeLimitSeconds = 30,
    this.points = 1000,
    required this.orderIndex,
    required this.answers,
  });

  Map<String, dynamic> toJson() {
    return {
      'text': text,
      'timeLimitSeconds': timeLimitSeconds,
      'points': points,
      'orderIndex': orderIndex,
      'answers': answers.map((a) => a.toJson()).toList(),
    };
  }
}

class CreateQuizModel {
  final String title;
  final String? description;
  final int userId;
  final List<CreateQuestionModel> questions;

  CreateQuizModel({
    required this.title,
    this.description,
    required this.userId,
    required this.questions,
  });

  Map<String, dynamic> toJson() {
    return {
      'title': title,
      if (description != null) 'description': description,
      'userId': userId,
      'questions': questions.map((q) => q.toJson()).toList(),
    };
  }
}

