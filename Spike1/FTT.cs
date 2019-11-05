using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace Spike1
{
    class FTT
    {
        public const double Pi = Math.PI;

        public static Complex[] DecimationInTime(Complex[] frame, bool direct)
        {
            if (frame.Length == 1)
            {
                return frame;
            }

            int frameHalfSize = frame.Length >> 1;
            int frameFullSize = frame.Length;

            Complex[] frameOdd = new Complex[frameHalfSize];
            Complex[] frameEven = new Complex[frameHalfSize];

            for (int i = 0; i < frameHalfSize; i++)
            {
                int j = i << 1;
                frameOdd[i] = frame[j + 1];
                frameEven[i] = frame[j];
            }

            Complex[] spectrumOdd = DecimationInTime(frameOdd, direct);
            Complex[] spectrumEven = DecimationInTime(frameEven, direct);

            var arg = direct ? -2 * Pi / frameFullSize : 2 * Pi / frameFullSize;
            Complex omegaPowBase = new Complex(Math.Cos(arg), Math.Sin(arg));
            Complex omega = Complex.One;
            Complex[] spectrum = new Complex[frameFullSize];

            for (int j = 0; j < frameHalfSize; j++)
            {
                spectrum[j] = spectrumEven[j] + omega * spectrumOdd[j];
                spectrum[j + frameHalfSize] = spectrumEven[j] - omega * spectrumOdd[j];
                omega *= omegaPowBase;
            }

            return spectrum;
        }

        public static Complex[] DecimationInFrequency(Complex[] frame, bool direct)
        {
            if (frame.Length == 1) return frame;
            int halfSampleSize = frame.Length >> 1;
            int fullSampleSize = frame.Length;

            var arg = direct ? -2 * Pi / fullSampleSize : 2 * Pi / fullSampleSize;
            Complex omegaPowBase = new Complex(Math.Cos(arg), Math.Sin(arg));
            Complex omega = Complex.One;
            Complex[] spectrum = new Complex[fullSampleSize];

            for (int j = 0; j < halfSampleSize; j++)
            {
                spectrum[j] = frame[j] + frame[j + halfSampleSize];
                spectrum[j + halfSampleSize] = omega * (frame[j] + frame[j + halfSampleSize]);
                omega *= omegaPowBase;
            }

            Complex[] yTop = new Complex[halfSampleSize];
            Complex[] yBottom = new Complex[halfSampleSize];

            for (int i = 0; i < halfSampleSize; i++)
            {
                yTop[i] = spectrum[i];
                yBottom[i] = spectrum[i + halfSampleSize];
            }
            yTop = DecimationInFrequency(yTop, direct);
            yBottom = DecimationInFrequency(yBottom, direct);

            for (int i = 0; i < halfSampleSize; i++)
            {
                int j = i << 1;
                spectrum[j] = yTop[i];
                spectrum[j + 1] = yBottom[i];
            }
            return spectrum;
        }

        public static Dictionary<double, double> GetJoinedSpectrum(IList<Complex> spectrum0, IList<Complex> spectrum1, double shiftPerFrame, double sampleRate)
        {
            int frameSize = spectrum0.Count;
            double frameTime = frameSize / sampleRate;
            double shiftTime = frameTime / shiftPerFrame;
            double binToFrequancy = sampleRate / frameSize;
            Dictionary<double, double> dictionary = new Dictionary<double, double>();

            for (int bin = 0; bin < frameSize; bin++)
            {
                double omegaExpected = 2 * Pi * (bin * binToFrequancy);
                double omegaActual = (spectrum1[bin].Phase - spectrum0[bin].Phase) / shiftTime;
                double omegaDelta = Align(omegaActual - omegaExpected, 2 * Pi);
                double binDelta = omegaDelta / (2 * Pi * binToFrequancy);
                double frequancyActual = (bin + binDelta) * binToFrequancy;
                double magnitude = spectrum1[bin].Magnitude + spectrum0[bin].Magnitude;
                dictionary.Add(frequancyActual, magnitude * (0.5 + Math.Abs(binDelta)));
            }
            return dictionary;
        }

        public static double Align(double angle, double period)
        {
            int qpd = (int)(angle / period);
            if (qpd >= 0) qpd += qpd & 1;
            else qpd -= qpd & 1;
            angle -= period * qpd;
            return angle;
        }
    }
}
