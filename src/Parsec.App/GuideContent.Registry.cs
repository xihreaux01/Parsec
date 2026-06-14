using System.Collections.Generic;

namespace Parsec.App;

public static partial class FractalGuide
{
    // Filled in Task 7. Temporary minimal stub so the builder compiles and tests run.
    public static GuideContent Resolve(FractalType type, int deepFormula) => new()
    {
        Title = type.ToString(),
        WhatItIs = new[] { "" },
        HowComputed = new[] { "" },
        BestResults = new[] { "" },
    };
}
