using System.Globalization;

namespace TwitchHeists.Core.Services;

public static class PointValueNormalizer
{
    public static decimal Normalize(decimal value)
    {
        return Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal NormalizeNonNegative(decimal value)
    {
        return Math.Max(0m, Normalize(value));
    }

    public static string Format(decimal value)
    {
        return Normalize(value).ToString("0", CultureInfo.InvariantCulture);
    }
}
