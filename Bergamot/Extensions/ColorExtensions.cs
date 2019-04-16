using System.Drawing;

namespace Bergamot.Extensions
{
	public static class ColorExtensions
	{
		public static int Diff(this Color self, Color other)
		{
			return
				self.A - other.A +
				self.R - other.R +
				self.G - other.G +
				self.B - other.B;
		}
	}
}
