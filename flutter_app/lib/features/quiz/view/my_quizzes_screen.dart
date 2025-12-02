import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/quiz_bloc.dart';
import '../bloc/quiz_event.dart';
import '../bloc/quiz_state.dart';
import '../../../core/di/injection.dart';
import '../../../data/repositories/quiz_repository_impl.dart';
import 'quiz_host_screen.dart';

/// My Quizzes Screen
/// 
/// Viser alle tidligere oprettede quizzers og giver mulighed for at genbruge dem.
class MyQuizzesScreen extends StatefulWidget {
  const MyQuizzesScreen({super.key});

  @override
  State<MyQuizzesScreen> createState() => _MyQuizzesScreenState();
}

class _MyQuizzesScreenState extends State<MyQuizzesScreen> {
  @override
  void initState() {
    super.initState();
    // Hent quizzers når screen åbnes
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<QuizBloc>().add(const LoadAllQuizzesEvent());
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Mine Quizzers'),
        backgroundColor: Theme.of(context).colorScheme.primary,
        foregroundColor: Colors.white,
      ),
      body: BlocListener<QuizBloc, QuizState>(
        listener: (context, state) {
          if (state is SessionCreated) {
            // Session oprettet - naviger til host screen
            // Vi skal hente quiz info først
            _navigateToHostScreen(state.session.quizId);
          } else if (state is QuizError) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(state.message),
                backgroundColor: Colors.red,
              ),
            );
          }
        },
        child: BlocBuilder<QuizBloc, QuizState>(
          builder: (context, state) {
            if (state is QuizLoading) {
              return const Center(child: CircularProgressIndicator());
            } else if (state is AllQuizzesLoaded) {
              final quizzes = state.quizzes;
              
              if (quizzes.isEmpty) {
                return Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(
                        Icons.quiz_outlined,
                        size: 80,
                        color: Colors.grey[400],
                      ),
                      const SizedBox(height: 16),
                      Text(
                        'Ingen quizzers endnu',
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                              color: Colors.grey[600],
                            ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Opret din første quiz!',
                        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                              color: Colors.grey[500],
                            ),
                      ),
                    ],
                  ),
                );
              }

              return ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: quizzes.length,
                itemBuilder: (context, index) {
                  final quiz = quizzes[index];
                  return Card(
                    elevation: 2,
                    margin: const EdgeInsets.only(bottom: 12),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: ListTile(
                      contentPadding: const EdgeInsets.all(16),
                      leading: CircleAvatar(
                        backgroundColor: Theme.of(context).colorScheme.primaryContainer,
                        child: Icon(
                          Icons.quiz,
                          color: Theme.of(context).colorScheme.primary,
                        ),
                      ),
                      title: Text(
                        quiz.title,
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 16,
                        ),
                      ),
                      subtitle: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (quiz.description != null) ...[
                            const SizedBox(height: 4),
                            Text(
                              quiz.description!,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ],
                          const SizedBox(height: 8),
                          Row(
                            children: [
                              Icon(Icons.help_outline, size: 16, color: Colors.grey[600]),
                              const SizedBox(width: 4),
                              Text(
                                '${quiz.questionCount} spørgsmål',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: Colors.grey[600],
                                ),
                              ),
                              const SizedBox(width: 16),
                              Icon(Icons.calendar_today, size: 16, color: Colors.grey[600]),
                              const SizedBox(width: 4),
                              Text(
                                _formatDate(quiz.createdAt),
                                style: TextStyle(
                                  fontSize: 12,
                                  color: Colors.grey[600],
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                      trailing: IconButton(
                        icon: const Icon(Icons.play_arrow),
                        onPressed: () {
                          // Opret ny session med denne quiz
                          context.read<QuizBloc>().add(
                                CreateSessionEvent(quizId: quiz.id),
                              );
                        },
                        tooltip: 'Start session',
                      ),
                    ),
                  );
                },
              );
            } else if (state is QuizError) {
              return Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.error_outline, size: 64, color: Colors.red[300]),
                    const SizedBox(height: 16),
                    Text(
                      'Fejl ved indlæsning',
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                    const SizedBox(height: 8),
                    Text(
                      state.message,
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            color: Colors.grey[600],
                          ),
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 16),
                    ElevatedButton(
                      onPressed: () {
                        context.read<QuizBloc>().add(const LoadAllQuizzesEvent());
                      },
                      child: const Text('Prøv igen'),
                    ),
                  ],
                ),
              );
            }

            return const Center(child: CircularProgressIndicator());
          },
        ),
      ),
    );
  }

  Future<void> _navigateToHostScreen(int quizId) async {
    // Hent quiz info først
    final repository = getIt<QuizRepositoryImpl>();
    final quizResult = await repository.getQuizById(quizId);
    
    if (quizResult.isSuccess && mounted) {
      final quiz = quizResult.dataOrNull!;
      
      // Naviger til host screen - den opretter automatisk session
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (context) => QuizHostScreen(quiz: quiz),
        ),
      );
    }
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final difference = now.difference(date);

    if (difference.inDays == 0) {
      return 'I dag';
    } else if (difference.inDays == 1) {
      return 'I går';
    } else if (difference.inDays < 7) {
      return '${difference.inDays} dage siden';
    } else {
      return '${date.day}/${date.month}/${date.year}';
    }
  }
}

