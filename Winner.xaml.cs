using System.Windows;

namespace Word_Search
{
    /// <summary>
    /// Interaction logic for Winner.xaml
    /// </summary>
    public partial class Winner : Window
    {
        public Winner()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
