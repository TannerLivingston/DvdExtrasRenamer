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
