using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Novolis.Manuscript.IO;

/// <summary>Result of a chapter-tree mutation (apply or dry-run).</summary>
public sealed class ChapterMutationResult
{
    /// <summary>Creates a mutation result.</summary>
    public ChapterMutationResult(bool applied, string message, object? plan = null)
    {
        Applied = applied;
        Message = message;
        Plan = plan;
    }

    /// <summary>Whether changes were written to disk.</summary>
    public bool Applied { get; }

    /// <summary>Human-readable status.</summary>
    public string Message { get; }

    /// <summary>Optional plan payload for dry-run inspection.</summary>
    public object? Plan { get; }
}

/// <summary>
/// Insert / promote / sync operations against chapter folders (<c>Chapters/</c> NMP/1 or legacy <c>chapters/</c>).
/// Ported from the books repo <c>book-tool</c>.
/// </summary>
public static class LegacyChapterSurgery
{
    static readonly Regex BooktoolsComment = new(@"<!--\s*booktools-chapter:\s*([\d.]+)\s*-->", RegexOptions.Compiled);
    static readonly Regex FrontMatter = new(@"(?s)^---\s*\r?\n(.*?)\r?\n---\s*\r?\n", RegexOptions.Compiled);
    static readonly Regex YamlChapter = new(@"^\s*chapter:\s*([\d.]+)\s*(?:#.*)?$", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex HeadingChapter = new(@"^\s*#\s*Chapter\s+(\d+(?:\.\d+)?)\s*-\s*(.+)\s*$", RegexOptions.Compiled);
    static readonly Regex HeadingTitleOnly = new(@"^\s*#\s*Chapter\s+\d+(?:\.\d+)?\s*-\s*(.+)\s*$", RegexOptions.Compiled);

    /// <summary>Inserts a new integer chapter after an anchor key, bumping later keys.</summary>
    public static ChapterMutationResult InsertAfter(string chaptersDir, double afterKey, string title, bool apply)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaptersDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var rows = LoadRows(chaptersDir);
        var anchor = FindAnchor(rows, afterKey)
            ?? throw new InvalidOperationException($"No anchor for key {FormatKey(afterKey)}.");
        var newKey = Math.Floor(anchor.Meta.Key) + 1.0;
        var bumpPlan = rows.Where(r => r.Meta.Key >= newKey - 1e-9)
            .OrderByDescending(r => r.Meta.Key)
            .Select(r => new KeyPlanItem(r, r.Meta.Key + 1.0))
            .ToList();
        var newFileName = FileNameFor(rows.Select(r => r.Meta.Key).Append(newKey).Max(), newKey, title);
        var newPath = Path.Combine(chaptersDir, newFileName);
        if (File.Exists(newPath))
            throw new InvalidOperationException($"Target exists: {newFileName}");

        if (apply)
        {
            ApplyKeyPlan(bumpPlan);
            ApplyRenamePlan(BuildSyncPlan(chaptersDir, LoadRows(chaptersDir)));
            WriteChapterStub(chaptersDir, newKey, title, AnyBooktoolsTag(chaptersDir));
            ApplyRenamePlan(BuildSyncPlan(chaptersDir, LoadRows(chaptersDir)));
        }

        return new ChapterMutationResult(apply, apply ? "Chapter inserted." : "Insert-after dry run.", new
        {
            newKey = FormatKey(newKey),
            title,
            bumps = PlanData(bumpPlan),
        });
    }

    /// <summary>Inserts a decimal chapter key without bumping neighbors.</summary>
    public static ChapterMutationResult InsertBetween(string chaptersDir, double key, string title, bool apply)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaptersDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var rows = LoadRows(chaptersDir);
        if (rows.Any(r => NearlyEqual(r.Meta.Key, key)))
            throw new InvalidOperationException($"Key already exists: {FormatKey(key)}");
        var fileName = FileNameFor(rows.Select(r => r.Meta.Key).Append(key).Max(), key, title);
        if (apply)
            WriteChapterStub(chaptersDir, key, title, includeBooktoolsTag: true);

        return new ChapterMutationResult(apply, apply ? "Decimal chapter inserted." : "Insert-between dry run.", new
        {
            key = FormatKey(key),
            title,
            fileName,
        });
    }

    /// <summary>Promotes a decimal key to an integer, bumping later chapters.</summary>
    public static ChapterMutationResult PromoteDecimal(string chaptersDir, double fromKey, double toKey, bool apply)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaptersDir);
        if (!IsInteger(toKey))
            throw new InvalidOperationException("toKey must be an integer.");
        var plan = BuildPromotePlan(chaptersDir, fromKey, toKey);
        if (apply)
        {
            ApplyKeyPlan(plan);
            ApplyRenamePlan(BuildSyncPlan(chaptersDir, LoadRows(chaptersDir)));
        }

        return new ChapterMutationResult(apply, apply ? "Decimal promotion applied." : "Decimal promotion dry run.", PlanData(plan));
    }

    /// <summary>Renames chapter files to match sort keys and titles.</summary>
    public static ChapterMutationResult SyncFilenames(string chaptersDir, bool apply)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaptersDir);
        var rows = LoadRows(chaptersDir);
        var plan = BuildSyncPlan(chaptersDir, rows);
        var changes = plan.Where(p => !SamePath(p.Source, p.Destination)).ToList();
        if (apply)
            ApplyRenamePlan(plan);
        var data = changes.Select(c => new { from = Path.GetFileName(c.Source), to = Path.GetFileName(c.Destination) }).ToList();
        return new ChapterMutationResult(apply, apply ? "Filename sync applied." : "Filename sync dry run.", data);
    }

    static List<object> PlanData(List<KeyPlanItem> plan) =>
        plan.OrderBy(p => p.Row.Meta.Key).Select(p => (object)new
        {
            file = p.Row.Name,
            from = FormatKey(p.Row.Meta.Key),
            to = FormatKey(p.NewKey),
        }).ToList();

    static List<ChapterRow> LoadRows(string chaptersDir) =>
        Directory.GetFiles(chaptersDir, "*.md", SearchOption.TopDirectoryOnly)
            .Select(f => new ChapterRow(f, Path.GetFileName(f), GetMetadata(f)))
            .ToList();

    static ChapterRow? FindAnchor(List<ChapterRow> rows, double key)
    {
        var exact = rows.FirstOrDefault(r => NearlyEqual(r.Meta.Key, key));
        if (exact != null)
            return exact;
        var floor = (int)Math.Floor(key + 1e-9);
        return rows.Where(r => (int)Math.Floor(r.Meta.Key + 1e-9) == floor).OrderByDescending(r => r.Meta.Key).FirstOrDefault();
    }

    static List<KeyPlanItem> BuildPromotePlan(string chaptersDir, double fromKey, double toKey)
    {
        var rows = LoadRows(chaptersDir).Where(r => r.Meta.Key < double.PositiveInfinity).ToList();
        if (!rows.Any(r => NearlyEqual(r.Meta.Key, fromKey)))
            throw new InvalidOperationException($"No chapter with sort key {FormatKey(fromKey)}.");
        var plan = rows.Where(r => NearlyEqual(r.Meta.Key, fromKey) || r.Meta.Key >= toKey - 1e-9)
            .OrderByDescending(r => r.Meta.Key)
            .Select(r => new KeyPlanItem(r, NearlyEqual(r.Meta.Key, fromKey) ? toKey : r.Meta.Key + 1.0))
            .ToList();
        var duplicates = plan.GroupBy(p => p.NewKey).Where(g => g.Count() > 1).ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException($"Planned duplicate keys: {string.Join(", ", duplicates.Select(g => FormatKey(g.Key)))}");
        return plan;
    }

    static void ApplyKeyPlan(List<KeyPlanItem> plan)
    {
        foreach (var item in plan)
        {
            var raw = File.ReadAllText(item.Row.Path);
            var updated = RewriteSortKey(raw, item.NewKey);
            File.WriteAllText(item.Row.Path, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    static List<RenamePlanItem> BuildSyncPlan(string chaptersDir, List<ChapterRow> rows)
    {
        var parsed = rows.Where(r => r.Meta.Key < double.PositiveInfinity).ToList();
        if (parsed.Count == 0)
            return [];
        var maxWhole = parsed.Select(r => (int)Math.Floor(r.Meta.Key)).Max();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var plan = new List<RenamePlanItem>();
        foreach (var row in parsed.OrderBy(r => r.Meta.Key).ThenBy(r => r.Name, StringComparer.Ordinal))
        {
            var stem = GetChapterMarkdownStem(row.Meta.Key, row.Meta.Title, maxWhole);
            var target = stem + ".md";
            var baseStem = stem;
            var suffix = 2;
            while (!used.Add(target))
                target = $"{baseStem}-{suffix++}.md";
            plan.Add(new RenamePlanItem(row.Path, Path.Combine(chaptersDir, target)));
        }

        return plan;
    }

    static void ApplyRenamePlan(List<RenamePlanItem> plan)
    {
        var moves = plan.Where(p => !SamePath(p.Source, p.Destination)).ToList();
        var tempMoves = new List<(string Temp, string Destination)>();
        foreach (var item in moves)
        {
            var temp = item.Source + ".ms-io-tmp-" + Guid.NewGuid().ToString("N");
            File.Move(item.Source, temp);
            tempMoves.Add((temp, item.Destination));
        }

        foreach (var (temp, destination) in tempMoves)
        {
            if (File.Exists(destination))
                throw new InvalidOperationException($"Target exists: {destination}");
            File.Move(temp, destination);
        }
    }

    static void WriteChapterStub(string chaptersDir, double key, string title, bool includeBooktoolsTag)
    {
        var rows = LoadRows(chaptersDir);
        var fileName = FileNameFor(rows.Select(r => r.Meta.Key).Where(k => k < double.PositiveInfinity).Append(key).Max(), key, title);
        var path = Path.Combine(chaptersDir, fileName);
        if (File.Exists(path))
            throw new InvalidOperationException($"File already exists: {fileName}");
        var body = new StringBuilder();
        if (includeBooktoolsTag)
        {
            body.AppendLine($"<!-- booktools-chapter: {FormatKey(key)} -->");
            body.AppendLine();
        }

        body.AppendLine($"# Chapter {FormatKey(key)} - {title}");
        body.AppendLine();
        File.WriteAllText(path, body.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    static string FileNameFor(double maxKey, double key, string title) =>
        GetChapterMarkdownStem(key, title, Math.Max((int)Math.Floor(maxKey), (int)Math.Floor(key))) + ".md";

    static bool AnyBooktoolsTag(string chaptersDir) =>
        Directory.GetFiles(chaptersDir, "*.md", SearchOption.TopDirectoryOnly).Any(f =>
            File.ReadLines(f).Take(25).Any(line => BooktoolsComment.IsMatch(line)));

    static string FormatKey(double key)
    {
        var floor = Math.Floor(key);
        return Math.Abs(key - floor) < 1e-9
            ? ((int)floor).ToString(CultureInfo.InvariantCulture)
            : key.ToString(CultureInfo.InvariantCulture);
    }

    static ChapterMeta GetMetadata(string filePath)
    {
        var name = Path.GetFileName(filePath);
        if (name.Equals("00-frontmatter.md", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("-frontmatter.md", StringComparison.OrdinalIgnoreCase))
            return new ChapterMeta(-1, "frontmatter-filename", GetHeadingTitle(filePath));

        var raw = File.ReadAllText(filePath);
        if (string.IsNullOrEmpty(raw))
            return new ChapterMeta(double.PositiveInfinity, "empty", null);
        if (raw.StartsWith('\uFEFF'))
            raw = raw[1..];
        var lines = raw.Split(["\r\n", "\n"], StringSplitOptions.None);

        foreach (var line in lines.Take(20))
        {
            var match = BooktoolsComment.Match(line);
            if (match.Success)
                return new ChapterMeta(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), "booktools-comment", GetHeadingTitle(filePath));
        }

        var frontMatter = FrontMatter.Match(raw);
        if (frontMatter.Success)
        {
            foreach (var line in frontMatter.Groups[1].Value.Split(["\r\n", "\n"], StringSplitOptions.None))
            {
                var match = YamlChapter.Match(line);
                if (match.Success)
                    return new ChapterMeta(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), "yaml", GetHeadingTitle(filePath));
            }
        }

        foreach (var line in lines)
        {
            var match = HeadingChapter.Match(line);
            if (match.Success)
                return new ChapterMeta(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), "heading", match.Groups[2].Value.Trim());
        }

        return new ChapterMeta(double.PositiveInfinity, "none", null);
    }

    static string? GetHeadingTitle(string filePath)
    {
        foreach (var line in File.ReadLines(filePath))
        {
            var match = HeadingTitleOnly.Match(line);
            if (match.Success)
                return match.Groups[1].Value.Trim();
        }

        return null;
    }

    static string RewriteSortKey(string raw, double newKey)
    {
        if (raw.StartsWith('\uFEFF'))
            raw = raw[1..];
        var lines = raw.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
        for (var i = 0; i < Math.Min(35, lines.Count); i++)
        {
            if (BooktoolsComment.IsMatch(lines[i]))
                lines[i] = $"<!-- booktools-chapter: {FormatKey(newKey)} -->";
        }

        var text = string.Join("\n", lines);
        var frontMatter = FrontMatter.Match(text);
        if (frontMatter.Success)
        {
            var inner = frontMatter.Groups[1].Value;
            var yaml = Regex.Match(inner, @"^(?m)(\s*chapter:\s*)[\d.]+(\s*)$", RegexOptions.Multiline);
            if (yaml.Success)
            {
                var replacement = inner[..yaml.Index]
                                  + $"{yaml.Groups[1].Value}{FormatKey(newKey)}{yaml.Groups[2].Value}"
                                  + inner[(yaml.Index + yaml.Length)..];
                text = text[..frontMatter.Index] + "---\n" + replacement + "\n---" + text[(frontMatter.Index + frontMatter.Length)..];
            }
        }

        var heading = Regex.Match(text, @"^(\s*#\s*Chapter\s+)\d+(?:\.\d+)?(\s*-\s*.+)$", RegexOptions.Multiline);
        if (heading.Success)
            text = text[..heading.Index] + $"{heading.Groups[1].Value}{FormatKey(newKey)}{heading.Groups[2].Value}" + text[(heading.Index + heading.Length)..];
        return text;
    }

    static string GetChapterMarkdownStem(double key, string? title, int maxWholeChapter)
    {
        if (key < 0)
            return "00-frontmatter";
        var slug = ConvertToKebabSlug(title);
        var width = Math.Max(2, maxWholeChapter.ToString(CultureInfo.InvariantCulture).Length);
        var whole = (int)Math.Floor(key);
        var wholePadded = whole.ToString("D" + width);
        if (Math.Abs(key - whole) < 1e-9)
            return $"{wholePadded}-{slug}";
        var fraction = Math.Clamp((int)Math.Round((key - whole) * 1000), 1, 999).ToString("D3");
        return $"{wholePadded}-{fraction}-{slug}";
    }

    static string ConvertToKebabSlug(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "chapter";
        var slug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return slug.Length > 0 ? slug : "chapter";
    }

    static bool IsInteger(double value) => Math.Abs(value - Math.Round(value)) < 1e-9;
    static bool NearlyEqual(double left, double right) => Math.Abs(left - right) < 1e-9;
    static bool SamePath(string left, string right) => Path.GetFullPath(left).Equals(Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);

    sealed record ChapterMeta(double Key, string Source, string? Title);
    sealed record ChapterRow(string Path, string Name, ChapterMeta Meta);
    sealed record RenamePlanItem(string Source, string Destination);
    sealed record KeyPlanItem(ChapterRow Row, double NewKey);
}
