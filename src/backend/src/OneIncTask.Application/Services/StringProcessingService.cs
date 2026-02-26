using System.Text;

namespace OneIncTask.Application.Services;

public class StringProcessingService : IStringProcessingService
{
    public string BuildProcessedString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var charCounts = new SortedDictionary<char, int>();
        foreach (var c in input)
        {
            if (charCounts.ContainsKey(c))
                charCounts[c]++;
            else
                charCounts[c] = 1;
        }

        var sb = new StringBuilder();
        foreach (var kvp in charCounts)
        {
            sb.Append(kvp.Key);
            sb.Append(kvp.Value);
        }

        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        sb.Append('/');
        sb.Append(base64);

        return sb.ToString();
    }
}
