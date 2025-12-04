import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/quiz_bloc.dart';
import '../bloc/quiz_event.dart';
import '../bloc/quiz_state.dart';
import '../../auth/bloc/auth_bloc.dart';
import '../../auth/bloc/auth_event.dart';
import '../../auth/bloc/auth_state.dart';
import '../../auth/view/auth_test_page.dart';
import 'create_quiz_screen.dart';
import 'quiz_waiting_screen.dart';
import 'my_quizzes_screen.dart';

/// Quiz Entry Screen
/// 
/// Første skærm når appen åbnes.
/// Giver mulighed for at:
/// - Logge ind (med Google/GitHub SSO eller email/password)
/// - Indtaste quiz PIN for at deltage uden login
class QuizEntryScreen extends StatefulWidget {
  const QuizEntryScreen({super.key});

  @override
  State<QuizEntryScreen> createState() => _QuizEntryScreenState();
}

class _QuizEntryScreenState extends State<QuizEntryScreen> {
  final TextEditingController _pinController = TextEditingController();
  final TextEditingController _nicknameController = TextEditingController();
  final _formKey = GlobalKey<FormState>();
  bool _showNicknameInput = false;

  @override
  void dispose() {
    _pinController.dispose();
    _nicknameController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: BlocListener<QuizBloc, QuizState>(
          listener: (context, state) {
            if (state is QuizSessionFound) {
              // Session fundet - vis nickname input
              setState(() {
                _showNicknameInput = true;
              });
            } else if (state is QuizJoined) {
              // Deltager har joinet - naviger til waiting screen
              Navigator.pushReplacement(
                context,
                MaterialPageRoute(
                  builder: (context) => QuizWaitingScreen(
                    session: state.session,
                    participant: state.participant,
                  ),
                ),
              );
            } else if (state is QuizError) {
              // Vis fejl
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(state.message),
                  backgroundColor: Colors.red,
                ),
              );
            }
          },
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24.0),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 60),
                
                // Logo/Title
                Icon(
                  Icons.quiz,
                  size: 80,
                  color: Theme.of(context).colorScheme.primary,
                ),
                const SizedBox(height: 24),
                Text(
                  'Kahoot Quiz',
                  style: Theme.of(context).textTheme.headlineLarge?.copyWith(
                        fontWeight: FontWeight.bold,
                        color: Theme.of(context).colorScheme.primary,
                      ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                Text(
                  'Deltag i en quiz eller log ind',
                  style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                        color: Colors.grey[600],
                      ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 60),

                // PIN Input Section
                if (!_showNicknameInput) _buildPinInputSection(context),

                // Nickname Input Section
                if (_showNicknameInput) _buildNicknameInputSection(context),

                const SizedBox(height: 40),

                // Divider
                Row(
                  children: [
                    Expanded(child: Divider(color: Colors.grey[300])),
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      child: Text(
                        'ELLER',
                        style: TextStyle(color: Colors.grey[600]),
                      ),
                    ),
                    Expanded(child: Divider(color: Colors.grey[300])),
                  ],
                ),

                const SizedBox(height: 40),

                // Login Section
                _buildLoginSection(context),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildPinInputSection(BuildContext context) {
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Indtast Quiz PIN',
            style: Theme.of(context).textTheme.titleLarge?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 24),
          TextFormField(
            controller: _pinController,
            decoration: InputDecoration(
              labelText: 'Quiz PIN',
              hintText: '123456',
              prefixIcon: const Icon(Icons.pin),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
              ),
              filled: true,
              fillColor: Colors.grey[50],
            ),
            keyboardType: TextInputType.number,
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 24,
              fontWeight: FontWeight.bold,
              letterSpacing: 4,
            ),
            maxLength: 6,
            validator: (value) {
              if (value == null || value.isEmpty) {
                return 'Indtast en PIN';
              }
              if (value.length != 6) {
                return 'PIN skal være 6 cifre';
              }
              return null;
            },
          ),
          const SizedBox(height: 24),
          BlocBuilder<QuizBloc, QuizState>(
            builder: (context, state) {
              final isLoading = state is QuizLoading;
              return ElevatedButton(
                onPressed: isLoading
                    ? null
                    : () {
                        if (_formKey.currentState!.validate()) {
                          context.read<QuizBloc>().add(
                                GetSessionByPinEvent(
                                  pin: _pinController.text.trim(),
                                ),
                              );
                        }
                      },
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
                        'Deltag i Quiz',
                        style: TextStyle(fontSize: 16),
                      ),
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _buildNicknameInputSection(BuildContext context) {
    return BlocBuilder<QuizBloc, QuizState>(
      builder: (context, state) {
        if (state is! QuizSessionFound) {
          return const SizedBox.shrink();
        }

        final session = state.session;

        return Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Session Info Card
              Card(
                elevation: 2,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      Icon(
                        Icons.check_circle,
                        color: Colors.green,
                        size: 48,
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Session fundet!',
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        session.quizTitle,
                        style: Theme.of(context).textTheme.bodyLarge,
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: 8),
                      Text(
                        '${session.participantCount} deltagere',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: Colors.grey[600],
                            ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 24),
              TextFormField(
                controller: _nicknameController,
                decoration: InputDecoration(
                  labelText: 'Dit Nickname',
                  hintText: 'Indtast dit navn',
                  prefixIcon: const Icon(Icons.person),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  filled: true,
                  fillColor: Colors.grey[50],
                ),
                textCapitalization: TextCapitalization.words,
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return 'Indtast et nickname';
                  }
                  if (value.length < 2) {
                    return 'Nickname skal være mindst 2 tegn';
                  }
                  if (value.length > 20) {
                    return 'Nickname må maks være 20 tegn';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () {
                        setState(() {
                          _showNicknameInput = false;
                          _nicknameController.clear();
                        });
                        context.read<QuizBloc>().add(const ResetQuizEvent());
                      },
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: const Text('Tilbage'),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    flex: 2,
                    child: ElevatedButton(
                      onPressed: state is QuizLoading
                          ? null
                          : () {
                              if (_formKey.currentState!.validate()) {
                                context.read<QuizBloc>().add(
                                      JoinSessionEvent(
                                        sessionPin: session.sessionPin,
                                        nickname: _nicknameController.text.trim(),
                                      ),
                                    );
                              }
                            },
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: state is QuizLoading
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Text(
                              'Join Quiz',
                              style: TextStyle(fontSize: 16),
                            ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildLoginSection(BuildContext context) {
    return BlocBuilder<AuthBloc, AuthState>(
      builder: (context, authState) {
        if (authState is AuthAuthenticated) {
          // Bruger er allerede logget ind
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Card(
                elevation: 2,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      if (authState.user.picture != null)
                        ClipOval(
                          child: Image.network(
                            authState.user.picture!,
                            width: 40,
                            height: 40,
                            fit: BoxFit.cover,
                            errorBuilder: (context, error, stackTrace) {
                              return const Icon(Icons.person, size: 40);
                            },
                          ),
                        )
                      else
                        const Icon(Icons.person, size: 40),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              authState.user.username.isNotEmpty
                                  ? authState.user.username
                                  : authState.user.email,
                              style: const TextStyle(
                                fontWeight: FontWeight.bold,
                                fontSize: 16,
                              ),
                            ),
                            Text(
                              'Logget ind',
                              style: TextStyle(
                                color: Colors.grey[600],
                                fontSize: 14,
                              ),
                            ),
                          ],
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.quiz),
                        onPressed: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => const MyQuizzesScreen(),
                            ),
                          );
                        },
                        tooltip: 'Mine Quizzers',
                      ),
                      IconButton(
                        icon: const Icon(Icons.add),
                        onPressed: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => const CreateQuizScreen(),
                            ),
                          );
                        },
                        tooltip: 'Opret Quiz',
                      ),
                      IconButton(
                        icon: const Icon(Icons.logout),
                        onPressed: () {
                          context.read<AuthBloc>().add(const LogoutEvent());
                        },
                        tooltip: 'Log ud',
                      ),
                    ],
                  ),
                ),
              ),
            ],
          );
        }

        // Bruger er ikke logget ind - vis login knapper
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Log ind for at oprette quizzers',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: () {
                // Naviger til auth test page for login
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (context) => const AuthTestPage(),
                  ),
                );
              },
              icon: const Icon(Icons.login),
              label: const Text('Log ind'),
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
            ),
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: () {
                // Naviger til mine quizzers screen
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (context) => const MyQuizzesScreen(),
                  ),
                );
              },
              icon: const Icon(Icons.quiz),
              label: const Text('Mine Quizzers'),
              style: OutlinedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}

