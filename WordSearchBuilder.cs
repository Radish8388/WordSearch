using System.Diagnostics;

namespace Word_Search
{
    public class WordSearchBuilder
    {
        public static WordSearchResult BuildPuzzle(List<string> candidateWords, int size, int attempts = 20)
        {
            List<string> bestSkipped = null;
            char[,] bestGrid = null;
            List<PlacedWord> bestPlaced = null;

            for (int i = 0; i < attempts; i++)
            {
                var generator = new WordSearchGenerator(size);
                var skipped = generator.GeneratePuzzle(candidateWords);

                if (bestSkipped == null || skipped.Count < bestSkipped.Count)
                {
                    bestSkipped = skipped;
                    bestGrid = generator.Grid;
                    bestPlaced = generator.PlacedWords.ToList();
                }

                // sort the placed words
                bestPlaced.Sort((a, b) => a.Word.CompareTo(b.Word));

                if (bestSkipped.Count == 0)
                    break; // fit everything already, no need to keep trying
            }

            return new WordSearchResult
            {
                Grid = bestGrid,
                PlacedWords = bestPlaced,
                SkippedWords = bestSkipped
            };
        }
    }

    public class WordSearchResult
    {
        public char[,] Grid;
        public List<PlacedWord> PlacedWords;
        public List<string> SkippedWords;
    }

    public class WordSearchGenerator
    {
        private readonly char[,] _grid;
        private readonly int _size;
        private readonly Random _rng = new Random();
        private readonly List<PlacedWord> _placedWords = new List<PlacedWord>();

        // 8 directions as (rowDelta, colDelta)
        private static readonly (int dr, int dc)[] Directions = new (int, int)[]
        {
        (0, 1),   // right
        (0, -1),  // left
        (1, 0),   // down
        (-1, 0),  // up
        (1, 1),   // down-right
        (1, -1),  // down-left
        (-1, 1),  // up-right
        (-1, -1)  // up-left
        };

        public WordSearchGenerator(int size)
        {
            _size = size;
            _grid = new char[size, size];
        }

        /*
        public bool GeneratePuzzle(List<string> words, int maxAttemptsPerWord = 100)
        {
            // Place longest words first — they're hardest to fit later
            var sortedWords = words
                .Select(w => w.ToUpperInvariant())
                .OrderByDescending(w => w.Length)
                .ToList();

            foreach (string word in sortedWords)
            {
                if (!TryPlaceWord(word, maxAttemptsPerWord))
                    return false; // caller can decide: retry whole puzzle, shrink word list, grow grid, etc.
            }

            FillEmptyCells();
            return true;
        }
        */
        public List<string> GeneratePuzzle(List<string> candidateWords, int maxAttemptsPerWord = 100)
        {
            var skippedWords = new List<string>();

            var sortedWords = candidateWords
                .Select(w => w.ToUpperInvariant())
                .OrderByDescending(w => w.Length)
                .ToList();

            foreach (string word in sortedWords)
            {
                if (!TryPlaceWord(word, maxAttemptsPerWord))
                    skippedWords.Add(word);
            }

            FillEmptyCells();
            return skippedWords; // caller can see what didn't make it in, if they care
        }

        private bool TryPlaceWord(string word, int maxAttempts)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var (dr, dc) = Directions[_rng.Next(Directions.Length)];

                // Valid starting range so the word fits within bounds in this direction
                int minRow = dr < 0 ? word.Length - 1 : 0;
                int maxRow = dr > 0 ? _size - word.Length : _size - 1;
                int minCol = dc < 0 ? word.Length - 1 : 0;
                int maxCol = dc > 0 ? _size - word.Length : _size - 1;

                if (minRow > maxRow || minCol > maxCol)
                    continue; // word can't fit in this direction at all

                int startRow = _rng.Next(minRow, maxRow + 1);
                int startCol = _rng.Next(minCol, maxCol + 1);

                if (CanPlaceWord(word, startRow, startCol, dr, dc))
                {
                    PlaceWord(word, startRow, startCol, dr, dc);
                    return true;
                }
            }
            return false;
        }

        private bool CanPlaceWord(string word, int startRow, int startCol, int dr, int dc)
        {
            for (int i = 0; i < word.Length; i++)
            {
                int r = startRow + dr * i;
                int c = startCol + dc * i;
                char existing = _grid[r, c];

                if (existing != '\0' && existing != word[i])
                    return false; // conflict: occupied by a different letter
            }
            return true;
        }

        private void PlaceWord(string word, int startRow, int startCol, int dr, int dc)
        {
            for (int i = 0; i < word.Length; i++)
            {
                int r = startRow + dr * i;
                int c = startCol + dc * i;
                _grid[r, c] = word[i];
            }

            _placedWords.Add(new PlacedWord
            {
                Word = word,
                StartRow = startRow,
                StartCol = startCol,
                DeltaRow = dr,
                DeltaCol = dc
            });
        }

        private void FillEmptyCells()
        {
            for (int r = 0; r < _size; r++)
            {
                for (int c = 0; c < _size; c++)
                {
                    if (_grid[r, c] == '\0')
                        _grid[r, c] = (char)('A' + _rng.Next(26));
                }
            }
        }

        public char[,] Grid => _grid;
        public IReadOnlyList<PlacedWord> PlacedWords => _placedWords;
    }

    public class PlacedWord
    {
        public string Word;
        public int StartRow;
        public int StartCol;
        public int DeltaRow;
        public int DeltaCol;
        public bool Found;

        public int EndRow => StartRow + DeltaRow * (Word.Length - 1);
        public int EndCol => StartCol + DeltaCol * (Word.Length - 1);
    }

    public class WordSearchSession
    {
        private List<string> _candidateWords;
        private List<string> _unusedWords;
        private readonly List<string> _fullWordList;
        private readonly int _refillThreshold;

        public WordSearchSession(List<string> fullWordList, int refillThreshold = 50)
        {
            _fullWordList = fullWordList;
            _refillThreshold = refillThreshold;
            _unusedWords = Shuffle(fullWordList);
            _candidateWords = new List<string>();
        }

        public WordSearchResult GenerateNextPuzzle(int gridSize)
        {
            _candidateWords.Clear();
            if (_unusedWords.Count > 0)
            {
                int max = Math.Min(49, _unusedWords.Count - 1);
                for (int i = max; i >= 0; i--)
                {
                    _candidateWords.Add(_unusedWords[i]);
                    _unusedWords.RemoveAt(i);
                }
            }

            var result = WordSearchBuilder.BuildPuzzle(_candidateWords, gridSize);
            Debug.WriteLine($"size={gridSize}, words={result.PlacedWords.Count}");

            if (_unusedWords.Count < _refillThreshold)
            {
                _unusedWords = Shuffle(_fullWordList);
                Debug.WriteLine("Refilled word list.");
            }

            return result;
        }

        private List<string> Shuffle(List<string> words)
        {
            return words.OrderBy(_ => Guid.NewGuid()).ToList();
            // or a proper Fisher-Yates if you'd rather avoid the Guid-sort idiom
        }
    }

}
