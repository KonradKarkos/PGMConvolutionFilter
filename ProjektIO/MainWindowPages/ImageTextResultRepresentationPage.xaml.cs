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

namespace ProjektIO
{
    /// <summary>
    /// Logika interakcji dla klasy ImageTextRepresentation.xaml
    /// </summary>
    public partial class ImageTextResultRepresentationPage : Page
    {
        public ImageTextResultRepresentationPage()
        {
            InitializeComponent();
        }
        //zmiana widoku na plik powstały synchronicznie/asynchronicznie
        private void BSwitchTxt_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayBoxKon.Visibility == Visibility.Visible)
            {
                DisplayBoxKon_Copy.Visibility = Visibility.Visible;
                DisplayBoxKon.Visibility = Visibility.Hidden;
                BSwitchTxt.Content = "Asynchroniczny";
            }
            else
            {
                DisplayBoxKon.Visibility = Visibility.Visible;
                DisplayBoxKon_Copy.Visibility = Visibility.Hidden;
                BSwitchTxt.Content = "Synchroniczy";
            }
        }
    }
}
