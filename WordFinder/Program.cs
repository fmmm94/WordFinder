using Newtonsoft.Json;
using WordFinderApp;

Console.WriteLine("Loading matrix from file Matrix.txt");
string matrixFilePath = Path.Combine("Matrix.txt");
IEnumerable<string> lines = File.ReadLines(matrixFilePath);

Console.WriteLine("Loading words from file Wordstream.json");
string wordstreamFilePath = Path.Combine("Wordstream.json");
IEnumerable<string> wordstream;
using (StreamReader r = new(wordstreamFilePath))
{
    string json = r.ReadToEnd();
    wordstream = JsonConvert.DeserializeObject<IEnumerable<string>>(json)!;
}
Console.WriteLine($"Found {wordstream.Count()} words as input");

WordFinder wordFinder = new(lines);
wordFinder.Find(wordstream);
Console.WriteLine("End.");