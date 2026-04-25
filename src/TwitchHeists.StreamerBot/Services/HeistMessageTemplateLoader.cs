using System.Text.Json;
using TwitchHeists.StreamerBot.Configuration;

namespace TwitchHeists.StreamerBot.Services;

public sealed class HeistMessageTemplateLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HeistMessageTemplates Load(string? templatePath)
    {
        var defaults = HeistMessageTemplates.CreateDefault();
        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(templatePath);
            var parsed = JsonSerializer.Deserialize<HeistMessageTemplates>(json, SerializerOptions) ?? new HeistMessageTemplates();
            return new HeistMessageTemplates
            {
                StartMessages = EnsureGroup(parsed.StartMessages, defaults.StartMessages),
                CooldownMessages = EnsureGroup(parsed.CooldownMessages, defaults.CooldownMessages),
                ReminderMessages = EnsureGroup(parsed.ReminderMessages, defaults.ReminderMessages),
                SuccessHeadlines = EnsureGroup(parsed.SuccessHeadlines, defaults.SuccessHeadlines),
                FailureHeadlines = EnsureGroup(parsed.FailureHeadlines, defaults.FailureHeadlines),
                SuccessCallouts = EnsureGroup(parsed.SuccessCallouts, defaults.SuccessCallouts),
                FailureCallouts = EnsureGroup(parsed.FailureCallouts, defaults.FailureCallouts),
                SacrificeCallouts = EnsureGroup(parsed.SacrificeCallouts, defaults.SacrificeCallouts),
                ResultSummaries = EnsureGroup(parsed.ResultSummaries, defaults.ResultSummaries),
                InsufficientCrewMessages = EnsureGroup(parsed.InsufficientCrewMessages, defaults.InsufficientCrewMessages)
            };
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The TwitchHeists heist message template file is invalid JSON: {templatePath}.", exception);
        }
    }

    private static List<string> EnsureGroup(List<string>? source, List<string> fallback)
    {
        if (source is null || source.Count == 0)
        {
            return new List<string>(fallback);
        }

        var sanitized = new List<string>();
        foreach (var entry in source)
        {
            if (!string.IsNullOrWhiteSpace(entry))
            {
                sanitized.Add(entry.Trim());
            }
        }

        return sanitized.Count == 0 ? new List<string>(fallback) : sanitized;
    }
}
