namespace ColourLens;

internal enum Deficiency { None, Protan, Deutan, Tritan, Achromat }

internal static class ColorFilters
{
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

    /// <summary>
    /// Builds a MAGCOLOREFFECT for the deficiency. Severity below 1 blends linearly towards normal
    /// vision, approximating the anomalous (-omaly) forms.
    /// </summary>
    public static float[] Effect(Deficiency kind, float severity)
    {
        var effect = new float[25];
        for (int i = 0; i < 5; i++)
            effect[i * 5 + i] = 1f;

        var matrix = kind switch
        {
            Deficiency.Protan => Protanopia,
            Deficiency.Deutan => Deuteranopia,
            Deficiency.Tritan => Tritanopia,
            Deficiency.Achromat => Monochromacy,
            _ => null,
        };
        if (matrix is null)
            return effect;

        float s = Math.Clamp(severity, 0f, 1f);
        // MAGCOLOREFFECT is row-vector form (rgb' = rgb · T), so T is the transpose of M.
        for (int input = 0; input < 3; input++)
            for (int output = 0; output < 3; output++)
            {
                float identity = input == output ? 1f : 0f;
                effect[input * 5 + output] = identity + s * (matrix[output, input] - identity);
            }
        return effect;
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
}
