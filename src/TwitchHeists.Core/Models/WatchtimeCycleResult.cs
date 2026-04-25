using System.Collections.Generic;

namespace TwitchHeists.Core.Models;

public sealed class WatchtimeCycleResult
{
    public IReadOnlyList<ViewerRewardResult> Rewards { get; set; } = Array.Empty<ViewerRewardResult>();

    public IReadOnlyList<ViewerPresenceRecord> ActivePresence { get; set; } = Array.Empty<ViewerPresenceRecord>();

    public IReadOnlyList<ViewerPresenceRecord> ExpiredPresence { get; set; } = Array.Empty<ViewerPresenceRecord>();
}
