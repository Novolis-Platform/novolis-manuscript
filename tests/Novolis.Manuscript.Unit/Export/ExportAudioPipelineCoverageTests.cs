using Novolis.Audio.Voice.EdgeTts;
using Novolis.Manuscript.Export.Audio;

namespace Novolis.Manuscript.Unit;

public sealed class ExportAudioPipelineCoverageTests
{
    static readonly byte[] TinyMp3 =
    [
        0xFF, 0xF3, 0x48, 0xC4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    ];

    [Test]
    public async Task ConcatenateMp3_with_gap_and_async_paths()
    {
        var empty = AudiobookAssembler.ConcatenateMp3([]);
        await Assert.That(empty.Length).IsEqualTo(0);
        var withEmptyPart = AudiobookAssembler.ConcatenateMp3([TinyMp3, [], TinyMp3], gapMs: 50);
        await Assert.That(withEmptyPart.Length).IsGreaterThan(TinyMp3.Length);

        var temp = Path.Combine(Path.GetTempPath(), $"ms-concat-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            var a = Path.Combine(temp, "a.mp3");
            var b = Path.Combine(temp, "b.mp3");
            await File.WriteAllBytesAsync(a, TinyMp3);
            await File.WriteAllBytesAsync(b, TinyMp3);
            var asyncEmpty = await AudiobookAssembler.ConcatenateMp3Async([]);
            await Assert.That(asyncEmpty.Length).IsEqualTo(0);
            var noGap = await AudiobookAssembler.ConcatenateMp3Async([a, b], gapMs: 0);
            await Assert.That(noGap.Length).IsEqualTo(TinyMp3.Length * 2);
            var withGap = await AudiobookAssembler.ConcatenateMp3Async([a, b], gapMs: 40);
            await Assert.That(withGap.Length).IsGreaterThan(noGap.Length);
            await Assert.That(async () => await AudiobookAssembler.ConcatenateMp3Async(
                    [Path.Combine(temp, "missing.mp3")]))
                .ThrowsExactly<FileNotFoundException>();
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Test]
    public async Task Pipeline_concat_cache_filter_and_scene_breaks()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"ms-pipe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            var ch1 = Path.Combine(temp, "ch01.md");
            var ch2 = Path.Combine(temp, "ch02.md");
            await File.WriteAllTextAsync(ch1, "# One\n\nHello world.\n\n***\n\nAfter break.\n");
            await File.WriteAllTextAsync(ch2, "# Two\n\nSecond chapter body.\n");

            var synth = new CountingSynthesizer();
            var pipeline = new AudiobookPipeline(synth);
            var outDir = Path.Combine(temp, "out");
            var chapters = new[]
            {
                new AudiobookChapterInput("ch01", "One", ch1),
                new AudiobookChapterInput("ch02", "Two", ch2),
            };
            var voice = VoiceSettings.FromProfile(
                EdgeVoiceProfiles.Narrator,
                new Dictionary<string, string> { ["Hello"] = "Hallo" });

            var first = await pipeline.GenerateAsync(
                "book",
                chapters,
                voice,
                new AudiobookOptions
                {
                    OutputDirectory = outDir,
                    AssembleMode = AudiobookAssembleMode.ConcatMp3,
                    ChapterGapMs = 30,
                    ParallelJobs = 2,
                });
            await Assert.That(first.ConcatenatedMp3Path).IsNotNull();
            await Assert.That(File.Exists(first.ConcatenatedMp3Path!)).IsTrue();
            await Assert.That(synth.Calls).IsGreaterThan(0);
            var callsAfterFirst = synth.Calls;

            var second = await pipeline.GenerateAsync(
                "book",
                chapters,
                voice,
                new AudiobookOptions
                {
                    OutputDirectory = outDir,
                    AssembleMode = AudiobookAssembleMode.None,
                    Force = false,
                });
            await Assert.That(second.ChapterPaths.Count).IsEqualTo(2);
            await Assert.That(synth.Calls).IsEqualTo(callsAfterFirst);

            await Assert.That(async () => await pipeline.GenerateAsync(
                    "book",
                    chapters,
                    voice,
                    new AudiobookOptions
                    {
                        OutputDirectory = Path.Combine(temp, "filtered"),
                        ChapterFilter = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "missing" },
                    }))
                .ThrowsExactly<InvalidOperationException>();

            await Assert.That(async () => await pipeline.GenerateAsync(
                    "book",
                    [],
                    voice,
                    new AudiobookOptions { OutputDirectory = Path.Combine(temp, "empty") }))
                .ThrowsExactly<ArgumentException>();

            var chapterProgress = new AudiobookChapterProgress
            {
                ChapterId = "ch01",
                Title = "One",
                State = AudiobookChapterState.Running,
                CompletedSegments = 1,
                TotalSegments = 3,
                Fraction = 0.3,
            };
            await Assert.That(chapterProgress.StatusLabel).IsEqualTo("1/3");
            await Assert.That(new AudiobookChapterProgress
            {
                ChapterId = "x",
                Title = "x",
                State = AudiobookChapterState.Pending,
            }.StatusLabel).IsEqualTo("pending");
            await Assert.That(new AudiobookChapterProgress
            {
                ChapterId = "x",
                Title = "x",
                State = AudiobookChapterState.Cached,
            }.StatusLabel).IsEqualTo("cached");
            await Assert.That(new AudiobookChapterProgress
            {
                ChapterId = "x",
                Title = "x",
                State = AudiobookChapterState.Failed,
            }.StatusLabel).IsEqualTo("failed");
            await Assert.That(new AudiobookChapterProgress
            {
                ChapterId = "x",
                Title = "x",
                State = AudiobookChapterState.Running,
            }.StatusLabel).IsEqualTo("starting…");
            await Assert.That(new AudiobookChapterProgress
            {
                ChapterId = "x",
                Title = "x",
                State = AudiobookChapterState.Completed,
            }.StatusLabel).IsEqualTo("done");
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Test]
    public async Task WriteM4bAsync_argument_validation()
    {
        await Assert.That(async () => await AudiobookAssembler.WriteM4bAsync(
                [],
                [],
                Path.Combine(Path.GetTempPath(), "x.m4b")))
            .ThrowsExactly<ArgumentException>();
        await Assert.That(async () => await AudiobookAssembler.WriteM4bAsync(
                ["a.mp3"],
                ["A", "B"],
                Path.Combine(Path.GetTempPath(), "x.m4b")))
            .ThrowsExactly<ArgumentException>();
    }

    sealed class CountingSynthesizer : ISynthesizer
    {
        public int Calls { get; private set; }

        public Task<byte[]> SynthesizeToMp3Async(
            string text,
            VoiceSettings settings,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(TinyMp3);
        }

        public Task SaveMp3Async(
            string text,
            string path,
            VoiceSettings settings,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return File.WriteAllBytesAsync(path, TinyMp3, cancellationToken);
        }
    }
}
