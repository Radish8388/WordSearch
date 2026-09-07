using System.Windows;

namespace Word_Search
{
    /// <summary>
    /// Interaction logic for NoMoreHints.xaml
    /// </summary>
    public partial class NoMoreHints : Window
    {
        public NoMoreHints()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
