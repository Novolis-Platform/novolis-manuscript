namespace Novolis.Manuscript.Export.Audio;

/// <summary>How chapter MP3s are assembled after synthesis.</summary>
public enum AudiobookAssembleMode
{
    /// <summary>Leave per-chapter MP3s only.</summary>
    None,

    /// <summary>Concatenate chapter MP3s into a single book MP3.</summary>
    ConcatMp3,

    /// <summary>Encode chapter MP3s into an M4B audiobook.</summary>
    M4b,

    /// <summary>Produce both concatenated MP3 and M4B.</summary>
    Both,
}
