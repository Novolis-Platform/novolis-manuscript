using System.Text.RegularExpressions;

namespace Novolis.Manuscript.Protocol.Internal;

static partial class IdentifierRules
{
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex KebabCase();

    public static bool IsValidId(string name) =>
        !string.IsNullOrEmpty(name)
        && !ProtocolNames.ReservedDirectoryNames.Contains(name)
        && KebabCase().IsMatch(name);

    public static bool IsHiddenName(string name) =>
        name.StartsWith('_') || name.StartsWith('.');
}
