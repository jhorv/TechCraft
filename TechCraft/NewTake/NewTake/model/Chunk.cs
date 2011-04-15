﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using NewTake.view;

namespace NewTake.model
{

    public class Chunk
    {
        public const byte CHUNK_XMAX = 16;
        public const byte CHUNK_YMAX = 128;
        public const byte CHUNK_ZMAX = 16;

        public Chunk N, S, E, W, NE, NW, SE, SW; //TODO infinite y would require Top , Bottom, maybe vertical diagonals

        public static Vector3i SIZE = new Vector3i(CHUNK_XMAX, CHUNK_YMAX, CHUNK_ZMAX);

        //public Block[, ,] Blocks;

        /// <summary>
        /// Contained blocks as a flattened array.
        /// </summary>
        public Block[] Blocks;

        /* 
        For accessing array for x,z,y coordianate use the pattern: Blocks[x * Chunk.FlattenOffset + z * Chunk.CHUNK_YMAX + y]
        For allowing sequental access on blocks using iterations, the blocks are stored as [x,z,y]. So basically iterate x first, z then and y last.
        Consider the following pattern;
        for (int x = 0; x < Chunk.WidthInBlocks; x++)
        {
            for (int z = 0; z < Chunk.LenghtInBlocks; z++)
            {
                int offset = x * Chunk.FlattenOffset + z * Chunk.HeightInBlocks; // we don't want this x-z value to be calculated each in in y-loop!
                for (int y = 0; y < Chunk.HeightInBlocks; y++)
                {
                    var block=Blocks[offset + y].Type 
        */

        /// <summary>
        /// Used when accessing flatten blocks array.
        /// </summary>
        public static int FlattenOffset = CHUNK_ZMAX * CHUNK_YMAX;

        public readonly Vector3i Position;
        public readonly Vector3i Index;

        public bool dirty;
        public bool visible;
        public bool generated;
        public bool built;

        public ChunkRenderer Renderer;

        private BoundingBox _boundingBox;

        public Vector3i highestSolidBlock = new Vector3i(0, 0, 0);
        //highestNoneBlock starts at 0 so it will be adjusted. if you start at highest it will never be adjusted ! 
        
        public Vector3i lowestNoneBlock = new Vector3i(0, CHUNK_YMAX, 0);

        public Chunk(Vector3i index)
        {
            dirty = true;
            visible = true;
            generated = false;

            Index = index;

            Position = new Vector3i(index.X * CHUNK_XMAX, index.Y * CHUNK_YMAX, index.Z * CHUNK_ZMAX);
            //Blocks = new Block[CHUNK_XMAX, CHUNK_YMAX, CHUNK_ZMAX]; //TODO test 3d sparse impl performance and memory
            this.Blocks = new Block[CHUNK_XMAX * CHUNK_ZMAX * CHUNK_YMAX];
            _boundingBox = new BoundingBox(new Vector3(Position.X, Position.Y, Position.Z), new Vector3(Position.X + CHUNK_XMAX, Position.Y + CHUNK_YMAX, Position.Z + CHUNK_ZMAX));
        }

        public void setBlock(int x, int y, int z, Block b)
        {
            if (b.Type == BlockType.None)
            {
                if (lowestNoneBlock.Y > y)
                {
                    lowestNoneBlock = new Vector3i((uint)x, (uint)y, (uint)z);
                }
            }
            else if (highestSolidBlock.Y < y)
            {
                //TODO uint vs int is currently a mess and in fact here it should be bytes !
                highestSolidBlock = new Vector3i((uint)x, (uint)y, (uint)z);
            }
             
            //Blocks[x, y, z] = b;//comment this line : you should have nothing on screen, else you ve been setting blocks directly in array !
            Blocks[x * Chunk.FlattenOffset + z * Chunk.CHUNK_YMAX + y] = b;
        }

        public BoundingBox BoundingBox
        {
            get { return _boundingBox; }
        }

        public bool outOfBounds(byte x, byte y, byte z)
        {
            return (x < 0 || x >= CHUNK_XMAX || y < 0 || y >= CHUNK_YMAX || z < 0 || z >= CHUNK_ZMAX);
        }

    }
}
