namespace FlowLens.Infrastructure.Analysis.Helpers;

public static class LayerDetector
{
    public static string Detect(string? ns)
    {
        if (string.IsNullOrEmpty(ns)) return "Global";

        var parts = ns.Split('.');

        if (parts.Length > 1)
        {
            return parts[1];
        }

        return parts[0];
    }
}