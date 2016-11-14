﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sean.Shared
{
    public struct Vector2
    {
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public float x;
        public float y;
    }

    public class ArraySize
    {
        public int minZ;
        public int maxZ;
        public int minX;
        public int maxX;
        // terrain height
        public int minY;
        public int maxY;

        public int scale = 1;
        public int zWidth { get { return maxZ - minZ; } }
        public int xHeight { get { return maxX - minX; } }

        public float NormalizeZ(int z) { return (float)(z - minZ) / zWidth; }
        public float NormalizeX(int x) { return (float)(x - minX) / xHeight; }
        public int UnNormX(double x) { return (int)(x * xHeight) + minX; }
        public int UnNormZ(double z) { return (int)(z * zWidth) + minZ; }
    }

    public class ArrayLine<T> 
    {
        public ArrayLine (ArraySize size)
        {
            _size = size;
            _data = new T[ToArrayCoord(_size.maxX)];
        }

        public T this[int x]
        {
            get { return _data[ToArrayCoord(x)]; }
            set { _data[ToArrayCoord(x)] = value; }
        } 
           
        public void Set(int x, T value)
        {
            _data [ToArrayCoord(x)] = value;
        }

        public IEnumerator<T> GetCells ()
        {
            for (int x = 0; x < ToArrayCoord (_size.maxX); x++) 
            {
                yield return _data [x];
            }
        }

        private int ToArrayCoord(int x)
        {
            return (x - _size.minX) / _size.scale;
        }

        public void Render()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int x = 0; x < ToArrayCoord(_size.maxX); x++)
            {
                builder.Append(_data[x]);
            }
            Console.WriteLine(builder.ToString());
        }
            
        private T[] _data;
        private ArraySize _size;
    }

    public class Array<T>
    {
        public Array(int x, int z)
        {
            _size = new ArraySize (){ maxX=x, maxZ=z };
            _data = new ArrayLine<T>[ToArrayCoord(_size.maxZ)];
            for (int i = 0; i < ToArrayCoord(_size.maxZ); i++)
            {
                _data[i] = new ArrayLine<T>(_size);
            }
        }
        public Array (ArraySize size)
        {
            _size = size;
            _data = new ArrayLine<T>[ToArrayCoord(_size.maxZ)];
            for (int z = 0; z < ToArrayCoord(_size.maxZ); z++)
            {
                _data[z] = new ArrayLine<T>(_size);
            }
        }

        public bool IsValidCoord(int x, int z)
        {
            return x >= _size.minX && x < _size.maxX && z >= _size.minZ && z < _size.maxZ;
        }
        public ArraySize Size { get { return _size; } }
            
        public T this[int x, int z]
        {
            get { return _data[ToArrayCoord(z)][x]; }
            set { _data[ToArrayCoord(z)][x] = value; }
        } 
            
        public void Set(int x, int z, T value)
        {
            _data [ToArrayCoord(z)].Set(x, value);
        }
            
        public void Render()
        {
            for (int z = 0; z < ToArrayCoord(_size.maxZ); z++) 
            {
                _data[z].Render();
            }
        }
            
        private int ToArrayCoord(int z)
        {
            return (z - _size.minZ) / _size.scale;
        }

        private ArrayLine<T>[] _data;
        private ArraySize _size;

        public byte[] Serialize()
        {
            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new System.IO.MemoryStream())
            {
                for (int z = 0; z < _size.maxZ; z=z+_size.scale)
                {
                    for (int x = 0; x < _size.maxX; x=x+_size.scale) 
                    {
                        var item = this[z,x];
                        binaryFormatter.Serialize(memoryStream, item);
                    }
                }
                return memoryStream.ToArray();
            }
        }
        public void DeSerialize(byte[] data)
        {
            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new System.IO.MemoryStream(data))
            {
                for (int z = 0; z < _size.maxZ; z=z+_size.scale)
                {
                    for (int x = 0; x < _size.maxX; x=x+_size.scale) 
                    {
                        var item = (T)binaryFormatter.Deserialize(memoryStream);
                        this[z,x] = item;
                    }
                }
            }
        }
    }
}

