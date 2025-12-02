import 'dart:async';
import 'package:flutter/material.dart';
import '../../../data/models/quiz/session_model.dart';
import '../../../data/models/quiz/participant_model.dart';
import '../../../core/di/injection.dart';
import '../../../data/repositories/quiz_repository_impl.dart';
import 'quiz_participation_screen.dart';

/// Quiz Waiting Screen
/// 
/// Skærm for deltagere der venter på at quizzen starter.
/// Poller session status og navigerer til participation screen når quiz starter.
class QuizWaitingScreen extends StatefulWidget {
  final SessionModel session;
  final ParticipantModel participant;

  const QuizWaitingScreen({
    super.key,
    required this.session,
    required this.participant,
  });

  @override
  State<QuizWaitingScreen> createState() => _QuizWaitingScreenState();
}

class _QuizWaitingScreenState extends State<QuizWaitingScreen> {
  Timer? _pollTimer;
  SessionModel? _currentSession;

  @override
  void initState() {
    super.initState();
    _currentSession = widget.session;
    _startPolling();
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    super.dispose();
  }

  void _startPolling() {
    // Poll hver 2 sekunder for at tjekke om session er startet
    _pollTimer = Timer.periodic(const Duration(seconds: 2), (timer) async {
      final repository = getIt<QuizRepositoryImpl>();
      final result = await repository.getSessionByPin(widget.session.sessionPin);

      if (result.isSuccess) {
        final updatedSession = result.dataOrNull!;
        
        setState(() {
          _currentSession = updatedSession;
        });

        // Hvis session er startet, naviger til participation screen
        if (updatedSession.status == 'InProgress') {
          timer.cancel();
          if (mounted) {
            Navigator.pushReplacement(
              context,
              MaterialPageRoute(
                builder: (context) => QuizParticipationScreen(
                  session: updatedSession,
                  participant: widget.participant,
                ),
              ),
            );
          }
        }
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_currentSession?.quizTitle ?? widget.session.quizTitle),
        backgroundColor: Theme.of(context).colorScheme.primary,
        foregroundColor: Colors.white,
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              // Waiting Animation
              Icon(
                Icons.hourglass_empty,
                size: 120,
                color: Theme.of(context).colorScheme.primary,
              ),
              const SizedBox(height: 32),

              // Title
              Text(
                'Venter på at quizzen starter...',
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 16),

              // Quiz Info
              Card(
                elevation: 2,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      Text(
                        _currentSession?.quizTitle ?? widget.session.quizTitle,
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: 16),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.people, size: 20, color: Colors.grey[600]),
                          const SizedBox(width: 8),
                          Text(
                            '${_currentSession?.participantCount ?? widget.session.participantCount} deltagere',
                            style: Theme.of(context).textTheme.bodyLarge,
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 32),

              // Participant Info
              Card(
                elevation: 1,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      Icon(Icons.person, color: Theme.of(context).colorScheme.primary),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Dit Nickname',
                              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                                    color: Colors.grey[600],
                                  ),
                            ),
                            Text(
                              widget.participant.nickname,
                              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                                    fontWeight: FontWeight.bold,
                                  ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 24),

              // Loading Indicator
              const CircularProgressIndicator(),
              const SizedBox(height: 16),
              Text(
                'Venter på at host starter quizzen...',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      color: Colors.grey[600],
                    ),
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

