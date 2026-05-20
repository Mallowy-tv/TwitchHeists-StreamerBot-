using System.Globalization;
using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;
using TwitchHeists.StreamerBot.Configuration;

namespace TwitchHeists.StreamerBot.Services;

public sealed class HeistMessageComposer
{
    private static readonly Random DefaultRandom = new();
    private readonly HeistMessageTemplates templates;
    private readonly HeistSettings heistSettings;
    private readonly Func<int, int> chooseIndex;

    public HeistMessageComposer(HeistMessageTemplates templates, Func<int, int>? chooseIndex = null)
        : this(templates, new HeistSettings(), chooseIndex)
    {
    }

    public HeistMessageComposer(HeistMessageTemplates templates, HeistSettings heistSettings, Func<int, int>? chooseIndex = null)
    {
        this.templates = templates;
        this.heistSettings = heistSettings;
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

    public string ComposeInsufficientBalance(string viewer, decimal stakeAmount)
    {
        return ReplaceTokens(
            SelectTemplate(templates.InsufficientBalanceMessages),
            new Dictionary<string, string>
            {
                ["viewer"] = viewer,
                ["stake"] = FormatAmount(stakeAmount)
            });
    }

    public string ComposeAlreadyJoined(string viewer)
    {
        return ReplaceTokens(
            SelectTemplate(templates.AlreadyJoinedMessages),
            new Dictionary<string, string>
            {
                ["viewer"] = viewer
            });
    }

    public string ComposeMinimumJoinAmount(string viewer, decimal minimumJoinAmount)
    {
        return ReplaceTokens(
            SelectTemplate(templates.MinimumJoinAmountMessages),
            new Dictionary<string, string>
            {
                ["viewer"] = viewer,
                ["minimumJoinAmount"] = FormatAmount(minimumJoinAmount)
            });
    }

    public string ComposeResolution(HeistResolutionResult resolution)
    {
        if (resolution.FinalState == HeistRoundState.InsufficientCrew)
        {
            return ReplaceTokens(
                SelectTemplate(templates.InsufficientCrewMessages),
                BuildParticipantTokens(
                    winner: null,
                    loser: null,
                    resolution));
        }

        var parts = new List<string>();
        var isSuccess = resolution.FinalState == HeistRoundState.ResolvedSuccess;
        parts.Add(SelectTemplate(isSuccess ? templates.SuccessHeadlines : templates.FailureHeadlines));
        parts.AddRange(BuildCallouts(resolution, isSuccess));
        parts.Add(BuildSummary(resolution));
        return JoinUniqueParts(parts);
    }

    private IEnumerable<string> BuildCallouts(HeistResolutionResult resolution, bool isSuccess)
    {
        var callouts = new List<string>();
        var usedTemplates = new HashSet<string>(StringComparer.Ordinal);
        var usedRenderedLines = new HashSet<string>(StringComparer.Ordinal);
        var maximumNamedCallouts = Math.Max(0, heistSettings.MaximumNamedResolutionCallouts);
        if (maximumNamedCallouts == 0)
        {
            return callouts;
        }

        if (isSuccess)
        {
            if (resolution.Losers.Count > 0 && resolution.Winners.Count > 0 && callouts.Count < maximumNamedCallouts)
            {
                var sacrificeTemplate = SelectTemplate(templates.SacrificeCallouts, usedTemplates);
                if (!string.IsNullOrWhiteSpace(sacrificeTemplate))
                {
                    var sacrificeLine =
                        ReplaceTokens(
                            sacrificeTemplate,
                            BuildParticipantTokens(
                                resolution.Winners[0],
                                resolution.Losers[0],
                                resolution));

                    if (usedRenderedLines.Add(sacrificeLine))
                    {
                        usedTemplates.Add(sacrificeTemplate);
                        callouts.Add(sacrificeLine);
                    }
                }
            }

            foreach (var winner in resolution.Winners)
            {
                if (callouts.Count >= maximumNamedCallouts)
                {
                    break;
                }

                var successTemplate = SelectTemplate(templates.SuccessCallouts, usedTemplates);
                if (string.IsNullOrWhiteSpace(successTemplate))
                {
                    break;
                }

                var successLine =
                    ReplaceTokens(
                        successTemplate,
                        BuildParticipantTokens(
                            winner,
                            resolution.Losers.Count > 0 ? resolution.Losers[0] : null,
                            resolution));

                if (!usedRenderedLines.Add(successLine))
                {
                    continue;
                }

                usedTemplates.Add(successTemplate);
                callouts.Add(successLine);
            }

            return callouts;
        }

        foreach (var loser in resolution.Losers)
        {
            if (callouts.Count >= maximumNamedCallouts)
            {
                break;
            }

            var failureTemplate = SelectTemplate(templates.FailureCallouts, usedTemplates);
            if (string.IsNullOrWhiteSpace(failureTemplate))
            {
            break;
            }

            var failureLine =
                ReplaceTokens(
                    failureTemplate,
                BuildParticipantTokens(null, loser, resolution));

            if (!usedRenderedLines.Add(failureLine))
            {
            continue;
            }

            usedTemplates.Add(failureTemplate);
            callouts.Add(failureLine);
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
        var participantCount = resolution.Winners.Count + resolution.Losers.Count + resolution.RefundedParticipants.Count;
        return new Dictionary<string, string>
        {
            ["winner"] = winner is null ? string.Empty : FormatViewerName(winner.Identity),
            ["loser"] = loser is null ? string.Empty : FormatViewerName(loser.Identity),
            ["payout"] = winner is null ? string.Empty : FormatAmount(winner.PayoutAmount),
            ["winnerCount"] = resolution.Winners.Count.ToString(CultureInfo.InvariantCulture),
            ["loserCount"] = resolution.Losers.Count.ToString(CultureInfo.InvariantCulture),
            ["participantCount"] = participantCount.ToString(CultureInfo.InvariantCulture),
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

        var index = ClampSelectedIndex(options.Count);
        return options[index];
    }

    private string SelectTemplate(IReadOnlyList<string> options, ISet<string> excludedTemplates)
    {
        if (options.Count == 0)
        {
            return string.Empty;
        }

        var availableOptions = options
            .Where(option => !excludedTemplates.Contains(option))
            .ToList();

        if (availableOptions.Count == 0)
        {
            return string.Empty;
        }

        if (availableOptions.Count == 1)
        {
            return availableOptions[0];
        }

        var index = ClampSelectedIndex(availableOptions.Count);
        return availableOptions[index];
    }

    private int ClampSelectedIndex(int optionCount)
    {
        var index = chooseIndex(optionCount);
        if (index < 0)
        {
            return 0;
        }

        return index >= optionCount
            ? optionCount - 1
            : index;
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

    private static string JoinUniqueParts(IEnumerable<string> parts)
    {
        var uniqueParts = new List<string>();
        var seenParts = new HashSet<string>(StringComparer.Ordinal);

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var trimmedPart = part.Trim();
            if (seenParts.Add(trimmedPart))
            {
                uniqueParts.Add(trimmedPart);
            }
        }

        return string.Join(" ", uniqueParts);
    }

    private static string FormatViewerName(ViewerIdentity identity)
    {
        return string.IsNullOrWhiteSpace(identity.Username)
            ? identity.DisplayName ?? string.Empty
            : identity.Username;
    }

    private static string FormatAmount(decimal amount)
    {
        return PointValueNormalizer.Format(amount);
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
