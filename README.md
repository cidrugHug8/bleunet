# BlueNet

## Overview
This library is a C# class library for calculating the BLEU and RIBES scores, which are metrics for evaluating the quality of machine translations. BLEU (Bilingual Evaluation Understudy) is an algorithm for evaluating the quality of text which has been machine-translated from one natural language to another. RIBES (Rank-based Intuitive Bilingual Evaluation Score) is an automatic metric for machine translation evaluation that is based on rank correlation coefficients between word pairs of reference and candidate translations.

## Installation
You can add this library to your project using the NuGet package manager.

```shell
Install-Package BleuNet
```

## Usage
The following code snippet shows the basic usage of this library.

```csharp
using BleuNet;

// Define the translated and reference sentences.
string referenceSentence = "The pessimist sees difficulty in every opportunity.";
string translatedSentence = "The pessimist sees difficulty at every opportunity.";

var referenceSentenceTokens = new string[][] { Utility.Tokenize(referenceSentence) };
var translatedSentenceTokens = new string[][] { Utility.Tokenize(translatedSentence) };

// Calculate the BLEU score.
double score = Metrics.CorpusBleu(referenceSentenceTokens, translatedSentenceTokens);

// Display the result.
Console.WriteLine("BLEU Score: " + score);

// Calculate the sentence BLEU score.
double sentenceBleu = Metrics.SentenceBleu(referenceSentenceTokens, Utility.Tokenize(translatedSentence));
Console.WriteLine("Sentence BLEU Score: " + sentenceBleu);
```

## New Update: Tokenize2 Method
I have added a new method `Tokenize2` to my library. This method's tokenization is designed to closely match the tokenization of the tokenizer.perl script included with the statistical machine translation tool Moses when specified with -l en.

Here is a basic usage example:

```csharp
string text = "The quick brown fox jumps over the lazy dog.";
string[] tokens = Utility.Tokenize2(text);
```

## References

**BLEU**:
1. Kishore Papineni, Salim Roukos, Todd Ward, and Wei-Jing Zhu, "[BLEU: a Method for Automatic Evaluation of Machine Translation](https://aclanthology.org/P02-1040)" (Papineni et al., ACL 2002)

**RIBES**:
1. Hideki Isozaki, Tsutomu Hirao, Kevin Duh, Katsuhito Sudoh, Hajime Tsukada, "[Automatic Evaluation of Translation Quality for Distant Language Pairs](https://aclanthology.org/D10-1092)" (Isozaki et al., EMNLP 2010)

## License
This project is licensed under the MIT license.
