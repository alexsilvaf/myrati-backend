namespace Myrati.Application.Common;

public static class IdGenerator
{
    public static string NextPrefixedId(string prefix, IEnumerable<string> existingIds, int pad = 3)
    {
        var next = existingIds
            .Select(id => id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? id[prefix.Length..]
                : string.Empty)
            .Select(suffix => int.TryParse(suffix, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}{next.ToString($"D{pad}")}";
    }

    public static string CreatePlanId(string productId, int index) => $"{productId}-PLAN-{index:D2}";

    public static string GenerateLicenseKey()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var random = Random.Shared;
        var groups = Enumerable.Range(0, 4)
            .Select(_ => new string(Enumerable.Range(0, 4).Select(__ => chars[random.Next(chars.Length)]).ToArray()));
        return string.Join("-", groups);
    }

    public static string GenerateSecret(int length = 16)
    {
        const string chars = "abcdef0123456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
