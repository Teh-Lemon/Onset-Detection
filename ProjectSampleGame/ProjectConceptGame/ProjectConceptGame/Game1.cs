using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NAudio.Wave;
using FinalYearProject_11010841;

namespace ProjectConceptGame
{
    enum State            
    {
        MENU, START_GAME, PLAYING, END
    };

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // Game variables
        float dt = 0.0f;
        State state = State.MENU;
        int score = 0;
        string topText = "";
        string scoreText = "Score: ";
        bool previousDrum = false;
        bool drumDown = false;
        string[] songFile = new string[1] {""};
        bool autoplay = false;

        const float THRESHOLD_WINDOW = 0.5f;
        const float ONSET_SENSITIVITY = 1.5f;



        //  Used to get keyboard input
        KeyboardState previousKbState;
        KeyboardState kbState = Keyboard.GetState();

        // Pixels per second
        const int PIXELS_SECOND = 1500;
        const float MARKER_POS = 0.15f;
        int markerPosX;
        float noteSize;

        // Audio analysis engine
        AudioAnalysis audioAnalysis;

        // Textures
        Texture2D beatTexture;
        Texture2D bgTexture;
        Texture2D markerTexture;

        // Fonts
        SpriteFont MenuFont;

        // Game Objects
        List<GameObject> gameObjects = new List<GameObject>();
        List<GameObject> notes = new List<GameObject>();
        GameObject marker;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Resolution 720p
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false;
            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            // Set up the audio analysis tool
            audioAnalysis = new AudioAnalysis();

            // Fonts
            MenuFont = this.Content.Load<SpriteFont>("Fonts/Segoe20");

            // Textures
            beatTexture = this.Content.Load<Texture2D>("Sprites/circle");
            bgTexture = this.Content.Load<Texture2D>("Sprites/background");
            markerTexture = this.Content.Load <Texture2D>("Sprites/marker");

            // Screen percentages
            int markerPosX = (int)(graphics.PreferredBackBufferWidth * MARKER_POS);
            int screenHeight50 = graphics.PreferredBackBufferHeight / 2;
            noteSize = (float)(beatTexture.Width / 1.5);

            // Game Obects
            GameObject background = new GameObject(bgTexture);
            background.Origin = new Vector2(0, bgTexture.Height / 2);
            background.Position = new Vector2(0, screenHeight50);

            // Player marker
            marker = new GameObject(markerTexture);
            marker.Origin = Vector2.One * markerTexture.Width / 2;
            marker.Position = new Vector2(markerPosX, screenHeight50);

            gameObjects.Add(background);
            gameObjects.Add(marker);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here            
            previousKbState = kbState;
            kbState = Keyboard.GetState();

            // Esc to close game
            if (kbState.IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }

            dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (state)
            {
                case State.MENU:
                    score = 0;
                    topText = "Enter to begin. A to autoplay";
                    autoplay = false;

                    if (GetSongFilePath("song.txt")[0] != songFile[0])
                    {
                        audioAnalysis.DisposeAudioAnalysis();
                        songFile = GetSongFilePath("song.txt");
                        AnalyseSong(songFile[0]);
                    }

                    PlaceBeats();

                    if (kbState.IsKeyDown(Keys.Enter))
                    {
                        state = State.START_GAME;
                    }
                    else if (kbState.IsKeyDown(Keys.A))
                    {
                        autoplay = true;
                        state = State.START_GAME;
                    }

                    break;

                case State.START_GAME:
                    audioAnalysis.PlayAudio();

                    topText = "";
                    state = State.PLAYING;
                    break;

                case State.PLAYING:

                    // If the Quit button is pressed, end game
                    if (kbState.IsKeyDown(Keys.R))
                    {
                        state = State.END;
                    }
                    // If the playback has reached the end of the audio track, end game
                    if (audioAnalysis.PCMStream.Position >= audioAnalysis.PCMStream.Length)
                    {
                        state = State.END;
                    }

                    // Check if the Hit button is pressed
                    drumDown = false;
                    if (kbState.IsKeyDown(Keys.Left)
                        || kbState.IsKeyDown(Keys.Right)
                        || autoplay)
                    {
                        drumDown = true;
                    }

                    for (int i = 0; i < gameObjects.Count; i++)
                    {
                        gameObjects[i].update(dt);
                    }

                    for (int i = 0; i < notes.Count; i++)
                    {                        
                        notes[i].update(dt);

                        /*
                        // Remove notes that go off screen
                        if (notes[i].Position.X + notes[i].Origin.X < 0)
                        {
                            notes[i].Active = false;
                        }
                        else if (notes[i].Position.Y + notes[i].Origin.Y < 0)
                        {
                            notes[i].Active = false;
                        }
                        else if (notes[i].Alpha <= 0)
                        {
                            notes[i].Active = false;
                        }*/

                        // Check for hit collision
                        if (drumDown)
                        {
                            float notePos = notes[i].Position.X;

                            // If note is on top of the marker
                            if (Math.Abs(markerPosX - notePos) <= noteSize
                                && marker.Position.Y == notes[i].Position.Y)
                            {
                                // Increase the score
                                score += 100;
                                // Play a hit and fade away animation
                                notes[i].AlphaVelocity = -5.0f;
                                notes[i].Velocity = new Vector2(0, -1000);
                                notes[i].TintColor = Color.GhostWhite;
                            }
                        }  
                    }
                    break;

                case State.END:
                    audioAnalysis.StopAudio();
                    
                    topText = "Game Over. R to restart";
                    if (previousKbState.IsKeyUp(Keys.R) && kbState.IsKeyDown(Keys.R))
                    {
                        state = State.MENU;
                    }
                    break;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            for (int i = 0; i < gameObjects.Count; i++)
            {
                gameObjects[i].draw(spriteBatch);
            }

            for (int i = 0; i < notes.Count; i++)
            {
                notes[i].draw(spriteBatch);
            }

            // Draw text
            int screenWidth5 = graphics.PreferredBackBufferWidth / 20;
            int screenWidth25 = graphics.PreferredBackBufferWidth / 3;
            int screenHeight20 = graphics.PreferredBackBufferHeight / 5;
            int screenHeight5 = graphics.PreferredBackBufferHeight / 20;

            spriteBatch.DrawString(MenuFont, scoreText + score.ToString(),
                new Vector2(screenWidth5, screenHeight5), Color.White);

            spriteBatch.DrawString(MenuFont, topText,
                new Vector2(screenWidth25, screenHeight20), Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }
        
        // Audio Analysis engine needs to be disposed off before closing game
        protected override void OnExiting(object sender, EventArgs args)
        {
            audioAnalysis.DisposeAudioAnalysis();

            base.OnExiting(sender, args);
        }

        // Get song file path
        string[] GetSongFilePath(string filename)
        {
            if (filename != "")
            {
                //  File to be read in
                string[] settingsFile = System.IO.File.ReadAllLines(filename);

                return settingsFile;
            }

            return null;
        }

        void AnalyseSong(string filePath)
        {
            // Add the audio file name to the window title
            this.Window.Title = "11010841 Proof of Concept - " + filePath;
            // Load the audio file from the given file path
            audioAnalysis.LoadAudioFromFile(filePath);
            // Find the onsets
            //audioAnalysis.PerformSpectralFlux(ONSET_SENSITIVITY);
            audioAnalysis.DetectOnsets(ONSET_SENSITIVITY);
            // Normalize the intensity of the onsets
            audioAnalysis.NormalizeOnsets(0);
        }

        // Generate beats
        void PlaceBeats()
        {
            notes.Clear();

            // Screen percentages
            markerPosX = (int)(graphics.PreferredBackBufferWidth * MARKER_POS);
            int halfScreenHeight = graphics.PreferredBackBufferHeight / 2;

            // Go through the list of onsets detected
            for (int itemNo = 0; itemNo < audioAnalysis.GetOnsets().Length; itemNo++)
            {
                // Retrieve the item from the audio analysis onset detected list
                float onset = audioAnalysis.GetOnsets()[itemNo];

                // If the item is an onset
                if (onset > 0.0)
                {
                    GameObject newNote = new GameObject(beatTexture);

                    // Set the origin of the note in the centre
                    newNote.Origin = Vector2.One * beatTexture.Height / 2;

                    // Length of time each item in the onset detected list represents
                    float timePos = audioAnalysis.GetTimePerSample();

                    // Place the note in accordance to its position in the song
                    float xPosition = (itemNo * timePos);
                    // Stretch the timeline so the notes aren't bunch up
                    xPosition *= PIXELS_SECOND;
                    // Add an offset, the marker represents the current position in the song
                    xPosition += markerPosX;

                    // Set the note in the correct position
                    newNote.Position = new Vector2(xPosition, halfScreenHeight);

                    // Set the note moving towards the left
                    newNote.Velocity = new Vector2(-PIXELS_SECOND, 0);

                    if (onset > 0.5f)
                    {
                        newNote.TintColor = Color.Orange;
                    }
                    else
                    {
                        newNote.TintColor = Color.MediumPurple;
                    }

                    
                    notes.Add(newNote);
                }
            }
        }
    }
}
