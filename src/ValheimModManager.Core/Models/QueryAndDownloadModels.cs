namespace ValheimModManager.Core.Models;

using System;
using System.Collections.Generic;

public sealed record DownloadContext(
    Dictionary<string, string>? ExtraParameters = null
);

public sealed record DownloadTicket(
    Uri DownloadUri,
    string FileName,
    long ExpectedSize = 0,
    Dictionary<string, string>? Headers = null
);

public sealed record ModQuery(
    string? SearchText = null,
    string? Category = null,
    string? Author = null,
    string? ProviderId = null,
    bool IncludeDeprecated = false,
    int PageIndex = 0,
    int PageSize = 50
);
