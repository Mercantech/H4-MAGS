import 'package:flutter/material.dart';
import '../../../data/models/quiz/session_model.dart';
import '../../../data/models/quiz/leaderboard_entry_model.dart';

/// Quiz Results Screen
/// 
/// Viser resultater og leaderboard efter quiz er færdig.
class QuizResultsScreen extends StatelessWidget {
  final SessionModel session;
  final int participantId;
  final List<LeaderboardEntryModel> leaderboard;

  const QuizResultsScreen({
    super.key,
    required this.session,
    required this.participantId,
    required this.leaderboard,
  });

  @override
  Widget build(BuildContext context) {
    // Find deltagerens position
    final participantEntry = leaderboard.firstWhere(
      (entry) => entry.participantId == participantId,
      orElse: () => LeaderboardEntryModel(
        participantId: participantId,
        nickname: 'Ukendt',
        totalPoints: 0,
        rank: leaderboard.length + 1,
      ),
    );

    return Scaffold(
      appBar: AppBar(
        title: Text(session.quizTitle),
        backgroundColor: Theme.of(context).colorScheme.primary,
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Resultat Card
            Card(
              elevation: 4,
              color: Theme.of(context).colorScheme.primaryContainer,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(16),
              ),
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  children: [
                    // Rank Icon
                    if (participantEntry.rank == 1)
                      Icon(
                        Icons.emoji_events,
                        size: 80,
                        color: Colors.amber[700],
                      )
                    else if (participantEntry.rank == 2)
                      Icon(
                        Icons.emoji_events,
                        size: 80,
                        color: Colors.grey[400],
                      )
                    else if (participantEntry.rank == 3)
                      Icon(
                        Icons.emoji_events,
                        size: 80,
                        color: Colors.brown[400],
                      )
                    else
                      Icon(
                        Icons.check_circle,
                        size: 80,
                        color: Theme.of(context).colorScheme.primary,
                      ),
                    const SizedBox(height: 16),
                    Text(
                      'Plads ${participantEntry.rank}',
                      style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      participantEntry.nickname,
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                    const SizedBox(height: 16),
                    Text(
                      '${participantEntry.totalPoints}',
                      style: Theme.of(context).textTheme.displayLarge?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: Theme.of(context).colorScheme.primary,
                          ),
                    ),
                    Text(
                      'Point',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                            color: Colors.grey[600],
                          ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 32),

            // Leaderboard
            Text(
              'Leaderboard',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 16),
            if (leaderboard.isEmpty)
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text(
                    'Ingen resultater endnu',
                    style: Theme.of(context).textTheme.bodyMedium,
                    textAlign: TextAlign.center,
                  ),
                ),
              )
            else
              ...leaderboard.map((participant) {
                final isCurrentUser = participant.participantId == participantId;

                return Card(
                  elevation: isCurrentUser ? 4 : 1,
                  color: isCurrentUser
                      ? Theme.of(context).colorScheme.primaryContainer
                      : null,
                  margin: const EdgeInsets.only(bottom: 8),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: ListTile(
                    leading: CircleAvatar(
                      backgroundColor: isCurrentUser
                          ? Theme.of(context).colorScheme.primary
                          : Colors.grey[300],
                      child: Text(
                        '${participant.rank}',
                        style: TextStyle(
                          color: isCurrentUser ? Colors.white : Colors.black87,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    title: Text(
                      participant.nickname,
                      style: TextStyle(
                        fontWeight: isCurrentUser ? FontWeight.bold : FontWeight.normal,
                      ),
                    ),
                    trailing: Text(
                      '${participant.totalPoints}',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: isCurrentUser
                                ? Theme.of(context).colorScheme.primary
                                : null,
                          ),
                    ),
                  ),
                );
              }),
            const SizedBox(height: 32),

            // Back Button
            ElevatedButton(
              onPressed: () {
                // Naviger tilbage til entry screen
                Navigator.of(context).popUntil((route) => route.isFirst);
              },
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              child: const Text(
                'Tilbage til start',
                style: TextStyle(fontSize: 16),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

