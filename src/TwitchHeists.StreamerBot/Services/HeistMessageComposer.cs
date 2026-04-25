using System.Globalization;
using TwitchHeists.Core.Models;
using TwitchHeists.StreamerBot.Configuration;

namespace TwitchHeists.StreamerBot.Services;

public sealed class HeistMessageComposer
{
    private static readonly Random DefaultRandom = new();
    private readonly HeistMessageTemplates templates;
    private readonly Func<int, int> chooseIndex;

    public HeistMessageComposer(HeistMessageTemplates templates, Func<int, int>? chooseIndex = null)
    {
        this.templates = templates;
        this.chooseIndex = chooseIndex ?? DefaultRandom.Next;
    }

    public string ComposeStart(string starter, decimal stakeAmount, TimeSpan joinWindow)
    {
        return ReplaceTokens(
            SelectTemplate(templates.StartMessages),
            new Dictionary<string, string>
            {
                ["starter"] = starter,
                ["stake"] = FormatAmount(stakeAmount),
                ["joinWindow"] = FormatJoinWindow(joinWindow)
            });
    }

    public string ComposeCooldown(TimeSpan remaining)
    {
        return ReplaceTokens(
            SelectTemplate(templates.CooldownMessages),
            new Dictionary<string, string>
            {
                ["cooldownRemaining"] = FormatRemainingTime(remaining)
            });
    }

    public string ComposeReminder(decimal pot, int participantCount, string countdown)
    {
        return ReplaceTokens(
            SelectTemplate(templates.ReminderMessages),
            new Dictionary<string, string>
            {
                ["countdown"] = countdown,
                ["pot"] = FormatAmount(pot),
                ["participantCount"] = participantCount.ToString(CultureInfo.InvariantCulture)
            });
    }

    public string ComposeResolution(HeistResolutionResult resolution)
    {
        var parts = new List<string>();
        var isSuccess = resolution.FinalState == HeistRoundState.ResolvedSuccess;
        parts.Add(SelectTemplate(isSuccess ? templates.SuccessHeadlines : templates.FailureHeadlines));
        parts.AddRange(BuildCallouts(resolution, isSuccess));
        parts.Add(BuildSummary(resolution));
        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private IEnumerable<string> BuildCallouts(HeistResolutionResult resolution, bool isSuccess)
    {
        var callouts = new List<string>();
        if (isSuccess)
        {
            if (resolution.Losers.Count > 0 && resolution.Winners.Count > 0)
            {
                callouts.Add(
                    ReplaceTokens(
                        SelectTemplate(templates.SacrificeCallouts),
                        BuildParticipantTokens(
                            resolution.Winners[0],
                            resolution.Losers[0],
                            resolution)));
            }

            foreach (var winner in resolution.Winners)
            {
                if (callouts.Count >= 2)
                {
                    break;
                }

                callouts.Add(
                    ReplaceTokens(
                        SelectTemplate(templates.SuccessCallouts),
                        BuildParticipantTokens(
                            winner,
                            resolution.Losers.Count > 0 ? resolution.Losers[0] : null,
                            resolution)));
            }

            return callouts;
        }

        foreach (var loser in resolution.Losers)
        {
            if (callouts.Count >= 2)
            {
                break;
            }

            callouts.Add(
                ReplaceTokens(
                    SelectTemplate(templates.FailureCallouts),
                    BuildParticipantTokens(null, loser, resolution)));
        }

        return callouts;
    }

    private string BuildSummary(HeistResolutionResult resolution)
    {
        return ReplaceTokens(
            SelectTemplate(templates.ResultSummaries),
            BuildParticipantTokens(
                resolution.Winners.Count > 0 ? resolution.Winners[0] : null,
                resolution.Losers.Count > 0 ? resolution.Losers[0] : null,
                resolution));
    }

    private Dictionary<string, string> BuildParticipantTokens(
        HeistParticipant? winner,
        HeistParticipant? loser,
        HeistResolutionResult resolution)
    {
        return new Dictionary<string, string>
        {
            ["winner"] = winner is null ? string.Empty : FormatViewerName(winner.Identity),
            ["loser"] = loser is null ? string.Empty : FormatViewerName(loser.Identity),
            ["payout"] = winner is null ? string.Empty : FormatAmount(winner.PayoutAmount),
            ["winnerCount"] = resolution.Winners.Count.ToString(CultureInfo.InvariantCulture),
            ["loserCount"] = resolution.Losers.Count.ToString(CultureInfo.InvariantCulture),
            ["resolvedPot"] = FormatAmount(resolution.ResolvedPot),
            ["successChancePercent"] = FormatPercentage(resolution.SuccessChance)
        };
    }

    private string SelectTemplate(IReadOnlyList<string> options)
    {
        if (options.Count == 1)
        {
            return options[0];
        }

        var index = chooseIndex(options.Count);
        if (index < 0)
        {
            index = 0;
        }

        if (index >= options.Count)
        {
            index = options.Count - 1;
        }

        return options[index];
    }

    private static string ReplaceTokens(string template, IReadOnlyDictionary<string, string> values)
    {
        var result = template;
        foreach (var entry in values)
        {
            result = result.Replace($"{{{entry.Key}}}", entry.Value);
        }

        return result;
    }

    private static string FormatViewerName(ViewerIdentity identity)
    {
        return string.IsNullOrWhiteSpace(identity.Username)
            ? identity.DisplayName ?? string.Empty
            : identity.Username;
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string FormatPercentage(decimal value)
    {
        return $"{(value * 100m).ToString("0.##", CultureInfo.InvariantCulture)}%";
    }

    private static string FormatJoinWindow(TimeSpan duration)
    {
        if (duration.TotalMinutes == 1)
        {
            return "1 minute";
        }

        if (duration.TotalMinutes == Math.Truncate(duration.TotalMinutes))
        {
            return $"{duration.TotalMinutes.ToString("0", CultureInfo.InvariantCulture)} minutes";
        }

        return FormatRemainingTime(duration);
    }

    private static string FormatRemainingTime(TimeSpan duration)
    {
        var remaining = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
        var totalMinutes = (int)remaining.TotalMinutes;
        var seconds = remaining.Seconds;

        if (totalMinutes <= 0)
        {
            return $"{seconds}s";
        }

        return $"{totalMinutes}m {seconds}s";
    }
}
