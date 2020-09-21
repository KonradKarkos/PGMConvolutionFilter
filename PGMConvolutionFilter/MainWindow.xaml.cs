using PGMConvolutionFilter.MainWindowPages;
using System.Windows;
using System.Windows.Controls;

namespace PGMConvolutionFilter
{
    public partial class MainWindow : Window
    {
        public ImageTextResultRepresentationPage ImageTextResultRepresentationPage;
        public ImageVisualResultRepresentationPage ImageVisualResultRepresentationPage;
        public LogsAndInputOutputOptionsPage LogsAndInputOutputOptionsPage;
        public CalculationOptionsPage CalculationOptionsPage;
        public MainWindow()
        {
            ImageTextResultRepresentationPage = new ImageTextResultRepresentationPage();
            ImageVisualResultRepresentationPage = new ImageVisualResultRepresentationPage();
            CalculationOptionsPage = new CalculationOptionsPage();
            LogsAndInputOutputOptionsPage = new LogsAndInputOutputOptionsPage(this);
            InitializeComponent();
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch(((TabItem)tabControl.Items[tabControl.SelectedIndex]).Name)
            {
                case "logsAndOptionsTabItem":
                    mainFrame.Content = LogsAndInputOutputOptionsPage;
                    break;
                case "textResultsTabItem":
                    mainFrame.Content = ImageTextResultRepresentationPage;
                    break;
                case "imageResultsTabItem":
                    mainFrame.Content = ImageVisualResultRepresentationPage;
                    break;
                case "asynchronousMethodsTabItem":
                    mainFrame.Content = CalculationOptionsPage;
                    break;
                default:
                    break;
            }
        }
    }
}