using Microsoft.Win32;
using System;
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

namespace ProjektIO
{
    public partial class MainWindow : Window
    {
        int threadcount = 10;
        private MyImage obraz;
        int wymiar;
        //kontener na wyniki funkcji procesów
        float[] Wartosci;
        public MainWindow()
        {
            InitializeComponent();
        }
        //funkcja odpowiadająca za otwarcie okna dialogowego do wybrania pliku do testów
        private void Wybierz_Bin_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Portable Grayscale Map (*.pgm)|*.pgm";
            if (openFileDialog.ShowDialog() == true)
            {
                //odblokowanie checkboxa na wypadek gdyby był zaznaczony
                domyslny.IsChecked = false;
                //wypełnienie textboxa ścieżką pliku
                BoxPoczBin.Text = openFileDialog.FileName;
                //zaproponowanie przykładowych nazw dla plików do zapisu
                BoxKonBin.Text = BoxPoczBin.Text.Insert(BoxPoczBin.Text.Length - 4, "Koncowy");
                BoxObrazKon.Text = BoxPoczBin.Text.Remove(BoxPoczBin.Text.Length - 4, 4).Insert(BoxPoczBin.Text.Length - 4, ".png");
                BoxTxtKon.Text = BoxPoczBin.Text.Remove(BoxPoczBin.Text.Length - 4, 4).Insert(BoxPoczBin.Text.Length - 4, ".txt");
            }
        }
        //funkcja pomocnicza dla wczytywania linii z pliku pgm P5
        private String WczytanaLinia(BinaryReader br)
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
        //wczytanie linii nieoznaczonej jako komentarz z pliku pgm P5
        private String WczytajLinie(BinaryReader br)
        {
            String s = WczytanaLinia(br);
            while (s.StartsWith("#") || s.Equals(""))
            {
                s = WczytanaLinia(br);
            }
            return s;
        }
        //wczytanie linii nieoznaczonej jako komentarz z pliku pgm P2
        private String WczytajLinie(StreamReader sr)
        {
            String s = sr.ReadLine().Trim();
            while (s.StartsWith("#") || s.Equals(""))
            {
                s = sr.ReadLine().Trim();
            }
            return s;
        }
        //funkcja konwertująca bitmapę do bitmapimage aby było możliwe podpięcie obrazu pod kontrolkę image wpf
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
        //wczytanie kolejnej liczby oznaczającej kolor pixela za pomocą streamreadera (w wypadku binary readera jest to po prostu jeden bajt)
        private String liczba(StreamReader sr)
        {
            StringBuilder sb = new StringBuilder();
            char c = 'a';
            while(c!=' ')
            {
                c = (char)sr.Read();
                sb.Append(c);
            }
            return sb.ToString();
        }
        //wczytanie obrazu z podanej ścieżki
        private void BWczytajObraz_Click(object sender, RoutedEventArgs e)
        {
            //przekonwertowanie ilości wątków podanych przez użytkowanika
            if (Int32.TryParse(BoxWatki.Text, out threadcount))
            {
                if (threadcount < 1000)
                {
                    //wygenerowanie pustego obrazu 1024x1024 w wypadku zaznaczenia takiej opcji
                    if (domyslny.IsChecked == true)
                    {
                        if(BoxWYmiary.Text.Length<=0)
                        {
                            BoxWYmiary.Text = "1024";
                        }
                        if (Int32.TryParse(BoxWYmiary.Text, out wymiar))
                        {
                            if (wymiar > threadcount - 2 && wymiar*wymiar<Int32.MaxValue)
                            {
                                obraz = new MyImage(wymiar, wymiar);
                                //odblokowanie przycisku "Start"
                                button1.IsEnabled = true;
                                //poinformowanie użytkownika opowiednimi komunikatami o zakończeniu akcji
                                if (textBox.Text.Length > 0) textBox.Text += '\n';
                                textBox.Text += "==============================================" + '\n' + "Wczytano obraz domyśny o wymiarach " + BoxWYmiary.Text + BlockWymiar2.Text;
                                MessageBox.Show("Wczytywanie zakończone.");
                            }
                            else
                            {
                                MessageBox.Show("Podano zbyt mały lub zbyt duży wymiar obrazu domyślnego.");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Nieudana konwersja typu. Podany wymiar jest zbyt mały lub zbyt duży dla typu Int32.");
                        }
                    }
                    //wczytanie obrazu z podanej ścieżki
                    else
                    {
                        if (File.Exists(BoxPoczBin.Text))
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            StringBuilder sb = new StringBuilder();
                            BinaryReader br = new BinaryReader(File.Open(BoxPoczBin.Text, FileMode.Open));
                            //wczytanie wersji pliku pgm
                            String wersja = WczytajLinie(br);
                            sb.AppendLine(wersja);
                            if (!wersja.Equals("P5") && !wersja.Equals("P2"))
                            {
                                MessageBox.Show("Nieznany format pliku PGM - " + wersja);
                                br.Dispose();
                            }
                            else
                            {
                                int width, height;
                                //wczytanie wymiarów obrazu
                                String[] dane = WczytajLinie(br).Split(' ');
                                sb.AppendLine(dane[0] + " " + dane[1]);
                                width = Int32.Parse(dane[0]);
                                height = Int32.Parse(dane[1]);
                                if (height > threadcount-2)
                                {
                                    //wczytanie maksymalnej wartości koloru piksela w tym pliku
                                    int max = Int32.Parse(WczytajLinie(br));
                                    sb.AppendLine(max.ToString());
                                    if (max > 255)
                                    {
                                        MessageBox.Show("Zbyt duża wartość maksymalna piksela - " + max);
                                        br.Dispose();
                                    }
                                    else
                                    {
                                        obraz = new MyImage(width, height);
                                        float[] wartosci = new float[width * height];
                                        byte b = 0;
                                        int indeks = 0;
                                        //wczytanie pliku jeśli jego wersja to P2 (zapisana w ASCII)
                                        if (wersja.Equals("P2"))
                                        {
                                            br.Dispose();
                                            StreamReader sr = new StreamReader(BoxPoczBin.Text);
                                            //wczytanie i pominięcie linii które zostały już przypisane przez BinaryReader
                                            //wersja
                                            WczytajLinie(sr);
                                            //wymiary
                                            WczytajLinie(sr);
                                            //maksymalna wartość
                                            WczytajLinie(sr);
                                            //wczytanie po kolei wszystkich wartości pikseli
                                            for (int i = 0; i < height; i++)
                                            {
                                                for (int j = 0; j < width; j++)
                                                {
                                                    indeks = i * height + j;
                                                    wersja = liczba(sr);
                                                    wartosci[indeks] = float.Parse(wersja);
                                                    sb.Append(wersja + " ");
                                                }
                                                sb.AppendLine();
                                            }
                                        }
                                        //wczytanie pliku jeśli jego wersja to P5 (zapisana binarnie)
                                        else
                                        {
                                            for (int i = 0; i < height; i++)
                                            {
                                                for (int j = 0; j < width; j++)
                                                {
                                                    indeks = i * height + j;
                                                    b = br.ReadByte();
                                                    wartosci[indeks] = b;
                                                    sb.Append(b + " ");
                                                }
                                                sb.AppendLine();
                                            }
                                        }
                                        //wyświetlenie wczytanego pliku w odpowiednim textbox'ie
                                        DisplayBoxPocz.Text = sb.ToString();
                                        //przypisanie wczytanych wartości do obiektu przechowującego informacje o pliku pgm
                                        obraz.Values = (float[])wartosci;
                                        Bitmap poczatkowy = new Bitmap(width, height);
                                        Color c = new Color();
                                        int wartosc;
                                        //utworzenie obrazu w celu wyświetlenia wczytanego pliku
                                        for (int i = 0; i < height; i++)
                                        {
                                            for (int j = 0; j < width; j++)
                                            {
                                                wartosc = (int)wartosci[i * height + j];
                                                //ustawienie koloru piksela na odpowiedni odcień szarości
                                                c = Color.FromArgb(wartosc, wartosc, wartosc);
                                                poczatkowy.SetPixel(j, i, c);
                                            }
                                        }
                                        //przypisanie utworzonego obrazu do kontrolki wpf
                                        image.Source = BitmapToImageSource(poczatkowy);
                                        br.Dispose();
                                        sw.Stop();
                                        //poinformowanie użytkownika o wczytaniu obrazu
                                        if (textBox.Text.Length > 0) textBox.Text += '\n';
                                        textBox.Text += "==============================================" + '\n' + "Ścieżka wczytanego pliku: "+ BoxPoczBin.Text + '\n' + "Czas wczytania obrazu: " + sw.Elapsed.TotalMilliseconds + " ms.";
                                        MessageBox.Show("Wczytywanie zakończone.");
                                        button1.IsEnabled = true;
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Wczytywany obraz ma zbyt małą wysokość dla podanej liczby wątków!");
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Podany plik nie istnieje!");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Ustwawiono zbyt dużą ilość wątków!");
                }
            }
            else
            {
                MessageBox.Show("Konwersja liczby wątków nie powiodła się.");
            }
        }
        //metoda konwertująca imagesource do bitmapy
        private Bitmap ImagetoBitMap(MyImage mapa)
        {
            int height = mapa.Size[0];
            int width = mapa.Size[1];
            Bitmap result = new Bitmap(width, height);
            Color c = new Color();
            int wartosc;
            float[] wartosci = mapa.Values;
            for(int i=0;i<height;i++)
            {
                for(int j=0;j<width;j++)
                {
                    wartosc = (int)wartosci[i * height + j];
                    c = Color.FromArgb(wartosc, wartosc, wartosc);
                    result.SetPixel(j, i, c);
                }
            }
            return result;
        }
        //funkcja z której korzystają wątki aplikujące filtr we wnętrzu obrazu
        private void normalny(object dane)
        {
            int height = (int)((object[])dane)[0];
            int width = (int)((object[])dane)[1];
            float[] values = (float[])((object[])dane)[2];
            int poczatek = (int)((object[])dane)[3];
            int koniec = (int)((object[])dane)[4];
            int numer = (int)((object[])dane)[5];
            CountdownEvent c = (CountdownEvent)((object[])dane)[6];
            //CountdownEvent c2 = (CountdownEvent)((object[])dane)[7];
            //float[] nowe = new float[values.Length];
            int index;
            //pominięcie pierwszego wiersza pikseli aby nie wychodzić poza zakres
            if (poczatek == 0) poczatek++;
            //przesunięcie granicy nakładania filtru dla ostatniego wątku jeśli wysokość obrazu nie była wielokrotnością liczby wątków
            if (numer == threadcount-1) koniec = height-1;
            for (int i = poczatek; i < koniec; i++)
            {
                for (int j = 1; j < width - 1; j++)
                {
                    index = i * width + j;
                    Wartosci[index] =
                        values[index] * 0.6f +
                        values[index + 1] * 0.1f +
                        values[index - 1] * 0.1f +
                        values[index + width] * 0.1f +
                        values[index - width] * 0.1f;
                }
            }
            c.Signal();
        }
        //funkcja z której korzystają wątki aplikujące filtr na brzegach
        private void boczny(object dane)
        {
            int height = (int)((object[])dane)[0];
            int width = (int)((object[])dane)[1];
            float[] values = (float[])((object[])dane)[2];
            CountdownEvent c = (CountdownEvent)((object[])dane)[3];
            //CountdownEvent c2 = (CountdownEvent)((object[])dane)[4];
            //float[] nowe = new float[values.Length];
            for (int i = 1; i < width - 1; i++)
            { // ^- pierwszy wiersz pikseli obrazka
                Wartosci[i] =
                    values[i] * 0.6f +
                    values[i + 1] * 0.1f +
                    values[i - 1] * 0.1f +
                    values[i + width] * 0.1f;
            }
            for (int i = height * (width - 1) + 1; i < width * height - 1; i++)
            { // v- ostatni wiersz pikseli obrazka
                Wartosci[i] =
                    values[i] * 0.6f +
                    values[i + 1] * 0.1f +
                    values[i - 1] * 0.1f +
                    values[i - width] * 0.1f;
            }
            for (int i = width; i < height * (width - 1); i += width)
            { // <| pierwsza kolumna pikseli obrazka
                Wartosci[i] =
                    values[i] * 0.6f +
                    values[i + 1] * 0.1f +
                    values[i + width] * 0.1f +
                    values[i - width] * 0.1f;
            }
            for (int i = 2 * width - 1; i < height * width - 1; i += width)
            { // >| ostatnia kolumna pikseli obrazka
                Wartosci[i] =
                    values[i] * 0.6f +
                    values[i - 1] * 0.1f +
                    values[i + width] * 0.1f +
                    values[i - width] * 0.1f;
            }
            //piksel w lewym górnym rogu obrazka
            Wartosci[0] =
                    values[0] * 0.6f +
                    //piksel na prawo
                    values[0 + 1] * 0.1f +
                    //piksel pod nim
                    values[0 + width] * 0.1f;
            //piksel w prawym górnym rogu
            Wartosci[width - 1] =
                    values[width - 1] * 0.6f +
                    //piksel na lewo
                    values[width - 1 - 1] * 0.1f +
                    //piksel pod nim
                    values[width - 1 + width] * 0.1f;
            //piksel w lewym dolnym rogu
            Wartosci[height * (width - 1)] =
                    values[height * (width - 1)] * 0.6f +
                    //piksel nad nim
                    values[height * (width - 1) - width] * 0.1f +
                    //piksel na prawo
                    values[height * (width - 1) + 1] * 0.1f;
            //piksel w prawym dolnym rogu
            Wartosci[height * width - 1] =
                    values[height * width - 1] * 0.6f +
                    //piksel na lewo
                    values[height * width - 1 - 1] * 0.1f +
                    //piksel nad nim
                    values[height * width - 1 - width] * 0.1f;
            c.Signal();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            button1.IsEnabled = false;
            int iteracje;
            if (!Int32.TryParse(BoxWatki.Text, out threadcount))
            {
                MessageBox.Show("Nieudana konwersja typu przy konwersji ilości wątków.");
            }
            else
            {
                if (threadcount > 1000 || obraz.Size[0] < threadcount - 2)
                {
                    MessageBox.Show("Podano zbyt dużą ilość wątków!");
                }
                else
                {
                    if (!Int32.TryParse(BoxIteracje.Text, out iteracje))
                    {
                        MessageBox.Show("Nieudana konwersja ilości iteracji, spróbuj zmniejszyć ilość potencjalnych iteracji.");
                    }
                    else
                    {
                        textBox.Text += '\n' + "Ustawiona ilość iteracji: " + iteracje + '\n' + "Ustawiona ilość wątków: " + threadcount;
                        PB.Maximum = iteracje * 2;
                        PB.Visibility = Visibility.Visible;
                        MyImage testowy = obraz;
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        //synchroniczne nałożenie filtru
                        for (int i = 0; i < iteracje; i++)
                        {
                            testowy = testowy.convolution();
                        }
                        sw.Stop();
                        this.Dispatcher.Invoke(() => { PB.Value = PB.Maximum / 2; ; }, DispatcherPriority.ContextIdle);
                        StringBuilder sb = new StringBuilder();
                        int wordcount = 4;
                        int indeks = 0;
                        String tekst = DisplayBoxPocz.Text;
                        //wczytanie czterech wartości: wersji, szerokości, wysokości i maksymalnej wartości piksela jeśli nie jest to obraz domyślny
                        if (domyslny.IsChecked == false)
                        {
                            char c;
                            while (wordcount > 0)
                            {
                                c = tekst[indeks];
                                sb.Append(c);
                                if (c.Equals('\n') || c.Equals(' ')) wordcount--;
                                indeks++;
                            }
                        }
                        //dołączenie wartości dla domyślnego obrazu
                        else
                        {
                            sb.AppendLine("P5");
                            sb.AppendLine(wymiar + " " + wymiar);
                            sb.AppendLine("255");
                        }
                        int height = testowy.Size[0];
                        int width = testowy.Size[1];
                        Wartosci = testowy.Values;
                        //wczytanie wartości uzyskanych w wersji synchronicznej
                        for (int i = 0; i < height; i++)
                        {
                            for (int j = 0; j < width; j++)
                            {
                                sb.Append(Wartosci[i * height + j] + " ");
                            }
                            sb.AppendLine();
                        }
                        //wyświetlenie danych uzyskanych w wersji synchronicznej
                        DisplayBoxKon.Text = sb.ToString();
                        //wyświetlenie obrazu uzyskanego synchronicznie
                        image_Copy.Source = BitmapToImageSource(ImagetoBitMap(testowy));
                        textBox.Text += '\n' + "Czas zastosowania filtru splotowego w wersji synchronicznej: " + sw.Elapsed.TotalMilliseconds + " ms.";
                        //wersja asynchroniczna
                        testowy = obraz;
                        //wyznaczenie obszarów obliczeń dla wątków
                        int podzielone = height / threadcount;
                        //event służący do zasygnalizowania wątkowi głównemu kiedy może zsumować wyniki obliczeń wątków
                        CountdownEvent countdownEvent;
                        //CountdownEvent[] CountdownEvent = new CountdownEvent[threadcount+2];
                        Wartosci = obraz.Values;
                        indeks = 0;
                        float[] dummy = new float[Wartosci.Length];
                        sw.Restart();
                        for (int i = 0; i < iteracje; i++)
                        {
                            Wartosci.CopyTo(dummy, 0);
                            countdownEvent = new CountdownEvent(threadcount + 1);
                            for (int j = 0; j < threadcount; j++)
                            {
                                ThreadPool.QueueUserWorkItem(normalny, new object[] { height, width, dummy, podzielone * j, podzielone * (j + 1), j, countdownEvent });
                            }
                            ThreadPool.QueueUserWorkItem(boczny, new object[] { height, width, dummy, countdownEvent });
                            //czekanie aż wszystkie wątki skończą obliczenia
                            countdownEvent.Wait();
                        }
                        sw.Stop();
                        testowy.Values = Wartosci;
                        this.Dispatcher.Invoke(() => { PB.Value = PB.Maximum / 2; ; }, DispatcherPriority.ContextIdle);
                        sb.Clear();
                        wordcount = 4;
                        indeks = 0;
                        //wczytanie wartości uzyskanych w wersji asynchronicznej
                        if (domyslny.IsChecked == false)
                        {
                            char c;
                            while (wordcount > 0)
                            {
                                c = tekst[indeks];
                                sb.Append(c);
                                if (c.Equals('\n') || c.Equals(' ')) wordcount--;
                                indeks++;
                            }
                        }
                        else
                        {
                            sb.AppendLine("P5");
                            sb.AppendLine(wymiar + " " + wymiar);
                            sb.AppendLine("255");
                        }
                        for (int i = 0; i < height; i++)
                        {
                            for (int j = 0; j < width; j++)
                            {
                                sb.Append(Wartosci[i * height + j] + " ");
                            }
                            sb.AppendLine();
                        }
                        DisplayBoxKon_Copy.Text = sb.ToString();
                        image_Copy1.Source = BitmapToImageSource(ImagetoBitMap(testowy));
                        obraz = testowy;
                        textBox.Text += (char)10 + "Czas zastosowania filtru splotowego w wersji asynchronicznej: " + sw.Elapsed.TotalMilliseconds + " ms.";
                        PB.Visibility = Visibility.Hidden;
                        PB.Value = 0;
                        BZapiszBin.IsEnabled = true;
                        BZapiszObraz.IsEnabled = true;
                        BZapiszTxt.IsEnabled = true;
                        MessageBox.Show("Obliczenia w wersji synchronicznej i asynchronicznej zakończone!");
                    }
                }
            }
            button1.IsEnabled = true;
        }
        //umożliwienie wczytania pliku tylko kiedy textbox z ścieżką nie jest pusty
        private void BoxPoczBin_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BoxPoczBin.Text.Length > 0)
                BWczytajPocz.IsEnabled = true;
            else
                BWczytajPocz.IsEnabled = false;
        }

        private void Domyslny_Checked(object sender, RoutedEventArgs e)
        {
            BoxPoczBin.Text = "Domyslny.pgm";
            BoxKonBin.Text = BoxPoczBin.Text.Insert(BoxPoczBin.Text.Length - 4, "Koncowy");
            BoxObrazKon.Text = BoxPoczBin.Text.Remove(BoxPoczBin.Text.Length - 4, 4).Insert(BoxPoczBin.Text.Length - 4, ".png");
            BoxTxtKon.Text = BoxPoczBin.Text.Remove(BoxPoczBin.Text.Length - 4, 4).Insert(BoxPoczBin.Text.Length - 4, ".txt");
            BoxWYmiary.IsEnabled = false;
        }

        private void Domyslny_Unchecked(object sender, RoutedEventArgs e)
        {
            BoxPoczBin.Text = "";
            BoxKonBin.Text = "";
            BoxObrazKon.Text = "";
            BoxTxtKon.Text = "";
            BoxWYmiary.IsEnabled = true;
        }

        private void BZapiszObraz_Click(object sender, RoutedEventArgs e)
        {
            ImagetoBitMap(obraz).Save(BoxObrazKon.Text);
            MessageBox.Show("Poprawnie zapisano do pliku.");
        }
        //metoda zapisująca wynik do pliku tekstowego
        private void BZapiszTxt_Click(object sender, RoutedEventArgs e)
        {
            StreamWriter sw = new StreamWriter(BoxTxtKon.Text);
            int linie = DisplayBoxKon.LineCount;
            if (linie <= 0)
            {
                for (int i = 0; i < linie; i++)
                {
                    sw.WriteLine(DisplayBoxKon.GetLineText(i));
                }
            }
            else
            {
                sw.Write(DisplayBoxKon.Text);
            }
            sw.Dispose();
            MessageBox.Show("Poprawnie zapisano do pliku.");
        }
        //zapisanie pliku wynikowego w formie binarnej
        private void BZapiszBin_Click(object sender, RoutedEventArgs e)
        {
            BinaryWriter bw = new BinaryWriter(File.Open(BoxKonBin.Text, FileMode.OpenOrCreate));
            char c = 'a';
            StringBuilder sb = new StringBuilder();
            String tekst = DisplayBoxKon.Text;
            //jest to wersja binarna, więc musi mieć oznaczenie P5
            bw.Write('P');
            bw.Write('5');
            bw.Write('\n');
            int wordcount = 3;
            int indeks = 2;
            //ustawienie indeksu na pierwszą liczbę
            while (!Char.IsDigit(tekst[indeks])) indeks++;
            //zapisanie wymiarów i maksymalnej wartości piksela obrazu
            while (wordcount > 0)
            {
                c = tekst[indeks];
                bw.Write(c);
                if (c.Equals('\n') || c.Equals(' ')) wordcount--;
                indeks++;
            }
            //zapisanie wartości pikseli wyniku
            for (int i=indeks;i<DisplayBoxKon.Text.Length;i++)
            {
                c = tekst[i];
                //wczytywanie pojedynczych liczb
                while (!c.Equals('\n') && !c.Equals(' ') && !c.Equals('\r'))
                {
                    sb.Append(c);
                    i++;
                    c = tekst[i];
                }
                if (sb.ToString().Length > 0)
                {
                    //zapisanie tylko wartości jedności liczby
                    bw.Write(byte.Parse(sb.ToString().Split(',')[0]));
                    sb.Clear();
                }
            }
            bw.Dispose();
            MessageBox.Show("Poprawnie zapisano do pliku.");
        }
        //zmiana widoku na plik powstały synchronicznie/asynchronicznie
        private void BSwitchTxt_Click(object sender, RoutedEventArgs e)
        {
            if(DisplayBoxKon.Visibility==Visibility.Visible)
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            char c = '\n';
            MessageBox.Show("Program służy do zastosowania filtru splotowego domyślnie 200 razy na pliku pgm za pomocą 11 wątków (pomiędzy 10 jest podzielone wnętrze obrazu, a 1 zajmuje się pikselami brzegowymi) i porównania czasowego z wersją synchroniczną." +
                c + "Wyniki obu operacji można porównać z oryginalnym obrazem w zakładkach \"Tekstowe\" i \"Obraz\" przełączając widok na wynik operacji asynchronicznej i synchronicznej przyciskiem w prawym górnym rogu okna." +
                c + "Wyniki są zapisywane do pliku \"Results.txt\"");
        }
        //metoda sprawdzająca czy do danego textboxa są wprowadzane tylko liczby
        private void Sprawdz_liczby(object sender, TextCompositionEventArgs e)
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
        //zapisanie wyników działania programu do pliku
        private void BZapiszWyniki_Click(object sender, RoutedEventArgs e)
        {
            StreamWriter sw = new StreamWriter("Results.txt");
            int linie = textBox.LineCount;
            if (linie <= 0)
            {
                for (int i = 0; i < linie; i++)
                {
                    sw.WriteLine(textBox.GetLineText(i));
                }
            }
            else
            {
                sw.Write(textBox.Text);
            }
            sw.Dispose();
            MessageBox.Show("Poprawnie zapisano do pliku.");
        }

        private void BoxWYmiary_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(BlockWymiar2!=null)
            BlockWymiar2.Text = 'x' + BoxWYmiary.Text;
        }
    }
}