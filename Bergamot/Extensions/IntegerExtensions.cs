using System;
using System.Collections.Generic;

namespace Bergamot.Extensions
{
	public static class IntegerExtensions
	{
		public static int Clamp(this int value, int min, int max) => value < min ? min : value > max ? max : value;

		public static int Sqr(this int self) => self * self;

		public static int EuclideanMod(this int self, int divisor)
		{
			var r = self % divisor;
			return r < 0 ? r + divisor : r;
		}
	}
}
