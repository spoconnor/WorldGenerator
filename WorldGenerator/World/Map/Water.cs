﻿using Sean.Shared;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Sean.WorldGenerator
{
    public class River
    {
        private IWorld worldInstance;
        public HashSet<Position> Coords { get; set; }
        public Position Source { get; set; }
        public bool Growing { get; private set; }

        private HashSet<Position> _emptyCoords;
        private Dictionary<Position, float> _heights;
        private float _minScore;
        private Position _minPos;
        private int _maxLength;

        private const int WATER = 2; // TODO - stick these somewhere
        private const int GRASS = 4;
        private const int RIVER = 31;

        public River(IWorld world)
        {
            worldInstance = world;
            Growing = true;
            _maxLength = int.MaxValue; //128;
            _minPos = new Position(0, Global.CHUNK_HEIGHT, 0);
            _minScore = Global.CHUNK_HEIGHT;
            Coords = new HashSet<Position>();
            _emptyCoords = new HashSet<Position>();
            _heights = new Dictionary<Position, float>();
            Source = FindGoodSourceSpot();
            Add(Source, _minScore);
            CalcScore(Source);
        }
        public Position FindGoodSourceSpot()
        {
            var bestScore = 0.0;
            var best = new Position(0,0,0);
            for (int i = 0; i < 30; i++)
            {
                int x = Settings.Random.Next(worldInstance.GlobalMap.Size.minX, worldInstance.GlobalMap.Size.maxX);
                int z = Settings.Random.Next(worldInstance.GlobalMap.Size.minZ, worldInstance.GlobalMap.Size.maxZ);

                var riverScore = PotentialRiver(x, z);
                //Log.WriteInfo($"[River.FindGoodSourceSpot] '{x},{z}' = {riverScore}");
                if (riverScore > bestScore)
                {
                    bestScore = riverScore;
                    best = new Position(x, 0, z);
                }
            }
            var y = worldInstance.GetBlockHeight (best.X, best.Z);
            return new Position(best.X, y, best.Z);
        }
        private float PotentialRiver(int x, int z)
        {
            var heightScore = (Global.CHUNK_HEIGHT - (double)(worldInstance.GlobalMap[x, z] - Global.waterLevel)) / Global.CHUNK_HEIGHT;
            var midX = (worldInstance.GlobalMap.Size.maxX / 2);
            var midZ = (worldInstance.GlobalMap.Size.maxZ / 2);
            var max = Math.Max(midX, midZ);
            var posScore = (max - (Math.Sqrt((midX - x) * (midX - x) + (midZ - z) * (midZ - z)))) / max;
            return (float)(heightScore + posScore);
        }

        public void Grow()
        {
            Add(_minPos, _minScore);
        }
        public void Add(Position pos, float score)
        {
            if (pos.X == 0 && pos.Z == 0) return;

            Coords.Add(pos);
            _emptyCoords.Remove(pos);
            _heights.Remove(pos);

            var newWaterBlock = false;
            var block = worldInstance.GetBlock (pos.X, pos.Y, pos.Z);
            switch (block.Type)
            {
                case BlockType.Water1: block = new Block(BlockType.Water4); break;
                case BlockType.Water2: block = new Block(BlockType.Water4); break;
                case BlockType.Water3: block = new Block(BlockType.Water4); break;
                case BlockType.Water4: block = new Block(BlockType.Water7); break;
                case BlockType.Water5: block = new Block(BlockType.Water7); break;
                case BlockType.Water6: block = new Block(BlockType.Water7); break;
                case BlockType.Water7: block = new Block(BlockType.Water); break;
                default:
                    block = new Block(BlockType.Water1);
                    newWaterBlock = true;
                    break;
            }
            worldInstance.SetBlock(pos.X, pos.Y, pos.Z, block);
            if (block.Type != BlockType.Water)
            {
                _emptyCoords.Add(pos);
                CalcScore(pos); // re-adds to _heights
            }

            if (newWaterBlock)
            {
                var below = worldInstance.GetBlock (pos.X, pos.Y-1, pos.Z);
                if (below.Type == BlockType.Water)
                {
                    // Mark so we don't render it
                    // TODO - check surrounding blocks
                    worldInstance.SetBlock(pos.X, pos.Y-1, pos.Z, new Block(BlockType.UnderWater));
                }

                if (worldInstance.GetBlock(pos.X-1, pos.Y - 1, pos.Z).IsWater
                    && worldInstance.GetBlock(pos.X+1, pos.Y - 1, pos.Z).IsWater
                    && worldInstance.GetBlock(pos.X, pos.Y - 1, pos.Z-1).IsWater
                    && worldInstance.GetBlock(pos.X, pos.Y - 1, pos.Z+1).IsWater)
                {
                    // Lake detected
                }
            }

            ClearBlockAboveWater(pos.X, pos.Y+1, pos.Z);
            ClearBlockAboveWater(pos.X, pos.Y+2, pos.Z);
            ClearBlockAboveWater(pos.X+1, pos.Y+1, pos.Z);
            ClearBlockAboveWater(pos.X-1, pos.Y+1, pos.Z);
            ClearBlockAboveWater(pos.X, pos.Y+1, pos.Z+1);
            ClearBlockAboveWater(pos.X, pos.Y+1, pos.Z-1);
            ClearBlockAboveWater(pos.X+1, pos.Y+1, pos.Z+1);
            ClearBlockAboveWater(pos.X+1, pos.Y+1, pos.Z-1);
            ClearBlockAboveWater(pos.X-1, pos.Y+1, pos.Z+1);
            ClearBlockAboveWater(pos.X-1, pos.Y+1, pos.Z-1);

            worldInstance.GlobalMapTerrain.Set(pos.X, pos.Z, RIVER);
            //Log.WriteInfo($"[River.Add] Adding {pos}");

            AddIfEmpty(pos.X, pos.Y - 1, pos.Z);
            AddIfEmpty(pos.X+1, pos.Y, pos.Z);
            AddIfEmpty(pos.X-1, pos.Y, pos.Z);
            AddIfEmpty(pos.X, pos.Y, pos.Z+1);
            AddIfEmpty(pos.X, pos.Y, pos.Z-1);
            AddIfEmpty(pos.X, pos.Y + 1, pos.Z);

            if (_minPos == pos)
                FindNextLowest();
        }
        private void ClearBlockAboveWater(int x,int y,int z)
        {
            var airBlock = new Block(BlockType.Air);
            var above = worldInstance.GetBlock (x, y, z);
            if (above.Type == BlockType.Dirt || above.Type == BlockType.Grass) {
                worldInstance.SetBlock (x, y, z, airBlock);
            }
        }

        private void AddIfEmpty(int x,int y,int z)
        {
            if (Source.GetDistanceExact (new Position (x, y, z)) > _maxLength) {
                // Temporary code to limit length of river
                Growing = false;
                return;
            }

            if (!worldInstance.IsValidBlockLocation (x, y, z)) {
                // Have reached edge of map
                Growing = false;
                return;
            }
                
            var block = worldInstance.GetBlock (x, y, z);
            if (block.IsWater) {
                if (!Coords.Contains (new Position (x, y, z))) {
                    // Have reached another river or the ocean
                    Growing = false;
                }
                return;
            }
            //if (!block.IsSolid)
            if (block.Type == BlockType.Air || block.Type == BlockType.Dirt || block.Type == BlockType.Grass)
            {
                var pos = new Position(x,y,z);
                _emptyCoords.Add(pos);
                CalcScore(pos);

//                // Add solid block down form lower riverbed
//                var blockBelow = worldInstance.GetBlock (x, y-1, z);
//                if (blockBelow.IsSolid) {
//                    if (blockBelow.IsWater) {
//                        if (!Coords.Contains (new Position (x, y-1, z))) {
//                            // Have reached another river or the ocean
//                            Growing = false;
//                        }
//                        return;
//                    }
//                    pos = new Position(x,y-1,z);
//                    _emptyCoords.Add(pos);
//                    CalcScore(pos);
//                }
            }
        }
        private void CalcScore(Position pos)
        {
//            var chunk = new ChunkCoords(pos);
            float score;
            //const int comp = 7; // compare range
            try
            {
                float neighbours = 0;
                var h =  worldInstance.GetBlockHeight(pos.X,pos.Z);
                for (var i=1;i<8;i++)
                {
                    neighbours += TestCalcScore(h, i, pos.X+i,pos.Z);
                    neighbours += TestCalcScore(h, i, pos.X-i,pos.Z);
                    neighbours += TestCalcScore(h, i, pos.X,pos.Z+i);
                    neighbours += TestCalcScore(h, i, pos.X,pos.Z-i);
                    neighbours += TestCalcScore(h, i, pos.X+i,pos.Z+i);
                    neighbours += TestCalcScore(h, i, pos.X+i,pos.Z-i);
                    neighbours += TestCalcScore(h, i, pos.X-i,pos.Z+i);
                    neighbours += TestCalcScore(h, i, pos.X-i,pos.Z-i);
                }

                //var a = worldInstance.GetBlockHeight(pos.X+comp,pos.Z);
                //var b = worldInstance.GetBlockHeight(pos.X-comp,pos.Z);
                //var c = worldInstance.GetBlockHeight(pos.X,pos.Z+comp);
                //var d = worldInstance.GetBlockHeight(pos.X,pos.Z-comp);
                //var e = worldInstance.GetBlockHeight(pos.X+comp,pos.Z+comp);
                //var f = worldInstance.GetBlockHeight(pos.X-comp,pos.Z+comp);
                //var g = worldInstance.GetBlockHeight(pos.X+comp,pos.Z-comp);
                //var h = worldInstance.GetBlockHeight(pos.X-comp,pos.Z-comp);
                //var neighbours = Math.Max(((a + b + c + d+e+f+g+h) / 8) - (float)pos.Y, 0);

                var block = worldInstance.GetBlock (pos.X, pos.Y, pos.Z);
                var currentWaterHeight = block.WaterHeight;

                score = ((float)pos.Y 
                    + ((float)currentWaterHeight / 16) 
                    + (neighbours / 20)
                    //+ (slope / 10)
                    ) / Global.CHUNK_HEIGHT;
            }
            catch (Exception) { // TODO Handle out of array bounds errors better
                score = pos.Y;
            }
            if (!_heights.ContainsKey(pos))
                _heights.Add(pos, score);
            if (score < _minScore)
            {
                _minScore = score;
                _minPos = pos;
            }
        }
        private float TestCalcScore(int h, int i, int x,int z)
        {
            float a = worldInstance.GetBlockHeight(x,z);
            if (a >= h)
                return 0.0f;
            return (a - h) / (float)(Math.Pow(2,i));
        }
        private void FindNextLowest()
        {
            _minScore = Global.CHUNK_HEIGHT;
            foreach (var pos in _emptyCoords)
            {
                var score = _heights[pos];
                if (score < _minScore)
                {
                    _minScore = score;
                    _minPos = pos;
                }
            }
        }
        /*
        // Find next lowest, which is also next to current min
        private void FindNextLowest()
        {
            var potentials = new List<Position>();
            _minScore = Global.CHUNK_HEIGHT;
            foreach(var pos in _emptyCoords)
            {
                var score = _heights[pos];
                if (score < _minScore)
                {
                    potentials.Clear();
                    potentials.Add(pos);
                    _minScore = score;
                }
                else if (score == _minScore)
                {
                    potentials.Add(pos);
                }
            }
            if (potentials.Count == 0)
                return;
            foreach(var pos in potentials)
            {
                if (Math.Abs(pos.X - _minPos.X) == 1
                    || Math.Abs(pos.Y - _minPos.Y) == 1
                    || Math.Abs(pos.Z - _minPos.Z) == 1)
                {
                    _minPos = pos;
                    return;
                }
            }
            _minPos = potentials[0];
        }
        */
    }

    public class Water
    {
        public Water (IWorld world)
        {
            worldInstance = world;
            Rivers = new List<River>();
            Run();
            //GenerateRivers();

            //ProcessWater(); // TODO - run from own thread
        }

        private IWorld worldInstance;
        private List<River> Rivers;
        private Thread thread;
        private const int WATER = 2; // TODO - stick these somewhere
        private const int GRASS = 4;
        private const int RIVER = 5;

        public void Run()
        {
            thread = new Thread(new ThreadStart(StartThread));
            thread.Start();
        }

        private void CreateRiver()
        {
            for (var i = 0; i < Settings.RiverCount; i++)
            {
                var river = new River(worldInstance);
                var chunkCoords = new ChunkCoords (river.Source);
                Log.WriteInfo ($"Creating river from chunk {chunkCoords}");
                Rivers.Add(river);
            }
        }

        private void StartThread()
        {
            try
            {
                CreateRiver();
                bool growing = true;
                while (growing)
                {
                    if (Rivers.Count != 0)
                    {
                        growing = false;
                        foreach (var river in Rivers)
                        {
                            river.Grow();
                            growing |= river.Growing;
                        }
                    }
                    Thread.Sleep(5);
                }
            }
            catch (Exception ex) {
                Log.WriteError ($"Water thread crashed - {ex.Message}");
            }
        }
    }
}

