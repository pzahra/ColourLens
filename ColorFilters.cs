namespace ColourLens;

internal enum Deficiency { None, Protan, Deutan, Tritan, Achromat }

internal enum FilterView
{
    /// <summary>Show how the screen looks with the deficiency.</summary>
    Simulate,

    /// <summary>Recolour the screen so someone with the deficiency can tell colours apart.</summary>
    Correct,

    /// <summary>Correct, then simulate: what the corrected screen looks like with the deficiency.</summary>
    Check,
}

internal static class ColorFilters
{
    private static readonly float[,] Identity = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

    // Machado, Oliveira & Fernandes (2009), "A Physiologically-based Model for Simulation of
    // Color Vision Deficiency", severity 1.0. Row-major, column-vector form: rgb' = M · rgb.
    private static readonly float[,] Protanopia =
    {
        {  0.152286f,  1.052583f, -0.204868f },
        {  0.114503f,  0.786281f,  0.099216f },
        { -0.003882f, -0.048116f,  1.051998f },
    };

    private static readonly float[,] Deuteranopia =
    {
        {  0.367322f,  0.860646f, -0.227968f },
        {  0.280085f,  0.672501f,  0.047413f },
        { -0.011820f,  0.042940f,  0.968881f },
    };

    private static readonly float[,] Tritanopia =
    {
        {  1.255528f, -0.076749f, -0.178779f },
        { -0.078411f,  0.930809f,  0.147602f },
        {  0.004733f,  0.691367f,  0.303900f },
    };

    // Every channel becomes the Rec. 601 luma, i.e. brightness only.
    private static readonly float[,] Monochromacy =
    {
        { 0.299f, 0.587f, 0.114f },
        { 0.299f, 0.587f, 0.114f },
        { 0.299f, 0.587f, 0.114f },
    };

    // Daltonisation (Fidaner, Lin & Ozguven 2005): corrected = rgb + Shift · (rgb − Sim · rgb).
    // The error term is what the viewer loses; Shift moves it into channels they can still see.
    // Red/green loss goes into green and blue.
    private static readonly float[,] RedGreenShift =
    {
        { 0.0f, 0, 0 },
        { 0.7f, 1, 0 },
        { 0.7f, 0, 1 },
    };

    // Blue/yellow loss goes onto the red-green axis, which tritans still see. (Fidaner's
    // red/green shift barely helps tritans.)
    private static readonly float[,] BlueShift =
    {
        { 1, 0,  0.7f },
        { 0, 1, -0.7f },
        { 0, 0,  0.0f },
    };

    public static bool CanCorrect(Deficiency kind) => kind is Deficiency.Protan or Deficiency.Deutan or Deficiency.Tritan;

    /// <summary>
    /// Builds a MAGCOLOREFFECT (5×5, row = input channel, column = output channel). Severity
    /// below 1 blends the simulation linearly towards normal vision, approximating the anomalous
    /// (-omaly) forms; correction targets that same severity.
    /// </summary>
    public static float[] Effect(Deficiency kind, FilterView view, float severity)
    {
        var m = Matrix(kind, view, severity);
        var effect = new float[25];
        // MAGCOLOREFFECT is row-vector form (rgb' = rgb · T), so T is the transpose of M.
        for (int input = 0; input < 3; input++)
            for (int output = 0; output < 3; output++)
                effect[input * 5 + output] = m[output, input];
        effect[3 * 5 + 3] = 1f; // alpha
        effect[4 * 5 + 4] = 1f; // homogeneous
        return effect;
    }

    private static float[,] Matrix(Deficiency kind, FilterView view, float severity)
    {
        var simulation = Simulation(kind, Math.Clamp(severity, 0f, 1f));
        if (view == FilterView.Simulate || !CanCorrect(kind))
            return simulation;

        var shift = kind == Deficiency.Tritan ? BlueShift : RedGreenShift;
        var correction = Add(Identity, Multiply(shift, Subtract(Identity, simulation)));
        return view == FilterView.Correct ? correction : Multiply(simulation, correction);
    }

    private static float[,] Simulation(Deficiency kind, float severity)
    {
        var full = kind switch
        {
            Deficiency.Protan => Protanopia,
            Deficiency.Deutan => Deuteranopia,
            Deficiency.Tritan => Tritanopia,
            Deficiency.Achromat => Monochromacy,
            _ => Identity,
        };
        // Identity + s · (M − Identity)
        return Add(Identity, Scale(Subtract(full, Identity), severity));
    }

    public static string Name(Deficiency kind, float severity)
    {
        bool full = severity >= 0.995f;
        return kind switch
        {
            Deficiency.Protan => full ? "Protanopia" : "Protanomaly",
            Deficiency.Deutan => full ? "Deuteranopia" : "Deuteranomaly",
            Deficiency.Tritan => full ? "Tritanopia" : "Tritanomaly",
            Deficiency.Achromat => full ? "Achromatopsia" : "Achromatomaly",
            _ => "Normal vision",
        };
    }

    public static string MenuText(Deficiency kind) => kind switch
    {
        Deficiency.Protan => "Protan — red-blind / red-weak",
        Deficiency.Deutan => "Deutan — green-blind / green-weak (most common)",
        Deficiency.Tritan => "Tritan — blue-blind / blue-weak",
        Deficiency.Achromat => "Achromat — no colour vision",
        _ => "Off — normal vision",
    };

    public static string Label(FilterView view) => view.ToString();

    public static string MenuText(FilterView view) => view switch
    {
        FilterView.Correct => "Correct — recolour to help someone with it",
        FilterView.Check => "Check — corrected, as seen with the deficiency",
        _ => "Simulate — how it looks with the deficiency",
    };

    private static float[,] Multiply(float[,] a, float[,] b)
    {
        var r = new float[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                for (int k = 0; k < 3; k++)
                    r[i, j] += a[i, k] * b[k, j];
        return r;
    }

    private static float[,] Add(float[,] a, float[,] b) => Map(a, b, (x, y) => x + y);

    private static float[,] Subtract(float[,] a, float[,] b) => Map(a, b, (x, y) => x - y);

    private static float[,] Scale(float[,] a, float s) => Map(a, a, (x, _) => x * s);

    private static float[,] Map(float[,] a, float[,] b, Func<float, float, float> f)
    {
        var r = new float[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                r[i, j] = f(a[i, j], b[i, j]);
        return r;
    }
}
