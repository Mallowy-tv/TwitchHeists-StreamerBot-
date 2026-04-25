using System;

namespace TwitchHeists.Core.Models;

public sealed class HeistParticipant
{
    public ViewerIdentity Identity { get; set; } = new ViewerIdentity();

    public decimal StakeAmount { get; set; }

    public DateTimeOffset JoinedAtUtc { get; set; }

    public bool IsWinner { get; set; }

    public decimal PayoutAmount { get; set; }
}
