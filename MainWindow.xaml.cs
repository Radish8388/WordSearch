using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

/* TODO list
 * multiple word lists???
 */

namespace Word_Search
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer _hintTimer;
        DispatcherTimer _gameTimer;
        Stopwatch _playingTime;
        int _puzzleSize = 0;
        WordSearchSession _session8;
        WordSearchSession _session12;
        WordSearchSession _session16;
        double _leftMargin, _topMargin;
        double _cellSize;
        char[,] _grid;
        List<PlacedWord> _placedWords;
        int _fontSize = 0;
        int _topWord = 0;
        int _displayedWords = 0;
        Rectangle _selectionRect;
        Rectangle _hintRect;
        int _startRow, _startCol;
        bool _isMouseDown = false;
        int _rowHeight = 30;
        bool _gameOver = true;
        int _wordsRemaining = 1000;
        int _gameScore = 0;
        int _hintsRemaining = 3;
        bool _isPaused = false;

        public MainWindow()
        {
            InitializeComponent();

            // initialize hint timer
            _hintTimer = new DispatcherTimer();
            _hintTimer.Interval = TimeSpan.FromMilliseconds(1500);
            _hintTimer.Tick += HintTimer_Tick;

            // initialize game timer
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _gameTimer.Tick += GameTimer_Tick;

            _playingTime = new Stopwatch();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // load the properties from disk
            Properties.Settings.Default.Reload();

            // check for upgrade
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            this.Left = Properties.Settings.Default.WindowLeft;
            this.Top = Properties.Settings.Default.WindowTop;
            this.Width = Properties.Settings.Default.WindowWidth;
            this.Height = Properties.Settings.Default.WindowHeight;

            // load other properties here
            _puzzleSize = Properties.Settings.Default.PuzzleSize;

            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            // ensure window size doesn't exceed screen size
            if (this.Width > screenWidth) this.Width = screenWidth;
            if (this.Height > screenHeight) this.Height = screenHeight;

            // ensure window is not off the left or top
            if (this.Left < 0) this.Left = 0;
            if (this.Top < 0) this.Top = 0;

            // ensure window is not off the right or bottom
            if (this.Left + this.Width > screenWidth)
                this.Left = screenWidth - this.Width;
            if (this.Top + this.Height > screenHeight)
                this.Top = screenHeight - this.Height;

            if (Properties.Settings.Default.WindowState == "Maximized")
                this.WindowState = WindowState.Maximized;

            // do other initialization
            Initialize();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.WindowState = this.WindowState.ToString();
            if (this.WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.WindowLeft = this.Left;
                Properties.Settings.Default.WindowTop = this.Top;
                Properties.Settings.Default.WindowWidth = this.Width;
                Properties.Settings.Default.WindowHeight = this.Height;
            }

            // save other properties here
            Properties.Settings.Default.PuzzleSize = _puzzleSize;

            Properties.Settings.Default.Save();
        }

        private void Size8_Click(object sender, RoutedEventArgs e)
        {
            _puzzleSize = 8;
            NewPuzzle(_puzzleSize);
        }

        private void Size12_Click(object sender, RoutedEventArgs e)
        {
            _puzzleSize = 12;
            NewPuzzle(_puzzleSize);
        }

        private void Size16_Click(object sender, RoutedEventArgs e)
        {
            _puzzleSize = 16;
            NewPuzzle(_puzzleSize);
        }

        private void canvas1_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_gameOver)
            {
                _isMouseDown = true;
                Point p = e.GetPosition(canvas1);
                int row = (int)((p.Y - _topMargin) / _cellSize);
                row = Math.Clamp(row, 0, _puzzleSize - 1);
                int col = (int)((p.X - _leftMargin) / _cellSize);
                col = Math.Clamp(col, 0, _puzzleSize - 1);

                _selectionRect.Visibility = Visibility.Visible;
                canvas1.CaptureMouse();
                _startRow = row;
                _startCol = col;
                //Debug.WriteLine($"Down: start row,col={_startRow},{_startCol}, end row,col={row},{col}");
                //DrawSelectionRectangle(row, col);
                HighlightWord(_selectionRect, _startRow, _startCol, row, col);
            }
        }

        private void canvas1_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                Point p = e.GetPosition(canvas1);
                int row = (int)((p.Y - _topMargin) / _cellSize);
                row = Math.Clamp(row, 0, _puzzleSize - 1);
                int col = (int)((p.X - _leftMargin) / _cellSize);
                col = Math.Clamp(col, 0, _puzzleSize - 1);
                //Debug.WriteLine($"Move: start row,col={_startRow},{_startCol}, end row,col={row},{col}");

                //DrawSelectionRectangle(row, col);
                HighlightWord(_selectionRect, _startRow, _startCol, row, col);
            }
        }

        private void canvas1_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isMouseDown)
            {
                _isMouseDown = false;
                _selectionRect.Visibility = Visibility.Collapsed;
                canvas1.ReleaseMouseCapture();
                Point p = e.GetPosition(canvas1);
                int row = (int)((p.Y - _topMargin) / _cellSize);
                row = Math.Clamp(row, 0, _puzzleSize - 1);
                int col = (int)((p.X - _leftMargin) / _cellSize);
                col = Math.Clamp(col, 0, _puzzleSize - 1);
                //Debug.WriteLine($"Up: start row,col={_startRow},{_startCol}, end row,col={row},{col}");
                int wordNumber = FindSelectionInWordList(_startRow, _startCol, row, col);
                //Debug.WriteLine($"found word # {wordNumber}");
                if (wordNumber >= 0 && !_placedWords[wordNumber].Found)
                {
                    _placedWords[wordNumber].Found = true;
                    RedrawPuzzle();
                    RedrawWordList();
                    _wordsRemaining--;
                    _gameScore += _placedWords[wordNumber].Word.Length * 5;
                    Score.Text = $"Score : {_gameScore}";
                    if (_wordsRemaining <= 0)
                    {
                        _gameOver = true;
                        //_gameScore += _puzzleSize * _puzzleSize * 10 - (int)_playingTime.Elapsed.TotalSeconds;
                        int timeBonus = Math.Max(0, _puzzleSize * _puzzleSize * 10 - (int)_playingTime.Elapsed.TotalSeconds);
                        _gameScore += timeBonus;
                        Score.Text = $"Score : {_gameScore}";
                        RecordHighScore(_gameScore, DateTime.Now);
                        Winner dialog = new Winner();
                        dialog.Owner = this;
                        bool? result = dialog.ShowDialog();
                    }
                }
            }
        }

        private void canvas1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DetermineSize();
            RedrawPuzzle();
            RedrawWordList();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            NewPuzzle(_puzzleSize);
        }

        private void canvas2_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DetermineSize();
            RedrawPuzzle();
            RedrawWordList();
        }

        private void canvas2_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_hintsRemaining <= 0)
            {
                NoMoreHints dialog = new NoMoreHints();
                dialog.Owner = this;
                bool? result = dialog.ShowDialog();
            }
            else
            {
                Point p = e.GetPosition(canvas2);
                int row = ((int)p.Y - 5) / _rowHeight;
                int i = _topWord + row;
                _hintRect.Visibility = Visibility.Visible;
                HighlightWord(_hintRect, _placedWords[i].StartRow, _placedWords[i].StartCol,
                    _placedWords[i].EndRow, _placedWords[i].EndCol);
                _hintTimer.Start();
                _hintsRemaining--;
                _gameScore -= _placedWords[i].Word.Length * 5;
                Score.Text = $"Score : {_gameScore}";
            }
        }

        private void Initialize()
        {
            // get small word list
            string[] lines;
            string wordFile = "pack://application:,,,/words/smallList.txt";
            List<string> wordList = new List<string>();

            var uri = new Uri(wordFile, UriKind.Absolute);
            var stream = Application.GetResourceStream(uri);
            using (StreamReader reader = new StreamReader(stream.Stream))
            {
                string content = reader.ReadToEnd();
                lines = content.Split('\n');
            }

            wordList.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                string word = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(word))
                    wordList.Add(word);
            }
            _session8 = new WordSearchSession(wordList);

            // get medium word list
            wordFile = "pack://application:,,,/words/mediumList.txt";

            uri = new Uri(wordFile, UriKind.Absolute);
            stream = Application.GetResourceStream(uri);
            using (StreamReader reader = new StreamReader(stream.Stream))
            {
                string content = reader.ReadToEnd();
                lines = content.Split('\n');
            }

            wordList.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                string word = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(word))
                    wordList.Add(word);
            }
            _session12 = new WordSearchSession(wordList);

            // get large word list
            wordFile = "pack://application:,,,/words/largeList.txt";

            uri = new Uri(wordFile, UriKind.Absolute);
            stream = Application.GetResourceStream(uri);
            using (StreamReader reader = new StreamReader(stream.Stream))
            {
                string content = reader.ReadToEnd();
                lines = content.Split('\n');
            }

            wordList.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                string word = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(word))
                    wordList.Add(word);
            }
            _session16 = new WordSearchSession(wordList);
        }

        private void NewPuzzle(int size)
        {
            WordSearchResult? result = null;
            switch (size)
            {
                case 8: result = _session8.GenerateNextPuzzle(_puzzleSize); break;
                case 12: result = _session12.GenerateNextPuzzle(_puzzleSize); break;
                case 16: result = _session16.GenerateNextPuzzle(_puzzleSize); break;
            }
            if (result != null)
            {
                _grid = result.Grid;
                _placedWords = result.PlacedWords;
                _wordsRemaining = _placedWords.Count;
                _topWord = 0;
                _gameOver = false;
                _gameScore = 0;
                _hintsRemaining = 3;
                _gameTimer.Start();
                _playingTime.Restart();
                DetermineSize();
                RedrawPuzzle();
                RedrawWordList();
            }
        }

        private void DetermineSize()
        {
            double size = Math.Min(canvas1.ActualWidth, canvas1.ActualHeight);
            _leftMargin = (canvas1.ActualWidth - size) / 2.0;
            _topMargin = (canvas1.ActualHeight - size) / 2.0;
            _cellSize = size / _puzzleSize;

            TextBlock label = new TextBlock();
            label.Text = "W";
            for (int i = 100; i > 0; i--)
            {
                label.FontSize = i;
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double width = label.DesiredSize.Width;
                double height = label.DesiredSize.Height;
                if (width < _cellSize * 0.75 && height < _cellSize * 0.75)
                {
                    _fontSize = i;
                    break;
                }
            }
            //Debug.WriteLine($"font size = {_fontSize}");
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            _topWord -= _displayedWords - 1;
            _topWord = Math.Max(_topWord, 0);
            RedrawWordList();
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            _topWord += _displayedWords - 1;
            _topWord = Math.Min(_topWord, _placedWords.Count - _displayedWords);
            _topWord = Math.Max(_topWord, 0);
            RedrawWordList();
        }

        private void RedrawPuzzle()
        {
            canvas1.Children.Clear();

            Rectangle outer = new Rectangle();
            outer.Width = _cellSize * _puzzleSize;
            outer.Height = _cellSize * _puzzleSize;
            outer.Stroke = Brushes.Black;
            outer.StrokeThickness = 2;
            Canvas.SetLeft(outer, _leftMargin);
            Canvas.SetTop(outer, _topMargin);
            canvas1.Children.Add(outer);

            TextBlock letter;
            if (_grid != null)
            {
                for (int r = 0; r < _puzzleSize; r++)
                    for (int c = 0; c < _puzzleSize; c++)
                    {
                        letter = new TextBlock();
                        if (_grid[r, c] < 'A' || _grid[r, c] > 'Z')
                        {
                            int number = _grid[r, c];
                            letter.Text = number.ToString();
                            letter.FontSize = 10;
                        }
                        else
                        {
                            letter.Text = _grid[r, c].ToString();
                            letter.FontSize = _fontSize;
                        }
                        letter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        double width = letter.DesiredSize.Width;
                        double height = letter.DesiredSize.Height;
                        double left = _leftMargin + (c+0.5) * _cellSize - width * 0.5;
                        double top = _topMargin + (r+0.5) * _cellSize - height * 0.5;
                        Canvas.SetLeft(letter, left);
                        Canvas.SetTop(letter, top);
                        Panel.SetZIndex(letter, 10);
                        canvas1.Children.Add(letter);

                        Rectangle inner = new Rectangle();
                        inner.Width = _cellSize;
                        inner.Height = _cellSize;
                        inner.Stroke = Brushes.Black;
                        inner.StrokeThickness = 1;
                        Canvas.SetLeft(inner, _leftMargin + c * _cellSize);
                        Canvas.SetTop(inner, _topMargin + r * _cellSize);
                        //canvas1.Children.Add(inner);
                    }

                _selectionRect = new Rectangle();
                _selectionRect.Visibility = Visibility.Collapsed;
                _selectionRect.Fill = Brushes.Yellow;
                Panel.SetZIndex(_selectionRect, 5);
                canvas1.Children.Add(_selectionRect);

                _hintRect = new Rectangle();
                _hintRect.Visibility = Visibility.Collapsed;
                _hintRect.Fill = Brushes.DeepPink;
                Panel.SetZIndex(_hintRect, 4);
                canvas1.Children.Add(_hintRect);

                Rectangle rect;
                for (int i=0; i< _placedWords.Count; i++)
                {
                    if (_placedWords[i].Found)
                    {
                        rect = new Rectangle();
                        rect.Fill = Brushes.Lime;
                        HighlightWord(rect, _placedWords[i].StartRow, _placedWords[i].StartCol,
                            _placedWords[i].EndRow, _placedWords[i].EndCol);
                        canvas1.Children.Add(rect);
                    }
                }
            }
        }

        private void RedrawWordList()
        {
            canvas2.Children.Clear();
            TextBlock word;

            if (_placedWords != null)
            {
                _displayedWords = ((int)canvas2.ActualHeight - 10) / _rowHeight;
                int numberToShow = Math.Min(_displayedWords, _placedWords.Count);
                for (int i = 0; i < numberToShow; i++)
                {
                    word = new TextBlock();
                    word.Text = _placedWords[_topWord+i].Word;
                    word.FontSize = _rowHeight * 0.8;
                    if (_placedWords[_topWord + i].Found)
                    {
                        word.Background = Brushes.Lime;
                        word.Foreground = Brushes.Black;
                    }
                    double left = 5;
                    double top = 5 + i * _rowHeight;
                    Canvas.SetLeft(word, left);
                    Canvas.SetTop(word, top);
                    canvas2.Children.Add(word);
                }
                //Debug.WriteLine($"placed words = {_placedWords.Count}");
            }
        }

        private void DrawSelectionRectangle(int row, int col)
        {
            _selectionRect.RadiusX = _cellSize * 0.4;
            _selectionRect.RadiusY = _cellSize * 0.4;
            _selectionRect.RenderTransformOrigin = new Point(0, 0);
            _selectionRect.RenderTransform = new RotateTransform(0);
            //Debug.WriteLine($"Draw: start row,col={_startRow},{_startCol}, end row,col={row},{col}");

            if (row == _startRow && col == _startCol) // single cell
            {
                _selectionRect.Width = _cellSize * 0.8;
                _selectionRect.Height = _cellSize * 0.8;
                Canvas.SetLeft(_selectionRect, _leftMargin + (_startCol + 0.1) * _cellSize);
                Canvas.SetTop(_selectionRect, _topMargin + (_startRow + 0.1) * _cellSize);
            }

            else if (row == _startRow && col > _startCol) // left to right
            {
                _selectionRect.Width = 2 * _cellSize * 0.9 + (col - _startCol - 1) * _cellSize;
                _selectionRect.Height = _cellSize * 0.8;
                Canvas.SetLeft(_selectionRect, _leftMargin + (_startCol + 0.1) * _cellSize);
                Canvas.SetTop(_selectionRect, _topMargin + (_startRow + 0.1) * _cellSize);
            }

            else if (row == _startRow && col < _startCol) // right to left
            {
                _selectionRect.Width = 2 * _cellSize * 0.9 + (_startCol - col - 1) * _cellSize;
                _selectionRect.Height = _cellSize * 0.8;
                Canvas.SetLeft(_selectionRect, _leftMargin + (col + 0.1) * _cellSize);
                Canvas.SetTop(_selectionRect, _topMargin + (row + 0.1) * _cellSize);
            }

            else if (row > _startRow && col == _startCol) // upper to lower
            {
                _selectionRect.Height = 2 * _cellSize * 0.9 + (row - _startRow - 1) * _cellSize;
                _selectionRect.Width = _cellSize * 0.8;
                Canvas.SetLeft(_selectionRect, _leftMargin + (_startCol + 0.1) * _cellSize);
                Canvas.SetTop(_selectionRect, _topMargin + (_startRow + 0.1) * _cellSize);
            }

            else if (row < _startRow && col == _startCol) // lower to upper
            {
                _selectionRect.Height = 2 * _cellSize * 0.9 + (_startRow - row - 1) * _cellSize;
                _selectionRect.Width = _cellSize * 0.8;
                Canvas.SetLeft(_selectionRect, _leftMargin + (col + 0.1) * _cellSize);
                Canvas.SetTop(_selectionRect, _topMargin + (row + 0.1) * _cellSize);
            }

            else if (Math.Abs(row - _startRow) == Math.Abs(col - _startCol)) // all diagonals
            {

                if (row > _startRow && col > _startCol) // upper left to lower right
                {
                    _selectionRect.Width = 2 * _cellSize * 0.775 * 1.414 + (col - _startCol - 1) * _cellSize * 1.414;
                    _selectionRect.Height = _cellSize * 0.8;
                    _selectionRect.RenderTransformOrigin = new Point(0, 0);
                    _selectionRect.RenderTransform = new RotateTransform(45);
                    Canvas.SetLeft(_selectionRect, _leftMargin + (_startCol + 0.5) * _cellSize);
                    Canvas.SetTop(_selectionRect, _topMargin + (_startRow - 0.05) * _cellSize);
                }

                else if (row < _startRow && col > _startCol) // lower left to upper right
                {
                    _selectionRect.Width = 2 * _cellSize * 0.775 * 1.414 + (col - _startCol - 1) * _cellSize * 1.414;
                    _selectionRect.Height = _cellSize * 0.8;
                    _selectionRect.RenderTransformOrigin = new Point(0, 1);
                    _selectionRect.RenderTransform = new RotateTransform(-45);
                    Canvas.SetLeft(_selectionRect, _leftMargin + (_startCol + 0.5) * _cellSize);
                    Canvas.SetTop(_selectionRect, _topMargin + (_startRow + 0.25) * _cellSize);
                }

                else if (row > _startRow && col < _startCol) // upper right to lower left
                {
                    _selectionRect.Width = 2 * _cellSize * 0.775 * 1.414 + (_startCol - col - 1) * _cellSize * 1.414;
                    _selectionRect.Height = _cellSize * 0.8;
                    _selectionRect.RenderTransformOrigin = new Point(0, 0);
                    _selectionRect.RenderTransform = new RotateTransform(135);
                    Canvas.SetLeft(_selectionRect, _leftMargin + (_startCol + 1.05) * _cellSize);
                    Canvas.SetTop(_selectionRect, _topMargin + (_startRow + 0.5) * _cellSize);
                }

                else if (row < _startRow && col < _startCol) // lower right to upper left
                {
                    _selectionRect.Width = 2 * _cellSize * 0.775 * 1.414 + (_startCol - col - 1) * _cellSize * 1.414;
                    _selectionRect.Height = _cellSize * 0.8;
                    _selectionRect.RenderTransformOrigin = new Point(0, 1);
                    _selectionRect.RenderTransform = new RotateTransform(-135);
                    Canvas.SetLeft(_selectionRect, _leftMargin + (_startCol + 1.05) * _cellSize);
                    Canvas.SetTop(_selectionRect, _topMargin + (_startRow - 0.3) * _cellSize);
                }

                else
                {
                    _selectionRect.Width = _cellSize * 0.8;
                    _selectionRect.Height = _cellSize * 0.8;
                    Canvas.SetLeft(_selectionRect, _leftMargin + (_startCol + 0.1) * _cellSize);
                    Canvas.SetTop(_selectionRect, _topMargin + (_startRow + 0.1) * _cellSize);
                }

            }

            else
            {
                _selectionRect.Width = _cellSize * 0.8;
                _selectionRect.Height = _cellSize * 0.8;
                Canvas.SetLeft(_selectionRect, _leftMargin + (_startCol + 0.1) * _cellSize);
                Canvas.SetTop(_selectionRect, _topMargin + (_startRow + 0.1) * _cellSize);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Debug.WriteLine($"Window width = {this.Width}, height = {this.Height}");
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!_gameOver && !_isPaused)
                _playingTime.Stop();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (!_gameOver && !_isPaused)
                _playingTime.Start();
        }

        private int FindSelectionInWordList(int startRow, int startCol, int endRow, int endCol)
        {
            for (int i=0; i<_placedWords.Count; i++)
            {
                if ((startRow == _placedWords[i].StartRow) &&
                    (endRow == _placedWords[i].EndRow) &&
                    (startCol == _placedWords[i].StartCol) &&
                    (endCol == _placedWords[i].EndCol))
                    return i;
                if ((endRow == _placedWords[i].StartRow) &&
                    (startRow == _placedWords[i].EndRow) &&
                    (endCol == _placedWords[i].StartCol) &&
                    (startCol == _placedWords[i].EndCol))
                    return i;
            }
            return -1;
        }

        private void HighlightWord(Rectangle rect, int startRow, int startCol, int endRow, int endCol)
        {
            rect.RadiusX = _cellSize * 0.4;
            rect.RadiusY = _cellSize * 0.4;
            rect.RenderTransformOrigin = new Point(0, 0);
            rect.RenderTransform = new RotateTransform(0);
            //Debug.WriteLine($"Draw: start row,col={_startRow},{_startCol}, end row,col={row},{col}");

            if (endRow == startRow && endCol == startCol) // single cell
            {
                rect.Width = _cellSize * 0.8;
                rect.Height = _cellSize * 0.8;
                Canvas.SetLeft(rect, _leftMargin + (startCol + 0.1) * _cellSize);
                Canvas.SetTop(rect, _topMargin + (startRow + 0.1) * _cellSize);
            }

            else if (endRow == startRow && endCol > startCol) // left to right
            {
                rect.Width = 2 * _cellSize * 0.9 + (endCol - startCol - 1) * _cellSize;
                rect.Height = _cellSize * 0.8;
                Canvas.SetLeft(rect, _leftMargin + (startCol + 0.1) * _cellSize);
                Canvas.SetTop(rect, _topMargin + (startRow + 0.1) * _cellSize);
            }

            else if (endRow == startRow && endCol < startCol) // right to left
            {
                rect.Width = 2 * _cellSize * 0.9 + (startCol - endCol - 1) * _cellSize;
                rect.Height = _cellSize * 0.8;
                Canvas.SetLeft(rect, _leftMargin + (endCol + 0.1) * _cellSize);
                Canvas.SetTop(rect, _topMargin + (endRow + 0.1) * _cellSize);
            }

            else if (endRow > startRow && endCol == startCol) // upper to lower
            {
                rect.Height = 2 * _cellSize * 0.9 + (endRow - startRow - 1) * _cellSize;
                rect.Width = _cellSize * 0.8;
                Canvas.SetLeft(rect, _leftMargin + (startCol + 0.1) * _cellSize);
                Canvas.SetTop(rect, _topMargin + (startRow + 0.1) * _cellSize);
            }

            else if (endRow < startRow && endCol == startCol) // lower to upper
            {
                rect.Height = 2 * _cellSize * 0.9 + (startRow - endRow - 1) * _cellSize;
                rect.Width = _cellSize * 0.8;
                Canvas.SetLeft(rect, _leftMargin + (endCol + 0.1) * _cellSize);
                Canvas.SetTop(rect, _topMargin + (endRow + 0.1) * _cellSize);
            }

            else if (Math.Abs(endRow - startRow) == Math.Abs(endCol - startCol)) // all diagonals
            {

                if (endRow > startRow && endCol > startCol) // upper left to lower right
                {
                    rect.Width = 2 * _cellSize * 0.775 * 1.414 + (endCol - startCol - 1) * _cellSize * 1.414;
                    rect.Height = _cellSize * 0.8;
                    rect.RenderTransformOrigin = new Point(0, 0);
                    rect.RenderTransform = new RotateTransform(45);
                    Canvas.SetLeft(rect, _leftMargin + (startCol + 0.5) * _cellSize);
                    Canvas.SetTop(rect, _topMargin + (startRow - 0.05) * _cellSize);
                }

                else if (endRow < startRow && endCol > startCol) // lower left to upper right
                {
                    rect.Width = 2 * _cellSize * 0.775 * 1.414 + (endCol - startCol - 1) * _cellSize * 1.414;
                    rect.Height = _cellSize * 0.8;
                    rect.RenderTransformOrigin = new Point(0, 1);
                    rect.RenderTransform = new RotateTransform(-45);
                    Canvas.SetLeft(rect, _leftMargin + (startCol + 0.5) * _cellSize);
                    Canvas.SetTop(rect, _topMargin + (startRow + 0.25) * _cellSize);
                }

                else if (endRow > startRow && endCol < startCol) // upper right to lower left
                {
                    rect.Width = 2 * _cellSize * 0.775 * 1.414 + (startCol - endCol - 1) * _cellSize * 1.414;
                    rect.Height = _cellSize * 0.8;
                    rect.RenderTransformOrigin = new Point(0, 0);
                    rect.RenderTransform = new RotateTransform(135);
                    Canvas.SetLeft(rect, _leftMargin + (startCol + 1.05) * _cellSize);
                    Canvas.SetTop(rect, _topMargin + (startRow + 0.5) * _cellSize);
                }

                else if (endRow < startRow && endCol < startCol) // lower right to upper left
                {
                    rect.Width = 2 * _cellSize * 0.775 * 1.414 + (startCol - endCol - 1) * _cellSize * 1.414;
                    rect.Height = _cellSize * 0.8;
                    rect.RenderTransformOrigin = new Point(0, 1);
                    rect.RenderTransform = new RotateTransform(-135);
                    Canvas.SetLeft(rect, _leftMargin + (startCol + 1.05) * _cellSize);
                    Canvas.SetTop(rect, _topMargin + (startRow - 0.3) * _cellSize);
                }

                else
                {
                    rect.Width = _cellSize * 0.8;
                    rect.Height = _cellSize * 0.8;
                    Canvas.SetLeft(rect, _leftMargin + (startCol + 0.1) * _cellSize);
                    Canvas.SetTop(rect, _topMargin + (startRow + 0.1) * _cellSize);
                }

            }

            else
            {
                rect.Width = _cellSize * 0.8;
                rect.Height = _cellSize * 0.8;
                Canvas.SetLeft(rect, _leftMargin + (startCol + 0.1) * _cellSize);
                Canvas.SetTop(rect, _topMargin + (startRow + 0.1) * _cellSize);
            }

        }

        private void Scores_Click(object sender, RoutedEventArgs e)
        {
            HighScores dialog = new HighScores();
            dialog.Owner = this;
            bool? result = dialog.ShowDialog();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                // Resume
                _playingTime.Start();
                canvas1.Visibility = Visibility.Visible;
                PauseMenuItem.Header = "Pause";
            }
            else
            {
                // Pause
                _playingTime.Stop();
                canvas1.Visibility = Visibility.Hidden; // or Collapsed, depending on your layout needs
                PauseMenuItem.Header = "Resume";
            }

            _isPaused = !_isPaused;
        }

        private void HintTimer_Tick(object? sender, EventArgs e)
        {
            _hintRect.Visibility = Visibility.Collapsed;
            _hintTimer.Stop();
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            int m = _playingTime.Elapsed.Minutes;
            int s = _playingTime.Elapsed.Seconds;
            int h = _playingTime.Elapsed.Hours;
            if (h == 0)
                Time.Text = $"Time : {m:D2}:{s:D2}";
            else
                Time.Text = $"Time : {h}:{m:D2}:{s:D2}";
            Score.Text = $"Score : {_gameScore}";
            if (_gameOver)
                _gameTimer.Stop();
        }

        private void RecordHighScore(int newScore, DateTime completionTime)
        {
            HighScoresList scores = HighScores.ReadScores();

            if (_puzzleSize == 8)
            {
                if (newScore > scores.SmallBest)
                {
                    scores.SmallBest = newScore;
                    scores.SmallDate = completionTime;
                }
            }
            if (_puzzleSize == 12)
            {
                if (newScore > scores.MediumBest)
                {
                    scores.MediumBest = newScore;
                    scores.MediumDate = completionTime;
                }
            }
            if (_puzzleSize == 16)
            {
                if (newScore > scores.LargeBest)
                {
                    scores.LargeBest = newScore;
                    scores.LargeDate = completionTime;
                }
            }

            string json = JsonSerializer.Serialize(scores);
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string asteroidsFolder = System.IO.Path.Combine(appDataFolder, "Radish");
            asteroidsFolder = System.IO.Path.Combine(asteroidsFolder, "WordSearch");
            string filePath = System.IO.Path.Combine(asteroidsFolder, "highscores.json");
            Directory.CreateDirectory(asteroidsFolder); // ensure the folder exists first
            File.WriteAllText(filePath, json);
        }
    }
}