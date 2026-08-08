using System.Diagnostics.CodeAnalysis;
using NAudio.MediaFoundation;

namespace Novolis.Manuscript.Export.Audio;

[ExcludeFromCodeCoverage(Justification = "Windows Media Foundation native startup.")]
static class MediaFoundationSupport
{
    static bool? _available;

    public static bool TryEnsureAvailable()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        if (_available.HasValue)
            return _available.Value;

        try
        {
            MediaFoundationApi.Startup();
            _available = true;
        }
        catch
        {
            _available = false;
        }

        return _available.Value;
    }

    public static void EnsureAvailableOrThrow(string feature)
    {
        if (!TryEnsureAvailable())
        {
            throw new PlatformNotSupportedException(
                $"{feature} requires Windows Media Foundation, which is unavailable on this system.");
        }
    }
}
