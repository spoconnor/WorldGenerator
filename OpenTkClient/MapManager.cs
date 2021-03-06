﻿using System;
using Sean.Shared;
using System.Collections.Generic;

namespace OpenTkClient
{
    public static class MapManager
    {
        //private static Chunks chunks = new Chunks ();
		private static SortedList<ChunkCoords, Chunk> _chunksN = new SortedList<ChunkCoords, Chunk>();
		private static SortedList<ChunkCoords, Chunk> _chunksS = new SortedList<ChunkCoords, Chunk>();
		private static SortedList<ChunkCoords, Chunk> _chunksE = new SortedList<ChunkCoords, Chunk>();
		private static SortedList<ChunkCoords, Chunk> _chunksW = new SortedList<ChunkCoords, Chunk>();
        private static Array<byte> worldMapHeight;
        private static Array<byte> worldMapTerrain;
        private static int chunkMidpoint = Global.CHUNK_SIZE / 2;

        private static object _lock = new object ();

        public static void SetWorldMapTerrain(Array<byte> map)
        {
            worldMapTerrain = map;
        }
        public static void SetWorldMapHeight(Array<byte> map)
        {
            worldMapHeight = map;
        }

        public static void AddChunk(ChunkCoords coords, Chunk chunk)
        {
			lock (_lock) {
				_chunksN.Add (coords, chunk);
				_chunksS.Add (new ChunkCoords(Global.MaxChunkLimit - coords.X, Global.MaxChunkLimit - coords.Z), chunk);
				_chunksE.Add (new ChunkCoords(Global.MaxChunkLimit - coords.X, coords.Z), chunk);
				_chunksW.Add (new ChunkCoords(coords.X, Global.MaxChunkLimit - coords.Z), chunk);
			}
        }

        public static void SetBlock(Position position, Block newBlock)
        {
            lock (_lock)
            {
                var coords = new ChunkCoords(position);
                _chunksN[coords].Blocks[position] = newBlock;
            }
        }

		public static IEnumerable<Tuple<Position, BlockType>> GetBlocks(Facing direction)
        {
			lock (_lock) {
				var list = _chunksN;
				switch (direction) {
				case Facing.North:
					list = _chunksN;
					break;
				case Facing.East:
					list = _chunksE;
					break;
				case Facing.South:
					list = _chunksS;
					break;
				case Facing.West:
					list = _chunksW;
					break;
				}
				foreach (var chunk in list) {
					foreach (var item in chunk.Value.GetVisibleIterator(direction)) {
						yield return item;
					}
				}
			}
        }

        public static IEnumerable<Tuple<Position, BlockType>> GetWorldMapBlocks(Facing direction)        {
            lock (_lock)
            {
                if (worldMapHeight != null && worldMapTerrain != null)
                {
                    // TODO - facing direction
                    var s = worldMapHeight.Size.scale;
                    for (int z = worldMapHeight.Size.minZ; z < worldMapHeight.Size.maxZ - s; z += s)
                    {
                        for (int x = worldMapHeight.Size.minX; x < worldMapHeight.Size.maxX - s; x += s)
                        {
                            //if (Math.Abs(z - Global.LookingAt.Z) <= 100 && Math.Abs(x - Global.LookingAt.X) <= 100)
                            yield return 
                                new Tuple<Position, BlockType>(
                                    new Position(x+chunkMidpoint, worldMapHeight[x,z], z+chunkMidpoint), 
                                    (BlockType)worldMapTerrain[x,z]);
                        }
                    }
                }
            }
        }
    }
}

