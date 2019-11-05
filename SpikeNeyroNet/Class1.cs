using System;
using System.Collections.Generic;
using System.Linq;

namespace SpikeNeyroNetGen1
{
    public class Neyron
    {
        /// <summary>
        /// Класс реализует алгоритм отдельно взятого нейрона.
        /// </summary>
        /// <remarks name="weights">Представляет собой список пар значений double, int. 
        /// Первое значениe является весом синапса, второе значение - метка времени (мкс), когда прошёл последний сигнал
        /// </remarks>
        /// <value name="tickalive">Параметр, хранящий значение времени (мкс), в течение которого нейрон генерирует сигнал</value>
        /// <value name="killall">Параметр, принимающий значение true в момент активации нейрона. Отключает работу других нейронов в классе NeyroLayer </value>
        /// <value name="signals">Представляет собой список пар значений double, int. 
        /// Первое значениe является амплитудой сигнала, второе значение - метка времени (мкс), когда прошёл последний сигнал</value>
        /// <value name="inputsCount">Параметр, обозначающий количество входов нейрона. Равен длине списка весов и сигналов</value>

        private List<Tuple<double, long>> weights = new List<Tuple<double, long>>();
        private int tickalive = 0;
        private int inputsCount = 0;
        private int timeLeak = 0;
        private double potencial = 0;
        private double iTreshold = 0;
        private long tLTP = 0;
        private long lastTime = 0;
        private long tRef = 0;
        private bool wasActivated = false;
        private double aInc = 0;
        private double aDec = 0;
        private double bInc = 0;
        private double bDec = 0;
        private double wMin = 0;
        private double wMax = 0;

        public bool output { get; private set; } = false;

        //Функция инициализации нейрона
        public Neyron(string initMethod, int inputs, int tleak, double tresh, long tltp, double ainc, double adec, double binc, double bdec, double wmin, double wmax, long tref)
        {
            int[] wts = InitWeights(initMethod, inputs);
            int[] sigs = Enumerable.Range(1, inputs).Select(x => 0).ToArray();
            foreach (double weight in wts)
            {
                weights.Add(new Tuple<double, long>((double)weight / 1000, 0));
            }
            timeLeak = tleak;
            iTreshold = tresh;
            tLTP = tltp;
            aInc = ainc;
            bInc = binc;
            aDec = adec;
            bDec = bdec;
            wMin = wmin;
            wMax = wmax;
            tRef = tref;

        }

        //Функция инициализации весов. 
        //Классическая инициализация происходит при любом методе, который не предусмотрен данной функцией.
        public int[] InitWeights(string method = "classic", int inputs = 1)
        {
            var rnd = new Random(1234567);
            switch (method)
            {
                default:
                    return Enumerable.Range(1, inputs).Select(x => rnd.Next(1, 1000)).ToArray();
                case "LETISpikes":
                    return Enumerable.Range(1, inputs).Select(x => rnd.Next(160000, 960000)).ToArray();
            }
        }

        /// <summary>
        /// Функция обновляет информацию об уровне потениала, накопленного нейроном. Реализуется утечка потенциала <code>potencial*Math.Exp(-(time - lastTime)/timeLeak)</code>
        /// После подсчёта производится проверка на превышение сигналом порогового уровня. Если результат положительный, выставляются флаги активации выхода и деактивации слоя. 
        /// Значение потенциала обнуляется.
        /// </summary>
        /// <param name="inputs">Массив входов, в версии 1.0 элементы принимают значение 0 или 1</param>
        private void SignalIntegrity(bool[] inputs, long timestamp)
        {
            long time = timestamp;
            List<Tuple<double, long>> temp = new List<Tuple<double, long>>();
            temp.AddRange(weights);
            bool hasSignal = false;
            double nextPotencial = potencial * Math.Exp(-(time - lastTime) / timeLeak);
            foreach (var weight in temp.Select((value, index) => new { value, index }))
            {
                if (inputs[weight.index] == true)
                {
                    nextPotencial += weight.value.Item1;
                    weights[weight.index] = new Tuple<double, long>(weight.value.Item1, time);
                    hasSignal = true;
                }
            }

            if (hasSignal) potencial = nextPotencial;

            lastTime = time;
            if (potencial > iTreshold)
            {
                output = true;
                potencial = 0;
                wasActivated = true;
            }
        }

        /// <summary>
        /// Функция обновления весов.
        /// </summary>
        public void WeightsUpdate(bool inhibit)
        {
            List<Tuple<double, long>> temp = new List<Tuple<double, long>>();
            temp.AddRange(weights);

            foreach (var weight in temp.Select((value, index) => new { value, index }))
            {
                double newWeight = 0;
                if (!inhibit)
                {
                    if (weight.value.Item2 <= lastTime && weight.value.Item2 > (lastTime - tLTP))
                    {
                        newWeight = weight.value.Item1 + aInc * Math.Exp(-bInc * (weight.value.Item1 - wMin) / (wMax - wMin));
                    }
                    else
                    {
                        newWeight = weight.value.Item1 - aDec * Math.Exp(-bDec * (wMax - weight.value.Item1) / (wMax - wMin));
                    }
                    if (newWeight < wMin) newWeight = wMin;
                    if (newWeight > wMax) newWeight = wMax;
                    weights[weight.index] = new Tuple<double, long>(newWeight, weight.value.Item2);
                }
            }
        }

        /// <summary>
        /// Обработчик нейрона, определяющий железо для расчёта.
        /// </summary>
        /// <param name="method"> Выбор железа "cpu" или "gpu"</param>
        /// <param name="inputs"> Массив входов </param>
        public void Compute(string method, bool[] inputs, long timestamp)
        {
            switch (method)
            {
                case "cpu":
                    CpuCompute(inputs, timestamp);
                    break;
                default:
                    CpuCompute(inputs, timestamp);
                    break;
            }
        }

        /// <summary>
        /// Вычисление на cpu
        /// </summary>
        /// <param name="inputs"></param>
        private void CpuCompute(bool[] inputs, long timestamp)
        {
            long time = timestamp;
            if (wasActivated == true)
            {
                output = false;
                if (time > (lastTime + tRef))
                {
                    wasActivated = false;
                    SignalIntegrity(inputs, timestamp);
                }
            }
            else
            {
                SignalIntegrity(inputs, timestamp);
            }

        }

    }


    /// <summary>
    /// Класс реализует слой нейронов. 
    /// </summary>
    public class NeyroLayer
    {
        private Tuple<bool, long> killall = new Tuple<bool, long>(false, 0);
        private int countNeyrons = 0;
        private int inputs = 0;
        private List<bool> outputs = new List<bool>();
        private List<Neyron> neyrons = new List<Neyron>();
        private long lasttime = 0;

        public long tInhibit { get; set; } = 200;
        public long tRef { get; set; } = 10000;
        public int timeLeak {get; set;}= 500;
        public double iTreshold {get; set;}= 1000000;
        public long tLTP {get; set;}= 300;
        public double aInc {get; set;}= 100;
        public double aDec {get; set;}= 55;
        public double bInc {get; set;}= 0;
        public double bDec {get; set;}= 0;
        public double wMin {get; set;}= 0.8;
        public double wMax {get; set;}= 1200;


        public NeyroLayer(int number, int inputs)
        {
         for(int i = 0; i<number; i++)
            {
                outputs.Add(false);
                Neyron neyron = new Neyron("LETISpikes", inputs, timeLeak, iTreshold, tLTP, aInc, aDec, bInc, bDec, wMin, wMax, tRef);
                neyrons.Add(neyron);
            }
        }

        public void Compute(bool[] inputs, long timestamp = 0, string method = "cpu")
        {
            long time = 0;
            if (timestamp == 0)
            {
                time = (long)(DateTime.Now.Ticks / 10);
            }
            else
            {
                time = timestamp;
            }
            switch (method)
            {
                case "cpu":
                    CpuCompute(inputs, time);
                    break;
                default:
                    CpuCompute(inputs, time);
                    break;
            }
        }


        public void CpuCompute(bool[] inputs, long timestamp)
        {
            outputs.Clear();

            if (killall.Item1 == true && killall.Item2 < timestamp - tInhibit)
            {
                long time = killall.Item2;
                killall = new Tuple<bool, long>(false, time);
            }

            if (killall.Item1 == false)
            {
                foreach (Neyron neyron in neyrons)
                {
                    if (!killall.Item1) {
                        neyron.Compute("cpu", inputs, timestamp);
                        if (neyron.output == true && killall.Item1 == false)
                        {
                            killall = new Tuple<bool, long>(true, timestamp);
                        }
                        outputs.Add(neyron.output);
                    }
                    else
                    {
                        outputs.Add(false);
                    }
                }
                
            }
            for (int i = 0; i < outputs.Count; i++)
            {
                if (outputs[i] == true)
                {
                    neyrons[i].WeightsUpdate(false);
                }
                else
                {
                    neyrons[i].WeightsUpdate(true);
                }
            }
        }

        public bool[] GetOutput()
        {
            return outputs.ToArray();
        }

    }

    public class NeyroNet
    {
        private List<NeyroLayer> layers = new List<NeyroLayer>();
        private List<bool> outputs = new List<bool>();


        public void AddLayer(NeyroLayer layer)
        {
            layers.Add(layer);
        }

        public void Compute(bool[] inputs, long timestamp = 0, string method = "cpu")
        {
            long time = 0;
            if (timestamp == 0)
            {
                time = (long)(DateTime.Now.Ticks / 10);
            }
            else
            {
                time = timestamp;
            }
            switch (method)
            {
                default:
                    CpuCompute(inputs, time);
                    break;
            }
        }

        private void CpuCompute(bool[] inputs, long timestamp)
        {
            foreach (var layer in layers.Select((value, index) => new { value, index}))
            {
                if (layer.index == 0)
                {
                    layer.value.Compute(inputs, timestamp);
                }
                else
                {
                    layer.value.Compute(layers[layer.index - 1].GetOutput());
                }
            }
            outputs = layers[layers.Count - 1].GetOutput().Cast<bool>().ToList();
        } 

        public bool[] GetOutput()
        {
            return outputs.ToArray();
        }
    }
}
