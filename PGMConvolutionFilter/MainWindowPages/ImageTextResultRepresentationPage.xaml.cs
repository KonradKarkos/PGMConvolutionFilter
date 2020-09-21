using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PGMConvolutionFilter
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
        private void switchDisplayedResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (synchronouslyFilteredImageDisplayTextBox.Visibility == Visibility.Visible)
            {
                asynchronouslyFilteredImageDisplayTextBox.Visibility = Visibility.Visible;
                synchronouslyFilteredImageDisplayTextBox.Visibility = Visibility.Hidden;
                switchDisplayedResultsButton.Content = "Asynchronous";
            }
            else
            {
                synchronouslyFilteredImageDisplayTextBox.Visibility = Visibility.Visible;
                asynchronouslyFilteredImageDisplayTextBox.Visibility = Visibility.Hidden;
                switchDisplayedResultsButton.Content = "Synchronous";
            }
        }
        public void SaveResultImageAsText(string filePath)
        {
            StreamWriter sw = new StreamWriter(filePath);
            int lineCount = synchronouslyFilteredImageDisplayTextBox.LineCount;
            if (lineCount <= 0)
            {
                for (int i = 0; i < lineCount; i++)
                {
                    sw.WriteLine(synchronouslyFilteredImageDisplayTextBox.GetLineText(i));
                }
            }
            else
            {
                sw.Write(synchronouslyFilteredImageDisplayTextBox.Text);
            }
            sw.Dispose();
            MessageBox.Show("Successfully saved PGM as txt file.");
        }
        public void SaveResultImageAsBinary(string filePath)
        {
            BinaryWriter bw = new BinaryWriter(File.Open(filePath, FileMode.OpenOrCreate));
            StringBuilder sb = new StringBuilder();
            String resultImageTextRepresentaton = synchronouslyFilteredImageDisplayTextBox.Text;
            bw.Write('P');
            bw.Write('5');
            bw.Write('\n');
            int PGMFormatKeyWordCount = 3;
            int inTextIndex = 2;
            while (!Char.IsDigit(resultImageTextRepresentaton[inTextIndex])) inTextIndex++;
            while (PGMFormatKeyWordCount > 0)
            {
                bw.Write(resultImageTextRepresentaton[inTextIndex]);
                if (resultImageTextRepresentaton[inTextIndex].Equals('\n') || resultImageTextRepresentaton[inTextIndex].Equals(' ')) PGMFormatKeyWordCount--;
                inTextIndex++;
            }
            for (int i = inTextIndex; i < synchronouslyFilteredImageDisplayTextBox.Text.Length; i++)
            {
                while (!resultImageTextRepresentaton[i].Equals('\n') && !resultImageTextRepresentaton[i].Equals(' ') && !resultImageTextRepresentaton[i].Equals('\r'))
                {
                    sb.Append(resultImageTextRepresentaton[i]);
                    i++;
                }
                if (sb.Length > 0)
                {
                    bw.Write(byte.Parse(sb.ToString().Split(',')[0]));
                    sb.Clear();
                }
            }
            bw.Dispose();
            MessageBox.Show("Successfully saved results.");
        }
        public void CheckForResultsDifferences()
        {
            if (synchronouslyFilteredImageDisplayTextBox.Text.Equals(asynchronouslyFilteredImageDisplayTextBox.Text))
            {
                displayEqualResultsTextBlock.Foreground = Brushes.Green;
                displayEqualResultsTextBlock.Text = "Yes";
            }
            else
            {
                displayEqualResultsTextBlock.Foreground = Brushes.Red;
                displayEqualResultsTextBlock.Text = "No";
            }
        }
    }
}
