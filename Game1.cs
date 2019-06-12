using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Assessment
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        int displaywidth = 800;
        int displayheight = 600;
        float aspectratio;
        object3d player = new object3d();
        object3d rock = new object3d();
        object3d bullet = new object3d();
        camera3d gamecam = new camera3d();
        directionalLightSource sunlight;
        Random randomiser = new Random();
        BoundingBox TriggerBoxDoorOpen;
        BoundingBox TriggerBoxRockFall;
        bool rockFalling = false;
        bool doorOpening = false;
        Vector3 acceleration = new Vector3();
        basicCuboid door;
        basicCuboid[] walls = new basicCuboid[20];
        int doorSequenceTimer;
        int doorSequenceFinalTime = 2500;

        //transform data
        public Vector3 position = Vector3.Zero;
        public Vector3 rotation = Vector3.Zero;
        public Vector3 scale = Vector3.One;
        public Vector3 position_old = Vector3.Zero;
        public Vector3 position_older = Vector3.Zero;
        public Vector3 velocity_old = Vector3.Zero;
        public Vector3 acceleration_old = Vector3.Zero;

        //Physics
        //velocity
        public Vector3 velocity = Vector3.Zero;
        //add a variable to keep track of when Rock started to fall
        public float RockFallStart = 0f;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            graphics.PreferredBackBufferWidth = displaywidth;
            graphics.PreferredBackBufferHeight = displayheight;
            graphics.ApplyChanges();
            aspectratio = (float)displaywidth / (float)displayheight;
            gamecam.position = new Vector3(50, 50, 50);
            gamecam.target = new Vector3(0, 0, 0);
            gamecam.fieldOfView = MathHelper.ToRadians(90);
            gamecam.whichWayIsUp = Vector3.Up;
            gamecam.nearPlane = 1f;
            gamecam.farPlane = 50000f;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            BoundingRenderer.InitializeGraphics(graphics.GraphicsDevice);
            spriteBatch = new SpriteBatch(GraphicsDevice);
            player.LoadModel(Content, "Ship");
            player.rotation = new Vector3(0f, 0f, 0f);
            player.position.X = 0;
            player.position.Y = 0;
            player.position.Z = 0;
            player.scale = 0.1f;
            rock.LoadModel(Content, "Meteor");
            rock.scale = 0.1f;
            rock.position = new Vector3(25, 60, -50);
            for (int c = 0; c < walls.Length; c++)
            {
                walls[c] = new basicCuboid(GraphicsDevice);
                walls[c].LoadContent(Content, "WallTexture");
                walls[c].scale = new Vector3(5, 30, 60);
                if (c < 5)
                    walls[c].SetUpVertices(new Vector3(-70, 0, 60 * (c + 1)));
                else if (c < 10)
                    walls[c].SetUpVertices(new Vector3(-70, 0, -60 * (c - 4)));
                else
                {
                    walls[c].scale = new Vector3(60, 30, 5);
                    walls[c].SetUpVertices(new Vector3(-70 + (c - 10) * 60, 0, -300));
                }
            }

            door = new basicCuboid(GraphicsDevice);
            door.LoadContent(Content, "WallTexture");
            door.scale = new Vector3(5, 30, 60);
            door.SetUpVertices(new Vector3(-70, 0, 0));
            TriggerBoxDoorOpen = new BoundingBox(new Vector3(-95, 0, 0), new Vector3(-
            45, 10, 60));
            TriggerBoxRockFall = new BoundingBox(new Vector3(-5, -5, -55), new
            Vector3(55, 5, -45));
            sunlight.diffuseColor = new Vector3(10);
            sunlight.specularColor = new Vector3(1f, 1f, 1f);
            sunlight.direction = Vector3.Normalize(new Vector3(1.5f, -1.5f, -1.5f));
        }

        public enum IntegrationMethod { ForwardEuler, LeapFrog, Verlet };
        IntegrationMethod currentIntegrationMethod = IntegrationMethod.ForwardEuler;

        private void MovePlayer(int dt)
        {    
            switch (currentIntegrationMethod)
            {
                case IntegrationMethod.ForwardEuler:
                    //// This method is deprecated due to stability issues.
                    player.position += player.velocity * dt;
                    player.velocity += acceleration * dt;

                    break;

                ///////////////////////////////////////////////////////////////////
                //
                // CODE FOR TASK 2 HERE
                //
                ///////////////////////////////////////////////////////////////////
                case IntegrationMethod.LeapFrog:
                    //Verlocity verlet AKA leapfrog-------------------------------
                    
                    //calculate velocity at half way through the frame, using last frames acceleration
                    Vector3 velocity_half = velocity_old + acceleration_old * dt * 0.5f;

                    //calculate position using this halfway velocity
                    player.position = position_old + velocity_half * dt;

                    //calculate the new velocity for this frame, using this frames acceleration and time step
                    player.velocity = velocity_half + acceleration * dt * 0.5f;

                    // Apply an overall linear drag ("Friction")
                    player.velocity *= 0.9f;

                    //Move current values to previous frame (old) values
                    acceleration_old = acceleration;
                    velocity_old = player.velocity;
                    position_older = position_old;
                    position_old = player.position;

                    break;

                case IntegrationMethod.Verlet:

                    //Move current values to previous frame (old) values
                    acceleration_old = acceleration;
                    velocity_old = player.velocity;
                    position_older = position_old;
                    position_old = player.position;

                    //update acceleration
                    if (position_older != position_old)
                    {
                        Vector3 drag = position_older - position_old;
                        drag.Normalize();
                        drag *= 0.0005f;
                        acceleration += drag;
                    }

                    //No velocity equation required
                    //position = 2(position_old - position_older) + acceleration* time^2
                    player.position = 2 * position_old - position_older + acceleration * dt * dt;

                    break;
            }
        }


        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            int dt = gameTime.ElapsedGameTime.Milliseconds;
            base.Update(gameTime);
            player.storedPos = player.position;
            Vector3 storedAcc = acceleration;
            acceleration = new Vector3(0, 0, 0);
            if (Keyboard.GetState().IsKeyDown(Keys.Left)) player.rotation.Y += 0.1f;
            if (Keyboard.GetState().IsKeyDown(Keys.Right)) player.rotation.Y -= 0.1f;
            player.velocity *= 0.9f; // friction
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
            {
                acceleration.X = (float)Math.Sin(player.rotation.Y + Math.PI) * 0.001f;
                acceleration.Z = (float)Math.Cos(player.rotation.Y + Math.PI) * 0.001f;
            }
            // camera follow
            gamecam.position = new Vector3(50, 50, 50) + player.position;
            gamecam.target = player.position;

            MovePlayer(dt);
            foreach (basicCuboid WallSegment in walls)
            {
                if (player.hitBox.Intersects(WallSegment.collisionbox))
                {
                    ElasticCollision(WallSegment);
                }
            }
            if (player.hitBox.Intersects(door.collisionbox))
            {
                ElasticCollision(door);
            }
            if (player.hitBox.Intersects(TriggerBoxRockFall) && !rockFalling)
            {
                rockFalling = true;
                //assign rock falls start time
                RockFallStart = dt;

            }
            if (rockFalling && rock.position.Y >= 0) //if the rock is falling AND it is not already on the floor do this
            {
                Vector3 gravity = new Vector3(0, -10f, 0);
                ///////////////////////////////////////////////////////////////////
                //
                // CODE FOR TASK 4 HERE
                //
                ///////////////////////////////////////////////////////////////////


                //calculate time since rocks started falling
                //calculate the rock's new y position using the derived equation
                //stop when you reach the ground (0)
                Vector3 rockStartPosition = new Vector3(25, 60, -50);
                float timeSinceFall = (float)gameTime.TotalGameTime.TotalSeconds - RockFallStart;
                
                rock.position.Y = gravity.Y * timeSinceFall * timeSinceFall / 2f + rock.velocity.Y * timeSinceFall + rockStartPosition.Y;

                if (rock.position.Y < 0f)
                {
                    rock.position.Y = 0f;
                    RockFallStart = 0f;
                     rockFalling = false;
                }
               
            }
            if (player.hitBox.Intersects(TriggerBoxDoorOpen))
            {
                doorOpening = true;
            }
            if (doorOpening)
            {
                Vector3 newPos = new Vector3();
                Vector3 doorStartPoint = new Vector3(-70, 0, 0);
                Vector3 doorEndPoint = new Vector3(-70, 30, 0);
                ///////////////////////////////////////////////////////////////////
                //
                // CODE FOR TASK 5 HERE
                //
                ///////////////////////////////////////////////////////////////////
               
                // ------------------------------
                // EASING
                // ------------------------------
                //get game time
                doorSequenceTimer += gameTime.ElapsedGameTime.Milliseconds;
                if (doorSequenceTimer >= doorSequenceFinalTime)
                {
                    //reset timer
                    doorSequenceTimer = doorSequenceFinalTime;
                }
                newPos = CubicInterpolation(doorStartPoint, doorEndPoint, (float)doorSequenceTimer, (float)doorSequenceFinalTime);
                door.SetUpVertices(newPos);
            }

            base.Update(gameTime);
        }

        private void ElasticCollision(basicCuboid w)
        {
            //old collision data
            //player.velocity *= -1;
            //player.position = player.storedPos;
            ///////////////////////////////////////////////////////////////////
            //
            // TASK 7 HERE
            //
            ///////////////////////////////////////////////////////////////////
            ///

            //declare necessary variables
            //we need the perpendicular vector to the face of the box we hit
            //to do this, we need TWO vectors ON the face of the box we hit
            Vector3 faceVector1;
            Vector3 faceVector2;

            //get the corners of the box we hit
            //so we can calculate the face vectors

            Vector3[] corners = w.collisionbox.GetCorners();
            //This returns the corners of the box, faces that are perpendicular(90') to the Z axis
            //0-3 is the near face and 4-7 is the far face of the cube
            //starts in the upper left, upper right, lower right, lower left (clockwise formation)

            //move back our player to their previous position
            //so they aren't inside the box
            player.position = player.storedPos;

            //is the players new position overlapping in the X direction
            if ((player.hitBox.Min.X - player.velocity.X) > w.collisionbox.Max.X
            || (player.hitBox.Max.X - player.velocity.X) > w.collisionbox.Min.X)
            {
                // overlapping from Right or Left

                //line from back bottom right the front top right point
                faceVector1 = corners[1] - corners[6];
                //line from back bottom right going to front bottom right
                faceVector2 = corners[2] - corners[6];
            }
            //if we are not overlapping, right or left            
            else
            {//we are overlapping in the front or back (Z-axis)

                //line from front top left to the front top right point
                faceVector1 = corners[1] - corners[0];
                //line from front top left going to front bottom right
                faceVector2 = corners[2] - corners[0];
            }
            //we ignore the possibility of a y-direction

            //get a cross product between these two vectors to define a normal perpendicular to the plane (face)
            Vector3 normal = Vector3.Cross(faceVector1, faceVector2);
            //make into a unit vector(of length 1)
            normal.Normalize(); // divide normal by it's own magnitude

            //use this normal vector to reflect the players velocity
            // (this uses a dot product equation internally)
            player.velocity = Vector3.Reflect(player.velocity, normal);
        }
        ///////////////////////////////////////////////////////////////////
        //
        // CODE FOR TASK 6  HERE
        //
        ///////////////////////////////////////////////////////////////////
        public Vector3 CubicInterpolation(Vector3 initialPos, Vector3 endPos, float
        time, float duration)
        {
            // Calculate our independant variable time as a proportion (ratio) of time passed to the total duration
            // (between 0 and 1)
            float t = time / duration;

            // Calculate p (position aka distance traveled from start)
            // Using our derived quadratic equation
            // Produces a fraction of the complete distance (between 0 and 1)
            // This is our scaling factor

            //p(t) = 02t^3 + 3t^2
            float p = -2f * t * t * t + 3f * t * t;

            // Determine the total distance to be traveled
            Vector3 totalDistance = endPos - initialPos;
            // endpoint = startPoint + totalDistance

            // Determine the distance traveled (how far we have actually gone so far)
            // By scaling the total distance by our generated scaling factor (p)
            Vector3 distanceTraveled = totalDistance * p;

            // Determine the new position by adding the distance traveled to the start point
            Vector3 newPosition = initialPos + distanceTraveled;

            return newPosition;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            spriteBatch.End();
            graphics.GraphicsDevice.BlendState = BlendState.Opaque; // set up 3d rendering so its not transparent
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            player.Draw(gamecam, sunlight);
            rock.Draw(gamecam, sunlight);
            door.Draw(gamecam.ViewMatrix(), gamecam.ProjectionMatrix());
            foreach (basicCuboid w in walls)
                w.Draw(gamecam.ViewMatrix(), gamecam.ProjectionMatrix());

            bullet.Draw(gamecam, sunlight);
            BoundingRenderer.RenderBox(player.hitBox, gamecam.ViewMatrix(),
            gamecam.ProjectionMatrix(), Color.White);
            BoundingRenderer.RenderBox(rock.hitBox, gamecam.ViewMatrix(),
            gamecam.ProjectionMatrix(), Color.White);
            BoundingRenderer.RenderBox(TriggerBoxDoorOpen, gamecam.ViewMatrix(),
            gamecam.ProjectionMatrix(), player.hitBox.Intersects(TriggerBoxDoorOpen) ? Color.White
            : Color.CornflowerBlue);
            BoundingRenderer.RenderBox(TriggerBoxRockFall, gamecam.ViewMatrix(),
            gamecam.ProjectionMatrix(), player.hitBox.Intersects(TriggerBoxRockFall) ? Color.White
            : Color.CornflowerBlue);
            BoundingRenderer.RenderBox(door.collisionbox, gamecam.ViewMatrix(),
            gamecam.ProjectionMatrix(), player.hitBox.Intersects(TriggerBoxRockFall) ? Color.White
            : Color.CornflowerBlue);

            base.Draw(gameTime);
        }
    }
}
