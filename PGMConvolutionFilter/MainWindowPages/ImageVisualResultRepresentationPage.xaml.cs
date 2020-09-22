using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PGMConvolutionFilter
{
    /// <summary>
    /// Logika interakcji dla klasy ImageVisualResultRepresentationPage.xaml
    /// </summary>
    public partial class ImageVisualResultRepresentationPage : Page
    {
        public ImageVisualResultRepresentationPage()
        {
            InitializeComponent();
        }
        private void switchDisplayedResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (synchronouslyFilteredImage.Visibility == Visibility.Visible)
            {
                synchronouslyFilteredImage.Visibility = Visibility.Collapsed;
                asynchronouslyFilteredImage.Visibility = Visibility.Visible;
                switchDisplayedResultsButton.Content = "Asynchronous";
            }
            else
            {
                asynchronouslyFilteredImage.Visibility = Visibility.Collapsed;
                synchronouslyFilteredImage.Visibility = Visibility.Visible;
                switchDisplayedResultsButton.Content = "Synchronous";
            }
        }
    }
}
