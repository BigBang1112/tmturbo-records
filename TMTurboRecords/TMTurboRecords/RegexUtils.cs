﻿using System.Text.RegularExpressions;

namespace TMTurboRecords;

internal static partial class RegexUtils
{
    [GeneratedRegex(@"^[a-zA-Z0-9_]{26,27}$")]
    public static partial Regex MapUidRegex();

    [GeneratedRegex(@"execution time *: *(\d*\.?\d*) *s")]
    public static partial Regex ExecutionTimeRegex();
}
