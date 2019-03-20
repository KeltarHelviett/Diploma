using System.Drawing;

namespace Bergamot.Extensions
{
    public static class BitmapExtensions
    {
        public static bool TryGetPixel(this Bitmap image, int x, int y, out Color color)
        {
            color = x >= 0 && x < image.Width && y >= 0 && y < image.Height ? image.GetPixel(x, y) : Color.Empty;
            return !color.IsEmpty;
        }
    }
}
