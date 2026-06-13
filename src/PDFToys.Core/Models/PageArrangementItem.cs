namespace PDFToys.Core.Models;

/// <summary>
/// A source page reference and optional rotation.
/// </summary>
/// <param name="SourcePageIndex">0-based index of the source page.</param>
/// <param name="RotationDegrees">Clockwise rotation in degrees (0, 90, 180, or 270).</param>
public sealed record PageArrangementItem(int SourcePageIndex, int RotationDegrees = 0);
