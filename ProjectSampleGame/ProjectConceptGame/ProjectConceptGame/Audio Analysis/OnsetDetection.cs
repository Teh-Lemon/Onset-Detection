using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace FinalYearProject_11010841
{
    class OnsetDetection
    {
        FFT fft = new FFT();
        AudioFileReader PCM;
        int SampleSize;

        public float[] Onsets { get; set; }
        float[] LowOnsets { get; set; }
        float[] MidOnsets { get; set; }
        float[] HighOnsets { get; set; }

        float[] previousSpectrum;
        float[] spectrum;

        bool rectify;

        List<float> fluxes;


        // Constructor
        public OnsetDetection(AudioFileReader pcm, int sampleWindow)
        {
            PCM = pcm;
            SampleSize = sampleWindow;

            spectrum = new float[sampleWindow / 2 + 1];
            previousSpectrum = new float[spectrum.Length];
            rectify = true;
            fluxes = new List<float>();
        }

        /// <summary>
        ///  Perform Spectral Flux onset detection on loaded audio file
        ///  <para>Recommended onset detection algorithm for most needs</para>  
        /// </summary>
        ///  <param name="hamming">Apply hamming window before FFT function. 
        ///  <para>Smooths out the noise in between peaks.</para> 
        ///  <para>Small improvement but isn't too costly.</para> 
        ///  <para>Default: true</para></param>
        public bool AddFlux(float[] samples, bool hamming = true)
        {
            // Find the spectral flux of the audio
            if (samples != null)
            {
                // Perform Fast Fourier Transform on the audio samples
                fft.RealFFT(samples, hamming);

                // Update spectrums
                Array.Copy(spectrum, previousSpectrum, spectrum.Length);
                Array.Copy(fft.GetPowerSpectrum(), spectrum, spectrum.Length);

                fluxes.Add( CompareSpectrums(spectrum, previousSpectrum, rectify) );
                return false;
            }
            // End of audio file
            else
            {
                return true;
            }
        }

        /// <param name="thresholdTimeSpan">Amount of data used during threshold averaging, in seconds.
        /// <para>Default: 1</para></param>
        /// <param name="sensitivity">Sensitivivity of onset detection.
        /// <para>Lower increases the sensitivity</para>
        /// <para>Recommended: 1.3 - 1.6</para>
        /// <para>Default: 1.5</para></param>
        // Use threshold average to find the onsets from the spectral flux
        public void FindOnsets(float sensitivity = 1.5f, float thresholdTimeSpan = 0.5f)
        {
            float[] thresholdAverage = GetThresholdAverage(fluxes, SampleSize,
            thresholdTimeSpan, sensitivity);

            Onsets = GetPeaks(fluxes, thresholdAverage, SampleSize);
        }

        /// <summary>
        ///  Normalize the beats found.
        /// </summary>
        /// <param name="type">Type of normaliztion.
        /// <para>0 = Normalize onsets between 0 and max onset</para>
        /// <para>1 = Normalize onsets between min onset and max onset.</para></param>
        public void NormalizeOnsets(int type)
        {
            if (Onsets != null)
            {
                float max = 0;
                float min = 0;
                float difference = 0;

                // Find strongest/weakest onset
                for (int i = 0; i < Onsets.Length; i++)
                {
                    max = Math.Max(max, Onsets[i]);
                    min = Math.Min(min, Onsets[i]);
                }
                difference = max - min;

                // Normalize the onsets
                switch (type)
                {
                    case 0:                        
                        for (int i = 0; i < Onsets.Length; i++)
                        {
                            Onsets[i] /= max;
                        }
                        break;
                    case 1:
                        for (int i = 0; i < Onsets.Length; i++)
                        {
                            if (Onsets[i] == min)
                            {
                                Onsets[i] = 0.01f;
                            }
                            else
                            {
                                Onsets[i] -= min;
                                Onsets[i] /= difference;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            return;
        }



        #region Internals
        float CompareSpectrums(float[] spectrum, float[] previousSpectrum, bool rectify)
        {
            // Find difference between each respective bins of each spectrum
            // Sum up these differences
            float flux = 0;
            for (int i = 0; i < spectrum.Length; i++)
            {
                float value = (spectrum[i] - previousSpectrum[i]);
                // If ignoreNegativeEnergy is true
                // Only interested in rise in energy, ignore negative values
                if (!rectify || value > 0)
                {
                    flux += value;
                }
            }

            return flux;
        }

        // Finds the peaks in the flux above the threshold average
        float[] GetPeaks(List<float> data, float[] dataAverage, int sampleCount)
        {
            // Time window in which humans can not distinguish beats in seconds
            const float indistinguishableRange = 0.01f; // 10ms
            // Number of set of samples to ignore after an onset
            int immunityPeriod = (int)((float)sampleCount
                / (float)PCM.WaveFormat.SampleRate
                / indistinguishableRange);

            // Results
            float[] peaks = new float[data.Count];

            // For each sample
            for (int i = 0; i < data.Count; i++)
            {
                // Add the peak if above the average, else 0
                if (data[i] >= dataAverage[i])
                {
                    peaks[i] = data[i] - dataAverage[i];
                }
                else
                {
                    peaks[i] = 0.0f;
                }
            }

            // Prune the peaks list
            peaks[0] = 0.0f;
            for (int i = 1; i < peaks.Length - 1; i++)
            {
                // If the next value is lower than the current value, that means it is end of the peak
                if (peaks[i] < peaks[i + 1])
                {
                    peaks[i] = 0.0f;
                    continue;
                }

                // Remove peaks too close to each other
                if (peaks[i] > 0.0f)
                {
                    for (int j = i + 1; j < i + immunityPeriod; j++)
                    {
                        if (peaks[j] > 0)
                        {
                            peaks[j] = 0.0f;
                        }
                    }
                }
            }

            return peaks;
        }

        // Find the running average of the given list
        float[] GetThresholdAverage(List<float> data, int sampleWindow,
            float thresholdTimeSpan, float thresholdMultiplier)
        {
            List<float> thresholdAverage = new List<float>();

            // How many spectral fluxes to look at, at a time (approximation is fine)
            float sourceTimeSpan = (float)(sampleWindow) / (float)(PCM.WaveFormat.SampleRate);
            int windowSize = (int)(thresholdTimeSpan / sourceTimeSpan / 2);

            for (int i = 0; i < data.Count; i++)
            {
                // Max/Min Prevent index out of bounds error
                // Look at values to the left and right of the current spectral flux
                int start = Math.Max(i - windowSize, 0);
                int end = Math.Min(data.Count, i + windowSize);
                // Current average
                float mean = 0;

                // Sum up the surrounding values
                for (int j = start; j < end; j++)
                {
                    mean += data[j];
                }

                // Find the average
                mean /= (end - start);

                // Multiply mean to increase the sensitivity
                thresholdAverage.Add(mean * thresholdMultiplier);
            }

            return thresholdAverage.ToArray();
        }

        public float TimePerSample()
        {
            // Length of time per sample
            return (float)SampleSize / (float)PCM.WaveFormat.SampleRate;
        }

        #endregion

    }
}
