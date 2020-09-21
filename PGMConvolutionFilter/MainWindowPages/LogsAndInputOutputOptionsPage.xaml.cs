using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Drawing.Color;

namespace PGMConvolutionFilter
{
    /// <summary>
    /// Logika interakcji dla klasy LogsAndInputOutputOptions.xaml
    /// </summary>
    public partial class LogsAndInputOutputOptionsPage : Page
    {
        private int ThreadAmount = 10;
        private MyImage UploadedImage;
        int defaultImageDimensions;
        private MainWindow MainWindow;
        private float[][] ImagePixelsValues;
        private double AsynchronousCalculationTime;
        private double SynchronousCalculationTime;
        public LogsAndInputOutputOptionsPage(MainWindow mainWindow)
        {
            this.MainWindow = mainWindow;
            InitializeComponent();
        }
        private void chooseFileToComputeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Portable Grayscale Map (*.pgm)|*.pgm";
            if (openFileDialog.ShowDialog() == true)
            {
                useDefaultImageToComputeCheckBox.IsChecked = false;
                imageToComputeFilePathTextBox.Text = openFileDialog.FileName;
                resultImageBinarySavingFilePathTextBox.Text = imageToComputeFilePathTextBox.Text.Insert(imageToComputeFilePathTextBox.Text.Length - 4, "Result");
                resultImageSavingFilePathTextBox.Text = imageToComputeFilePathTextBox.Text.Remove(imageToComputeFilePathTextBox.Text.Length - 4, 4).Insert(imageToComputeFilePathTextBox.Text.Length - 4, ".png");
                resultImageTextSavingFilePathTextBox.Text = imageToComputeFilePathTextBox.Text.Remove(imageToComputeFilePathTextBox.Text.Length - 4, 4).Insert(imageToComputeFilePathTextBox.Text.Length - 4, ".txt");
            }
        }
        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
        private Bitmap ImageToBitMap(MyImage image)
        {
            int height = image.Height;
            int width = image.Width;
            Bitmap result = new Bitmap(width, height);
            Color c = new Color();
            int pixelValue;
            float[][] imagePixelsValues = image.Values;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    pixelValue = (int)imagePixelsValues[i][j];
                    c = Color.FromArgb(pixelValue, pixelValue, pixelValue);
                    result.SetPixel(j, i, c);
                }
            }
            return result;
        }
        private String ReadBinaryPGMLine(BinaryReader br)
        {
            String s = ReadBinaryLine(br);
            while (s.StartsWith("#") || s.Equals(""))
            {
                s = ReadBinaryLine(br);
            }
            return s;
        }
        private String ReadBinaryLine(BinaryReader br)
        {
            StringBuilder s = new StringBuilder();
            byte b = 0;
            while (b != 10)
            {
                b = br.ReadByte();
                char c = (char)b;
                s.Append(c);
            }
            return s.ToString().Trim();
        }
        private String ReadPGMLine(StreamReader sr)
        {
            String s = sr.ReadLine().Trim();
            while (s.StartsWith("#") || s.Equals(""))
            {
                s = sr.ReadLine().Trim();
            }
            return s;
        }
        private String ReadNextNumber(StreamReader sr)
        {
            StringBuilder sb = new StringBuilder();
            char c = 'a';
            while (c != ' ')
            {
                c = (char)sr.Read();
                sb.Append(c);
            }
            return sb.ToString();
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
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Program applies convolution filter 200 time by default on PGM file using 11 threads (10 computes inside of the image while the last one computes border pixels) and comparing time results with synchronous version.\n" +
                "Results of both calculations can be compared with original image in \"Text Results\" and \"Image Results\" tabs by switching between synchronous and asynchronous results by clickng button in top right corner of the tab.\n" +
                "There are three different asynchronous methods implemented, two of which are using threads that do not wait for each other because whole image fragment needed for calculation is passed to them (its size is determined by iterations amount).\n" +
                "Results are saved in \"Results.txt\" file");
        }
        private void loadImageToComputeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Int32.TryParse(threadAmountTextBox.Text, out ThreadAmount))
            {
                if (ThreadAmount < 1000)
                {
                    StringBuilder sb = new StringBuilder();
                    if (useDefaultImageToComputeCheckBox.IsChecked == true)
                    {
                        if (defaultImageDimensionsTextBox.Text.Length <= 0)
                        {
                            defaultImageDimensionsTextBox.Text = "1024";
                        }
                        if (Int32.TryParse(defaultImageDimensionsTextBox.Text, out defaultImageDimensions))
                        {
                            if (defaultImageDimensions > ThreadAmount - 2 && defaultImageDimensions * defaultImageDimensions < Int32.MaxValue)
                            {
                                bool checkerBoardLoaded = false;
                                int checkerBoardFieldsAmount = 0;
                                UploadedImage = new MyImage(defaultImageDimensions, defaultImageDimensions);
                                if (createCheckerBoardCheckBox.IsChecked == true && createCheckerBoardCheckBox.IsEnabled == true)
                                {
                                    if (!Int32.TryParse(checkerBoardFieldAmountTextBox.Text, out checkerBoardFieldsAmount))
                                    {
                                        MessageBox.Show("Couldn't parse checkerboard field amount", "Parsing error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                    else
                                    {
                                        if (checkerBoardFieldsAmount > defaultImageDimensions)
                                        {
                                            MessageBox.Show("Too many checkerboard fields. Program will load default empty image", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        }
                                        else
                                        {
                                            UploadedImage.CreateCheckerboard(checkerBoardFieldsAmount);
                                            checkerBoardLoaded = true;
                                        }
                                    }
                                }
                                sb.AppendLine("P5");
                                sb.AppendLine(defaultImageDimensions + " " + defaultImageDimensions);
                                sb.AppendLine("255");
                                for (int i = 0; i < defaultImageDimensions; i++)
                                {
                                    for (int j = 0; j < defaultImageDimensions; j++)
                                    {
                                        sb.Append(UploadedImage.Values[i][j] + " ");
                                    }
                                    sb.AppendLine();
                                }
                                MainWindow.ImageTextResultRepresentationPage.plainImageDisplayTextBox.Text += sb.ToString();
                                MainWindow.ImageVisualResultRepresentationPage.plainImage.Source = BitmapToImageSource(ImageToBitMap(UploadedImage));
                                startButton.IsEnabled = true;
                                if (logsTextBox.Text.Length > 0) logsTextBox.Text += '\n';
                                if (createCheckerBoardCheckBox.IsChecked == true && createCheckerBoardCheckBox.IsEnabled == true && checkerBoardLoaded)
                                {
                                    logsTextBox.Text += "============================================" + '\n' + "Loaded checkerboard with dimensions " + defaultImageDimensionsTextBox.Text + defaultImageDimensionsTextBlock.Text + " and " + checkerBoardFieldsAmount * checkerBoardFieldsAmount + " fields.";
                                }
                                else
                                {
                                    logsTextBox.Text += "============================================" + '\n' + "Loaded default empty image with dimensions " + defaultImageDimensionsTextBox.Text + defaultImageDimensionsTextBlock.Text;
                                }
                                MessageBox.Show("Loading completed");
                            }
                            else
                            {
                                MessageBox.Show("Default image dimensions are too small/big", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Couldn't parse default image dimensions", "Parsing error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        if (File.Exists(imageToComputeFilePathTextBox.Text))
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            BinaryReader br = new BinaryReader(File.Open(imageToComputeFilePathTextBox.Text, FileMode.Open));
                            String PGMVersion = ReadBinaryPGMLine(br);
                            sb.AppendLine(PGMVersion);
                            if (!PGMVersion.Equals("P5") && !PGMVersion.Equals("P2"))
                            {
                                MessageBox.Show("Couldn't recgonise PGM version - " + PGMVersion + ". Only P2 and P5 are allowed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                br.Dispose();
                            }
                            else
                            {
                                int width, height;
                                String[] imageWidthAndHeight = ReadBinaryPGMLine(br).Split(' ');
                                sb.AppendLine(imageWidthAndHeight[0] + " " + imageWidthAndHeight[1]);
                                width = Int32.Parse(imageWidthAndHeight[0]);
                                height = Int32.Parse(imageWidthAndHeight[1]);
                                if (height > ThreadAmount - 2)
                                {
                                    int maxPixelValue = Int32.Parse(ReadBinaryPGMLine(br));
                                    sb.AppendLine(maxPixelValue.ToString());
                                    if (maxPixelValue > 255)
                                    {
                                        MessageBox.Show("Specified pixel value is too big - " + maxPixelValue, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        br.Dispose();
                                    }
                                    else
                                    {
                                        UploadedImage = new MyImage(width, height);
                                        float[][] uploadedImagePixelValues = new float[height][];
                                        byte b = 0;
                                        if (PGMVersion.Equals("P2"))
                                        {
                                            br.Dispose();
                                            StreamReader sr = new StreamReader(imageToComputeFilePathTextBox.Text);
                                            //skip
                                            //version
                                            ReadPGMLine(sr);
                                            //dimensions
                                            ReadPGMLine(sr);
                                            //max pixel value
                                            ReadPGMLine(sr);
                                            for (int i = 0; i < height; i++)
                                            {
                                                uploadedImagePixelValues[i] = new float[width];
                                                for (int j = 0; j < width; j++)
                                                {
                                                    PGMVersion = ReadNextNumber(sr);
                                                    uploadedImagePixelValues[i][j] = float.Parse(PGMVersion);
                                                    sb.Append(PGMVersion + " ");
                                                }
                                                sb.AppendLine();
                                            }
                                        }
                                        else
                                        {
                                            for (int i = 0; i < height; i++)
                                            {
                                                uploadedImagePixelValues[i] = new float[width];
                                                for (int j = 0; j < width; j++)
                                                {
                                                    b = br.ReadByte();
                                                    uploadedImagePixelValues[i][j] = b;
                                                    sb.Append(b + " ");
                                                }
                                                sb.AppendLine();
                                            }
                                        }
                                        MainWindow.ImageTextResultRepresentationPage.plainImageDisplayTextBox.Text = sb.ToString();
                                        UploadedImage.Values = uploadedImagePixelValues;
                                        Bitmap beforeCalculationImageState = new Bitmap(width, height);
                                        Color c;
                                        int PGMPixelValue;
                                        for (int i = 0; i < height; i++)
                                        {
                                            for (int j = 0; j < width; j++)
                                            {
                                                PGMPixelValue = (int)uploadedImagePixelValues[i][j];
                                                c = Color.FromArgb(PGMPixelValue, PGMPixelValue, PGMPixelValue);
                                                beforeCalculationImageState.SetPixel(j, i, c);
                                            }
                                        }
                                        MainWindow.ImageVisualResultRepresentationPage.plainImage.Source = BitmapToImageSource(beforeCalculationImageState);
                                        br.Dispose();
                                        sw.Stop();
                                        if (logsTextBox.Text.Length > 0) logsTextBox.Text += '\n';
                                        logsTextBox.Text += "==============================================\nUploaded image path: " + imageToComputeFilePathTextBox.Text + "\nLoading time: " + sw.Elapsed.TotalMilliseconds + " ms.";
                                        MessageBox.Show("Loading completed");
                                        startButton.IsEnabled = true;
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Loaded image is too short for given thread amount", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Such file doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Set thread amount is too high", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Coludn't parse thread amount", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            startButton.IsEnabled = false;
            int iterationsPerRepeat;
            int repeatAmount;
            if (!Int32.TryParse(MainWindow.CalculationOptionsPage.calculationRepeatsTextBox.Text, out repeatAmount))
            {
                MessageBox.Show("Couldn't parse amount of repeats to average the result", "Parsing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (!Int32.TryParse(threadAmountTextBox.Text, out ThreadAmount))
                {
                    MessageBox.Show("Couldn't parse thread amount", "Parsing Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (ThreadAmount > 1000 || UploadedImage.Height < ThreadAmount - 2)
                    {
                        MessageBox.Show("Number of threads is too high", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        if (!Int32.TryParse(iterationsAmountTextBox.Text, out iterationsPerRepeat))
                        {
                            MessageBox.Show("Couldn't parse amount of iterations per repeat", "Parsing Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            logsTextBox.Text += "\nRepeats: " + repeatAmount + "\nIterations per repeat: " + iterationsPerRepeat + "\nThread amount: " + ThreadAmount;
                            PB.Visibility = Visibility.Visible;
                            PB.Value = 0;
                            PB.Maximum = repeatAmount * 2;
                            AsynchronousCalculationTime = 0.0;
                            SynchronousCalculationTime = 0.0;
                            SetUpSynchronousBackgroundWorker(repeatAmount, iterationsPerRepeat);
                        }
                    }
                }
            }
        }
        private void SetUpSynchronousBackgroundWorker(int repeatAmount, int iterationsPerRepeat)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += SynchronousBackgroundWorker_DoWork;
            worker.ProgressChanged += BackgroundWorker_ProgressChanged;
            worker.RunWorkerCompleted += SynchronousBackgroundWorker_RunWorkerCompleted;
            worker.RunWorkerAsync(argument: new int[] {repeatAmount, iterationsPerRepeat });
        }
        private void SynchronousBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int[] values = (int[])e.Argument;
            int repeatAmount = values[0];
            int iterationsPerRepeat = values[1];
            MyImage temporaryImageToPerformCalculations = UploadedImage;
            Stopwatch sw = new Stopwatch();
            for (int z = 0; z < repeatAmount; z++)
            {
                temporaryImageToPerformCalculations = UploadedImage;
                for (int i = 0; i < iterationsPerRepeat; i++)
                {
                    sw.Restart();
                    temporaryImageToPerformCalculations = temporaryImageToPerformCalculations.convolution();
                    sw.Stop();
                    SynchronousCalculationTime += sw.ElapsedMilliseconds;
                }
                (sender as BackgroundWorker).ReportProgress(0);
            }
            e.Result = new object[] {repeatAmount, iterationsPerRepeat, temporaryImageToPerformCalculations };
        }
        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            PB.Value++;
        }
        private void SynchronousBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int repeatAmount = (int)((object[])e.Result)[0];
            int iterationsPerRepeat = (int)((object[])e.Result)[1];
            MyImage temporaryImageToPerformCalculations = (MyImage)((object[])e.Result)[2];
            StringBuilder stringBuilder = new StringBuilder();
            int PGMFormatKeyWordCount = 4;
            int inTextIndex = 0;
            String uploadedImageTextRepresentation = MainWindow.ImageTextResultRepresentationPage.plainImageDisplayTextBox.Text;
            if (useDefaultImageToComputeCheckBox.IsChecked == false)
            {
                while (PGMFormatKeyWordCount > 0)
                {
                    stringBuilder.Append(uploadedImageTextRepresentation[inTextIndex]);
                    if (uploadedImageTextRepresentation[inTextIndex].Equals('\n') || uploadedImageTextRepresentation[inTextIndex].Equals(' ')) PGMFormatKeyWordCount--;
                    inTextIndex++;
                }
            }
            else
            {
                stringBuilder.AppendLine("P5");
                stringBuilder.AppendLine(defaultImageDimensions + " " + defaultImageDimensions);
                stringBuilder.AppendLine("255");
            }
            int height = temporaryImageToPerformCalculations.Height;
            int width = temporaryImageToPerformCalculations.Width;
            ImagePixelsValues = temporaryImageToPerformCalculations.Values;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    stringBuilder.Append(ImagePixelsValues[i][j] + " ");
                }
                stringBuilder.AppendLine();
            }
            MainWindow.ImageTextResultRepresentationPage.synchronouslyFilteredImageDisplayTextBox.Text = stringBuilder.ToString();
            MainWindow.ImageVisualResultRepresentationPage.synchronouslyFilteredImage.Source = BitmapToImageSource(ImageToBitMap(temporaryImageToPerformCalculations));
            logsTextBox.Text += "\nTime of applying convolution filter synchronously: " + SynchronousCalculationTime / (1000.0 * repeatAmount) + " s.";
            int mode = 0;
            if (MainWindow.CalculationOptionsPage.dependentThreadsRadioButton.IsChecked == true)
            {
                mode = 0;
            }
            else if (MainWindow.CalculationOptionsPage.independentThreadsWithZeroDataFillingRadioButton.IsChecked == true)
            {
                mode = 1;
            }
            else if (MainWindow.CalculationOptionsPage.independentThreadsWithoutZeroDataFillingRadioButton.IsChecked == true)
            {
                mode = 2;
            }
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += SetUpAsynchronousBackgroundWorkers;
            worker.ProgressChanged += BackgroundWorker_ProgressChanged;
            worker.RunWorkerCompleted += AsynchronousBackgroundWorker_RunWorkerCompleted;
            worker.RunWorkerAsync(argument: new int[] { repeatAmount, iterationsPerRepeat, mode });
        }
        private void SetUpAsynchronousBackgroundWorkers(object sender, DoWorkEventArgs e)
        {
            int[] values = (int[])e.Argument;
            int repeatAmount = values[0];
            int iterationsPerRepeat = values[1];
            int mode = values[2];
            MyImage temporaryImageToPerformCalculations = UploadedImage;
            int height = temporaryImageToPerformCalculations.Height;
            int width = temporaryImageToPerformCalculations.Width;
            int amountOfRowsToApplyFilterOnBySingleThread = height / ThreadAmount;
            CountdownEvent countdownEvent;
            Stopwatch sw = new Stopwatch();
            int[] fragmentsToComputeStartingRowsInnerIndexes = new int[ThreadAmount];
            BackgroundWorker[] backgroundWorkers;
            if (mode == 0)
            {
                float[][] threadsValuesToCompute = new float[height][];
                for (int i = 0; i < height; i++)
                {
                    threadsValuesToCompute[i] = new float[width];
                }
                for (int z = 0; z < repeatAmount; z++)
                {
                    for (int u = 0; u < UploadedImage.Values.Length; u++)
                    {
                        UploadedImage.Values[u].CopyTo(ImagePixelsValues[u], 0);
                    }
                    for (int i = 0; i < iterationsPerRepeat; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            ImagePixelsValues[j].CopyTo(threadsValuesToCompute[j], 0);
                        }
                        countdownEvent = new CountdownEvent(ThreadAmount + 1);
                        backgroundWorkers = new BackgroundWorker[ThreadAmount + 1];
                        sw.Restart();
                        for (int j = 0; j < ThreadAmount; j++)
                        {
                            backgroundWorkers[j] = new BackgroundWorker();
                            backgroundWorkers[j].WorkerReportsProgress = false;
                            backgroundWorkers[j].DoWork += ComputeDependentlyWithoutSides;
                            backgroundWorkers[j].RunWorkerAsync(argument: new object[] { height, width, threadsValuesToCompute, amountOfRowsToApplyFilterOnBySingleThread * j, amountOfRowsToApplyFilterOnBySingleThread * (j + 1), j, countdownEvent });
                        }
                        backgroundWorkers[ThreadAmount] = new BackgroundWorker();
                        backgroundWorkers[ThreadAmount].WorkerReportsProgress = false;
                        backgroundWorkers[ThreadAmount].DoWork += ComputeSidesDependently;
                        backgroundWorkers[ThreadAmount].RunWorkerAsync(argument: new object[] { height, width, threadsValuesToCompute, countdownEvent });
                        countdownEvent.Wait();
                        sw.Stop();
                        AsynchronousCalculationTime += sw.ElapsedMilliseconds;
                    }
                    (sender as BackgroundWorker).ReportProgress(0);
                }
            }
            else if (mode == 1)
            {
                for (int z = 0; z < repeatAmount; z++)
                {
                    for (int u = 0; u < UploadedImage.Values.Length; u++)
                    {
                        UploadedImage.Values[u].CopyTo(ImagePixelsValues[u], 0);
                    }
                    float[][][] threadsValuesToCompute = DivideImagePixelValuesBetweenThreads(amountOfRowsToApplyFilterOnBySingleThread, iterationsPerRepeat, width, true, fragmentsToComputeStartingRowsInnerIndexes);
                    countdownEvent = new CountdownEvent(ThreadAmount);
                    backgroundWorkers = new BackgroundWorker[ThreadAmount];
                    sw.Restart();
                    for (int i = 0; i < ThreadAmount; i++)
                    {
                        backgroundWorkers[i] = new BackgroundWorker();
                        backgroundWorkers[i].WorkerReportsProgress = false;
                        backgroundWorkers[i].DoWork += ComputeIndependentlyWithAdditionalZeroFillers;
                        backgroundWorkers[i].RunWorkerAsync(argument: new object[] { width, threadsValuesToCompute[i], amountOfRowsToApplyFilterOnBySingleThread * i, countdownEvent, iterationsPerRepeat });
                    }
                    countdownEvent.Wait();
                    sw.Stop();
                    AsynchronousCalculationTime += sw.ElapsedMilliseconds;
                    (sender as BackgroundWorker).ReportProgress(0);
                }
            }
            else if (mode == 2)
            {
                for (int z = 0; z < repeatAmount; z++)
                {
                    for (int u = 0; u < UploadedImage.Values.Length; u++)
                    {
                        UploadedImage.Values[u].CopyTo(ImagePixelsValues[u], 0);
                    }
                    float[][][] threadsValuesToCompute = DivideImagePixelValuesBetweenThreads(amountOfRowsToApplyFilterOnBySingleThread, iterationsPerRepeat, width, false, fragmentsToComputeStartingRowsInnerIndexes);
                    countdownEvent = new CountdownEvent(ThreadAmount);
                    backgroundWorkers = new BackgroundWorker[ThreadAmount];
                    sw.Restart();
                    for (int i = 0; i < ThreadAmount; i++)
                    {
                        if (i == ThreadAmount - 1)
                        {
                            backgroundWorkers[i] = new BackgroundWorker();
                            backgroundWorkers[i].WorkerReportsProgress = false;
                            backgroundWorkers[i].DoWork += ComputeIndependentlyWithoutAdditionalZeroFillers;
                            backgroundWorkers[i].RunWorkerAsync(argument: new object[] { ImagePixelsValues.Length - i * amountOfRowsToApplyFilterOnBySingleThread, width, threadsValuesToCompute[i], fragmentsToComputeStartingRowsInnerIndexes[i], amountOfRowsToApplyFilterOnBySingleThread * i, countdownEvent, iterationsPerRepeat });
                        }
                        else
                        {
                            backgroundWorkers[i] = new BackgroundWorker();
                            backgroundWorkers[i].WorkerReportsProgress = false;
                            backgroundWorkers[i].DoWork += ComputeIndependentlyWithoutAdditionalZeroFillers;
                            backgroundWorkers[i].RunWorkerAsync(argument: new object[] { amountOfRowsToApplyFilterOnBySingleThread, width, threadsValuesToCompute[i], fragmentsToComputeStartingRowsInnerIndexes[i], amountOfRowsToApplyFilterOnBySingleThread * i, countdownEvent, iterationsPerRepeat });
                        }
                    }
                    countdownEvent.Wait();
                    sw.Stop();
                    AsynchronousCalculationTime += sw.ElapsedMilliseconds;
                    (sender as BackgroundWorker).ReportProgress(0);
                }
            }
            e.Result = repeatAmount;
        }
        private void AsynchronousBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int repeatAmount = (int)e.Result;
            MyImage temporaryImageToPerformCalculations = new MyImage(UploadedImage.Width, UploadedImage.Height);
            temporaryImageToPerformCalculations.Values = ImagePixelsValues;
            int width = temporaryImageToPerformCalculations.Width;
            int height = temporaryImageToPerformCalculations.Height;
            StringBuilder stringBuilder = new StringBuilder();
            int PGMFormatKeyWordCount = 4;
            int inTextIndex = 0;
            String uploadedImageTextRepresentation = MainWindow.ImageTextResultRepresentationPage.plainImageDisplayTextBox.Text;
            if (useDefaultImageToComputeCheckBox.IsChecked == false)
            {
                while (PGMFormatKeyWordCount > 0)
                {
                    stringBuilder.Append(uploadedImageTextRepresentation[inTextIndex]);
                    if (uploadedImageTextRepresentation[inTextIndex].Equals('\n') || uploadedImageTextRepresentation[inTextIndex].Equals(' ')) PGMFormatKeyWordCount--;
                    inTextIndex++;
                }
            }
            else
            {
                stringBuilder.AppendLine("P5");
                stringBuilder.AppendLine(defaultImageDimensions + " " + defaultImageDimensions);
                stringBuilder.AppendLine("255");
            }
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    stringBuilder.Append(ImagePixelsValues[i][j] + " ");
                }
                stringBuilder.AppendLine();
            }
            MainWindow.ImageTextResultRepresentationPage.asynchronouslyFilteredImageDisplayTextBox.Text = stringBuilder.ToString();
            MainWindow.ImageVisualResultRepresentationPage.asynchronouslyFilteredImage.Source = BitmapToImageSource(ImageToBitMap(temporaryImageToPerformCalculations));
            MainWindow.ImageTextResultRepresentationPage.CheckForResultsDifferences();
            if (MainWindow.CalculationOptionsPage.dependentThreadsRadioButton.IsChecked == true)
            {
                logsTextBox.Text += "\nTime of applying convolution filter asynchronously (" + MainWindow.CalculationOptionsPage.dependentThreadsTextBlock.Text + "): " + AsynchronousCalculationTime / (1000.0 * repeatAmount) + " s.";
            }
            else if (MainWindow.CalculationOptionsPage.independentThreadsWithoutZeroDataFillingRadioButton.IsChecked == true)
            {

                logsTextBox.Text += "\nTime of applying convolution filter asynchronously (" + MainWindow.CalculationOptionsPage.independentThreadsWithoutZeroDataFillingTextBlock.Text + "): " + AsynchronousCalculationTime / (1000.0 * repeatAmount) + " s.";
            }
            else
            {
                logsTextBox.Text += "\nTime of applying convolution filter asynchronously (" + MainWindow.CalculationOptionsPage.independentThreadsWithZeroDataFillingTextBlock.Text + "): " + AsynchronousCalculationTime / (1000.0 * repeatAmount) + " s.";
            }
            logsTextBox.Text += "\nAsynchronous is " + SynchronousCalculationTime / AsynchronousCalculationTime + " times faster";
            PB.Visibility = Visibility.Hidden;
            PB.Value = 0;
            saveResultImageAsBinaryButton.IsEnabled = true;
            saveResultImageButton.IsEnabled = true;
            saveResultImageAsTextButton.IsEnabled = true;
            startButton.IsEnabled = true;
            MessageBox.Show("Synchronous and asynchronous calculations have finished");
        }
        private void imageToComputeFilePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (imageToComputeFilePathTextBox.Text.Length > 0)
                loadImageToComputeButton.IsEnabled = true;
            else
                loadImageToComputeButton.IsEnabled = false;
        }

        private void useDefaultImageToComputeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            imageToComputeFilePathTextBox.Text = "Default.pgm";
            resultImageBinarySavingFilePathTextBox.Text = imageToComputeFilePathTextBox.Text.Insert(imageToComputeFilePathTextBox.Text.Length - 4, "ResultFile");
            resultImageSavingFilePathTextBox.Text = imageToComputeFilePathTextBox.Text.Remove(imageToComputeFilePathTextBox.Text.Length - 4, 4).Insert(imageToComputeFilePathTextBox.Text.Length - 4, ".png");
            resultImageTextSavingFilePathTextBox.Text = imageToComputeFilePathTextBox.Text.Remove(imageToComputeFilePathTextBox.Text.Length - 4, 4).Insert(imageToComputeFilePathTextBox.Text.Length - 4, ".txt");
            defaultImageDimensionsTextBox.IsEnabled = false;
            createCheckerBoardCheckBox.IsEnabled = true;
            checkerBoardFieldAmountTextBox.IsEnabled = true;
        }

        private void useDefaultImageToComputeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            imageToComputeFilePathTextBox.Text = "";
            resultImageBinarySavingFilePathTextBox.Text = "";
            resultImageSavingFilePathTextBox.Text = "";
            resultImageTextSavingFilePathTextBox.Text = "";
            defaultImageDimensionsTextBox.IsEnabled = true;
            createCheckerBoardCheckBox.IsEnabled = false;
            checkerBoardFieldAmountTextBox.IsEnabled = false;
        }

        private void saveResultImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ImageToBitMap(UploadedImage).Save(resultImageSavingFilePathTextBox.Text);
            }
            catch
            {
                MessageBox.Show("Couldn't save image to file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            MessageBox.Show("Successfully saved image to file.");
        }
        private void saveResultImageAsTextButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ImageTextResultRepresentationPage.SaveResultImageAsText(resultImageTextSavingFilePathTextBox.Text);
        }
        private void saveResultImageAsBinaryButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ImageTextResultRepresentationPage.SaveResultImageAsBinary(resultImageBinarySavingFilePathTextBox.Text);
        }
        private void saveLogsButton_Click(object sender, RoutedEventArgs e)
        {
            StreamWriter streamWriter = new StreamWriter("Results.txt");
            int lineCount = logsTextBox.LineCount;
            if (lineCount <= 0)
            {
                for (int i = 0; i < lineCount; i++)
                {
                    streamWriter.WriteLine(logsTextBox.GetLineText(i));
                }
            }
            else
            {
                streamWriter.Write(logsTextBox.Text);
            }
            streamWriter.Dispose();
            MessageBox.Show("Successfully saved results.");
        }

        private void defaultImageDimensionsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (defaultImageDimensionsTextBlock != null)
                defaultImageDimensionsTextBlock.Text = 'x' + defaultImageDimensionsTextBox.Text;
        }

        private void checkerBoardFieldAmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (checkerBoardFieldAmountTextBlock != null)
                checkerBoardFieldAmountTextBlock.Text = 'x' + checkerBoardFieldAmountTextBox.Text;
        }
        private void ComputeDependentlyWithoutSides(object sender, DoWorkEventArgs e)
        {
            int imageHeight = (int)((object[])e.Argument)[0];
            int imageWidth = (int)((object[])e.Argument)[1];
            float[][] values = (float[][])((object[])e.Argument)[2];
            int rowToStartCalculationsAt = (int)((object[])e.Argument)[3];
            int rowToEndCalculationsAt = (int)((object[])e.Argument)[4];
            int threadIndex = (int)((object[])e.Argument)[5];
            CountdownEvent c = (CountdownEvent)((object[])e.Argument)[6];
            if (rowToStartCalculationsAt == 0)
            {
                rowToStartCalculationsAt++;
            }
            if (threadIndex == ThreadAmount - 1)
            {
                rowToEndCalculationsAt = imageHeight - 1;
            }
            for (int i = rowToStartCalculationsAt; i < rowToEndCalculationsAt; i++)
            {
                for (int j = 1; j < imageWidth - 1; j++)
                {
                    ImagePixelsValues[i][j] =
                        values[i][j] * 0.6f +
                        values[i + 1][j] * 0.1f +
                        values[i - 1][j] * 0.1f +
                        values[i][j + 1] * 0.1f +
                        values[i][j - 1] * 0.1f;
                }
            }
            c.Signal();
        }
        private void ComputeSidesDependently(object sender, DoWorkEventArgs e)
        {
            int imageHeight = (int)((object[])e.Argument)[0];
            int imageWidth = (int)((object[])e.Argument)[1];
            float[][] values = (float[][])((object[])e.Argument)[2];
            CountdownEvent c = (CountdownEvent)((object[])e.Argument)[3];
            for (int i = 1; i < imageWidth - 1; i++)
            {
                ImagePixelsValues[imageHeight - 1][i] =
                    values[imageHeight - 1][i] * 0.6f +
                    values[imageHeight - 1][i + 1] * 0.1f +
                    values[imageHeight - 1][i - 1] * 0.1f +
                    values[imageHeight - 2][i] * 0.1f;
                ImagePixelsValues[0][i] =
                    values[0][i] * 0.6f +
                    values[0][i + 1] * 0.1f +
                    values[0][i - 1] * 0.1f +
                    values[1][i] * 0.1f;
            }
            for (int i = 1; i < imageHeight - 1; i++)
            {
                ImagePixelsValues[i][0] =
                    values[i][0] * 0.6f +
                    values[i][1] * 0.1f +
                    values[i + 1][0] * 0.1f +
                    values[i - 1][0] * 0.1f;
                ImagePixelsValues[i][imageWidth - 1] =
                    values[i][imageWidth - 1] * 0.6f +
                    values[i][imageWidth - 2] * 0.1f +
                    values[i + 1][imageWidth - 1] * 0.1f +
                    values[i - 1][imageWidth - 1] * 0.1f;
            }
            ImagePixelsValues[0][0] =
                    values[0][0] * 0.6f +
                    values[0][1] * 0.1f +
                    values[1][0] * 0.1f;
            ImagePixelsValues[0][imageWidth - 1] =
                    values[0][imageWidth - 1] * 0.6f +
                    values[0][imageWidth - 2] * 0.1f +
                    values[1][imageWidth - 1] * 0.1f;
            ImagePixelsValues[imageHeight - 1][0] =
                    values[imageHeight - 1][0] * 0.6f +
                    values[imageHeight - 2][0] * 0.1f +
                    values[imageHeight - 1][1] * 0.1f;
            ImagePixelsValues[imageHeight - 1][imageWidth - 1] =
                    values[imageHeight - 1][imageWidth - 1] * 0.6f +
                    values[imageHeight - 1][imageWidth - 2] * 0.1f +
                    values[imageHeight - 2][imageWidth - 1] * 0.1f;
            c.Signal();
        }
        private void ComputeIndependentlyWithAdditionalZeroFillers(object sender, DoWorkEventArgs e)
        {
            int imageWidth = (int)((object[])e.Argument)[0];
            float[][] values = (float[][])((object[])e.Argument)[1];
            int rowToStartCalculationAt = 0;
            int rowsToUseInCalculationCount = values.Length;
            int imageToComputeFragmentStartingRowIndex = (int)((object[])e.Argument)[2];
            CountdownEvent c = (CountdownEvent)((object[])e.Argument)[3];
            int iterations = (int)((object[])e.Argument)[4];
            float[][] computedValues = new float[values.Length][];
            float[][] tmp;
            for (int i = 0; i < values.Length; i++)
            {
                computedValues[i] = new float[imageWidth];
            }
            for (int z = 0; z < iterations; z++)
            {
                rowToStartCalculationAt++;
                rowsToUseInCalculationCount--;
                for (int i = rowToStartCalculationAt; i < rowsToUseInCalculationCount - 1; i++)
                {
                    for (int j = 1; j < imageWidth - 1; j++)
                    {
                        computedValues[i][j] =
                            values[i][j] * 0.6f +
                            values[i][j + 1] * 0.1f +
                            values[i][j - 1] * 0.1f +
                            values[i + 1][j] * 0.1f +
                            values[i - 1][j] * 0.1f;
                    }
                }
                for (int i = rowToStartCalculationAt; i < rowsToUseInCalculationCount - 1; i++)
                {
                    computedValues[i][0] =
                        values[i][0] * 0.6f +
                        values[i][1] * 0.1f +
                        values[i + 1][0] * 0.1f +
                        values[i - 1][0] * 0.1f;
                    computedValues[i][imageWidth - 1] =
                        values[i][imageWidth - 1] * 0.6f +
                        values[i][imageWidth - 2] * 0.1f +
                        values[i + 1][imageWidth - 1] * 0.1f +
                        values[i - 1][imageWidth - 1] * 0.1f;
                }
                tmp = values;
                values = computedValues;
                computedValues = tmp;
            }
            for (int i = rowToStartCalculationAt; i < rowsToUseInCalculationCount; i++)
            {
                ImagePixelsValues[imageToComputeFragmentStartingRowIndex] = computedValues[i];
                imageToComputeFragmentStartingRowIndex++;
            }
            c.Signal();
        }

        private void ComputeIndependentlyWithoutAdditionalZeroFillers(object sender, DoWorkEventArgs e)
        {
            int amountOfRowsToApplyFilterOn = (int)((object[])e.Argument)[0];
            int imageWidth = (int)((object[])e.Argument)[1];
            float[][] values = (float[][])((object[])e.Argument)[2];
            int rowAmount = values.Length;
            int rowToStartCalculationAt = 1;
            int rowsToUseInCalculationCount = values.Length - 1;
            int fragmentToComputeStartingRowIndex = (int)((object[])e.Argument)[3];
            int imageToComputeFragmentStartingRowIndex = (int)((object[])e.Argument)[4];
            CountdownEvent c = (CountdownEvent)((object[])e.Argument)[5];
            int iterations = (int)((object[])e.Argument)[6] + 1;
            float[][] computedValues = new float[rowAmount][];
            int fragmentToComputeEndingRowIndex = amountOfRowsToApplyFilterOn + fragmentToComputeStartingRowIndex;
            float[][] tmp;
            for (int i = 0; i < values.Length; i++)
            {
                computedValues[i] = new float[imageWidth];
            }
            while (rowsToUseInCalculationCount - iterations < fragmentToComputeEndingRowIndex && fragmentToComputeStartingRowIndex - iterations < 0 && iterations > 0)
            {
                for (int i = 1; i < rowAmount - 1; i++)
                {
                    for (int j = 1; j < imageWidth - 1; j++)
                    {
                        computedValues[i][j] =
                            values[i][j] * 0.6f +
                            values[i][j + 1] * 0.1f +
                            values[i][j - 1] * 0.1f +
                            values[i + 1][j] * 0.1f +
                            values[i - 1][j] * 0.1f;
                    }
                }
                for (int i = 1; i < imageWidth - 1; i++)
                {
                    computedValues[0][i] =
                        values[0][i] * 0.6f +
                        values[0][i + 1] * 0.1f +
                        values[0][i - 1] * 0.1f +
                        values[1][i] * 0.1f;
                }
                computedValues[0][0] =
                        values[0][0] * 0.6f +
                        values[0][1] * 0.1f +
                        values[1][0] * 0.1f;
                computedValues[0][imageWidth - 1] =
                        values[0][imageWidth - 1] * 0.6f +
                        values[0][imageWidth - 2] * 0.1f +
                        values[1][imageWidth - 1] * 0.1f;
                for (int i = 1; i < rowAmount - 1; i++)
                {
                    computedValues[i][0] =
                        values[i][0] * 0.6f +
                        values[i][1] * 0.1f +
                        values[i + 1][0] * 0.1f +
                        values[i - 1][0] * 0.1f;
                    computedValues[i][imageWidth - 1] =
                        values[i][imageWidth - 1] * 0.6f +
                        values[i][imageWidth - 2] * 0.1f +
                        values[i + 1][imageWidth - 1] * 0.1f +
                        values[i - 1][imageWidth - 1] * 0.1f;
                }
                for (int i = 1; i < imageWidth - 1; i++)
                {
                    computedValues[rowAmount - 1][i] =
                        values[rowAmount - 1][i] * 0.6f +
                         values[rowAmount - 1][i + 1] * 0.1f +
                         values[rowAmount - 1][i - 1] * 0.1f +
                         values[rowAmount - 2][i] * 0.1f;
                }
                computedValues[rowAmount - 1][0] =
                        values[rowAmount - 1][0] * 0.6f +
                        values[rowAmount - 2][0] * 0.1f +
                        values[rowAmount - 1][1] * 0.1f;
                computedValues[rowAmount - 1][imageWidth - 1] =
                        values[rowAmount - 1][imageWidth - 1] * 0.6f +
                        values[rowAmount - 1][imageWidth - 2] * 0.1f +
                        values[rowAmount - 2][imageWidth - 1] * 0.1f;
                tmp = values;
                values = computedValues;
                computedValues = tmp;
                iterations--;
            }
            while (fragmentToComputeStartingRowIndex - iterations < 0 && iterations > 0)
            {
                for (int i = 1; i < rowsToUseInCalculationCount - 1; i++)
                {
                    for (int j = 1; j < imageWidth - 1; j++)
                    {
                        computedValues[i][j] =
                            values[i][j] * 0.6f +
                            values[i][j + 1] * 0.1f +
                            values[i][j - 1] * 0.1f +
                            values[i + 1][j] * 0.1f +
                            values[i - 1][j] * 0.1f;
                    }
                }
                for (int i = 1; i < imageWidth - 1; i++)
                {
                    computedValues[0][i] =
                        values[0][i] * 0.6f +
                        values[0][i + 1] * 0.1f +
                        values[0][i - 1] * 0.1f +
                        values[1][i] * 0.1f;
                }
                computedValues[0][0] =
                        values[0][0] * 0.6f +
                        values[0][1] * 0.1f +
                        values[1][0] * 0.1f;
                computedValues[0][imageWidth - 1] =
                        values[0][imageWidth - 1] * 0.6f +
                        values[0][imageWidth - 2] * 0.1f +
                        values[1][imageWidth - 1] * 0.1f;
                for (int i = 1; i < rowsToUseInCalculationCount - 1; i++)
                {
                    computedValues[i][0] =
                        values[i][0] * 0.6f +
                        values[i][1] * 0.1f +
                        values[i + 1][0] * 0.1f +
                        values[i - 1][0] * 0.1f;
                    computedValues[i][imageWidth - 1] =
                        values[i][imageWidth - 1] * 0.6f +
                        values[i][imageWidth - 2] * 0.1f +
                        values[i + 1][imageWidth - 1] * 0.1f +
                        values[i - 1][imageWidth - 1] * 0.1f;
                }
                tmp = values;
                values = computedValues;
                computedValues = tmp;
                rowsToUseInCalculationCount--;
                iterations--;
            }
            while (rowsToUseInCalculationCount - iterations < fragmentToComputeEndingRowIndex && iterations > 0)
            {
                for (int i = rowToStartCalculationAt; i < rowAmount - 1; i++)
                {
                    for (int j = 1; j < imageWidth - 1; j++)
                    {
                        computedValues[i][j] =
                            values[i][j] * 0.6f +
                            values[i][j + 1] * 0.1f +
                            values[i][j - 1] * 0.1f +
                            values[i + 1][j] * 0.1f +
                            values[i - 1][j] * 0.1f;
                    }
                }
                for (int i = rowToStartCalculationAt; i < rowAmount - 1; i++)
                {
                    computedValues[i][0] =
                        values[i][0] * 0.6f +
                        values[i][1] * 0.1f +
                        values[i + 1][0] * 0.1f +
                        values[i - 1][0] * 0.1f;
                    computedValues[i][imageWidth - 1] =
                        values[i][imageWidth - 1] * 0.6f +
                        values[i][imageWidth - 2] * 0.1f +
                        values[i + 1][imageWidth - 1] * 0.1f +
                        values[i - 1][imageWidth - 1] * 0.1f;
                }
                for (int i = 1; i < imageWidth - 1; i++)
                {
                    computedValues[rowAmount - 1][i] =
                        values[rowAmount - 1][i] * 0.6f +
                         values[rowAmount - 1][i + 1] * 0.1f +
                         values[rowAmount - 1][i - 1] * 0.1f +
                         values[rowAmount - 2][i] * 0.1f;
                }
                computedValues[rowAmount - 1][0] =
                        values[rowAmount - 1][0] * 0.6f +
                        values[rowAmount - 2][0] * 0.1f +
                        values[rowAmount - 1][1] * 0.1f;
                computedValues[rowAmount - 1][imageWidth - 1] =
                        values[rowAmount - 1][imageWidth - 1] * 0.6f +
                        values[rowAmount - 1][imageWidth - 2] * 0.1f +
                        values[rowAmount - 2][imageWidth - 1] * 0.1f;
                tmp = values;
                values = computedValues;
                computedValues = tmp;
                rowToStartCalculationAt++;
                iterations--;
            }
            while (iterations > 0)
            {
                for (int i = rowToStartCalculationAt; i < rowsToUseInCalculationCount - 1; i++)
                {
                    for (int j = 1; j < imageWidth - 1; j++)
                    {
                        computedValues[i][j] =
                            values[i][j] * 0.6f +
                            values[i][j + 1] * 0.1f +
                            values[i][j - 1] * 0.1f +
                            values[i + 1][j] * 0.1f +
                            values[i - 1][j] * 0.1f;
                    }
                }
                for (int i = rowToStartCalculationAt; i < rowsToUseInCalculationCount - 1; i++)
                {
                    computedValues[i][0] =
                        values[i][0] * 0.6f +
                        values[i][1] * 0.1f +
                        values[i + 1][0] * 0.1f +
                        values[i - 1][0] * 0.1f;
                    computedValues[i][imageWidth - 1] =
                        values[i][imageWidth - 1] * 0.6f +
                        values[i][imageWidth - 2] * 0.1f +
                        values[i + 1][imageWidth - 1] * 0.1f +
                        values[i - 1][imageWidth - 1] * 0.1f;
                }
                tmp = values;
                values = computedValues;
                computedValues = tmp;
                rowToStartCalculationAt++;
                rowsToUseInCalculationCount--;
                iterations--;
            }
            for (int i = rowToStartCalculationAt - 1; i <= rowsToUseInCalculationCount; i++)
            {
                ImagePixelsValues[imageToComputeFragmentStartingRowIndex] = computedValues[i];
                imageToComputeFragmentStartingRowIndex++;
            }
            c.Signal();
        }
        private float[][][] DivideImagePixelValuesBetweenThreads(int amountOfRowsToApplyFilterOnBySingleThread, int iterations, int width, bool fillMissingPixelsWithZeros, int[] fragmentsToComputeStartingRowsInnerIndexes)
        {
            int rowsPerThread = amountOfRowsToApplyFilterOnBySingleThread + iterations * 2;
            float[][][] threadsValuesToCompute = new float[ThreadAmount][][];
            for (int i = 0; i < ThreadAmount; i++)
            {
                if (i == ThreadAmount - 1)
                {
                    rowsPerThread = (ImagePixelsValues.Length - i * amountOfRowsToApplyFilterOnBySingleThread) + iterations * 2;
                }
                threadsValuesToCompute[i] = new float[rowsPerThread][];
                for (int j = 0; j < rowsPerThread; j++)
                {
                    threadsValuesToCompute[i][j] = new float[width];
                }
            }
            rowsPerThread = amountOfRowsToApplyFilterOnBySingleThread + iterations * 2;
            int threadStartingRow;
            int rowToCopyIndex;
            for (int i = 0; i < ThreadAmount; i++)
            {
                threadStartingRow = i * amountOfRowsToApplyFilterOnBySingleThread;
                if (i == ThreadAmount - 1)
                {
                    rowsPerThread = (ImagePixelsValues.Length - i * amountOfRowsToApplyFilterOnBySingleThread) + iterations * 2;
                }
                if (threadStartingRow - iterations >= 0 && rowsPerThread + threadStartingRow - iterations < ImagePixelsValues.Length)
                {
                    rowToCopyIndex = threadStartingRow - iterations;
                    fragmentsToComputeStartingRowsInnerIndexes[i] = iterations;
                    for (int j = 0; j < rowsPerThread; j++)
                    {
                        ImagePixelsValues[rowToCopyIndex].CopyTo(threadsValuesToCompute[i][j], 0);
                        rowToCopyIndex++;
                    }
                }
                else if (threadStartingRow - iterations < 0 && threadStartingRow - iterations + rowsPerThread < ImagePixelsValues.Length)
                {
                    rowToCopyIndex = 0;
                    if (fillMissingPixelsWithZeros)
                    {
                        for (int d = 0; d < (iterations - threadStartingRow); d++)
                        {
                            for (int m = 0; m < width; m++)
                            {
                                threadsValuesToCompute[i][d][m] = 0;
                            }
                        }
                        for (int j = (iterations - threadStartingRow); j < rowsPerThread; j++)
                        {
                            ImagePixelsValues[rowToCopyIndex].CopyTo(threadsValuesToCompute[i][j], 0);
                            rowToCopyIndex++;
                        }
                    }
                    else
                    {
                        fragmentsToComputeStartingRowsInnerIndexes[i] = iterations - (iterations - threadStartingRow);
                        threadsValuesToCompute[i] = new float[rowsPerThread - (iterations - threadStartingRow)][];
                        for (int j = 0; j < threadsValuesToCompute[i].Length; j++)
                        {
                            threadsValuesToCompute[i][j] = new float[width];
                            ImagePixelsValues[j].CopyTo(threadsValuesToCompute[i][j], 0);
                        }
                    }
                }
                else if (threadStartingRow - iterations >= 0 && threadStartingRow - iterations + rowsPerThread > ImagePixelsValues.Length)
                {
                    rowToCopyIndex = (threadStartingRow - iterations);
                    if (!fillMissingPixelsWithZeros)
                    {
                        threadsValuesToCompute[i] = new float[ImagePixelsValues.Length - rowToCopyIndex][];
                        for (int j = 0; j < threadsValuesToCompute[i].Length; j++)
                        {
                            threadsValuesToCompute[i][j] = new float[width];
                        }
                        fragmentsToComputeStartingRowsInnerIndexes[i] = iterations;
                    }
                    for (int j = 0; j < ImagePixelsValues.Length - (threadStartingRow - iterations); j++)
                    {
                        ImagePixelsValues[rowToCopyIndex].CopyTo(threadsValuesToCompute[i][j], 0);
                        rowToCopyIndex++;
                    }
                    if (fillMissingPixelsWithZeros)
                    {
                        for (int y = ImagePixelsValues.Length - (threadStartingRow - iterations); y < rowsPerThread; y++)
                        {
                            for (int m = 0; m < width; m++)
                            {
                                threadsValuesToCompute[i][y][m] = 0;
                            }
                        }
                    }
                }
                else
                {
                    rowToCopyIndex = 0;
                    if (fillMissingPixelsWithZeros)
                    {
                        for (int d = 0; d < (iterations - threadStartingRow); d++)
                        {
                            for (int m = 0; m < width; m++)
                            {
                                threadsValuesToCompute[i][d][m] = 0;
                            }
                        }
                        for (int j = (iterations - threadStartingRow); j < ImagePixelsValues.Length - (threadStartingRow - iterations); j++)
                        {
                            ImagePixelsValues[rowToCopyIndex].CopyTo(threadsValuesToCompute[i][j], 0);
                            rowToCopyIndex++;
                        }
                        for (int y = ImagePixelsValues.Length - (threadStartingRow - iterations); y < rowsPerThread; y++)
                        {
                            for (int m = 0; m < width; m++)
                            {
                                threadsValuesToCompute[i][y][m] = 0;
                            }
                        }
                    }
                    else
                    {
                        fragmentsToComputeStartingRowsInnerIndexes[i] = iterations - (iterations - threadStartingRow);
                        threadsValuesToCompute[i] = new float[rowsPerThread - ((iterations - threadStartingRow) + (rowsPerThread - (ImagePixelsValues.Length - (threadStartingRow - iterations))))][];
                        for (int j = 0; j < threadsValuesToCompute[i].Length; j++)
                        {
                            threadsValuesToCompute[i][j] = new float[width];
                            ImagePixelsValues[j].CopyTo(threadsValuesToCompute[i][j], 0);
                        }
                    }
                }
            }
            return threadsValuesToCompute;
        }
    }
}
