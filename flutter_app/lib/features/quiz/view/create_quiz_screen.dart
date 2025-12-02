import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/quiz_bloc.dart';
import '../bloc/quiz_event.dart';
import '../bloc/quiz_state.dart';
import '../../auth/bloc/auth_bloc.dart';
import '../../auth/bloc/auth_state.dart';
import '../../../data/models/quiz/create_quiz_model.dart';
import 'quiz_host_screen.dart';

/// Create Quiz Screen
/// 
/// Simpel MVP til at oprette en quiz med 1 spørgsmål og 4 svar.
class CreateQuizScreen extends StatefulWidget {
  const CreateQuizScreen({super.key});

  @override
  State<CreateQuizScreen> createState() => _CreateQuizScreenState();
}

class _CreateQuizScreenState extends State<CreateQuizScreen> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _questionController = TextEditingController();
  final List<TextEditingController> _answerControllers = [
    TextEditingController(),
    TextEditingController(),
    TextEditingController(),
    TextEditingController(),
  ];
  int _correctAnswerIndex = 0;

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    _questionController.dispose();
    for (var controller in _answerControllers) {
      controller.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Opret Quiz'),
        backgroundColor: Theme.of(context).colorScheme.primary,
        foregroundColor: Colors.white,
      ),
      body: BlocListener<QuizBloc, QuizState>(
        listener: (context, state) {
          if (state is QuizCreated) {
            // Quiz oprettet - naviger til host screen
            // Host screen vil automatisk oprette session
            if (mounted) {
              Navigator.pushReplacement(
                context,
                MaterialPageRoute(
                  builder: (context) => QuizHostScreen(
                    quiz: state.quiz,
                  ),
                ),
              );
            }
          } else if (state is QuizError) {
            if (mounted) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(state.message),
                  backgroundColor: Colors.red,
                ),
              );
            }
          }
        },
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24.0),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Quiz Title
                TextFormField(
                  controller: _titleController,
                  decoration: InputDecoration(
                    labelText: 'Quiz Titel *',
                    hintText: 'F.eks. "Flutter Basics"',
                    prefixIcon: const Icon(Icons.title),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Indtast en titel';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // Quiz Description
                TextFormField(
                  controller: _descriptionController,
                  decoration: InputDecoration(
                    labelText: 'Beskrivelse (valgfri)',
                    hintText: 'Beskriv quizzen',
                    prefixIcon: const Icon(Icons.description),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  maxLines: 3,
                ),
                const SizedBox(height: 24),

                // Question
                Text(
                  'Spørgsmål',
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _questionController,
                  decoration: InputDecoration(
                    labelText: 'Spørgsmålstekst *',
                    hintText: 'F.eks. "Hvad er Flutter?"',
                    prefixIcon: const Icon(Icons.help_outline),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  maxLines: 2,
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Indtast et spørgsmål';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 24),

                // Answers
                Text(
                  'Svar (vælg det korrekte svar)',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
                const SizedBox(height: 16),
                ...List.generate(4, (index) {
                  return Padding(
                    padding: const EdgeInsets.only(bottom: 16),
                    child: Row(
                      children: [
                        Radio<int>(
                          value: index,
                          groupValue: _correctAnswerIndex,
                          onChanged: (value) {
                            setState(() {
                              _correctAnswerIndex = value!;
                            });
                          },
                        ),
                        Expanded(
                          child: TextFormField(
                            controller: _answerControllers[index],
                            decoration: InputDecoration(
                              labelText: 'Svar ${index + 1} *',
                              hintText: 'Indtast svar mulighed',
                              border: OutlineInputBorder(
                                borderRadius: BorderRadius.circular(12),
                              ),
                            ),
                            validator: (value) {
                              if (value == null || value.isEmpty) {
                                return 'Indtast et svar';
                              }
                              return null;
                            },
                          ),
                        ),
                      ],
                    ),
                  );
                }),
                const SizedBox(height: 32),

                // Create Button
                BlocBuilder<QuizBloc, QuizState>(
                  builder: (context, state) {
                    final isLoading = state is QuizLoading;
                    return ElevatedButton(
                      onPressed: isLoading ? null : _createQuiz,
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: isLoading
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Text(
                              'Opret Quiz',
                              style: TextStyle(fontSize: 16),
                            ),
                    );
                  },
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  void _createQuiz() {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    // Tjek om bruger er logget ind
    final authState = context.read<AuthBloc>().state;
    if (authState is! AuthAuthenticated) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Du skal være logget ind for at oprette en quiz'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    // Opret quiz model
    final answers = _answerControllers
        .asMap()
        .entries
        .map((entry) => CreateAnswerModel(
              text: entry.value.text.trim(),
              isCorrect: entry.key == _correctAnswerIndex,
              orderIndex: entry.key + 1,
            ))
        .toList();

    final question = CreateQuestionModel(
      text: _questionController.text.trim(),
      timeLimitSeconds: 30,
      points: 1000,
      orderIndex: 1,
      answers: answers,
    );

    final quiz = CreateQuizModel(
      title: _titleController.text.trim(),
      description: _descriptionController.text.trim().isEmpty
          ? null
          : _descriptionController.text.trim(),
      userId: authState.user.id,
      questions: [question],
    );

    // Dispatch event
    context.read<QuizBloc>().add(CreateQuizEvent(quiz: quiz));
  }
}

