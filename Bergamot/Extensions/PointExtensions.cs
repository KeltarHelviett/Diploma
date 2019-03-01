using System.Drawing;

namespace Bergamot.Extensions
{
    public static class PointExtensions
    {
        public static Point Add(this Point self, Point other)
        {
            return new Point(self.X + other.X, self.Y + other.Y);
        }
    }
}
