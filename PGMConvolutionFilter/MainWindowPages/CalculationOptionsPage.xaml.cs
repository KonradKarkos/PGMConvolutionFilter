using System.Windows.Controls;
using System.Windows.Input;

namespace PGMConvolutionFilter.MainWindowPages
{
    /// <summary>
    /// Logika interakcji dla klasy CalculationOptionsPage.xaml
    /// </summary>
    public partial class CalculationOptionsPage : Page
    {
        public CalculationOptionsPage()
        {
            InitializeComponent();
        }
        private void AllowOnlyDigits(object sender, TextCompositionEventArgs e)
        {
            int output;
            if (int.TryParse(e.Text, out output) == false)
            {
                e.Handled = true;
            }
            else
            {
                if (e.Text != "0" && e.Text != "1" && e.Text != "2" && e.Text != "3" && e.Text != "4" && e.Text != "5" && e.Text != "6" && e.Text != "7" && e.Text != "8" && e.Text != "9")
                {
                    e.Handled = true;
                }
            }
        }
    }
}
