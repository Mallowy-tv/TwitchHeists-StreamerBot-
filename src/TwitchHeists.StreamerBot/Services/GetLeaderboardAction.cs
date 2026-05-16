using System;
using System.Linq;
using TwitchHeists.Core.Models;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class GetLeaderboardAction
{
    private const int LeaderboardSize = 5;

    private readonly ViewerRepository viewerRepository;

    public GetLeaderboardAction(ViewerRepository viewerRepository)
    {
        this.viewerRepository = viewerRepository;
    }

    public ActionResponseDto Execute()
    {
        var leaderboard = viewerRepository.GetTopViewerBalances(LeaderboardSize);
        if (leaderboard.Count == 0)
        {
            return new ActionResponseDto
            {
                Success = true,
                Message = "No point balances found."
            };
        }

        var lines = leaderboard
            .Select((entry, index) => $"{index + 1}. {entry.Username} ({PointValueNormalizer.Format(entry.PointsBalance)})");

        return new ActionResponseDto
        {
            Success = true,
            Message = string.Join(Environment.NewLine, lines)
        };
    }
}
