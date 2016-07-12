using System;
using System.Collections.Generic;
using Sean.Shared;

namespace Sean.WorldGenerator.Noise
{
    // ScaleOffset applies a scaling and translation factor to the output of its source function, as v*scale+offset.

    internal class CImplicitScaleOffset : CImplicitModuleBase
    {
        protected DataSource m_scale { get; set; }
        protected DataSource m_offset { get; set; }

        public override float get (float x, float y)
        {
            return m_source.get (x, y) * m_scale.get (x, y) + m_offset.get (x, y);
        }

        public override float get (float x, float y, float z)
        {
            return m_source.get (x, y, z) * m_scale.get (x, y, z) + m_offset.get (x, y, z);
        }

        public override float get (float x, float y, float z, float w)
        {
            return m_source.get (x, y, z, w) * m_scale.get (x, y, z, w) + m_offset.get (x, y, z, w);
        }

        public override float get (float x, float y, float z, float w, float u, float v)
        {
            return m_source.get (x, y, z, w, u, v) * m_scale.get (x, y, z, w, u, v) + m_offset.get(x,y,z,w,u,v);
        }
    }
}
