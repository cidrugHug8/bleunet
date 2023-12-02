# BlueNet

## Overview
This library is a C# class library for calculating the BLEU score, a metric for evaluating the quality of machine translations.

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

var referenceSentenceTokens = new string[][] { BleuScore.Tokenize(referenceSentence) };
var translatedSentenceTokens = new string[][] { BleuScore.Tokenize(translatedSentence) };

// Calculate the BLEU score.
double score = BleuScore.CorpusBleu(referenceSentenceTokens, translatedSentenceTokens);

// Display the result.
Console.WriteLine("BLEU Score: " + score);
```

## License
This project is licensed under the MIT license.
