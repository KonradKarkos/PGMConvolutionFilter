using System;

namespace ProjektIO
{
    class MyImage
    {
        private float[] values;
        private int[] size;

        public MyImage(int width, int height)
        {
            this.Size = new int[2];
            Size[0] = height;
            Size[1] = width;
            values = new float[height * width];
            for (int i = 0; i < height * width; i++)
            {
                values[i] = 0;
            }
        }

        public MyImage convolution()
        {
            MyImage img = new MyImage(size[1], size[0]);
            int index;
            for (int i = 1; i < size[0] - 1; i++)
            {
                for (int j = 1; j < size[1] - 1; j++)
                {
                    index = i * size[1] + j;
                    img.values[index] =
                        values[index] * 0.6f +
                        values[index + 1] * 0.1f +
                        values[index - 1] * 0.1f +
                        values[index + size[1]] * 0.1f +
                        values[index - size[1]] * 0.1f;
                }
            }
            for (int i = 1; i < size[1] - 1; i++)
            { // ^- pierwszy wiersz pikseli obrazka
                img.values[i] =
                    values[i] * 0.6f +
                    values[i + 1] * 0.1f +
                    values[i - 1] * 0.1f +
                    values[i + size[1]] * 0.1f;
            }
            for (int i = size[0] * (size[1] - 1) + 1; i < size[1] * size[0] - 1; i++)
            { // v- ostatni wiersz pikseli obrazka
                img.values[i] =
                    values[i] * 0.6f +
                    values[i + 1] * 0.1f +
                    values[i - 1] * 0.1f +
                    values[i - size[1]] * 0.1f;
            }
            for (int i = size[1]; i < size[0] * (size[1] - 1); i += size[1])
            { // <| pierwsza kolumna pikseli obrazka
                img.values[i] =
                    values[i] * 0.6f +
                    values[i + 1] * 0.1f +
                    values[i + size[1]] * 0.1f +
                    values[i - size[1]] * 0.1f;
            }
            for (int i = 2 * size[1] - 1; i < size[0] * size[1] - 1; i += size[1])
            { // >| ostatnia kolumna pikseli obrazka
                img.values[i] =
                    values[i] * 0.6f +
                    values[i - 1] * 0.1f +
                    values[i + size[1]] * 0.1f +
                    values[i - size[1]] * 0.1f;
            }
            //piksel w lewym górnym rogu obrazka
            img.values[0] =
                    values[0] * 0.6f +
                    //piksel na prawo
                    values[0 + 1] * 0.1f +
                    //piksel pod nim
                    values[0 + size[1]] * 0.1f;
            //piksel w prawym górnym rogu
            img.values[size[1] - 1] =
                    values[size[1] - 1] * 0.6f +
                    //piksel na lewo
                    values[size[1] - 1 - 1] * 0.1f +
                    //piksel pod nim
                    values[size[1] - 1 + size[1]] * 0.1f;
            //piksel w lewym dolnym rogu
            img.values[size[0] * (size[1] - 1)] =
                    values[size[0] * (size[1] - 1)] * 0.6f +
                    //piksel nad nim
                    values[size[0] * (size[1] - 1) - size[1]] * 0.1f +
                    //piksel na prawo
                    values[size[0] * (size[1] - 1) + 1] * 0.1f;
            //piksel w prawym dolnym rogu
            img.values[size[0] * size[1] - 1] =
                    values[size[0] * size[1] - 1] * 0.6f +
                    //piksel na lewo
                    values[size[0] * size[1] - 1 - 1] * 0.1f +
                    //piksel nad nim
                    values[size[0] * size[1] - 1 - size[1]] * 0.1f;
            return img;
        }
        public void CreateCheckerboard(int l)
        {
            int index;
            int liczba_pikseli_na_pole_w;
            int liczba_pikseli_na_pole_h;
            for (int i = 0; i < size[0]; i++)
            {
                for (int j = 0; j < size[1]; j++)
                {
                    index = i * size[1] + j;
                    liczba_pikseli_na_pole_w = size[1] / l;
                    liczba_pikseli_na_pole_h = size[0] / l;
                    if (((i / liczba_pikseli_na_pole_h) + (j / liczba_pikseli_na_pole_w)) % 2 == 0)
                        values[index] = 0;
                    else
                        values[index] = 1;
                }
            }
        }
        public void printConsole()
        {
            for (int i = 0; i < size[0]; i++)
            {
                for (int j = 0; j < size[1]; j++)
                {
                    int index = i * size[1] + j;
                    Console.Write("{0}\t", values[index]);
                }
                Console.WriteLine();
            }
        }
        public float[] Values { get => values; set => values = value; }
        public int[] Size { get => size; set => size = value; }
    }
}