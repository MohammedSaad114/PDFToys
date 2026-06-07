using System.Collections.Generic;

namespace PDFToys.Core.Models;

public enum SplitPreset
{
    None,
    Half,
    Quarter
}

public sealed record SplitRange(int StartPage, int EndPage);

public sealed record SplitOptions(
    string OutputDirectory,
    int SplitEveryPages,
    IReadOnlyList<SplitRange>? CustomRanges = null,
    SplitPreset Preset = SplitPreset.None);