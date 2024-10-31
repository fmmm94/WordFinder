using System.ComponentModel;
using System.Diagnostics;

namespace WordFinderApp
{
    public class WordFinder
    {
        // Amount of word results
        private const int NUMBER_OF_RESULTS = 10;
        private const int MAX_SIZE = 64;

        // Our matrix rows. The format is a dictionary with the row index as key, and the string as value.
        private readonly string[] _rows;
        // Our matrix columns. The format is a dictionary with the column index as key, and the string as value. 
        private readonly string[] _columns;

        // Input list is assumed as list of rows:
        //      [
        //          "abc",
        //          "def",
        //          "ghi",
        //      ]
        // Matrix is assumed as:
        //      [ a b c ]
        //      [ d e f ]
        //      [ g h i ]
        // Matrix can be NxM, no need for it to be NxN
        public WordFinder(IEnumerable<string> matrix) 
        {
            Console.WriteLine($"Started Setting up matrix");
            int inputColumnSize = matrix.First().Length;
            if (matrix.Count() > MAX_SIZE || inputColumnSize > MAX_SIZE)
                throw new($"Matrix size over limit. Current limit {MAX_SIZE}");
            
            if (!matrix.All(x => x.Length == inputColumnSize))
                throw new($"Matrix columns do not have the same size");

            string[] rows = new string[matrix.Count()];
            string[] columns = new string[inputColumnSize];

            var configWatch = Stopwatch.StartNew();
            foreach ((int, string) matrixRow in matrix.Select((w, i) => (i, w))) 
            {
                // Here we save the rows for our matrix.
                rows[matrixRow.Item1] = matrixRow.Item2;

                // Fetch the letters on our row and add them to the column data.
                for (int i = 0; i < inputColumnSize; i++)
                    // We concat each char to the previous from the same column index.
                    columns[i] += matrixRow.Item2[i];
            }

            _rows = rows;
            _columns = columns;
            configWatch.Stop();
            Console.WriteLine($"Finished Setting up matrix. Elapsed: {configWatch.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Give a list of words as imputs, find those that exists in the constructor matrix, and returns those with highest count.
        /// </summary>
        /// <param name="wordstream">List of words to find their ocurrence count</param>
        /// <returns>Returns the words with the most counts. <see cref="NUMBER_OF_RESULTS"/> for the number of results</returns>
        public IEnumerable<string> Find(IEnumerable<string> wordstream)
        {
            Console.WriteLine($"Started finding the words");
            var searchWatch = Stopwatch.StartNew();
            Dictionary<string, int> frecuentWords = [];
            KeyValuePair<string, int> minValue = new("",0);

            // Group incoming words by their starting letter so we can filter out rows and columns that don't contain that letter.
            // This helps us avoid itererating over rows and columns for each incoming word unnecessarily.
            foreach (IGrouping<char, string> wordsByLetter in wordstream.Distinct().GroupBy(x => x[0], x => x))
            {
                // Save the column index we already searched while moving through the matrix.
                HashSet<int> searchedColumns = [];

                // Move through the matrix by each row.
                for (int rowIndex = 0; rowIndex < _rows.Length; rowIndex++)
                {
                    // Our current row
                    string row = _rows[rowIndex];

                    // If the row does not contain the first letter of the word group, then we can skip it.
                    int firstOccurenceIndex = row.IndexOf(wordsByLetter.Key);
                    if (firstOccurenceIndex == -1)
                        continue;

                    // Columns to check for out word group.
                    HashSet<int> columnsToSearch = [];

                    // Iterate over each instance of the letter in our row.
                    // If one is found, and we didn't check before, we will later check that column downwards.
                    int currentOccurenceIndex = firstOccurenceIndex;
                    while (currentOccurenceIndex > -1)
                    {
                        if (searchedColumns.Add(currentOccurenceIndex))
                            columnsToSearch.Add(currentOccurenceIndex);
                        currentOccurenceIndex = row.IndexOf(wordsByLetter.Key, currentOccurenceIndex + 1);
                    }

                    // Iterate through each word in out word group.
                    // All words here start with the same letter.
                    foreach (string word in wordsByLetter)
                    {
                        // We only want the text that comes after the first time we found our group letter.
                        // i.e: given 'ahellonam', and our group letter is 'h', we want 'helloam', since all
                        //      words here start with the same letter.
                        string rowText = row[firstOccurenceIndex..];

                        // When moving through the matrix, we evaluate first the row, and then all columns
                        // that contain that group letter. If the input has words with 1 letter, then
                        // we don't need to add the row count, as that letter count will be picked
                        // by the column text evaluation.
                        // i.e: a[k]jo[k]s (our group letter is k)
                        //      f[a]cl[k]j
                        int count = word.Length > 1 ? GetWordOcurrences(word, rowText) : 0;

                        // Evaluate the ocurrences of out word in the columns we found our group letter.
                        foreach (int columnIndex in columnsToSearch)
                            count += GetWordOcurrences(word, _columns[columnIndex][rowIndex..]);

                        if (count > 0)
                        {
                            // If word count exists, sum the old value and new one.
                            // If not, create a new entry with current count.
                            if (frecuentWords.TryGetValue(word, out int oldCount))
                                frecuentWords[word] += count;
                            else
                                frecuentWords.Add(word, count);
                        }
                    }
                }
            }
            searchWatch.Stop();
            Console.WriteLine($"Finished Searching. Elapsed: {searchWatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Result:");

            KeyValuePair<string,int>[] results = [.. frecuentWords.OrderByDescending(x => x.Value).Take(NUMBER_OF_RESULTS)];
            foreach (KeyValuePair<string, int> kvp in results)
            {
                Console.WriteLine($"    Word = {kvp.Key}, Count = {kvp.Value}");
            }
            return results.Select(word => word.Key);
        }

        private int GetWordOcurrences(string word, string text)
        {
            if(word.Length > text.Length || !text.Contains(word))
                return 0;

            return (text.Length - text.Replace(word, "").Length) / word.Length;
        }
    }
}
