/// Leaderboard Entry Model
class LeaderboardEntryModel {
  final int participantId;
  final String nickname;
  final int totalPoints;
  final int rank;

  LeaderboardEntryModel({
    required this.participantId,
    required this.nickname,
    required this.totalPoints,
    required this.rank,
  });

  factory LeaderboardEntryModel.fromJson(Map<String, dynamic> json) {
    return LeaderboardEntryModel(
      participantId: json['participantId'] as int,
      nickname: json['nickname'] as String,
      totalPoints: json['totalPoints'] as int,
      rank: json['rank'] as int,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'participantId': participantId,
      'nickname': nickname,
      'totalPoints': totalPoints,
      'rank': rank,
    };
  }
}

