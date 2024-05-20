using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Extensions;

internal static class PlatformExtensions
{
    public static IEnumerable<Platform> Platforms { get; } = Enum.GetValues<Platform>().Skip(1);

    public static string GetInitClientName(this Platform platform)
        => "init-" + platform.ToString().ToLowerInvariant();
}
