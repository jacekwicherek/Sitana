﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Common;

namespace Sitana.Framework.PP.Elements
{
    public abstract class Triangulatable
    {
        public readonly Vertices Triangles = new Vertices();

        public abstract void GenerateTriangles();
    }
}
