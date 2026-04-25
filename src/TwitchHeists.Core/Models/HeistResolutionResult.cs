using System;
using System.Collections.Generic;

namespace TwitchHeists.Core.Models;

public sealed class HeistResolutionResult
{
    public Guid RoundId { get; set; }

    public HeistRoundState FinalState { get; set; }

    public decimal SuccessChance { get; set; }

    public decimal OriginalPot { get; set; }

    public decimal ResolvedPot { get; set; }

    public IReadOnlyList<HeistParticipant> Winners { get; set; } = Array.Empty<HeistParticipant>();

    public IReadOnlyList<HeistParticipant> Losers { get; set; } = Array.Empty<HeistParticipant>();

    public IReadOnlyList<HeistParticipant> RefundedParticipants { get; set; } = Array.Empty<HeistParticipant>();

    public DateTimeOffset ResolvedAtUtc { get; set; }
}
