using System;

namespace TwitchHeists.Core.Options;

public sealed class HeistWinnerBand
{
    public HeistWinnerBand(int minimumParticipants, int maximumParticipants, int minimumWinners, int maximumWinners)
    {
        if (minimumParticipants < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumParticipants), "Minimum participants must be at least 1.");
        }

        if (maximumParticipants < minimumParticipants)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumParticipants), "Maximum participants must be greater than or equal to minimum participants.");
        }

        if (minimumWinners < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumWinners), "Minimum winners must be at least 1.");
        }

        if (maximumWinners < minimumWinners)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumWinners), "Maximum winners must be greater than or equal to minimum winners.");
        }

        MinimumParticipants = minimumParticipants;
        MaximumParticipants = maximumParticipants;
        MinimumWinners = minimumWinners;
        MaximumWinners = maximumWinners;
    }

    public int MinimumParticipants { get; }

    public int MaximumParticipants { get; }

    public int MinimumWinners { get; }

    public int MaximumWinners { get; }

    public bool ContainsParticipants(int participantCount)
    {
        return participantCount >= MinimumParticipants && participantCount <= MaximumParticipants;
    }

    public (int MinimumWinners, int MaximumWinners) GetClampedWinnerRange(int participantCount)
    {
        if (participantCount < MinimumParticipants)
        {
            throw new ArgumentOutOfRangeException(nameof(participantCount), "Participant count does not fall within this band.");
        }

        var effectiveParticipantCount = Math.Min(participantCount, MaximumParticipants);
        var clampedMaximum = Math.Min(MaximumWinners, effectiveParticipantCount);
        var clampedMinimum = Math.Min(MinimumWinners, clampedMaximum);
        return (clampedMinimum, clampedMaximum);
    }
}
