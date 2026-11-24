namespace Ess;

internal sealed class EditDistance
{
    public int Between(string first, string second)
    {
        var distances = new int[first.Length + 1, second.Length + 1];

        for (var firstIndex = 0; firstIndex <= first.Length; firstIndex++)
        {
            distances[firstIndex, 0] = firstIndex;
        }

        for (var secondIndex = 0; secondIndex <= second.Length; secondIndex++)
        {
            distances[0, secondIndex] = secondIndex;
        }

        for (var firstIndex = 1; firstIndex <= first.Length; firstIndex++)
        {
            for (var secondIndex = 1; secondIndex <= second.Length; secondIndex++)
            {
                var substitutionCost = first[firstIndex - 1] == second[secondIndex - 1] ? 0 : 1;

                var best = Math.Min(
                    Math.Min(distances[firstIndex - 1, secondIndex] + 1, distances[firstIndex, secondIndex - 1] + 1),
                    distances[firstIndex - 1, secondIndex - 1] + substitutionCost);

                var isSwappedPair = firstIndex > 1 && secondIndex > 1
                    && first[firstIndex - 1] == second[secondIndex - 2]
                    && first[firstIndex - 2] == second[secondIndex - 1];

                distances[firstIndex, secondIndex] = isSwappedPair
                    ? Math.Min(best, distances[firstIndex - 2, secondIndex - 2] + 1)
                    : best;
            }
        }

        return distances[first.Length, second.Length];
    }
}
