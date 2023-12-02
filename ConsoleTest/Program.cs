using BleuNet;
using System.IO.MemoryMappedFiles;

//// Define the translated and reference sentences.
//string referenceSentence = "The pessimist sees difficulty in every opportunity.";
//string translatedSentence = "The pessimist sees difficulty at every opportunity.";

//var referenceSentenceTokens = new string[][] { BleuScore.Tokenize(referenceSentence) };
//var translatedSentenceTokens = new string[][] { BleuScore.Tokenize(translatedSentence) };

//// Calculate the BLEU score.
//double score = BleuScore.CorpusBleu(referenceSentenceTokens, translatedSentenceTokens);
////double score = BleuScore.CorpusBleu(referenceSentenceTokens, translatedSentenceTokens, [0.33, 0.33, 0.33]);

//// Display the result.
//Console.WriteLine("BLEU Score: " + score);

//// Define the translated and reference sentences.
//string referenceSentence = "The pessimist sees difficulty in every opportunity.";
//string translatedSentence = "The pessimist sees difficulty at every opportunity.";

//var referenceSentenceTokens = new string[][] { BleuScore.Tokenize(referenceSentence) };
//var translatedSentenceTokens = new string[][] { BleuScore.Tokenize(translatedSentence) };

//// Calculate the BLEU score.
//double score = BleuScore.CorpusBleu(referenceSentenceTokens, translatedSentenceTokens);

//// Display the result.
//Console.WriteLine("BLEU Score: " + score);

//var humanTranslation = new string[][] {
//    "The pessimist sees difficulty in every opportunity.".Split(),
//    "The optimist sees opportunity in every difficulty.".Split() 
//};
//var machineTranslation = new string[][] {
//    "Pessimists see difficulties at every opportunity.".Split(),
//    "An optimist sees an opportunity in every difficulty.".Split()
//};
//var bleuScore = BleuScore.CorpusBleu(humanTranslation, machineTranslation);
//Console.WriteLine(bleuScore);

//var humanTranslations = new string[][][] {
//    new string[][] {
//        "The pessimist sees difficulty in every opportunity.".Split(),
//        "A pessimist sees difficulty in every possibility.".Split()
//    },
//    new string[][] { 
//        "The optimist sees opportunity in every difficulty.".Split()
//    }                                                             
//};
//var weights = new double[] { 0.25, 0.25, 0.25, 0.25 };
//var bleuScores = BleuScore.CorpusBleu(humanTranslations, machineTranslation, weights);
//Console.WriteLine(bleuScores);

//var z = BleuScore.Tokenize("The pessimist sees difficulty in every opportunity.");

//humanTranslation = new string[][] {
//    BleuScore.Tokenize("The pessimist sees difficulty in every opportunity."),
//    BleuScore.Tokenize("The optimist sees opportunity in every difficulty.")
//};
//machineTranslation = new string[][] {
//    BleuScore.Tokenize("Pessimists see difficulties at every opportunity."),
//    BleuScore.Tokenize("An optimist sees an opportunity in every difficulty.")
//};
//bleuScore = BleuScore.CorpusBleu(humanTranslation, machineTranslation);
//Console.WriteLine(bleuScore);

for (var i = 0; i < 100; i++)
{
    var references = File.ReadAllLines("./reference.txt")
    .Select(x => BleuScore.Tokenize(x))
    .ToArray();
    var hypotheses = File.ReadAllLines("./hypothesis.txt")
        .Select(x => BleuScore.Tokenize(x))
        .ToArray();
    var bleuScore = BleuScore.CorpusBleu(references, hypotheses);
    //Console.WriteLine("BLEU Score: " + bleuScore);
}
