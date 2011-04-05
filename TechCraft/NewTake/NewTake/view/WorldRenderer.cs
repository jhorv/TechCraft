﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewTake.model;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using System.Threading;
using System.Diagnostics;

namespace NewTake.view
{
    class WorldRenderer
    {
        private World world;
        public readonly GraphicsDevice GraphicsDevice;
        //private IList<ChunkRenderer> ChunkRenderers;
        Dictionary<Chunk, ChunkRenderer> ChunkRenderers;
        private Effect _solidBlockEffect;
        private Texture2D _textureAtlas;
        public const float FARPLANE = 140;
        public const int FOGNEAR = 90;
        public const int FOGFAR = 140;
        private readonly FirstPersonCamera camera;
        TimeSpan addTime = TimeSpan.Zero;
        TimeSpan removeTime = TimeSpan.Zero;
        private Queue<Chunk> _toBuild;
        private bool _running = true;
        private Thread _buildingThread;

        public WorldRenderer(GraphicsDevice graphicsDevice, FirstPersonCamera camera, World world)
        {
            this.world = world;
            this.GraphicsDevice = graphicsDevice;
            ChunkRenderers = new Dictionary<Chunk, ChunkRenderer>();
            world.visitChunks(initRendererAction);
            this.camera = camera;
            _toBuild = new Queue<Chunk>();

            _buildingThread = new Thread(new ThreadStart(BuildingThread));
            _buildingThread.Start();
        }

        public void initRendererAction(Vector3i vector)
        {
            Chunk chunk = world.viewableChunks[vector.X, vector.Z];
            ChunkRenderer cRenderer = new ChunkRenderer(GraphicsDevice, world, chunk);
            ChunkRenderers.Add(chunk, cRenderer);
        }

        public void loadContent(ContentManager content)
        {
            _textureAtlas = content.Load<Texture2D>("Textures\\blocks");
            _solidBlockEffect = content.Load<Effect>("Effects\\SolidBlockEffect");
        }

        public void Update(GameTime gameTime)
        {
            addTime += gameTime.ElapsedGameTime;
            removeTime += gameTime.ElapsedGameTime;

            if (addTime > TimeSpan.FromSeconds(2))
            {
                AddChunks();
                addTime -= TimeSpan.FromSeconds(2);
            }
            if (removeTime > TimeSpan.FromSeconds(10))
            {
                RemoveChunks();
                removeTime -= TimeSpan.FromSeconds(10);
            }
        }

        public void QueueBuild(Chunk chunk)
        {
            //Debug.WriteLine(string.Format("Queue Chunk {0}-{1}-{2}", (int)chunk.Position.X, (int)chunk.Position.Y, (int)chunk.Position.Z));
            lock (_toBuild)
            {
                _toBuild.Enqueue(chunk);
            }
        }

        private void RemoveChunks()
        {
            uint x = (uint)camera.Position.X;
            uint y = (uint)camera.Position.Y;
            uint z = (uint)camera.Position.Z;

            uint cx = x / Chunk.CHUNK_XMAX;
            uint cz = z / Chunk.CHUNK_ZMAX;

            uint lx = x % Chunk.CHUNK_XMAX;
            uint ly = y % Chunk.CHUNK_YMAX;
            uint lz = z % Chunk.CHUNK_ZMAX;

            Vector3i currentChunkIndex = world.viewableChunks[cx, cz].Index;

            for (uint j = cx - (World.VIEW_CHUNKS_X * 9); j < cx + (World.VIEW_CHUNKS_X * 9); j++)
            {
                for (uint l = cz - (World.VIEW_CHUNKS_Z * 9); l < cz + (World.VIEW_CHUNKS_Z * 9); l++)
                {
                    int distancecx = (int)(cx - j);
                    int distancecz = (int)(cz - l);

                    if (distancecx < 0) distancecx = 0 - distancecx;
                    if (distancecz < 0) distancecz = 0 - distancecz;

                    if ((distancecx > World.VIEW_CHUNKS_X * 3) || (distancecz > World.VIEW_CHUNKS_Z * 3))
                    {
                        if ((world.viewableChunks[j, l] != null))
                        {
                            Chunk chunk = world.viewableChunks[j, l];
                            chunk.visible = false;
                            world.viewableChunks[j, l] = null;
                            Vector3i newIndex = currentChunkIndex + new Vector3i((j - cx), 0, (l - cz));
                            ChunkRenderers.Remove(chunk);
                            //Debug.WriteLine(string.Format("Removed Chunk {0}-{1}-{2}", (int)chunk.Position.X, (int)chunk.Position.Y, (int)chunk.Position.Z));
                        }
                    }
                }
            }
        }

        public void BuildingThread()
        {
            while (_running)
            {
                Chunk buildChunk = null;
                bool doBuild = false;
                lock (_toBuild)
                {
                    if (_toBuild.Count > 0)
                    {
                        buildChunk = _toBuild.Dequeue();
                        doBuild = true;
                    }
                }
                if (doBuild)
                {
                    DoBuild(buildChunk);
                }
                Thread.Sleep(1);
            }
        }

        public void DoBuild(Chunk chunk)
        {
            world.builder.build(chunk);
        }

        private void AddChunks()
        {
            uint x = (uint)camera.Position.X;
            uint y = (uint)camera.Position.Y;
            uint z = (uint)camera.Position.Z;

            uint cx = x / Chunk.CHUNK_XMAX;
            uint cz = z / Chunk.CHUNK_ZMAX;

            uint lx = x % Chunk.CHUNK_XMAX;
            uint ly = y % Chunk.CHUNK_YMAX;
            uint lz = z % Chunk.CHUNK_ZMAX;

            Vector3i currentChunkIndex = world.viewableChunks[cx, cz].Index;

            for (uint j = cx - (World.VIEW_CHUNKS_X * 2); j < cx + (World.VIEW_CHUNKS_X * 2); j++)
            {
                for (uint l = cz - (World.VIEW_CHUNKS_Z * 2); l < cz + (World.VIEW_CHUNKS_Z * 2); l++)
                {
                    if (world.viewableChunks[j, l] == null)
                    {
                        Vector3i newIndex = currentChunkIndex + new Vector3i((j - cx), 0, (l - cz));
                        Chunk toAdd = new Chunk(newIndex);
                        world.viewableChunks[newIndex.X, newIndex.Z] = toAdd;
                        QueueBuild(toAdd);
                        initRendererAction(newIndex);
                    }
                }
            }
        }

        public void Draw(GameTime gameTime)
        {

            BoundingFrustum viewFrustum = new BoundingFrustum(camera.View * camera.Projection);

            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
            /* GraphicsDevice.RasterizerState = new RasterizerState()
             {
                 CullMode = CullMode.None,
                 FillMode = FillMode.WireFrame
             };*/

            _solidBlockEffect.Parameters["World"].SetValue(Matrix.Identity);
            _solidBlockEffect.Parameters["View"].SetValue(camera.View);
            _solidBlockEffect.Parameters["Projection"].SetValue(camera.Projection);
            _solidBlockEffect.Parameters["CameraPosition"].SetValue(camera.Position);
            _solidBlockEffect.Parameters["AmbientColor"].SetValue(Color.White.ToVector4());
            _solidBlockEffect.Parameters["AmbientIntensity"].SetValue(0.6f);
            _solidBlockEffect.Parameters["FogColor"].SetValue(Color.SkyBlue.ToVector4());
            _solidBlockEffect.Parameters["FogNear"].SetValue(FOGNEAR);
            _solidBlockEffect.Parameters["FogFar"].SetValue(FOGFAR);
            _solidBlockEffect.Parameters["BlockTexture"].SetValue(_textureAtlas);

            foreach (EffectPass pass in _solidBlockEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                foreach (Chunk key in ChunkRenderers.Keys)
                {
                    Chunk chunk = world.viewableChunks[ChunkRenderers[key].chunk.Index.X, ChunkRenderers[key].chunk.Index.Z];
                    if (chunk.BoundingBox.Intersects(viewFrustum))
                    {
                        ChunkRenderers[key].draw(gameTime);
                    }
                }
            }

        }
    }
}
