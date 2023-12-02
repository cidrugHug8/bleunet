using BleuNet;

namespace BleuNetTest
{
    public class BleuTests
    {
        [Fact]
        public void TestModifiedPrecision1()
        {
            var ref1 = "the cat is on the mat".Split();
            var ref2 = "there is a cat on the mat".Split();

            var hyp1 = "the the the the the the the".Split();

            var references = new string[][] { ref1, ref2 };

            var hyp1UnigramPrecision = BleuScore.ModifiedPrecision(references, hyp1, 1);
            Assert.Equal(0.2857, Math.Round(hyp1UnigramPrecision, 4));

            Assert.Equal(0.28571428, hyp1UnigramPrecision, 0.0001);

            Assert.Equal(0.0, BleuScore.ModifiedPrecision(references, hyp1, 2));
        }

        [Fact]
        public void TestModifiedPrecision2()
        {
            var ref1 = "It is a guide to action that ensures that the military will forever heed Party commands".Split();
            var ref2 = "It is the guiding principle which guarantees the military forces always being under the command of the Party".Split();
            var ref3 = "It is the practical guide for the army always to heed the directions of the party".Split();

            var hyp1 = "of the".Split();

            var references = new string[][] { ref1, ref2, ref3 };

            Assert.Equal(1.0, BleuScore.ModifiedPrecision(references, hyp1, 1));

            Assert.Equal(1.0, BleuScore.ModifiedPrecision(references, hyp1, 2));
        }

        [Fact]
        public void TestModifiedPrecision3()
        {
            var ref1 = "It is a guide to action that ensures that the military will forever heed Party commands".Split();
            var ref2 = "It is the guiding principle which guarantees the military forces always being under the command of the Party".Split();
            var ref3 = "It is the practical guide for the army always to heed the directions of the party".Split();

            var hyp1 = "It is a guide to action which ensures that the military always obeys the commands of the party".Split();
            var hyp2 = "It is to insure the troops forever hearing the activity guidebook that party direct".Split();

            var references = new string[][] { ref1, ref2, ref3 };

            var hyp1UnigramPrecision = BleuScore.ModifiedPrecision(references, hyp1, 1);
            var hyp2UnigramPrecision = BleuScore.ModifiedPrecision(references, hyp2, 1);

            Assert.Equal(0.94444444, hyp1UnigramPrecision, 0.0001);
            Assert.Equal(0.57142857, hyp2UnigramPrecision, 0.0001);

            Assert.Equal(0.9444, Math.Round(hyp1UnigramPrecision, 4));
            Assert.Equal(0.5714, Math.Round(hyp2UnigramPrecision, 4));

            var hyp1BigramPrecision = BleuScore.ModifiedPrecision(references, hyp1, 2);
            var hyp2BigramPrecision = BleuScore.ModifiedPrecision(references, hyp2, 2);

            Assert.Equal(0.58823529, hyp1BigramPrecision, 0.0001);
            Assert.Equal(0.07692307, hyp2BigramPrecision, 0.0001);

            Assert.Equal(0.5882, Math.Round(hyp1BigramPrecision, 4));
            Assert.Equal(0.0769, Math.Round(hyp2BigramPrecision, 4));
        }

        [Fact]
        public void TestBrevityPenalty()
        {
            var references = new string[][] { Enumerable.Repeat("a", 11).ToArray(), Enumerable.Repeat("a", 8).ToArray() };
            var hypothesis = Enumerable.Repeat("a", 7).ToArray();
            var hypLen = hypothesis.Length;
            var closestRefLen = BleuScore.ClosestRefLength(references, hypLen);
            Assert.Equal(0.8669, BleuScore.BrevityPenalty(closestRefLen, hypLen), 0.0001);

            references = [Enumerable.Repeat("a", 11).ToArray(), Enumerable.Repeat("a", 8).ToArray(), Enumerable.Repeat("a", 6).ToArray(), Enumerable.Repeat("a", 7).ToArray()];
            hypLen = hypothesis.Length;
            closestRefLen = BleuScore.ClosestRefLength(references, hypLen);
            Assert.Equal(1.0, BleuScore.BrevityPenalty(closestRefLen, hypLen));
        }

        [Fact]
        public void TestZeroMatches()
        {
            // Test case where there's 0 matches
            string[][] references = { "The candidate has no alignment to any of the references".Split() };
            string[] hypothesis = "John loves Mary".Split();

            // Test BLEU to nth order of n-grams, where n is len(hypothesis).
            for (int n = 1; n < hypothesis.Length; n++)
            {
                double[] weights = Enumerable.Repeat(1.0 / n, n).ToArray();  // Uniform weights.
                Assert.Equal(0.0, BleuScore.SentenceBleu(references, hypothesis, weights));
            }
        }

        [Fact]
        public void TestFullMatches()
        {
            // Test case where there's 100% matches
            string[][] references = new string[][] { "John loves Mary".Split() };
            string[] hypothesis = "John loves Mary".Split();

            // Test BLEU to nth order of n-grams, where n is len(hypothesis).
            for (int n = 1; n < hypothesis.Length; n++)
            {
                double[] weights = Enumerable.Repeat(1.0 / n, n).ToArray();  // Uniform weights.
                Assert.Equal(1.0, BleuScore.SentenceBleu(references, hypothesis, weights));
            }
        }

        [Fact]
        public void TestPartialMatchesHypothesisLongerThanReference()
        {
            string[][] references = { "John loves Mary".Split() };
            string[] hypothesis = "John loves Mary who loves Mike".Split();

            // Since no 4-grams matches were found the result should be zero
            // exp(w_1 * 1 * w_2 * 1 * w_3 * 1 * w_4 * -inf) = 0
            Assert.Equal(0.0, BleuScore.SentenceBleu(references, hypothesis), 0.0001);

            // Checks that the warning has been raised because len(reference) < 4.
            // In C#, there's no direct equivalent for Python's warnings, so this part is omitted.
        }
    }


    public class TestBLEUFringeCases
    {
        [Fact]
        public void TestCaseWhereNIsBiggerThanHypothesisLength()
        {
            var references = new string[] { "John", "loves", "Mary", "?" };
            var hypothesis = new string[] { "John", "loves", "Mary" };
            int n = hypothesis.Length + 1;
            double[] weights = new double[n];
            for (int i = 0; i < n; i++)
            {
                weights[i] = 1.0 / n;
            }

            double bleuScore = BleuScore.SentenceBleu(references, hypothesis, weights);

            Assert.Equal(0.0, bleuScore, 4);

            references = ["John", "loves", "Mary"];
            hypothesis = ["John", "loves", "Mary"];

            bleuScore = BleuScore.SentenceBleu(references, hypothesis, weights);

            Assert.Equal(0.0, bleuScore, 4);
        }

        [Fact]
        public void TestEmptyHypothesis()
        {
            var references = new string[] { "The", "candidate", "has", "no", "alignment", "to", "any", "of", "the", "references" };
            string[] hypothesis = [];

            double bleuScore = BleuScore.SentenceBleu(references, hypothesis);

            Assert.Equal(0.0, bleuScore);
        }

        [Fact]
        public void TestLengthOneHypothesis()
        {
            var references = new string[] { "The", "candidate", "has", "no", "alignment", "to", "any", "of", "the", "references" };
            var hypothesis = new string[] { "Foo" };

            //try
            //{
            //    double bleuScore = BleuScore.SentenceBleu(references, hypothesis, SmoothingFunction.Method4);
            //}
            //catch (Exception ex)
            //{
            //    Assert.IsType<ValueError>(ex);
            //}
        }

        [Fact]
        public void TestEmptyReferences()
        {
            var references = new string[][] { [] };
            var hypothesis = new string[] { "John", "loves", "Mary" };

            double bleuScore = BleuScore.SentenceBleu(references, hypothesis);

            Assert.Equal(0.0, bleuScore);
        }

        [Fact]
        public void TestEmptyReferencesAndHypothesis()
        {
            string[][] references = [[]];
            string[] hypothesis = [];

            double bleuScore = BleuScore.SentenceBleu(references, hypothesis);

            Assert.Equal(0.0, bleuScore);
        }

        [Fact]
        public void TestReferenceOrHypothesisShorterThanFourgrams()
        {
            var references = new string[] { "let", "it", "go" };
            var hypothesis = new string[] { "let", "go", "it" };

            double bleuScore = BleuScore.SentenceBleu(references, hypothesis);

            Assert.Equal(0.0, bleuScore, 4);
        }

        [Fact]
        public void TestNumpyWeights()
        {
            var references = new string[] { "The", "candidate", "has", "no", "alignment", "to", "any", "of", "the", "references" };
            var hypothesis = new string[] { "John", "loves", "Mary" };

            double[] weights = new double[4];
            for (int i = 0; i < 4; i++)
            {
                weights[i] = 0.25;
            }

            double bleuScore = BleuScore.SentenceBleu(references, hypothesis, weights);

            Assert.Equal(0.0, bleuScore);
        }
    }


    public class TestBLEUWithBadSentence
    {
        [Fact]
        public void TestCorpusBleuWithBadSentence()
        {
            var hyp = "Teo S yb , oe uNb , R , T t , , t Tue Ar saln S , , 5istsi l , 5oe R ulO sae oR R".Split();
            var refStr = "Their tasks include changing a pump on the faulty stokehold ."
                + "Likewise , two species that are very similar in morphology "
                + "were distinguished using genetics .";
            var references = new string[][][] { new string[][] { refStr.Split() } };
            var hypotheses = new string[][] { hyp };

            // Check that the warning is raised since no. of 2-grams < 0.
            // Verify that the BLEU output is undesired since no. of 2-grams < 0.
            Assert.Equal(0.0, BleuScore.CorpusBleu(references, hypotheses, new double[] { 0.25, 0.25, 0.25, 0.25 }), 0.0001);
        }
    }


    public class TestBLEUWithMultipleWeights
    {
        [Fact]
        public void TestCorpusBleuWithMultipleWeights()
        {
            string[] hyp1 = [
            "It",
                "is",
                "a",
                "guide",
                "to",
                "action",
                "which",
                "ensures",
                "that",
                "the",
                "military",
                "always",
                "obeys",
                "the",
                "commands",
                "of",
                "the",
                "party"];
            string[] ref1a = [
            "It",
                "is",
                "a",
                "guide",
                "to",
                "action",
                "that",
                "ensures",
                "that",
                "the",
                "military",
                "will",
                "forever",
                "heed",
                "Party",
                "commands"];
            string[] ref1b = [
            "It",
                "is",
                "the",
                "guiding",
                "principle",
                "which",
                "guarantees",
                "the",
                "military",
                "forces",
                "always",
                "being",
                "under",
                "the",
                "command",
                "of",
                "the",
                "Party"];
            string[] ref1c = [
    "It",
                "is",
                "the",
                "practical",
                "guide",
                "for",
                "the",
                "army",
                "always",
                "to",
                "heed",
                "the",
                "directions",
                "of",
                "the",
                "party",
            ];

            string[] hyp2 = [
    "he",
                "read",
                "the",
                "book",
                "because",
                "he",
                "was",
                "interested",
                "in",
                "world",
                "history",
            ];

            string[] ref2a = [
    "he",
                "was",
                "interested",
                "in",
                "world",
                "history",
                "because",
                "he",
                "read",
                "the",
                "book",
            ];
            var weight1 = new double[] { 1.0, 0.0, 0.0, 0.0 };
            var weight2 = new double[] { 0.25, 0.25, 0.25, 0.25 };
            var weight3 = new double[] { 0.0, 0.0, 0.0, 1.0 };

            double[] bleuScores = BleuScore.CorpusBleu(
                [[ref1a, ref1b, ref1c], [ref2a]],
                [hyp1, hyp2],
                new double[][] { weight1, weight2, weight3 }
            );

            Assert.Equal(bleuScores[0], BleuScore.CorpusBleu(
                [[ref1a, ref1b, ref1c], [ref2a]],
                [hyp1, hyp2],
                weight1
            ));

            Assert.Equal(bleuScores[1], BleuScore.CorpusBleu(
                [[ref1a, ref1b, ref1c], [ref2a]],
                [hyp1, hyp2],
                weight2
            ));

            Assert.Equal(bleuScores[2], BleuScore.CorpusBleu(
                [[ref1a, ref1b, ref1c], [ref2a]],
                [hyp1, hyp2],
                weight3
            ));

        }
    }
}