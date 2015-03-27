// Anthony Lee 11010841
// Main meat of the project
// Load's the given audio file into memory
// Plays/Pauses the audio file
// Performs analysis on the audio file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace FinalYearProject_11010841
{
    class AudioAnalysis
    {
        const int SAMPLE_SIZE = 1024;
        
        // Audio stream fed into the sound playback device
        //BlockAlignReductionStream stream;
        // Instance of sound playback device
        public WaveOutEvent outputDevice;

        // Fast Fourier Transform library
        FFT fft;

        /// <summary>
        /// Raw audio data
        /// </summary>
        public AudioFileReader PCMStream { get; set; }

        // Onset Detection
        OnsetDetection onsetDetection;
        public float[] OnsetsFound { get; set; }
        public float TimePerSample { get; set; }

        // Constructor
        public AudioAnalysis()
        {
            SetUpFFT();
        }

        ~AudioAnalysis()
        {
            DisposeOutputDevice();
        }

        // Used to set up the sound device
        private void InitialiseOutputDevice()
        {
            DisposeOutputDevice();

            outputDevice = new WaveOutEvent();
            //outputDevice.Init(new WaveChannel32(stream));
            outputDevice.Init(PCMStream);
        }

        public void LoadAudioFromFile(string filePath)
        {
            // MP3
            if (filePath.EndsWith(".mp3"))
            {
                PCMStream = new AudioFileReader(filePath);
            }
            // WAV
            else if (filePath.EndsWith(".wav"))
            {
                PCMStream = new AudioFileReader(filePath);
            }

            if (PCMStream != null)
            {
                // Throw an error is the audio has more channels than stereo
                if (PCMStream.WaveFormat.Channels > 2)
                {
                    throw new FormatException("Only Mono and Stereo are supported");
                }

                InitialiseOutputDevice();
                OnsetsFound = null;
            }
            else
            {
                throw new FormatException("Invalid audio file");
            }
        }

        // Play out the loaded audio file
        // Returns whether function was successful
        public bool PlayAudio()
        {
            if (PCMStream != null)
            {
                // If audio was previously stopped
                // Or audio has reached the end of the track
                // Reset the playback position to the beginning
                if (outputDevice.PlaybackState == PlaybackState.Stopped
                    || PCMStream.Position == PCMStream.Length)
                {
                    PCMStream.Position = 0;                    
                }

                outputDevice.Play();

                return true;
            }

            return false;
        }

        // Pause the audio file
        public bool PauseAudio()
        {
            if (PCMStream != null)
            {
                outputDevice.Pause();

                return true;
            }

            return false;
        }

        // Stop the audio file
        public bool StopAudio()
        {
            if (PCMStream != null)
            {
                outputDevice.Stop();
                PCMStream.Position = 0;

                return true;
            }

            return false;
        }

        // Track Position getter/setter
        public long GetTrackPosition()
        {
            return PCMStream.Position;
        }
        public void SetTrackPosition(long position)
        {
            PCMStream.Position = position;
        }

        public void DetectOnsets(float sensitivity = 1.5f)
        {
            onsetDetection = new OnsetDetection(PCMStream, 1024);
            // Has finished reading in the audio file
            bool finished = false;
            // Set the pcm data back to the beginning
            SetTrackPosition(0);

            do
            {
                // Read in audio data and find the flux values until end of audio file
                finished = onsetDetection.AddFlux(ReadMonoPCM());
            }
            while (!finished);

            // Find peaks
            onsetDetection.FindOnsets(sensitivity);
        }

        public void NormalizeOnsets(int type)
        {
            onsetDetection.NormalizeOnsets(type);
        }

        public float[] GetOnsets()
        {
            return onsetDetection.Onsets;
        }

        public float GetTimePerSample()
        {
            return onsetDetection.TimePerSample();
        }

        #region Internals

        // Read in a sample and convert it to mono
        float[] ReadMonoPCM()
        {
            int size = SAMPLE_SIZE;

            // If stereo
            if (PCMStream.WaveFormat.Channels == 2)
            {
                size *= 2;
            }

            float[] output = new float[size];

            // Read in a sample
            if (PCMStream.Read(output, 0, size) == 0)
            {
                // If end of audio file
                return null;
            }

            // If stereo, convert to mono
            if (PCMStream.WaveFormat.Channels == 2)
            {
                return ConvertStereoToMono(output);
            }
            else
            {
                return output;   
            }            
        }

        // Averages the 2 channels into 1
        float[] ConvertStereoToMono(float[] input)
        {
            float[] output = new float[input.Length / 2];
            int outputIndex = 0;
            
            float leftChannel = 0.0f;
            float rightChannel = 0.0f;

            // Go through each pair of samples
            // Average out the pair
            // Save to output
            for (int i = 0; i < input.Length; i += 2)
            {
                leftChannel = input[i];
                rightChannel = input[i + 1];

                // Average the two channels
                output[outputIndex] = (leftChannel + rightChannel) / 2;
                outputIndex++;
            }

            return output;
        }

        // Starts up the Fast Fourier Transform class
        void SetUpFFT()
        {
            fft = new FFT();
                
            //Determine how phase works on the forward and inverse transforms. 
            // (0, 1) default
            // (1, -1) for signal processing
            fft.A = 0;
            fft.B = 1;                
        }

        // Properly clean up sound output device
        public void DisposeOutputDevice()
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }
        }

        public void DisposeAudioAnalysis()
        {
            DisposeOutputDevice();

            if (PCMStream != null)
            {
                PCMStream.Dispose();
                PCMStream = null;

                OnsetsFound = null;
                TimePerSample = 0.0f;
            }
        }

        #endregion
    }    
}
