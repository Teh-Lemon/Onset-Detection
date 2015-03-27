// Anthony Lee 11010841
// Adds additional functionality to base LomontFFT
// Stores the complex numbers rather than just mutating them
// Adds features to find the power spectrum of the complex numbers
// Applies a hamming window to the data before doing the FFT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalYearProject_11010841
{
    class FFT : LomontFFT
    {
        float[] real;
        float[] imag;
        float[] spectrum;

        // Constructor
        public FFT() : base() { }

        /// <summary>
        /// Finds the absolute values of the complex numbers
        /// </summary>
        public float[] GetPowerSpectrum()
        {
            if (real != null)
            {
                FillSpectrum(ref spectrum);
                return spectrum;
            }
            else
            {
                return null;
            }
        }

        /// <summary>                                                                                            
        /// Compute the forward or inverse Fourier Transform of data, with                                       
        /// data containing real valued data only. The length must be a power                                       
        /// of 2.                                                                                                
        /// </summary>                                                                                           
        /// <param name="data">The real parts of the complex data</param>                                                                          
        /// <param name="forward">true for a forward transform, false for                                        
        /// inverse transform</param>    
        /// <param name="hamming">Whether to apply a hamming window before FFT</param>
        /// <returns>The output is complex                                         
        /// valued after the first two entries, stored in alternating real                                       
        /// and imaginary parts. The first two returned entries are the real                                     
        /// parts of the first and last value from the conjugate symmetric                                       
        /// output, which are necessarily real.</returns>
        public void RealFFT(float[] data, bool hamming = true)
        {
            // Copy data to a local array
            // Local array is stored so it can be used by other functions of the class
            double[] complexNumbers = new double[data.Length];
            data.CopyTo(complexNumbers, 0);

            if (hamming)
            {
                ApplyHammingWindow(complexNumbers);
            }

            // Perform FFT on local data
            base.RealFFT(complexNumbers, true);

            SeparateComplexNumbers(complexNumbers);
        }       

        #region Internals

        // Applies a hamming window to the data provided before FFT
        void ApplyHammingWindow(double[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= (0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (data.Length - 1)));
            }
        }

        // Sorts out the symmetry of the FFT results into separate real and imaginary numbers
        void SeparateComplexNumbers(double[] complexNumbers)
        {
            real = new float[complexNumbers.Length / 2 + 1];
            imag = new float[complexNumbers.Length / 2 + 1];
            // Location of the last purely real number
            int midPoint = complexNumbers.Length / 2;

            // First bin is purely real
            real[0] = (float)complexNumbers[0];
            imag[0] = 0.0f;

            // Fill in ascending complex numbers
            for (int i = 2; i < complexNumbers.Length - 1; i += 2)
            {
                real[i / 2] = (float)complexNumbers[i];
                imag[i / 2] = (float)complexNumbers[i + 1];
            }

            // Last of the purely real bins
            real[midPoint] = (float)complexNumbers[1];
            imag[midPoint] = 0.0f;
        }

        // Populates the spectrum array with the amplitudes of the data in real and imaginary
        void FillSpectrum(ref float[] data)
        {
            data = new float[real.Length];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (float)Math.Sqrt((real[i] * real[i]) + (imag[i] * imag[i]));
            }
        }
        #endregion
    }
}
