using System.IO;
using System.Text.Json;
using System.Windows;

namespace Word_Search
{
    /// <summary>
    /// Interaction logic for HighScores.xaml
    /// </summary>
    public partial class HighScores : Window
    {
        public HighScores()
        {
            InitializeComponent();
            //ReadScores();
            DisplayScores();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public static HighScoresList ReadScores()
        {
            HighScoresList? scores = null;

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string asteroidsFolder = Path.Combine(appDataFolder, "Radish");
            asteroidsFolder = Path.Combine(asteroidsFolder, "WordSearch");
            string filePath = Path.Combine(asteroidsFolder, "highscores.json");

            try
            {
                string json = File.ReadAllText(filePath);
                scores = JsonSerializer.Deserialize<HighScoresList>(json);
                if (scores == null)
                {
                    scores = MakeNewScores();
                }
            }
            catch (JsonException)
            {
                scores = MakeNewScores();
            }
            catch (IOException)
            {
                scores = MakeNewScores();
            }

            return scores;
        }

        private static HighScoresList MakeNewScores()
        {
            HighScoresList scores = new HighScoresList();
            scores.SmallBest = -1;
            scores.SmallDate = DateTime.MinValue;
            scores.MediumBest = -1;
            scores.MediumDate = DateTime.MinValue;
            scores.LargeBest = -1;
            scores.LargeDate = DateTime.MinValue;
            return scores;
        }

        private void DisplayScores()
        {
            HighScoresList scores = ReadScores();

            if (scores.SmallBest == -1)
                SmallScore.Text = "—";
            else
                SmallScore.Text = scores.SmallBest.ToString();

            if (scores.SmallBest == -1)
                SmallDate.Text = "—";
            else
                SmallDate.Text = scores.SmallDate.ToString("d");

            if (scores.MediumBest == -1)
                MediumScore.Text = "—";
            else
                MediumScore.Text = scores.MediumBest.ToString();

            if (scores.MediumBest == -1)
                MediumDate.Text = "—";
            else
                MediumDate.Text = scores.MediumDate.ToString("d");

            if (scores.LargeBest == -1)
                LargeScore.Text = "—";
            else
                LargeScore.Text = scores.LargeBest.ToString();

            if (scores.LargeBest == -1)
                LargeDate.Text = "—";
            else
                LargeDate.Text = scores.LargeDate.ToString("d");
        }
    }

    public class HighScoresList
    {
        public int SmallBest { get; set; }
        public DateTime SmallDate { get; set; }
        public int MediumBest { get; set; }
        public DateTime MediumDate { get; set; }
        public int LargeBest { get; set; }
        public DateTime LargeDate { get; set; }
    }
}
