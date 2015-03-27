using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectConceptGame
{
    public class GameObject
    {
        public Vector2 Origin { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Scale { get; set; }
        public Vector2 Velocity { get; set; }
        public Color TintColor { get; set; }
        public float Alpha { get; set; }
        public Texture2D Texture { get; set; }
        public float AlphaVelocity { get; set; }
        public bool Active { get; set; }

        public GameObject(Texture2D texture)
        {
            Origin = new Vector2(0, 0);
            Position = new Vector2(0, 0);
            Scale = Vector2.One;
            TintColor = Color.White;
            Alpha = 1.0f;
            Texture = texture;
            AlphaVelocity = 0.0f;
            Active = true;
        }

        public void update(float dt)
        {
            if (Active)
            {
                Position += (Velocity * dt);
                Alpha += (AlphaVelocity * dt);
            }
        }

        public void draw(SpriteBatch spriteBatch)
        {
            if (Active)
            {
                spriteBatch.Draw(Texture, Position - Origin, TintColor * Alpha);
            }
        }
    }
}
