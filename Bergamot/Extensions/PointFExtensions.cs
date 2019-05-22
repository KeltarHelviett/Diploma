using System.Drawing;

namespace Bergamot.Extensions
{
	public static class PointFExtensions
	{
		public static PointF Add(this PointF self, PointF other) => new PointF(self.X + other.X, self.Y + other.Y);

		public static PointF Sub(this PointF self, PointF other) => new PointF(self.X - other.X, self.Y - other.Y);

		public static PointF Clamp(this PointF self, float x1, float x2, float y1, float y2) =>
			new PointF(self.X.Clamp(x1, x2), self.Y.Clamp(y1, y2));

		public static PointF Swap(this PointF self) => new PointF(self.Y, self.X);

		public static PointF Mul(this PointF self, float multiplier) =>
			new PointF(self.X * multiplier, self.Y * multiplier);

		public static PointF Sqr(this PointF self) => new PointF(self.X * self.X, self.Y * self.Y);

		public static float Sum(this PointF self) => self.X + self.Y;

		public static float Cross(this PointF self, PointF other) => self.X * other.Y - self.Y * other.X;

		public static float Norm2(this PointF self) => self.X * self.X + self.Y * self.Y;

		public static float Dist2(this PointF self, PointF other) =>
			(self.X - other.X).Sqr() + (self.Y - other.Y).Sqr();

		public static bool InRect(this PointF self, PointF leftTop, PointF rightBot) =>
			self.X >= leftTop.X && self.X <= rightBot.X && self.Y >= leftTop.Y && self.Y <= rightBot.Y;
	}
}
