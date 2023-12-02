using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace BleuNet
{
    public static class BleuScore
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
        /// <param name="references">This is a single reference translation for the sentence. It's an array of words.</param>
        /// <param name="hypothesis">This is the translated sentence that you want to evaluate. It's an array of words.</param>
        /// <param name="weights">These are the weights for the n-gram precisions. By default, it's an array of four 0.25s, which means it considers up to 4-gram precisions.</param>
        /// <returns>The BLEU score, which is a value between 0 and 1, where 1 means the translation is perfect (matches a reference translation exactly).</returns>
        public static double SentenceBleu(string[] references, string[] hypothesis, double[] weights = null)
        {
            return SentenceBleu(new string[][] { references }, hypothesis, weights);
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
            StringBuilder ngram = new StringBuilder();
            for (int i = 0; i <= words.Length - n; i++)
            {
                ngram.Clear();
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

        public static string[] Tokenize(string line, bool lc = true)
        {
            string norm = line;

            // language-independent part:
            norm = norm.Replace("<skipped>", "");
            norm = norm.Replace("-\n", "");
            norm = norm.Replace("\n", " ");
            norm = norm.Replace("&quot;", "\"");
            norm = norm.Replace("&amp;", "&");
            norm = norm.Replace("&lt;", "<");
            norm = norm.Replace("&gt;", ">");

            if (lc)
            {
                norm = norm.ToLower();
            }

            // language-dependent part (assuming Western languages):
            norm = " " + norm + " ";
            norm = Regex.Replace(norm, "([\\{-\\~\\[-\\` -\\&\\(-\\+\\:-\\@\\/])", " $1 ");
            norm = Regex.Replace(norm, "([^0-9])([\\.,])", "$1 $2 ");
            norm = Regex.Replace(norm, "([\\.,])([^0-9])", " $1 $2");
            norm = Regex.Replace(norm, "([0-9])(-)", "$1 $2 ");
            norm = Regex.Replace(norm, "\\s+", " ");  // one space only between words
            norm = norm.Trim();  // no leading or trailing space

            var segmented = norm.Split();

            return segmented;
        }
    }

    //public class SmoothingFunction
    //{
    //    private double epsilon;
    //    private double alpha;
    //    private double k;

    //    public SmoothingFunction(double epsilon = 0.1, double alpha = 5, double k = 5)
    //    {
    //        this.epsilon = epsilon;
    //        this.alpha = alpha;
    //        this.k = k;
    //    }

    //    /// <summary>
    //    ///   No smoothing.
    //    /// </summary>
    //    public double[] Method0(double[] p_n)
    //    {
    //        var p_n_new = new double[p_n.Length];
    //        for (int i = 0; i < p_n.Length; i++)
    //        {
    //            if (p_n[i] != 0)
    //            {
    //                p_n_new[i] = p_n[i];
    //            }
    //            else
    //            {
    //                string _msg = string.Format(
    //                    "\nThe hypothesis contains 0 counts of {0}-gram overlaps.\n" +
    //                    "Therefore the BLEU score evaluates to 0, independently of\n" +
    //                    "how many N-gram overlaps of lower order it contains.\n" +
    //                    "Consider using lower n-gram order or use " +
    //                    "SmoothingFunction()", i + 1);
    //                Console.WriteLine(_msg);
    //                p_n_new[i] = double.Epsilon;
    //            }
    //        }
    //        return p_n_new;
    //    }

    //    /// <summary>
    //    /// Smoothing method 1: Add *epsilon* counts to precision with 0 counts.
    //    /// </summary>
    //    public double[] Method1(double[] p_n)
    //    {
    //        double[] p_n_new = new double[p_n.Length];
    //        for (int i = 0; i < p_n.Length; i++)
    //        {
    //            if (p_n[i] == 0)
    //            {
    //                p_n_new[i] = epsilon / p_n[i];
    //            }
    //            else
    //            {
    //                p_n_new[i] = p_n[i];
    //            }
    //        }
    //        return p_n_new;
    //    }

    //    /// <summary>
    //    ///  Smoothing method 2: Add 1 to both numerator and denominator from
    //    /// Chin-Yew Lin and Franz Josef Och(2004) ORANGE: a Method for
    //    /// Evaluating Automatic Evaluation Metrics for Machine Translation.
    //    /// In COLING 2004.
    //    /// </summary>
    //    public double[] Method2(double[] p_n)
    //    {
    //        double[] p_n_new = new double[p_n.Length];
    //        for (int i = 0; i < p_n.Length; i++)
    //        {
    //            if (i != 0)
    //            {
    //                p_n_new[i] = (p_n[i] + 1) / (p_n[i] + 1);
    //            }
    //            else
    //            {
    //                p_n_new[i] = p_n[i];
    //            }
    //        }
    //        return p_n_new;
    //    }

    //    /// <summary>
    //    /// Smoothing method 3: NIST geometric sequence smoothing
    //    /// The smoothing is computed by taking 1 / ( 2^k ), instead of 0, for each
    //    /// precision score whose matching n-gram count is null.
    //    /// k is 1 for the first 'n' value for which the n-gram match count is null.
    //    ///
    //    /// For example, if the text contains:
    //    ///
    //    /// - one 2-gram match
    //    /// - and (consequently) two 1-gram matches
    //    ///
    //    /// the n-gram count for each individual precision score would be:
    //    ///
    //    /// - n=1  =>  prec_count = 2     (two unigrams)
    //    /// - n=2  =>  prec_count = 1     (one bigram)
    //    /// - n=3  =>  prec_count = 1/2   (no trigram,  taking 'smoothed' value of 1 / ( 2^k ), with k=1)
    //    /// - n=4  =>  prec_count = 1/4   (no fourgram, taking 'smoothed' value of 1 / ( 2^k ), with k=2)
    //    /// </summary>
    //    public double[] Method3(double[] p_n)
    //    {
    //        int incvnt = 1;
    //        for (int i = 0; i < p_n.Length; i++)
    //        {
    //            if (p_n[i] == 0)
    //            {
    //                p_n[i] = 1 / (Math.Pow(2, incvnt) * p_n[i]);
    //                incvnt += 1;
    //            }
    //        }
    //        return p_n;
    //    }

    //    /// <summary>
    //    /// Smoothing method 4:
    //    /// Shorter translations may have inflated precision values due to having
    //    /// smaller denominators; therefore, we give them proportionally
    //    /// smaller smoothed counts. Instead of scaling to 1/(2^k), Chen and Cherry
    //    /// suggests dividing by 1/ln(len(T)), where T is the length of the translation.
    //    /// </summary>
    //    public double[] Method4(double[] p_n, string[][] references, string[] hypothesis, int hypLen = -1)
    //    {
    //        int incvnt = 1;
    //        int hyp_len;
    //        if (hypLen == -1) hyp_len = hypothesis.Length;
    //        else hyp_len = hypLen;
    //        for (int i = 0; i < p_n.Length; i++)
    //        {
    //            if (p_n[i] == 0 && hyp_len > 1)
    //            {
    //                double numerator = 1 / (Math.Pow(2, incvnt) * this.k / Math.Log(hyp_len));
    //                p_n[i] = numerator / p_n[i];
    //                incvnt += 1;
    //            }
    //        }
    //        return p_n;
    //    }

    //    /// <summary>
    //    /// Smoothing method 5:
    //    /// The matched counts for similar values of n should be similar. To a
    //    /// calculate the n-gram matched count, it averages the n−1, n and n+1 gram
    //    /// matched counts.
    //    /// </summary>
    //    public double[] Method5(double[] p_n, string[][] references, string[] hypothesis, int? hypLen = null)
    //    {
    //        int hyp_len;
    //        if (hypLen.HasValue)
    //        {
    //            hyp_len = hypLen.Value;
    //        }
    //        else
    //        {
    //            hyp_len = hypothesis.Length;
    //        }
    //        Dictionary<int, double> m = new Dictionary<int, double>();
    //        // Requires a precision value for an additional ngram order.
    //        // Here, you need to implement the 'modified_precision' method and add its result to 'p_n'.
    //        var p_n_plus1 = new double[p_n.Length + 1];
    //        p_n.CopyTo(p_n_plus1, 0);
    //        p_n_plus1[p_n.Length] = BleuScore.ModifiedPrecision(references, hypothesis, 5);
    //        m[-1] = p_n[0] + 1;
    //        for (int i = 0; i < p_n.Length; i++)
    //        {
    //            p_n[i] = (m[i - 1] + p_n[i] + p_n_plus1[i + 1]) / 3;
    //            m[i] = p_n[i];
    //        }
    //        return p_n;
    //    }

    //    /// <summary>
    //    /// Smoothing method 6:
    //    /// Interpolates the maximum likelihood estimate of the precision *p_n* with
    //    /// a prior estimate *pi0*. The prior is estimated by assuming that the ratio
    //    /// between pn and pn−1 will be the same as that between pn−1 and pn−2; from
    //    /// Gao and He (2013) Training MRF-Based Phrase Translation Models using
    //    /// Gradient Ascent. In NAACL.
    //    /// </summary>
    //    public double[] Method6(double[] p_n, string[][] references, string[] hypothesis, int? hypLen = null)
    //    {
    //        int hyp_len;
    //        if (hypLen.HasValue)
    //        {
    //            hyp_len = hypLen.Value;
    //        }
    //        else
    //        {
    //            hyp_len = hypothesis.Length;
    //        }
    //        // This smoothing only works when p_1 and p_2 is non-zero.
    //        // Raise an error with an appropriate message when the input is too short
    //        // to use this smoothing technique.
    //        if (p_n[2] == 0)
    //        {
    //            throw new ArgumentException("This smoothing method requires non-zero precision for bigrams.");
    //        }
    //        for (int i = 0; i < p_n.Length; i++)
    //        {
    //            if (i == 0 || i == 1)  // Skips the first 2 orders of ngrams.
    //            {
    //                continue;
    //            }
    //            else
    //            {
    //                double pi0 = p_n[i - 2] == 0 ? 0 : Math.Pow(p_n[i - 1], 2) / p_n[i - 2];
    //                // No. of ngrams in translation that matches the reference.
    //                double m = p_n[i];
    //                // No. of ngrams in translation.
    //                double l = hypothesis.Length - i + 1;  // You need to implement the 'ngrams' method and use its result here.
    //                                                       // Calculates the interpolated precision.
    //                p_n[i] = (m + this.alpha * pi0) / (l + this.alpha);
    //            }
    //        }
    //        return p_n;
    //    }

    //    /// <summary>
    //    /// Smoothing method 7:
    //    /// Interpolates methods 4 and 5.
    //    /// </summary>
    //    public double[] Method7(double[] p_n, string[][] references, string[] hypothesis, int? hypLen = null)
    //    {
    //        int hyp_len;
    //        if (hypLen.HasValue)
    //        {
    //            hyp_len = hypLen.Value;
    //        }
    //        else
    //        {
    //            hyp_len = hypothesis.Length;
    //        }
    //        p_n = Method4(p_n, references, hypothesis, hyp_len);
    //        p_n = Method5(p_n, references, hypothesis, hyp_len);
    //        return p_n;
    //    }
    //}
}
