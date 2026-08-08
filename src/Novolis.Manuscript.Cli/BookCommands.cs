using System.Text.Json;
using Novolis.IO.Paths;
using Novolis.Manuscript;
using Novolis.Manuscript.IO;

namespace Novolis.Manuscript.Cli;

static class BookCommands
{
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Run(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var opts = BookCliOptions.Parse(args.Skip(1).ToArray());
        var root = ResolveWorkspace(opts.StartDir);

        var result = command switch
        {
            "list-books" => ListBooks(root, opts.Json),
            "doctor" => Doctor(root, opts),
            "validate-order" => ValidateOrder(root, opts),
            "validate-staged" => ValidateStaged(root, opts.Json),
            "sync-filenames" => Mutate(root, opts, (dir, apply) => LegacyChapterSurgery.SyncFilenames(dir, apply)),
            "promote-decimal" => Promote(root, opts),
            "insert-after" => InsertAfter(root, opts),
            "insert-between" => InsertBetween(root, opts),
            "ascii-scan" => AsciiScan(root, opts),
            _ => throw new InvalidOperationException($"Unknown book command: {command}"),
        };
        return result;
    }

    static int ListBooks(string root, bool json)
    {
        if (!ManuscriptWorkspace.TryOpen(root, out var ws) || ws is null)
            throw new InvalidOperationException($"Not a manuscript workspace: {root}");

        var rows = new List<object>();
        foreach (var series in ws.Catalog.Load(ws.ContentRoot))
        {
            foreach (var book in series.Books)
            {
                rows.Add(new
                {
                    series = series.Id,
                    book = book.Id,
                    title = book.Title,
                    chapters = book.Chapters.Count,
                    path = book.DirectoryPath,
                });
            }
        }

        foreach (var book in ws.Catalog.LoadStandaloneBooks(ws.ContentRoot))
        {
            rows.Add(new
            {
                series = (string?)null,
                book = book.Id,
                title = book.Title,
                chapters = book.Chapters.Count,
                path = book.DirectoryPath,
            });
        }

        WriteOk("list-books", $"{rows.Count} book(s).", rows, json);
        return 0;
    }

    static int Doctor(string root, BookCliOptions opts)
    {
        if (!ManuscriptWorkspace.TryOpen(root, out var ws) || ws is null)
            throw new InvalidOperationException($"Not a manuscript workspace: {root}");

        IReadOnlyList<DiagnosticFinding> findings;
        if (!string.IsNullOrWhiteSpace(opts.BookFile) || !string.IsNullOrWhiteSpace(opts.Series))
        {
            var book = ResolveBook(ws, opts);
            findings = ManuscriptDoctor.Diagnose(book);
        }
        else
        {
            findings = ManuscriptDoctor.Diagnose(ws.ContentRoot);
        }

        var errors = findings.Count(f => f.Severity == DiagnosticSeverity.Error);
        WritePayload(
            ok: errors == 0,
            "doctor",
            errors == 0 ? "Doctor clean." : $"{errors} error(s).",
            findings.Select(f => new { severity = f.Severity.ToString(), code = f.Code, message = f.Message, path = f.Path }),
            opts.Json);
        return errors == 0 ? 0 : 1;
    }

    static int ValidateOrder(string root, BookCliOptions opts)
    {
        if (!ManuscriptWorkspace.TryOpen(root, out var ws) || ws is null)
            throw new InvalidOperationException($"Not a manuscript workspace: {root}");

        var books = string.IsNullOrWhiteSpace(opts.BookFile) && string.IsNullOrWhiteSpace(opts.Series)
            ? ws.Catalog.Load(ws.ContentRoot).SelectMany(s => s.Books)
                .Concat(ws.Catalog.LoadStandaloneBooks(ws.ContentRoot)).ToList()
            : [ResolveBook(ws, opts)];

        var problems = new List<object>();
        foreach (var book in books)
        {
            var chaptersDir = ResolveChaptersDir(book.DirectoryPath);
            var seen = new HashSet<double>();
            foreach (var file in Directory.GetFiles(chaptersDir, "*.md"))
            {
                var key = ChapterOrder.GetFilenameSortKey(file);
                if (double.IsPositiveInfinity(key))
                    continue;
                if (!seen.Add(key))
                    problems.Add(new { book = book.Id, file = Path.GetFileName(file), issue = $"duplicate order {key}" });
            }
        }

        WritePayload(problems.Count == 0, "validate-order",
            problems.Count == 0 ? "Order OK." : $"{problems.Count} problem(s).",
            problems, opts.Json);
        return problems.Count == 0 ? 0 : 1;
    }

    static int ValidateStaged(string root, bool json)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("diff");
        psi.ArgumentList.Add("--cached");
        psi.ArgumentList.Add("--name-only");
        psi.ArgumentList.Add("--diff-filter=ACMR");
        using var process = System.Diagnostics.Process.Start(psi)
                            ?? throw new InvalidOperationException("Could not start git.");
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(process.StandardError.ReadToEnd());

        var staged = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Replace('\\', '/').Contains("/Chapters/", StringComparison.OrdinalIgnoreCase)
                        && p.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.GetFullPath(Path.Combine(root, p)))
            .Where(File.Exists)
            .ToList();

        if (staged.Count == 0)
        {
            WriteOk("validate-staged", "No staged chapter files.", Array.Empty<object>(), json);
            return 0;
        }

        var byDir = staged.GroupBy(f => Path.GetDirectoryName(f)!, StringComparer.OrdinalIgnoreCase);
        var problems = new List<object>();
        foreach (var group in byDir)
        {
            var seen = new Dictionary<double, string>();
            foreach (var file in Directory.GetFiles(group.Key, "*.md"))
            {
                var key = ChapterOrder.GetFilenameSortKey(file);
                if (double.IsPositiveInfinity(key))
                    continue;
                if (seen.TryGetValue(key, out var prior))
                    problems.Add(new { dir = group.Key, key, a = Path.GetFileName(prior), b = Path.GetFileName(file) });
                else
                    seen[key] = file;
            }
        }

        WritePayload(problems.Count == 0, "validate-staged",
            problems.Count == 0 ? "Staged chapter order valid." : $"{problems.Count} conflict(s).",
            problems, json);
        return problems.Count == 0 ? 0 : 1;
    }

    static int Mutate(string root, BookCliOptions opts, Func<string, bool, ChapterMutationResult> action)
    {
        RequireMutation(opts);
        if (!ManuscriptWorkspace.TryOpen(root, out var ws) || ws is null)
            throw new InvalidOperationException($"Not a manuscript workspace: {root}");
        var book = ResolveBook(ws, opts);
        var chaptersDir = ResolveChaptersDir(book.DirectoryPath);
        var result = action(chaptersDir, opts.Apply);
        WritePayload(result.Applied || opts.DryRun, "mutate", result.Message, result.Plan, opts.Json);
        return 0;
    }

    static int Promote(string root, BookCliOptions opts)
    {
        if (opts.From is null || opts.To is null)
            throw new InvalidOperationException("promote-decimal requires --from and --to.");
        return Mutate(root, opts, (dir, apply) =>
            LegacyChapterSurgery.PromoteDecimal(dir, opts.From.Value, opts.To.Value, apply));
    }

    static int InsertAfter(string root, BookCliOptions opts)
    {
        if (opts.After is null || string.IsNullOrWhiteSpace(opts.Title))
            throw new InvalidOperationException("insert-after requires --after and --title.");
        return Mutate(root, opts, (dir, apply) =>
            LegacyChapterSurgery.InsertAfter(dir, opts.After.Value, opts.Title!, apply));
    }

    static int InsertBetween(string root, BookCliOptions opts)
    {
        if (opts.Key is null || string.IsNullOrWhiteSpace(opts.Title))
            throw new InvalidOperationException("insert-between requires --key and --title.");
        return Mutate(root, opts, (dir, apply) =>
            LegacyChapterSurgery.InsertBetween(dir, opts.Key.Value, opts.Title!, apply));
    }

    static int AsciiScan(string root, BookCliOptions opts)
    {
        if (!ManuscriptWorkspace.TryOpen(root, out var ws) || ws is null)
            throw new InvalidOperationException($"Not a manuscript workspace: {root}");
        var book = ResolveBook(ws, opts);
        var chaptersDir = ResolveChaptersDir(book.DirectoryPath);
        var issues = new List<object>();
        foreach (var file in Directory.GetFiles(chaptersDir, "*.md"))
        {
            var text = File.ReadAllText(file);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c is '\n' or '\r' or '\t')
                    continue;
                if (c < 32 || c > 126)
                {
                    issues.Add(new { file = Path.GetFileName(file), index = i, codepoint = $"U+{(int)c:X4}" });
                    if (issues.Count >= 100)
                        break;
                }
            }

            if (issues.Count >= 100)
                break;
        }

        WritePayload(issues.Count == 0, "ascii-scan",
            issues.Count == 0 ? "ASCII OK." : $"{issues.Count} issue(s).",
            issues, opts.Json);
        return issues.Count == 0 ? 0 : 1;
    }

    static BookInfo ResolveBook(ManuscriptWorkspace ws, BookCliOptions opts)
    {
        if (!string.IsNullOrWhiteSpace(opts.BookFile))
        {
            var bookDir = Path.GetDirectoryName(Path.GetFullPath(opts.BookFile))!;
            var id = Path.GetFileName(bookDir);
            return ws.Catalog.FindBook(ws.ContentRoot, opts.Series, id)
                   ?? LoadBookFallback(bookDir, opts.Series);
        }

        if (!string.IsNullOrWhiteSpace(opts.Series) && !string.IsNullOrWhiteSpace(opts.Book))
        {
            return ws.Catalog.FindBook(ws.ContentRoot, opts.Series, opts.Book)
                   ?? throw new FileNotFoundException($"Book not found: {opts.Series}/{opts.Book}");
        }

        if (!string.IsNullOrWhiteSpace(opts.ChaptersDir))
        {
            var bookDir = Directory.GetParent(Path.GetFullPath(opts.ChaptersDir))!.FullName;
            return LoadBookFallback(bookDir, opts.Series);
        }

        throw new InvalidOperationException("Select a book with --book-file, --chapters-dir, or --series/--book.");
    }

    static BookInfo LoadBookFallback(string bookDir, string? seriesId) =>
        ManuscriptCatalog.LoadBookDirectory(bookDir, seriesId);

    static string ResolveChaptersDir(string bookDir)
    {
        foreach (var name in new[] { "Chapters", "chapters" })
        {
            var path = Path.Combine(bookDir, name);
            if (Directory.Exists(path))
                return path;
        }

        throw new DirectoryNotFoundException($"Chapters directory not found under {bookDir}");
    }

    static string ResolveWorkspace(string? start)
    {
        var dir = string.IsNullOrWhiteSpace(start) ? Directory.GetCurrentDirectory() : Path.GetFullPath(start);
        if (ManuscriptWorkspace.TryOpen(dir, out var ws) && ws is not null)
            return ws.ContentRoot;
        if (RootFinder.TryFind(dir, d => File.Exists(Path.Combine(d.FullName, "manuscript.yaml")), out var root))
            return root;
        throw new InvalidOperationException($"Could not find manuscript workspace from {dir}");
    }

    static void RequireMutation(BookCliOptions opts)
    {
        if (!opts.Apply && !opts.DryRun)
            throw new InvalidOperationException("Mutating commands require --dry-run or --apply.");
    }

    static void WriteOk(string command, string message, object? data, bool json) =>
        WritePayload(true, command, message, data, json);

    static void WritePayload(bool ok, string command, string message, object? data, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { ok, command, message, data }, JsonOptions));
            return;
        }

        Console.WriteLine(ok ? $"OK: {message}" : $"FAIL: {message}");
        if (data is not null && !ok)
            Console.WriteLine(JsonSerializer.Serialize(data, JsonOptions));
    }

    static bool IsHelp(string value) => value is "-h" or "--help" or "help";

    static void PrintHelp()
    {
        Console.WriteLine("""
            novolis-manuscript book <command>

            Commands:
              list-books
              doctor
              validate-order
              validate-staged
              sync-filenames [--dry-run|--apply]
              promote-decimal --from N --to N [--dry-run|--apply]
              insert-after --after N --title "..." [--dry-run|--apply]
              insert-between --key N.N --title "..." [--dry-run|--apply]
              ascii-scan

            Book selection:
              -b, --book-file PATH
              --chapters-dir PATH
              --series ID --book ID
              --json
            """);
    }
}

sealed class BookCliOptions
{
    public string? StartDir { get; init; }
    public string? BookFile { get; init; }
    public string? ChaptersDir { get; init; }
    public string? Series { get; init; }
    public string? Book { get; init; }
    public string? Title { get; init; }
    public double? After { get; init; }
    public double? Key { get; init; }
    public double? From { get; init; }
    public double? To { get; init; }
    public bool Apply { get; init; }
    public bool DryRun { get; init; }
    public bool Json { get; init; }

    public static BookCliOptions Parse(string[] args)
    {
        string? start = null, bookFile = null, chapters = null, series = null, book = null, title = null;
        double? after = null, key = null, from = null, to = null;
        var apply = false;
        var dry = false;
        var json = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Need() => i + 1 < args.Length ? args[++i] : throw new InvalidOperationException($"Missing value for {a}");
            switch (a)
            {
                case "--root":
                case "--workspace":
                    start = Need();
                    break;
                case "-b":
                case "--book-file":
                    bookFile = Need();
                    break;
                case "--chapters-dir":
                    chapters = Need();
                    break;
                case "--series":
                    series = Need();
                    break;
                case "--book":
                    book = Need();
                    break;
                case "--title":
                    title = Need();
                    break;
                case "--after":
                    after = double.Parse(Need(), System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--key":
                    key = double.Parse(Need(), System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--from":
                    from = double.Parse(Need(), System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--to":
                    to = double.Parse(Need(), System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--apply":
                    apply = true;
                    break;
                case "--dry-run":
                    dry = true;
                    break;
                case "--json":
                    json = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown option: {a}");
            }
        }

        return new BookCliOptions
        {
            StartDir = start,
            BookFile = bookFile,
            ChaptersDir = chapters,
            Series = series,
            Book = book,
            Title = title,
            After = after,
            Key = key,
            From = from,
            To = to,
            Apply = apply,
            DryRun = dry,
            Json = json,
        };
    }
}
