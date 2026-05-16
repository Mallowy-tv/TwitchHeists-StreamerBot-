namespace TwitchHeists.Core.Services;

public sealed class RaffleWinnerCalculator
{
    public int ResolveWinnerCount(int participantCount)
    {
        if (participantCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(participantCount), "Participant count must be at least 1.");
        }

        if (participantCount == 1)
        {
            return 1;
        }

        if (participantCount <= 10)
        {
            return participantCount / 2;
        }

        if (participantCount <= 20)
        {
            return participantCount / 4;
        }

        if (participantCount <= 50)
        {
            return participantCount / 5;
        }

        if (participantCount <= 200)
        {
            return participantCount / 8;
        }

        return participantCount / 20;
    }
}
