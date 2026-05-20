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

    [JsonPropertyName("insufficientBalanceMessages")]
    public List<string> InsufficientBalanceMessages { get; set; } = new();

    [JsonPropertyName("alreadyJoinedMessages")]
    public List<string> AlreadyJoinedMessages { get; set; } = new();

    [JsonPropertyName("minimumJoinAmountMessages")]
    public List<string> MinimumJoinAmountMessages { get; set; } = new();

    public static HeistMessageTemplates CreateDefault()
    {
        return new HeistMessageTemplates
        {
            StartMessages = new List<string>
            {
                "@{starter} started a heist with {stake} points. Starting in {joinWindow}. Use !join <points> to join the crew.",
                "@{starter} is planning a vault run worth {stake} points. Crew leaves in {joinWindow}. Type !join <points> to get in.",
                "@{starter} kicked open the hideout door with {stake} points on the line. Heist begins in {joinWindow}. Use !join <points> to ride along.",
                "@{starter} is assembling a crew for a risky score. Buy-in is {stake} points. Starting in {joinWindow}.",
                "@{starter} rolled up with blueprints and {stake} points. Join the heist in the next {joinWindow} with !join <points>."
            },
            CooldownMessages = new List<string>
            {
                "A new heist can start in {cooldownRemaining}. You can still join the current crew of {participantCount} with !join <points> for a share of the {pot} point pot.",
                "The hideout is still cooling down for {cooldownRemaining}, but the current crew still has room. {participantCount} are already in for {pot} points.",
                "The crew is already gearing up. New heists unlock in {cooldownRemaining}. Use !join <points> to join the {participantCount} robbers chasing {pot} points.",
                "Another job can't start for {cooldownRemaining}, but the current score is still open. Pot is sitting at {pot} points with {participantCount} participants."
            },
            ReminderMessages = new List<string>
            {
                "Heist starts in {countdown}. Pot is now {pot} points across {participantCount} viewers. Use !join <points> to get in on the action.",
                "Only {countdown} left before the crew rolls out. {participantCount} robbers are chasing {pot} points.",
                "The van leaves in {countdown}. Current pot: {pot} points. Current crew size: {participantCount}.",
                "Final call before the doors close. {countdown} remaining to join the heist for a shot at {pot} points.",
                "Crew check in {countdown}. {participantCount} viewers are already risking it all for {pot} points."
            },
            SuccessHeadlines = new List<string>
            {
                "The crew cracked the vault and got away clean.",
                "The crew blasted through the alarms and escaped with the haul.",
                "The getaway driver nailed the timing and the crew got out.",
                "The crew vanished into the night before the cops arrived.",
                "The vault door folded and the crew cleaned the place out.",
                "Smoke filled the lobby while the crew escaped with the cash.",
                "The guards never saw it coming and the crew made off rich.",
                "A perfect distraction gave the crew just enough time to escape.",
                "The scanners were bypassed and the crew slipped away untouched.",
                "The entire operation went smoother than anyone expected."
            },
            FailureHeadlines = new List<string>
            {
                "Police captured the whole crew before anyone escaped.",
                "The crew got boxed in and everyone was gunned down.",
                "The alarms tripped early and the crew never made it out.",
                "The getaway van stalled and the cops closed in fast.",
                "Someone cracked under pressure and exposed the entire plan.",
                "Security locked the building down before the crew could escape.",
                "The crew ran straight into a police blockade.",
                "The safe exploded too early and brought the entire city down on them.",
                "A silent alarm turned the heist into a disaster.",
                "The escape tunnel collapsed and trapped the entire crew inside."
            },
            SuccessCallouts = new List<string>
            {
                "@{winner} slipped out with {payout} points.",
                "@{winner} dove into the van with {payout} points.",
                "@{winner} made it to the getaway car carrying {payout} points.",
                "@{winner} disappeared into the crowd with {payout} points.",
                "@{winner} climbed the fence and escaped with {payout} points.",
                "@{winner} stuffed a duffel bag with {payout} points and escaped.",
                "@{winner} outran the cops and secured {payout} points.",
                "@{winner} escaped through the sewers carrying {payout} points.",
                "@{winner} bribed the right guard and walked away with {payout} points.",
                "@{winner} hit the jackpot and escaped with {payout} points."
            },
            FailureCallouts = new List<string>
            {
                "@{loser} got left behind in the crossfire.",
                "@{loser} took a bullet covering the escape.",
                "@{loser} was hauled off in cuffs when the exit closed.",
                "@{loser} tripped the alarm at the worst possible moment.",
                "@{loser} got cornered before reaching the getaway van.",
                "@{loser} dropped the loot and surrendered.",
                "@{loser} got spotted by security cameras and pinned down.",
                "@{loser} missed the extraction point and got caught.",
                "@{loser} froze under pressure and paid the price.",
                "@{loser} got tackled by police just steps from freedom."
            },
            SacrificeCallouts = new List<string>
            {
                "@{loser} took a bullet for @{winner}.",
                "@{loser} held the line so @{winner} could escape.",
                "@{loser} drew the heat long enough for @{winner} to get out.",
                "@{loser} stayed behind to jam the vault doors for @{winner}.",
                "@{loser} distracted security while @{winner} slipped away.",
                "@{loser} threw themselves into danger so @{winner} could survive.",
                "@{loser} triggered a diversion to cover @{winner}'s escape.",
                "@{loser} sacrificed their cut to save @{winner}.",
                "@{loser} blocked the hallway long enough for @{winner} to escape.",
                "@{loser} bought precious seconds for @{winner} with a heroic stand."
            },
            ResultSummaries = new List<string>
            {
                "{winnerCount} got out with {resolvedPot} points while {loserCount} were left behind. Success chance was {successChancePercent}.",
                "{winnerCount} escaped the heist and split {resolvedPot} points. {loserCount} never made it out.",
                "{winnerCount} survived the operation while {loserCount} got caught. Total payout was {resolvedPot} points.",
                "The crew walked away with {resolvedPot} points. Survivors: {winnerCount}. Captured: {loserCount}.",
                "{winnerCount} robbers escaped with the loot while {loserCount} paid the price. Success rate: {successChancePercent}.",
                "The dust settled with {winnerCount} winners, {loserCount} losers, and {resolvedPot} points distributed."
            },
            InsufficientCrewMessages = new List<string>
            {
                "The crew was too small to leave the hideout. {participantCount} joined, so everyone got their points back.",
                "Not enough robbers signed on for the job. Only {participantCount} joined, so the heist was called off.",
                "The crew couldn't agree on a plan and the heist never started. Everyone got refunded.",
                "With only {participantCount} participants, the crew decided the risk wasn't worth it.",
                "The van never left the garage. Too few people joined the heist, so all points were returned."
            },
            InsufficientBalanceMessages = new List<string>
            {
                "@{viewer} you need at least {stake} points to join this heist.",
                "@{viewer} the crew checked your wallet and you're short on points.",
                "@{viewer} you don't have enough points to buy into this heist.",
                "@{viewer} the crew isn't handing out free rides. You need {stake} points.",
                "@{viewer} you're too broke for this job right now."
            },
            AlreadyJoinedMessages = new List<string>
            {
                "@{viewer} you're already part of the crew.",
                "@{viewer} you've already locked in your spot for this heist.",
                "@{viewer} the crew already has your name on the roster.",
                "@{viewer} you already joined the current operation.",
                "@{viewer} you're already in on this job."
            },
            MinimumJoinAmountMessages = new List<string>
            {
                "@{viewer} the minimum join amount for this heist is {minimumJoinAmount} points.",
                "@{viewer} you need to risk at least {minimumJoinAmount} points to join the crew.",
                "@{viewer} the crew won't suit up for less than {minimumJoinAmount} points.",
                "@{viewer} your stake is too low. Minimum entry is {minimumJoinAmount} points.",
                "@{viewer} this heist requires a minimum buy-in of {minimumJoinAmount} points."
            }
        };
    }
}
