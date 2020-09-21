using System;

namespace PGMConvolutionFilter
{
    class MyImage
    {
        private float[][] values;
        private int height;
        private int width;

        public MyImage(int width, int height)
        {
            this.height = height;
            this.width = width;
            values = new float[height][];
            for (int i = 0; i < height; i++)
            {
                values[i] = new float[width];
                for (int j = 0; j < width; j++)
                {
                    values[i][j] = 0;
                }
            }
        }

        public MyImage convolution()
        {
            MyImage img = new MyImage(width, height);
            for (int i = 1; i < height - 1; i++)
            {
                for (int j = 1; j < width - 1; j++)
                {
                    img.values[i][j] =
                        values[i][j] * 0.6f +
                        values[i][j+1] * 0.1f +
                        values[i][j-1] * 0.1f +
                        values[i-1][j] * 0.1f +
                        values[i+1][j] * 0.1f;
                }
            }
            for (int i = 1; i < width - 1; i++)
            { // ^- pierwszy wiersz pikseli obrazka
                img.values[0][i] =
                    values[0][i] * 0.6f +
                    values[0][i + 1] * 0.1f +
                    values[0][i - 1] * 0.1f +
                    values[1][i] * 0.1f;
                // v- ostatni wiersz pikseli obrazka
                img.values[height - 1][i] =
                    values[height - 1][i] * 0.6f +
                     values[height - 1][i + 1] * 0.1f +
                     values[height - 1][i - 1] * 0.1f +
                     values[height - 2][i] * 0.1f;
            }
            for (int i = 1; i < height-1; i++)
            { // <| pierwsza kolumna pikseli obrazka
                img.values[i][0] =
                    values[i][0] * 0.6f +
                    values[i][1] * 0.1f +
                    values[i +1][0] * 0.1f +
                    values[i - 1][0] * 0.1f;
              // >| ostatnia kolumna pikseli obrazka
                img.values[i][width-1] =
                    values[i][width - 1] * 0.6f +
                    values[i][width - 2] * 0.1f +
                    values[i + 1][width - 1] * 0.1f +
                    values[i - 1][width - 1] * 0.1f;
            }
            //piksel w lewym górnym rogu obrazka
            img.values[0][0] =
                    values[0][0] * 0.6f +
                    //piksel na prawo
                    values[0][1] * 0.1f +
                    //piksel pod nim
                    values[1][0] * 0.1f;
            //piksel w prawym górnym rogu
            img.values[0][width-1] =
                    values[0][width - 1] * 0.6f +
                    //piksel na lewo
                    values[0][width - 2] * 0.1f +
                    //piksel pod nim
                    values[1][width - 1] * 0.1f;
            //piksel w lewym dolnym rogu
            img.values[height-1][0] =
                    values[height - 1][0] * 0.6f +
                    //piksel nad nim
                    values[height - 2][0] * 0.1f +
                    //piksel na prawo
                    values[height - 1][1] * 0.1f;
            //piksel w prawym dolnym rogu
            img.values[height-1][width-1] =
                    values[height - 1][width - 1] * 0.6f +
                    //piksel na lewo
                    values[height - 1][width - 2] * 0.1f +
                    //piksel nad nim
                    values[height - 2][width - 1] * 0.1f;
            return img;
        }
        public void CreateCheckerboard(int l)
        {
            int liczba_pikseli_na_pole_w;
            int liczba_pikseli_na_pole_h;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    liczba_pikseli_na_pole_w = width / l;
                    liczba_pikseli_na_pole_h = height / l;
                    if (((i / liczba_pikseli_na_pole_h) + (j / liczba_pikseli_na_pole_w)) % 2 == 0)
                        values[i][j] = 0;
                    else
                        values[i][j] = 255;
                }
            }
        }
        public void printConsole()
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Console.Write("{0}\t", values[i][j]);
                }
                Console.WriteLine();
            }
        }
        public float[][] Values { get => values; set => values = value; }
        public int Height { get => height; set => height = value; }
        public int Width { get => width; set => width = value; }
    }
}