using System.IO.Compression;

/// <summary>
/// C# version of multi-bleu.perl.
/// </summary>
class MultiBleu
{
    static void AddToRef(string file, List<List<string>> refs)
    {
        var sentences = new List<string>();
        StreamReader reader;

        if (file.EndsWith(".gz"))
        {
            using (var originalFileStream = new FileStream(file, FileMode.Open))
            using (var decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
            using (reader = new StreamReader(decompressionStream))
            {
                while (!reader.EndOfStream)
                {
                    sentences.Add(reader.ReadLine());
                }
            }
        }
        else
        {
            using (reader = new StreamReader(file))
            {
                while (!reader.EndOfStream)
                {
                    sentences.Add(reader.ReadLine());
                }
            }
        }

        refs.Add(sentences);
    }
    static void LoadReferences(string stem, List<List<string>> refs)
    {
        var refIndex = 0;
        while (File.Exists($"{stem}{refIndex}"))
        {
            AddToRef($"{stem}{refIndex}", refs);
            refIndex++;
        }
        if (File.Exists(stem))
        {
            AddToRef(stem, refs);
        }
    }

    static Dictionary<string, int> GenerateNGrams(string[] words, int n)
    {
        var ngrams = new Dictionary<string, int>();
        for (int i = 0; i <= words.Length - n; i++)
        {
            var ngram = string.Join(" ", words.Skip(i).Take(n));
            if (!ngrams.ContainsKey(ngram))
            {
                ngrams[ngram] = 0;
            }
            ngrams[ngram]++;
        }
        return ngrams;
    }

    static Dictionary<string, int> GenerateReferenceNGrams(List<List<string>> references, int n, bool lowercase)
    {
        var ngrams = new Dictionary<string, int>();
        foreach (var reference in references)
        {
            foreach (var sentence in reference)
            {
                var words = lowercase ? sentence.ToLower().Split() : sentence.Split();
                for (int i = 0; i <= words.Length - n; i++)
                {
                    var ngram = string.Join(" ", words.Skip(i).Take(n));
                    if (!ngrams.ContainsKey(ngram))
                    {
                        ngrams[ngram] = 0;
                    }
                    ngrams[ngram]++;
                }
            }
        }
        return ngrams;
    }
    static (int diff, int length) FindClosestReferenceLength(string[] words, List<List<string>> references)
    {
        int closestDiff = int.MaxValue;
        int closestLength = int.MaxValue;
        foreach (var reference in references)
        {
            foreach (var sentence in reference)
            {
                var refWords = sentence.Split();
                var diff = Math.Abs(words.Length - refWords.Length);
                if (diff < closestDiff || (diff == closestDiff && refWords.Length < closestLength))
                {
                    closestDiff = diff;
                    closestLength = refWords.Length;
                }
            }
        }
        return (closestDiff, closestLength);
    }
    static double CalculateBrevityPenalty(int lengthTranslation, int lengthReference)
    {
        if (lengthTranslation > lengthReference)
        {
            return 1.0;
        }
        else if (lengthTranslation == 0)
        {
            return 0.0;
        }
        else
        {
            return Math.Exp(1 - (double)lengthReference / lengthTranslation);
        }
    }

    static double CalculateBleu(int[] correctCounts, int[] totalCounts, double brevityPenalty)
    {
        double bleuScore = 1.0;
        for (int i = 0; i < correctCounts.Length; i++)
        {
            if (totalCounts[i] == 0)
            {
                bleuScore *= 0.0;
            }
            else
            {
                bleuScore *= (double)correctCounts[i] / totalCounts[i];
            }
        }

        return brevityPenalty * Math.Pow(bleuScore, 0.25);
    }
    static void CalculateBleuScore(List<List<string>> references, bool lowercase)
    {
        var correctCounts = new int[4];
        var totalCounts = new int[4];
        int lengthTranslation = 0, lengthReference = 0;

        string inputSentence;
        while ((inputSentence = Console.ReadLine()) != null)
        {
            if (lowercase)
            {
                inputSentence = inputSentence.ToLower();
            }

            var words = inputSentence.Split();
            var lengthTranslationThisSentence = words.Length;
            var (closestDiff, closestLength) = FindClosestReferenceLength(words, references);

            lengthTranslation += lengthTranslationThisSentence;
            lengthReference += closestLength;

            for (int n = 1; n <= 4; n++)
            {
                var translationNGrams = GenerateNGrams(words, n);
                var referenceNGrams = GenerateReferenceNGrams(references, n, lowercase);

                foreach (var ngram in translationNGrams)
                {
                    totalCounts[n - 1] += ngram.Value;

                    if (referenceNGrams.TryGetValue(ngram.Key, out var count))
                    {
                        correctCounts[n - 1] += Math.Min(count, ngram.Value);
                    }
                }
            }
        }

        double brevityPenalty = CalculateBrevityPenalty(lengthTranslation, lengthReference);
        double bleuScore = CalculateBleu(correctCounts, totalCounts, brevityPenalty);

        Console.WriteLine($"BLEU = {bleuScore * 100:F2}, " +
                          $"{correctCounts[0] * 100.0 / totalCounts[0]:F1}/" +
                          $"{correctCounts[1] * 100.0 / totalCounts[1]:F1}/" +
                          $"{correctCounts[2] * 100.0 / totalCounts[2]:F1}/" +
                          $"{correctCounts[3] * 100.0 / totalCounts[3]:F1} " +
                          $"(BP={brevityPenalty:F3}, ratio={(double)lengthTranslation / lengthReference:F3}, " +
                          $"hyp_len={lengthTranslation}, ref_len={lengthReference})");
    }

    static void Main(string[] args)
    {
        bool lowercase = false;
        if (args.Length > 0 && args[0] == "-lc")
        {
            lowercase = true;
            args = args.Skip(1).ToArray();
        }

        string? stem = args.Length > 0 ? args[0] : null;
        if (string.IsNullOrEmpty(stem))
        {
            Console.Error.WriteLine("usage: multi-bleu.pl [-lc] reference < hypothesis");
            Console.Error.WriteLine("Reads the references from reference or reference0, reference1, ...");
            Environment.Exit(1);
        }

        if (!File.Exists(stem) && !File.Exists(stem + "0") && File.Exists(stem + ".ref0"))
        {
            stem += ".ref";
        }

        // ファイルの読み込み
        var refs = new List<List<string>>();
        LoadReferences(stem, refs);

        // コマンドラインから追加の参照を読み込む
        foreach (var additionalStem in args.Skip(1))
        {
            if (File.Exists(additionalStem))
            {
                AddToRef(additionalStem, refs);
            }
        }
        if (refs.Count == 0)
        {
            throw new FileNotFoundException($"ERROR: could not find reference file {stem}");
        }


        // BLEUスコアの計算を実行
        CalculateBleuScore(refs, lowercase);


    }
}