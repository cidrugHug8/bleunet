using System.Text.RegularExpressions;

namespace BleuNet
{
    /// <summary>
    /// The Utility class in the BleuNet namespace.
    /// This class provides utility methods for tokenizing strings and benchmarking.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Tokenizes the input string into an array of words.
        /// </summary>
        /// <param name="line">The input string to tokenize.</param>
        /// <param name="lc">A boolean value indicating whether to convert the input string to lower case. Default is true.</param>
        /// <returns>An array of words.</returns>
        public static string[] Tokenize(string line, bool lc = true)
        {
            string norm = line;

            // Convert the string to lower case if lc is true.
            if (lc)
            {
                norm = norm.ToLower();
            }

            // language-independent part:
            // Replace certain characters and strings with others.
            norm = norm.Replace("<skipped>", "");
            norm = norm.Replace("-\n", "");
            norm = norm.Replace("\n", " ");
            norm = norm.Replace("&quot;", "\"");
            norm = norm.Replace("&amp;", "&");
            norm = norm.Replace("&lt;", "<");
            norm = norm.Replace("&gt;", ">");

            // language-dependent part (assuming Western languages):
            // Add spaces around certain characters and strings.
            norm = " " + norm + " ";
            norm = Regex.Replace(norm, "([\\{-\\~\\[-\\` -\\&\\(-\\+\\:-\\@\\/])", " $1 ");
            norm = Regex.Replace(norm, "([^0-9])([\\.,])", "$1 $2 ");
            norm = Regex.Replace(norm, "([\\.,])([^0-9])", " $1 $2");
            norm = Regex.Replace(norm, "([0-9])(-)", "$1 $2 ");
            norm = Regex.Replace(norm, "\\s+", " ");  // one space only between words
            norm = norm.Trim();  // no leading or trailing space

            // Split the normalized string into words.
            var segmented = norm.Split();

            return segmented;
        }

        /// <summary>
        /// Runs a benchmark on the CorpusBleu method.
        /// </summary>
        /// <param name="referenceFilepath">The file path to the reference text file. Default is "./reference.txt".</param>
        /// <param name="hypothesisFilepath">The file path to the hypothesis text file. Default is "./hypothesis.txt".</param>
        public static void Benchmark(string referenceFilepath="./reference.txt", string hypothesisFilepath= "./hypothesis.txt")
        {
            for (var i = 0; i < 100; i++)
            {
                var references = File.ReadAllLines(referenceFilepath)
                .Select(x => Utility.Tokenize(x))
                .ToArray();
                var hypotheses = File.ReadAllLines(hypothesisFilepath)
                    .Select(x => Utility.Tokenize(x))
                    .ToArray();
                var bleuScore = Metrics.CorpusBleu(references, hypotheses);
            }
        }
    }
}
