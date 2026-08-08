namespace Novolis.Manuscript.Export.Audio;

/// <summary>Plays MP3 audio bytes for manuscript speech preview.</summary>
public interface IAudioPlayer
{
    /// <summary>Plays MP3 bytes until finished or cancelled.</summary>
    Task PlayAsync(byte[] mp3, CancellationToken cancellationToken = default);

    /// <summary>Stops any in-flight playback.</summary>
    void Stop();
}
