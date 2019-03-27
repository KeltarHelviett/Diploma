using System;
using System.Drawing;
using Bergamot.Extensions;

namespace Bergamot.DataStructures
{
    public struct Segment
    {
        public Point A, B;

        public Segment(Point a, Point b)
        {
            A = a;
            B = b;
        }

        public int Length => (int)Math.Ceiling(Math.Sqrt(A.Sub(B).Sqr().Sum()));
    }
}
