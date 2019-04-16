namespace Bergamot.Extensions
{
	public static class FloatExtensions
	{
		public static float Clamp(this float value, float min, float max) => value < min ? min : value > max ? max : value;

		public static float Sqr(this float self) => self * self;
	}
}
