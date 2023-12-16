using System.Diagnostics;
using System.Text;

namespace BleuNet
{
    public static class Metrics
    {
        /// <summary>
        /// Fraction class.
        /// </summary>
        public class Fraction
        {
            public int numerator { get; private set; }
            public int denominator { get; private set; }

            public double Value
            {
                get { return (double)numerator / denominator; }
            }

            public Fraction(int numerator, int denominator)
            {
                if (denominator == 0)
                {
                    throw new ArgumentException("Denominator cannot be zero.");
                }

                var gcd = GCD(numerator, denominator);
                this.numerator = numerator;
                this.denominator = denominator;
            }

            /// <summary>
            /// Greatest Common Divisor.
            /// </summary>
            private int GCD(int a, int b)
            {
                return b == 0 ? a : GCD(b, a % b);
            }
            public static implicit operator double(Fraction f)
            {
                return (double)f.numerator / f.denominator;
            }
            public bool IsZero()
            {
                return numerator == 0;
            }
        }

        /// <summary>
        /// The SentenceBleu method calculates the BLEU (Bilingual Evaluation Understudy) score for a given sentence.
        /// The BLEU score is a metric used in machine translation to measure the quality of translations.
        /// </summary>
        /// <param name="references">This is an array of reference translations for the sentence. Each reference translation is an array of words.</param>
        /// <param name="hypothesis">This is the translated sentence that you want to evaluate. It's an array of words.</param>
        /// <param name="weights">These are the weights for the n-gram precisions. By default, it's an array of four 0.25s, which means it considers up to 4-gram precisions.</param>
        /// <returns>The BLEU score, which is a value between 0 and 1, where 1 means the translation is perfect (matches a reference translation exactly).</returns>
        public static double SentenceBleu(string[][] references, string[] hypothesis, double[] weights = null)
        {
            if (weights == null)
            {
                weights = new double[] { 0.25, 0.25, 0.25, 0.25 };
            }

            var pN = new Fraction[weights.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                pN[i] = ModifiedPrecision(references, hypothesis, i + 1);
            }

            double score;
            if (pN.Any(p => p.IsZero() || double.IsNaN(p.Value)))
            {
                score = 0.0;
            }
            else
            {
                score = Math.Exp(pN.Select((p, i) => weights[i] * Math.Log(p.Value)).Sum());
            }

            int closestRefLen = ClosestRefLength(references, hypothesis.Length);
            double bp = BrevityPenalty(closestRefLen, hypothesis.Length);

            return bp * score;
        }

        /// <summary>
        /// The SentenceBleu method is an overloaded version of the previous SentenceBleu method. This version takes a single reference translation instead of an array of reference translations.
        /// </summary>
        /// <param name="reference">This is a single reference translation for the sentence. It's an array of words.</param>
        /// <param name="hypothesis">This is the translated sentence that you want to evaluate. It's an array of words.</param>
        /// <param name="weights">These are the weights for the n-gram precisions. By default, it's an array of four 0.25s, which means it considers up to 4-gram precisions.</param>
        /// <returns>The BLEU score, which is a value between 0 and 1, where 1 means the translation is perfect (matches a reference translation exactly).</returns>
        public static double SentenceBleu(string[] reference, string[] hypothesis, double[] weights = null)
        {
            return SentenceBleu(new string[][] { reference }, hypothesis, weights);
        }

        /// <summary>
        /// The CorpusBleu method calculates the BLEU (Bilingual Evaluation Understudy) score for a corpus of sentences.
        /// The BLEU score is a metric used in machine translation to measure the quality of translations.
        /// </summary>
        /// <param name="referencesList">This is an array of reference translations for each sentence in the corpus. Each item in the array is an array of reference translations for a sentence, and each reference translation is an array of words.</param>
        /// <param name="hypotheses">This is an array of translated sentences that you want to evaluate. Each translated sentence is an array of words.</param>
        /// <param name="weights">These are the weights for the n-gram precisions. By default, it's an array of four 0.25s, which means it considers up to 4-gram precisions.</param>
        /// <returns>The BLEU score, which is a value between 0 and 1, where 1 means the translation is perfect (matches a reference translation exactly).</returns>
        public static double CorpusBleu(string[][][] referencesList, string[][] hypotheses, double[] weights = null)
        {
            Debug.Assert(referencesList.Length == hypotheses.Length, "The number of hypotheses and their reference(s) should be the \"same\".");

            if (weights == null)
            {
                weights = new double[] { 0.25, 0.25, 0.25, 0.25 };
            }

            var pNumerators = new int[weights.Length];
            var pDenominators = new int[weights.Length];
            int refLength = 0;
            int hypLength = 0;

            for (int i = 0; i < hypotheses.Length; i++)
            {
                var hypothesis = hypotheses[i];
                var references = referencesList[i];

                for (int j = 0; j < weights.Length; j++)
                {
                    var p_i = ModifiedPrecision(references, hypothesis, j + 1);
                    pNumerators[j] += p_i.numerator;
                    pDenominators[j] += p_i.denominator;
                }

                refLength += ClosestRefLength(references, hypothesis.Length);
                hypLength += hypothesis.Length;
            }

            double score = 0.0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (pDenominators[i] != 0)
                {
                    score += weights[i] * Math.Log((double)pNumerators[i] / pDenominators[i]);
                }
            }
            score = Math.Exp(score);
            return BrevityPenalty(refLength, hypLength) * score;
        }

        /// <summary>
        /// The CorpusBleu method is another overloaded version of the previous CorpusBleu method. This version takes an array of weights arrays instead of a single weights array.
        /// </summary>
        /// <param name="references">This is an array of reference translations for the sentence. Each reference translation is an array of words.</param>
        /// <param name="hypotheses">This is an array of translated sentences that you want to evaluate. Each translated sentence is an array of words.</param>
        /// <param name="weights">This is an array of weights arrays for the n-gram precisions. Each weights array is an array of four 0.25s, which means it considers up to 4-gram precisions.</param>
        /// <returns>The scores array, which contains the BLEU score for each weights array. Each BLEU score is a value between 0 and 1, where 1 means the translation is perfect (matches a reference translation exactly).</returns>
        public static double CorpusBleu(string[][] references, string[][] hypotheses, double[] weights = null)
        {
            string[][][] referencesList = new string[references.Length][][];
            for (int i = 0; i < references.Length; i++)
            {
                referencesList[i] = new string[][] { references[i] };
            }
            return CorpusBleu(referencesList, hypotheses, weights);
        }

        /// <summary>
        /// The CorpusBleu method is another overloaded version of the previous CorpusBleu method. This version takes an array of weights arrays instead of a single weights array.
        /// </summary>
        /// <param name="referencesList">This is an array of reference translations for each sentence in the corpus. Each item in the array is an array of reference translations for a sentence, and each reference translation is an array of words.</param>
        /// <param name="hypotheses">This is an array of translated sentences that you want to evaluate. Each translated sentence is an array of words.</param>
        /// <param name="weights">This is an array of weights arrays for the n-gram precisions. Each weights array is an array of four 0.25s, which means it considers up to 4-gram precisions.</param>
        /// <returns>The scores array, which contains the BLEU score for each weights array. Each BLEU score is a value between 0 and 1, where 1 means the translation is perfect (matches a reference translation exactly).</returns>
        public static double[] CorpusBleu(string[][][] referencesList, string[][] hypotheses, double[][] weights = null)
        {
            if (weights == null)
            {
                weights = new double[referencesList.Length][];
                for (var i = 0; i < referencesList.Length; i++)
                {
                    weights[i] = new double[] { 0.25, 0.25, 0.25, 0.25 };
                }
            }

            double[] scores = new double[weights.Length];
            for (var i = 0; i < weights.Length; i++)
            {
                scores[i] = CorpusBleu(referencesList, hypotheses, weights[i]);
            }
            return scores;
        }

        public static Fraction ModifiedPrecision0(string[][] references, string[] hypothesis, int n)
        {
            // Extracts all ngrams in hypothesis
            // Set an empty Dictionary if hypothesis is empty.
            Dictionary<string, int> counts = hypothesis.Length >= n ? Ngrams(hypothesis, n) : new Dictionary<string, int>();

            // Extract a union of references' counts.
            Dictionary<string, int> maxCounts = new Dictionary<string, int>();
            foreach (var reference in references)
            {
                Dictionary<string, int> referenceCounts = reference.Length >= n ? Ngrams(reference, n) : new Dictionary<string, int>();
                foreach (var ngram in counts.Keys)
                {
                    if (referenceCounts.ContainsKey(ngram))
                    {
                        if (maxCounts.ContainsKey(ngram))
                        {
                            maxCounts[ngram] = Math.Max(maxCounts[ngram], referenceCounts[ngram]);
                        }
                        else
                        {
                            maxCounts[ngram] = referenceCounts[ngram];
                        }
                    }
                }
            }

            // Assigns the intersection between hypothesis and references' counts.
            Dictionary<string, int> clippedCounts = new Dictionary<string, int>();
            foreach (var item in counts)
            {
                if (maxCounts.ContainsKey(item.Key))
                {
                    clippedCounts[item.Key] = Math.Min(item.Value, maxCounts[item.Key]);
                }
            }

            var numerator = clippedCounts.Values.Sum();
            // Ensures that denominator is minimum 1 to avoid ZeroDivisionError.
            // Usually this happens when the ngram order is > len(reference).
            var denominator = Math.Max(1, counts.Values.Sum());

            return new Fraction(numerator, denominator);
        }

        public static Fraction ModifiedPrecision(string[][] references, string[] hypothesis, int n)
        {
            // Extracts all ngrams in hypothesis
            // Set an empty Dictionary if hypothesis is empty.
            Dictionary<string, int> counts = hypothesis.Length >= n ? Ngrams(hypothesis, n) : new Dictionary<string, int>();

            // Extract a union of references' counts.
            Dictionary<string, int> maxCounts = new Dictionary<string, int>();
            foreach (var reference in references)
            {
                Dictionary<string, int> referenceCounts = reference.Length >= n ? Ngrams(reference, n) : new Dictionary<string, int>();
                foreach (var ngram in counts.Keys)
                {
                    if (referenceCounts.TryGetValue(ngram, out int referenceCount))
                    {
                        if (maxCounts.TryGetValue(ngram, out int maxCount))
                        {
                            maxCounts[ngram] = Math.Max(maxCount, referenceCount);
                        }
                        else
                        {
                            maxCounts[ngram] = referenceCount;
                        }
                    }
                }
            }

            // Assigns the intersection between hypothesis and references' counts.
            Dictionary<string, int> clippedCounts = new Dictionary<string, int>();
            foreach (var item in counts)
            {
                if (maxCounts.TryGetValue(item.Key, out int maxCount))
                {
                    clippedCounts[item.Key] = Math.Min(item.Value, maxCount);
                }
            }

            var numerator = clippedCounts.Values.Sum();
            // Ensures that denominator is minimum 1 to avoid ZeroDivisionError.
            // Usually this happens when the ngram order is > len(reference).
            var denominator = Math.Max(1, counts.Values.Sum());

            return new Fraction(numerator, denominator);
        }

        public static int ClosestRefLength(string[][] references, int hypLen)
        {
            int[] refLens = Array.ConvertAll(references, reference => reference.Length);
            int closestRefLen = refLens.OrderBy(refLen => Math.Abs(refLen - hypLen)).First();

            return closestRefLen;
        }

        public static double BrevityPenalty(int closestRefLen, int hypLen)
        {
            if (hypLen > closestRefLen)
            {
                return 1;
            }
            // If hypothesis is empty, brevity penalty = 0 should result in BLEU = 0.0
            else if (hypLen == 0)
            {
                return 0;
            }
            else
            {
                return Math.Exp(1 - (double)closestRefLen / hypLen);
            }
        }

        private static Dictionary<string, int> Ngrams0(string[] words, int n)
        {
            Dictionary<string, int> ngrams = new Dictionary<string, int>();
            for (int i = 0; i <= words.Length - n; i++)
            {
                StringBuilder ngram = new StringBuilder();
                for (int j = 0; j < n; j++)
                {
                    if (j > 0) ngram.Append(" ");
                    ngram.Append(words[i + j]);
                }
                string ngramStr = ngram.ToString();
                if (ngrams.TryGetValue(ngramStr, out int currentCount))
                {
                    ngrams[ngramStr] = currentCount + 1;
                }
                else
                {
                    ngrams[ngramStr] = 1;
                }
            }
            return ngrams;
        }

        private static Dictionary<string, int> Ngrams(string[] words, int n)
        {
            Dictionary<string, int> ngrams = new Dictionary<string, int>(words.Length - n + 1);
            for (int i = 0; i <= words.Length - n; i++)
            {
                string ngramStr = string.Join(" ", words, i, n);
                if (ngrams.TryGetValue(ngramStr, out int currentCount))
                {
                    ngrams[ngramStr] = currentCount + 1;
                }
                else
                {
                    ngrams[ngramStr] = 1;
                }
            }
            return ngrams;
        }

        public static (double nkt, double precision, double bp) CalculateKendallsTau(string[] reference, string[] hypothesis)
        {
            static string MapWordsToUnicode(string[] words, Dictionary<string, int> wordDict)
            {
                StringBuilder _result = new StringBuilder();
                foreach (var w in words)
                {
                    if (!wordDict.ContainsKey(w))
                    {
                        wordDict[w] = wordDict.Count;
                    }
                    int unicodeValue = wordDict[w] + 0x4e00;
                    _result.Append(char.ConvertFromUtf32(unicodeValue));
                }
                return _result.ToString();
            }

            static int OverlappingCount(string pattern, string text)
            {
                try
                {
                    int pos = text.IndexOf(pattern);
                    if (pos > -1)
                    {
                        return 1 + OverlappingCount(pattern, text.Substring(pos + 1));
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    
                }
                return 0;
            }

            static string GetNgram(string text, int start, int length)
            {
                if (start < 0 || length < 0 || start> text.Length)
                {
                    return "";
                }
                int actualLength = Math.Min(length, text.Length - start);
                return text.Substring(start, actualLength);
            }

            var bp = Math.Min(1.0, Math.Exp(1.0 - 1.0 * reference.Length / hypothesis.Length));
            var intlist = new List<string> ();
            var wordDict = new Dictionary<string, int>();

            var mappedRef = MapWordsToUnicode(reference, wordDict);
            var mappedHyp = MapWordsToUnicode(hypothesis, wordDict);

            List<int> intList = new List<int>();
            for (int i = 0; i < hypothesis.Length; i++)
            {
                if (!reference.Contains(hypothesis[i]))
                {
                    continue;
                }
                else if (reference.Count(x => x == hypothesis[i]) == 1 && hypothesis.Count(x => x == hypothesis[i]) == 1)
                {
                    intList.Add(Array.IndexOf(reference, hypothesis[i]));
                }
                else
                {
                    for (int window = 1; window <= Math.Max(i + 1, hypothesis.Length - i); window++)
                    {
                        if (window <= i)
                        {
                            string ngram = GetNgram(mappedHyp, i - window, window + 1);
                            if (OverlappingCount(ngram, mappedRef) == 1 && OverlappingCount(ngram, mappedHyp) == 1)
                            {
                                intList.Add(mappedRef.IndexOf(ngram) + ngram.Length - 1);
                                break;
                            }
                        }
                        if (i + window < hypothesis.Length)
                        {
                            string ngram = GetNgram(mappedHyp, i, window + 1);
                            if (OverlappingCount(ngram, mappedRef) == 1 && OverlappingCount(ngram, mappedHyp) == 1)
                            {
                                intList.Add(mappedRef.IndexOf(ngram));
                                break;
                            }
                        }
                    }
                }
            }

            int n = intList.Count;
            if (n == 1 && reference.Length == 1)
            {
                var p0 = 1.0 / hypothesis.Length;
                return (1.0, p0, bp);
            }
            else if (n < 2)
            {
                return (0.0, 0.0, bp);
            }

            double ascending = 0.0;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (intList[i] < intList[j])
                    {
                        ascending++;
                    }
                }
            }

            double nkt = ascending / (n * (n - 1) / 2.0);

            var p = n / (double)hypothesis.Length;

            return (nkt, p, bp);
        }

        /// <summary>
        /// Calculates the RIBES (Rank-based Intuitive Bilingual Evaluation Score) for a set of hypotheses and references.
        /// </summary>
        /// <param name="referencesList">A three-dimensional array of strings. Each element is a list of reference translations for a single source sentence.</param>
        /// <param name="hypotheses">A two-dimensional array of strings. Each element is a hypothesis translation for a single source sentence.</param>
        /// <returns>A double representing the average RIBES score for all the hypotheses against their corresponding references.</returns>
        public static double CorppusRibes(string[][][] referencesList, string[][] hypotheses)
        {
            string[][][] _ReferencesList = referencesList
                .SelectMany((refs, i) => refs.Select((r, j) => new { i, j, r }))
                .GroupBy(x => x.j, x => new { x.i, x.r })
                .OrderBy(g => g.Key)
                .Select(g => g.OrderBy(x => x.i).Select(x => x.r).ToArray())
                .ToArray();

            var alpha = 0.25;
            var beta = 0.10;

            var bestRibesAcc = 0.0;
            var numValidRefs = 0;
            var ribesList = new double[_ReferencesList.Length];
            for (int i = 0; i < hypotheses.Length; i++)
            {
                double bestRibes = -1.0;

                foreach (var reference in _ReferencesList)
                {
                    try
                    {
                        var (nkt, precision, bp) = CalculateKendallsTau(reference[i], hypotheses[i]);                        
                        double ribes = nkt * Math.Pow(precision, alpha) * Math.Pow(bp, beta);
                        if (ribes > bestRibes)
                        {
                            bestRibes = ribes;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"Error in reference line {i}: {e.Message}");
                        throw;
                    }
                }

                if (bestRibes > -1.0)
                {
                    numValidRefs++;
                    bestRibesAcc += bestRibes;

                    ribesList[i] = bestRibes;
                }
            }

            return numValidRefs > 0 ? bestRibesAcc / numValidRefs : 0.0;
        }
    }
}
