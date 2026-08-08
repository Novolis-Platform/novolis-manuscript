namespace Novolis.Manuscript.Protocol.Internal;

static class WorkspaceLocator
{
    public static string LocateRoot(string startPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startPath);

        var current = Path.GetFullPath(startPath);
        if (File.Exists(current) && Path.GetFileName(current).Equals(ProtocolNames.WorkspaceMarker, StringComparison.Ordinal))
            return Path.GetDirectoryName(current)!;

        if (File.Exists(current))
            current = Path.GetDirectoryName(current) ?? current;

        while (true)
        {
            var marker = Path.Combine(current, ProtocolNames.WorkspaceMarker);
            if (File.Exists(marker))
                return current;

            var parent = Directory.GetParent(current);
            if (parent is null)
                throw new InvalidOperationException(
                    $"No {ProtocolNames.WorkspaceMarker} found walking upward from '{startPath}'.");

            current = parent.FullName;
        }
    }
}
