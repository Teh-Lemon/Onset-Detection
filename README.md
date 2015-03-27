Onset Detection
==============
Created in C#, this can be used to detect onsets (the notes in a song) by using the Spectral Flux algorithm. The Hamming Window Function is also used to reduce false positives.  
Created for an undergraduate final year independent project.

Video: https://www.youtube.com/watch?v=vMfhnrMsfa4

Screenshot of the sample project. 
![alt text](https://dl.dropboxusercontent.com/u/15765996/images/Final%20Year%20Project/2014-05-13_04-13-58.png "")

The blue line on the graph marks the current position in the song.  
The green line shows the spectral flux.  
The red line shows the running average + an offset.  
An onset is detected whenever the green line peaks above the red line.

Using this project
--------------
You'll need AudioAnalysis.cs, LomontFFT.cs, FFT.cs and OnsetDetection.cs. You'll also need NAudio 1.7 included in your project.  
Create an instance of AudioAnalysis.cs, this acts as an interface to all the functions available.  
"ProjectSampleGame\ProjectConceptGame\ProjectConceptGame\Game1.cs" shows an example of using the project. The main functions to focus on are AnalyseSong and PlaceBeats at the bottom of the file.  
Actual documentation to come.

Building the project
--------------
The project is developed within the sample environment.   
You will need Visual Studio 2010 and XNA 4.0 installed to run and build the sample project.

Third party libraries include:
- NAudio to read and play audio files. Note this is licensed under the Microsoft Public License (Ms-PL). Found here: https://github.com/naudio/NAudio/blob/master/license.txt
- Lomont's FFT to perform the Fast Fourier Transform.



