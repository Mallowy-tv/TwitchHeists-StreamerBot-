using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace TwitchHeists.Tests;

public sealed class StreamerBotExportTests
{
    [Theory]
    [InlineData("!raffle")]
    [InlineData("!sraffle")]
    public void ExportedRaffleActions_ParseCommandInputAndForwardWinnerPoints(string actionName)
    {
        var source = GetExportedActionSource(actionName);

        Assert.Contains("GetRaffleWinnerPoints", source, StringComparison.Ordinal);
        Assert.Contains("GetOptionalStringArg(\"input0\", \"input\")", source, StringComparison.Ordinal);
        Assert.Contains("private decimal? GetRaffleWinnerPoints()", source, StringComparison.Ordinal);
        Assert.Contains("return null;", source, StringComparison.Ordinal);
        Assert.Contains("if (winnerPoints.HasValue)", source, StringComparison.Ordinal);
        Assert.Contains("SetProperty(command, \"WinnerPoints\", winnerPoints);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("return 5000m;", source, StringComparison.Ordinal);
    }

    private static string GetExportedActionSource(string actionName)
    {
        var exportPath = Path.Combine(GetRepositoryRoot(), "TwitchHeists.txt");
        var encodedExport = File.ReadAllText(exportPath);
        var exportBytes = Convert.FromBase64String(encodedExport);

        using var exportStream = new MemoryStream(exportBytes, 4, exportBytes.Length - 4, writable: false);
        using var gzipStream = new GZipStream(exportStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8);
        using var document = JsonDocument.Parse(reader.ReadToEnd());

        var action = document.RootElement
            .GetProperty("data")
            .GetProperty("actions")
            .EnumerateArray()
            .Single(candidate => string.Equals(candidate.GetProperty("name").GetString(), actionName, StringComparison.Ordinal));

        var byteCode = action
            .GetProperty("subActions")[0]
            .GetProperty("byteCode")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(byteCode));

        return Encoding.UTF8.GetString(Convert.FromBase64String(byteCode!));
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}
