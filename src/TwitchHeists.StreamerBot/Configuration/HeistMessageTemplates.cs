using System.Text.Json.Serialization;

namespace TwitchHeists.StreamerBot.Configuration;

public sealed class HeistMessageTemplates
{
    [JsonPropertyName("startMessages")]
    public List<string> StartMessages { get; set; } = new();

    [JsonPropertyName("cooldownMessages")]
    public List<string> CooldownMessages { get; set; } = new();

    [JsonPropertyName("reminderMessages")]
    public List<string> ReminderMessages { get; set; } = new();

    [JsonPropertyName("successHeadlines")]
    public List<string> SuccessHeadlines { get; set; } = new();

    [JsonPropertyName("failureHeadlines")]
    public List<string> FailureHeadlines { get; set; } = new();

    [JsonPropertyName("successCallouts")]
    public List<string> SuccessCallouts { get; set; } = new();

    [JsonPropertyName("failureCallouts")]
    public List<string> FailureCallouts { get; set; } = new();

    [JsonPropertyName("sacrificeCallouts")]
    public List<string> SacrificeCallouts { get; set; } = new();

    [JsonPropertyName("resultSummaries")]
    public List<string> ResultSummaries { get; set; } = new();

    [JsonPropertyName("insufficientCrewMessages")]
    public List<string> InsufficientCrewMessages { get; set; } = new();

    public static HeistMessageTemplates CreateDefault()
    {
        return new HeistMessageTemplates
        {
            StartMessages = new List<string>
            {
                "{starter} started a heist with {stake} points. Starting in {joinWindow}."
            },
            CooldownMessages = new List<string>
            {
                "A new heist can start in {cooldownRemaining}."
            },
            ReminderMessages = new List<string>
            {
                "Heist starts in {countdown}. Pot is now {pot} points across {participantCount} viewers."
            },
            SuccessHeadlines = new List<string>
            {
                "The crew cracked the vault and got away clean.",
                "The crew blasted through the alarms and escaped with the haul.",
                "The getaway driver nailed the timing and the crew got out."
            },
            FailureHeadlines = new List<string>
            {
                "Police captured the whole crew before anyone escaped.",
                "The crew got boxed in and everyone was gunned down.",
                "The alarms tripped early and the crew never made it out."
            },
            SuccessCallouts = new List<string>
            {
                "{winner} slipped out with {payout} points.",
                "{winner} dove into the van with {payout} points.",
                "{winner} made it to the getaway car carrying {payout} points."
            },
            FailureCallouts = new List<string>
            {
                "{loser} got left behind in the crossfire.",
                "{loser} took a bullet covering the escape.",
                "{loser} was hauled off in cuffs when the exit closed."
            },
            SacrificeCallouts = new List<string>
            {
                "{loser} took a bullet for {winner}.",
                "{loser} held the line so {winner} could escape.",
                "{loser} drew the heat long enough for {winner} to get out."
            },
            ResultSummaries = new List<string>
            {
                "{winnerCount} got out with {resolvedPot} points while {loserCount} were left behind. Success chance was {successChancePercent}."
            },
            InsufficientCrewMessages = new List<string>
            {
                "The crew was too small to leave the hideout. {participantCount} joined, so everyone got their {resolvedPot} points back."
            }
        };
    }
}
