using System.Drawing;

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
    }
}
