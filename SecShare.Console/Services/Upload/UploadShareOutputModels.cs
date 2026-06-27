using SecShare.Console.Enums;

namespace SecShare.Console.Services.Upload;

public sealed record UploadShareOutputLine(IReadOnlyList<UploadShareOutputSegment> Segments);

public sealed record UploadShareOutputSegment(string Text, UploadShareOutputStyle Style);
