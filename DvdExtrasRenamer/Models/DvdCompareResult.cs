using System.Collections.Generic;

namespace DvdExtrasRenamer.Models;

public record DvdCompareResult(
    string Title,
    string Href,
    string Winner
);

public record DvdExtras(
    string Title,
    string Duration
);

/// <summary>
/// Represents a specific release of a DVD with its associated extras.
/// </summary>
public class DvdRelease
{
    public string Name { get; set; } = string.Empty;
    public List<DvdExtras> Extras { get; set; } = new();
}
