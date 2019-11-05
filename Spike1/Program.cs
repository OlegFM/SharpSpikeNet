using System;
using SpikeNeyroNetGen1;
using System.Numerics;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Shapes;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing.Processors.Drawing;

namespace Spike1
{
    class Program
    {
        static void Main(string[] args)
        {
            long timestamp = 100001;
            NeyroNet net = new NeyroNet();
            net.AddLayer(new NeyroLayer(100, 57600));

            int patternCol = 0;
            int patternCount = 0;
            string path = "../output_spectrum/output";
            Directory.CreateDirectory(path + "/patterns");
            for (int k = 0; k<200; k++)
            {
                string  sourcePath = "C:/Users/olege/source/repos/Spike1/contest_data/baby_cry/";
                WavReader wav = new WavReader(sourcePath+k+".wav");

                List<double> max_spect = new List<double>();

                Int16[] data = wav.GetData();
                int dataFramesCount = data.Length / 240;

                int framenumb = 0;
                var animatedImage = new Image<Rgba32>(240, 240);
                Directory.CreateDirectory(path + "/file" + k);
                animatedImage.Mutate(ctx => ctx.Fill(Rgba32.White));
                var file = File.Open(path + ".gif", FileMode.OpenOrCreate);
                animatedImage.SaveAsGif(file, new GifEncoder());
                file.Close();

                var netPattern = new Image<Rgba32>(10*2*dataFramesCount, 1000);
                netPattern.Mutate(ctx => ctx.Fill(Rgba32.WhiteSmoke));
                var inputPattern = new Image<Rgba32>(10 * 2 * dataFramesCount, 1000);
                inputPattern.Mutate(ctx => ctx.Fill(Rgba32.WhiteSmoke));
                for (int i = 0; i < dataFramesCount*2; i++)
                {
                    Complex[] frame1 = new Complex[240];
                    Complex[] frame2 = new Complex[240];

                    for (int j = 0; j < 240; j++)
                    {
                        try
                        {
                            frame1[j] = data[120 * i + j];
                        }
                        catch
                        {
                            frame1[j] = new Complex(0,0);
                        }
                        
                        try
                        {
                            frame2[j] = data[120 * i + j + 15];
                        }
                        catch
                        {
                            frame2[j] = new Complex(0,0);
                        }
                    }
                    Complex[] spectrum1 = FTT.DecimationInTime(frame1, true);
                    Complex[] spectrum2 = FTT.DecimationInTime(frame1, true);
                    for (var j = 0; j < 240; j++)
                    {
                        spectrum1[j] /= 240;
                        spectrum2[j] /= 240;
                    }
                    var spectrum = FTT.GetJoinedSpectrum(spectrum1, spectrum2, 15, 8000);
                    max_spect.Add(spectrum.Values.Max());

                    //Создание плоскости буллевых значений высотой 240 пикселей. Каждый пиксель численно равен 6 попугаям.
                    Dictionary<double, bool[]> map = new Dictionary<double, bool[]>();
                    for (var j = 0; j < 240; j++)
                    {
                        int amp = (int)Math.Floor(spectrum.ElementAt(j).Value / 6);
                        if (amp > 240) amp = 240;
                        bool[] col = Enumerable.Repeat<bool>(false, 240).ToArray();
                        for (int n = 0; n < amp; n++)
                        {
                            col[n] = true;
                        }
                        map.Add(spectrum.ElementAt(j).Key, col);
                    }

                    timestamp += 150;

                    //сохранение спектра + заброс в сеть
                    List<bool> inputs = new List<bool>();
                    var image = new Image<Rgba32>(240, 240);
                    image.Mutate(ctx => ctx.Fill(Rgba32.White));
                    int c = 0;
                    bool hasSignal = false;
                    foreach (var col in map)
                    {
                        for (int n = 0; n < 240; n++)
                        {
                            if (col.Value[n])
                            {
                                image.Mutate(ctx => ctx.Draw(Rgba32.DarkMagenta, 1, new EllipsePolygon(c, 240 - n, 1)));
                                hasSignal = true;
                            }
                            inputs.Add(col.Value[n]);
                        }
                        c++;
                    }

                    bool[] outputs = new bool[100];
                    if (hasSignal)
                    {
                        net.Compute(inputs.ToArray(), timestamp);
                        outputs = net.GetOutput();
                    }
                                  

                    for (int n = 0; n < outputs.Length; n++)
                    {
                        if (outputs[n] == true) netPattern.Mutate(ctx => ctx.Draw(Rgba32.OrangeRed, 2, new RectangularPolygon(10 * patternCol, 10 * n, 10, 10)));
                        if(inputs[n] == true) inputPattern.Mutate(ctx => ctx.Draw(Rgba32.OrangeRed, 10, new RectangularPolygon(10 * patternCol, 10 * n, 10, 10)));
                    }
                    patternCol++;
                    framenumb++;
                    if (hasSignal) image.Save(path+"/file"+k+"/"+ framenumb + ".png");
                }
                netPattern.Save(path + "/patterns/" + patternCount + ".png");
                inputPattern.Save(path + "/patterns/input" + patternCount + ".png");
                patternCol = 0;
                patternCount++;
                Console.WriteLine("Получен новый паттерн. Всего паттернов " + (patternCount));
                inputPattern.Mutate(ctx => ctx.Fill(Rgba32.WhiteSmoke));
                netPattern.Mutate(ctx => ctx.Fill(Rgba32.WhiteSmoke));
                Console.WriteLine("Обработан " + (k + 1) + " файл");
            }

            Console.WriteLine("Всего паттернов " + (patternCount));
        }
    }
}
