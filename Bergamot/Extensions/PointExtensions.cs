using System;
using System.Drawing;

namespace Bergamot.Extensions
{
	public static class PointExtensions
	{
		public static Point Add(this Point self, Point other) => new Point(self.X + other.X, self.Y + other.Y);

		public static Point Sub(this Point self, Point other) => new Point(self.X - other.X, self.Y - other.Y);

		public static Point Clamp(this Point self, int x1, int x2, int y1, int y2) =>
			new Point(self.X.Clamp(x1, x2), self.Y.Clamp(y1, y2));

		public static Point Swap(this Point self) => new Point(self.Y, self.X);

		public static Point Mul(this Point self, int multiplier) => new Point(self.X * multiplier, self.Y * multiplier);

		public static Point Sqr(this Point self) => new Point(self.X * self.X, self.Y * self.Y);

		public static int Sum(this Point self) => self.X + self.Y;

		public static int Cross(this Point self, Point other) => self.X * other.Y - self.Y * other.X;

		public static int Dist2(this Point self, Point other) =>
			(self.X - other.X).Sqr() + (self.Y - other.Y).Sqr();

		public static int Norm2(this Point self) => self.X * self.X + self.Y * self.Y;

		public static float Norm(this Point self) => (float)Math.Sqrt(self.Norm2());

		public static PointF Normalized(this Point self)
		{
			var norm = self.Norm();
			return new PointF(self.X / norm, self.Y / norm);
		}
	}
}
