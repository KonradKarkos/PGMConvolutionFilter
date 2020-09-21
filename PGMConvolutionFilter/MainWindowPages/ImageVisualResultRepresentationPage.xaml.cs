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
                asynchronouslyFilteredImage.Visibility = Visibility.Visible;
                synchronouslyFilteredImage.Visibility = Visibility.Hidden;
                switchDisplayedResultsButton.Content = "Asynchronous";
            }
            else
            {
                synchronouslyFilteredImage.Visibility = Visibility.Visible;
                asynchronouslyFilteredImage.Visibility = Visibility.Hidden;
                switchDisplayedResultsButton.Content = "Synchronous";
            }
        }
    }
}
