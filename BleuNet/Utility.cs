using System.Text.RegularExpressions;

namespace BleuNet
{
    /// <summary>
    /// The Utility class in the BleuNet namespace.
    /// This class provides utility methods for tokenizing strings and benchmarking.
    /// </summary>
    public static class Utility
    {
        private static readonly Dictionary<string, int> nonBreakingPrefix = new();

        static Utility()
        {
            LoadPrefixes();
        }
        
        private static void LoadPrefixes()
        {
            string[] lines = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", 
                "U", "V", "W", "X", "Y", "Z", "Adj", "Adm", "Adv", "Asst", "Bart", "Bldg", "Brig", "Bros", "Capt", "Cmdr", "Col", "Comdr", "Con", 
                "Corp", "Cpl", "DR", "Dr", "Drs", "Ens", "Gen", "Gov", "Hon", "Hr", "Hosp", "Insp", "Lt", "MM", "MR", "MRS", "MS", "Maj", "Messrs",
                "Mlle", "Mme", "Mr", "Mrs", "Ms", "Msgr", "Op", "Ord", "Pfc", "Ph", "Prof", "Pvt", "Rep", "Reps", "Res", "Rev", "Rt", "Sen", "Sens",
                "Sfc", "Sgt", "Sr", "St", "Supt", "Surg", "v", "vs", "i.e", "rev", "e.g", "Rs", "No #NUMERIC_ONLY# ", "Nos", "Art #NUMERIC_ONLY#",
                "Nr", "pp #NUMERIC_ONLY#", "Jan", "Feb", "Mar", "Apr", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            foreach (var line in lines)
            {
                string item = line.Trim();
                if (!String.IsNullOrEmpty(item) && !item.StartsWith("#"))
                {
                    if (Regex.IsMatch(item, @"(.*)\s+\#NUMERIC_ONLY\#"))
                    {
                        var match = Regex.Match(item, @"(.*)\s+\#NUMERIC_ONLY\#");
                        nonBreakingPrefix[match.Groups[1].Value] = 2;
                    }
                    else
                    {
                        nonBreakingPrefix[item] = 1;
                    }
                }
            }
        }

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
        /// Tokenizes the input text.
        /// </summary>
        /// <remarks>
        /// This method's tokenization is designed to closely match the tokenization of the tokenizer.perl script included with the statistical machine translation tool Moses when specified with -l en
        /// </remarks> 
        /// <param name="line">The input string to tokenize.</param>
        /// <param name="lc">A boolean value indicating whether to convert the input string to lower case. Default is true.</param>
        /// <returns>An array of words.</returns>
        public static string[] Tokenize2(string text, bool lc = true)
        {
            // The rest of the method implements the tokenization as in the Perl script
            text = " " + text.Trim() + " ";

            // Remove ASCII junk.
            text = Regex.Replace(text, @"\s+", " ");
            text = Regex.Replace(text, "[\u0000-\u001F]", "");

            //// Find protected patterns.
            //List<string> protectedTokens = new List<string>();
            //foreach (var protectedPattern in protectedPatterns)
            //{
            //    // Match and protect the patterns
            //    var matches = Regex.Matches(text, protectedPattern);
            //    foreach (Match match in matches)
            //    {
            //        protectedTokens.Add(match.Value);
            //    }
            //    // Replace found protected patterns in text with placeholders
            //    int i = 0;
            //    foreach (var token in protectedTokens)
            //    {
            //        string subst = $"THISISPROTECTED{i.ToString("D3")}";
            //        text = text.Replace(token, subst);
            //        i++;
            //    }
            //}
            //for (int i = 0; i < protectedTokens.Count; i++)
            //{
            //    string subst = $"THISISPROTECTED{i.ToString("D3")}";
            //    text = text.Replace(subst, protectedTokens[i]);
            //}

            text = Regex.Replace(text, " +", " ");
            text = text.Trim();

            text = Regex.Replace(text, @"([^\w\s\.'`,\-])", " $1 ");

            // Multi-dots stay together.
            text = Regex.Replace(text, @"\.\.+", "DOTMULTI$0");
            while (Regex.IsMatch(text, "DOTMULTI\\."))
            {
                text = Regex.Replace(text, @"DOTMULTI\.([^\.])", "DOTDOTMULTI $1");
                text = Regex.Replace(text, @"DOTMULTI\.", "DOTDOTMULTI");
            }

            // First, separate out "," except if it follows a non-number
            // Second, separate out "," except if it precedes a non-number
            text = Regex.Replace(text, @"([^\d]),", "$1 , ");
            text = Regex.Replace(text, @",([^\d])", " , $1");

            // Separate out "," after a number if it's the end of a sentence/string.
            text = Regex.Replace(text, @"(\d),$", "$1 ,");

            // Split contractions right - Adjust apostrophes in various contexts.
            text = Regex.Replace(text, @"([^\p{L}])'([^\p{L}])", "$1 ' $2"); // Non-letter, apostrophe, non-letter
            text = Regex.Replace(text, @"([^\p{L}\d])'([\p{L}])", "$1 ' $2"); // Non-letter/digit, apostrophe, letter
            text = Regex.Replace(text, @"([\p{L}])'([^\p{L}])", "$1 ' $2"); // Letter, apostrophe, non-letter
            text = Regex.Replace(text, @"([\p{L}])'([\p{L}])", "$1'$2");     // Letter, apostrophe, letter
            text = Regex.Replace(text, @"(\d)'([sS])", "$1'$2");             // Number, apostrophe, 's' (like in "1990's")

            string[] words = Regex.Split(text, @"\s+"); // Split text into words
            string resultText = "";
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                Match match = Regex.Match(word, @"^(\S+)\.$");

                if (match.Success)
                {
                    string pre = match.Groups[1].Value;
                    if (i == words.Length - 1)
                    {
                        word = pre + " ."; // Separate the period for the last word
                    }
                    else if ((pre.Contains(".") && Regex.IsMatch(pre, @"\p{L}")) ||
                             (nonBreakingPrefix.ContainsKey(pre) && nonBreakingPrefix[pre] == 1) ||
                             (i < words.Length - 1 && Regex.IsMatch(words[i + 1], @"^[\p{Ll}]")))
                    {
                        // No change for specific non-breaking prefixes or certain conditions
                    }
                    else if (nonBreakingPrefix.ContainsKey(pre) && nonBreakingPrefix[pre] == 2 &&
                             i < words.Length - 1 && Regex.IsMatch(words[i + 1], @"^[0-9]+"))
                    {
                        // No change for certain numeric conditions
                    }
                    else
                    {
                        word = pre + " ."; // Separate the period otherwise
                    }
                }

                resultText += word + " ";
            }
            text = resultText.Trim();

            // Clean up extraneous spaces
            text = Regex.Replace(text, " +", " ");
            text = text.Trim();

            // Fix punctuation at end of sentences ('.' at end of sentence is missed)
            text = Regex.Replace(text, @"\.\' ?$", " . ' ");

            //// Restore protected phrases
            //for (int i = 0; i < protectedPhrases.Count; i++)
            //{
            //    string subst = String.Format("THISISPROTECTED{0:D3}", i);
            //    text = text.Replace(subst, protectedPhrases[i]);
            //}

            // Restore multi-dots
            while (text.Contains("DOTDOTMULTI"))
            {
                text = text.Replace("DOTDOTMULTI", "DOTMULTI.");
            }
            text = text.Replace("DOTMULTI", ".");

            return text.Split();
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