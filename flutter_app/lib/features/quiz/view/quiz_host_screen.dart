import 'package:flutter/material.dart';
import '../../../data/models/quiz/quiz_model.dart';
import '../../../data/models/quiz/session_model.dart';
import 'enhanced_quiz_host_screen.dart';

/// Quiz Host Screen
/// 
/// Skærm for quiz host (lærer) til at:
/// - Starte en session
/// - Se deltagere
/// - Starte quizzen
/// 
/// Bruger EnhancedQuizHostScreen for forbedret funktionalitet med:
/// - Status visning
/// - Nuværende spørgsmål
/// - Tid tilbage
/// - Løbende leaderboard
class QuizHostScreen extends StatelessWidget {
  final QuizModel quiz;
  final SessionModel? initialSession;

  const QuizHostScreen({
    super.key,
    required this.quiz,
    this.initialSession,
  });

  @override
  Widget build(BuildContext context) {
    // Brug enhanced host screen
    return EnhancedQuizHostScreen(
      quiz: quiz,
      initialSession: initialSession,
    );
  }
}
