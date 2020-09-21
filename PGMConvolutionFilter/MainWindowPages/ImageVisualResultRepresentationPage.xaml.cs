using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        //zmiana widoku na obraz powstały synchronicznie/asynchronicznie
        private void BSwitchImg_Click(object sender, RoutedEventArgs e)
        {
            if (image_Copy.Visibility == Visibility.Visible)
            {
                image_Copy1.Visibility = Visibility.Visible;
                image_Copy.Visibility = Visibility.Hidden;
                BSwitchImg.Content = "Asynchroniczny";
            }
            else
            {
                image_Copy.Visibility = Visibility.Visible;
                image_Copy1.Visibility = Visibility.Hidden;
                BSwitchImg.Content = "Synchroniczy";
            }
        }
    }
}
