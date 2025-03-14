namespace NestedArgs;

public static class StringExtensions
{
    public static string? FuzzyMatch(string input, IEnumerable<string> options)
    {
        const int threshold = 3;
        var closestMatch = options.Select(option => new { option, distance = LevenshteinDistance(input, option) })
                                  .Where(x => x.distance <= threshold)
                                  .OrderBy(x => x.distance)
                                  .FirstOrDefault();
        return closestMatch?.option;
    }

    public static int LevenshteinDistance(string source1, string source2)
    {
        var source1Length = source1.Length;
        var source2Length = source2.Length;
        if ((long)(source1Length + 1) * (source2Length + 1) > int.MaxValue)
            throw new ArgumentException("Input strings are too long to compute Levenshtein distance.");

        var matrix = new int[source1Length + 1, source2Length + 1];

        if (source1Length == 0)
            return source2Length;

        if (source2Length == 0)
            return source1Length;

        for (var i = 0; i <= source1Length; matrix[i, 0] = i++){}
        for (var j = 0; j <= source2Length; matrix[0, j] = j++){}

        for (var i = 1; i <= source1Length; i++)
        {
            for (var j = 1; j <= source2Length; j++)
            {
                var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source1Length, source2Length];
    }
}
