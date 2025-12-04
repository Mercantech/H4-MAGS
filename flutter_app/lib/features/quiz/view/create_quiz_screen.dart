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
/// Tillader oprettelse af en quiz med flere spørgsmål.
class CreateQuizScreen extends StatefulWidget {
  const CreateQuizScreen({super.key});

  @override
  State<CreateQuizScreen> createState() => _CreateQuizScreenState();
}

class _QuestionData {
  final TextEditingController questionController;
  final List<TextEditingController> answerControllers;
  int correctAnswerIndex;
  int timeLimitSeconds;
  int points;

  _QuestionData()
      : questionController = TextEditingController(),
        answerControllers = List.generate(4, (_) => TextEditingController()),
        correctAnswerIndex = 0,
        timeLimitSeconds = 30,
        points = 1000;

  void dispose() {
    questionController.dispose();
    for (var controller in answerControllers) {
      controller.dispose();
    }
  }
}

class _CreateQuizScreenState extends State<CreateQuizScreen> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();
  final List<_QuestionData> _questions = [_QuestionData()]; // Start med 1 spørgsmål

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    for (var question in _questions) {
      question.dispose();
    }
    super.dispose();
  }

  void _addQuestion() {
    setState(() {
      _questions.add(_QuestionData());
    });
  }

  void _removeQuestion(int index) {
    if (_questions.length > 1) {
      setState(() {
        _questions[index].dispose();
        _questions.removeAt(index);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    // Tjek om bruger er logget ind - hvis ikke, redirect tilbage
    return BlocBuilder<AuthBloc, AuthState>(
      builder: (context, authState) {
        if (authState is! AuthAuthenticated) {
          // Bruger er ikke logget ind - redirect tilbage
          WidgetsBinding.instance.addPostFrameCallback((_) {
            if (mounted) {
              Navigator.pop(context);
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Du skal være logget ind for at oprette en quiz'),
                  backgroundColor: Colors.red,
                ),
              );
            }
          });
          return Scaffold(
            appBar: AppBar(
              title: const Text('Opret Quiz'),
              backgroundColor: Theme.of(context).colorScheme.primary,
              foregroundColor: Colors.white,
            ),
            body: const Center(
              child: CircularProgressIndicator(),
            ),
          );
        }

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

                // Questions Section
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Spørgsmål',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.add_circle),
                      onPressed: _addQuestion,
                      tooltip: 'Tilføj spørgsmål',
                      color: Theme.of(context).colorScheme.primary,
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                // List of Questions
                ...List.generate(_questions.length, (questionIndex) {
                  final questionData = _questions[questionIndex];
                  return Card(
                    margin: const EdgeInsets.only(bottom: 24),
                    elevation: 2,
                    child: Padding(
                      padding: const EdgeInsets.all(16.0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          // Question Header
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                'Spørgsmål ${questionIndex + 1}',
                                style: Theme.of(context)
                                    .textTheme
                                    .titleMedium
                                    ?.copyWith(
                                      fontWeight: FontWeight.bold,
                                    ),
                              ),
                              if (_questions.length > 1)
                                IconButton(
                                  icon: const Icon(Icons.delete_outline),
                                  onPressed: () => _removeQuestion(questionIndex),
                                  tooltip: 'Fjern spørgsmål',
                                  color: Colors.red,
                                ),
                            ],
                          ),
                          const SizedBox(height: 16),

                          // Question Text
                          TextFormField(
                            controller: questionData.questionController,
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
                          const SizedBox(height: 16),

                          // Answers
                          Text(
                            'Svar (vælg det korrekte svar)',
                            style: Theme.of(context)
                                .textTheme
                                .titleSmall
                                ?.copyWith(
                                  fontWeight: FontWeight.bold,
                                ),
                          ),
                          const SizedBox(height: 12),
                          ...List.generate(4, (answerIndex) {
                            return Padding(
                              padding: const EdgeInsets.only(bottom: 12),
                              child: Row(
                                children: [
                                  Radio<int>(
                                    value: answerIndex,
                                    groupValue: questionData.correctAnswerIndex,
                                    onChanged: (value) {
                                      setState(() {
                                        questionData.correctAnswerIndex = value!;
                                      });
                                    },
                                  ),
                                  Expanded(
                                    child: TextFormField(
                                      controller:
                                          questionData.answerControllers[answerIndex],
                                      decoration: InputDecoration(
                                        labelText: 'Svar ${answerIndex + 1} *',
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
                        ],
                      ),
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
      },
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

    // Opret quiz model med alle spørgsmål
    final questions = _questions.asMap().entries.map((entry) {
      final questionIndex = entry.key;
      final questionData = entry.value;

      final answers = questionData.answerControllers
          .asMap()
          .entries
          .map((answerEntry) => CreateAnswerModel(
                text: answerEntry.value.text.trim(),
                isCorrect: answerEntry.key == questionData.correctAnswerIndex,
                orderIndex: answerEntry.key + 1,
              ))
          .toList();

      return CreateQuestionModel(
        text: questionData.questionController.text.trim(),
        timeLimitSeconds: questionData.timeLimitSeconds,
        points: questionData.points,
        orderIndex: questionIndex + 1,
        answers: answers,
      );
    }).toList();

    final quiz = CreateQuizModel(
      title: _titleController.text.trim(),
      description: _descriptionController.text.trim().isEmpty
          ? null
          : _descriptionController.text.trim(),
      userId: authState.user.id,
      questions: questions,
    );

    // Dispatch event
    context.read<QuizBloc>().add(CreateQuizEvent(quiz: quiz));
  }
}

