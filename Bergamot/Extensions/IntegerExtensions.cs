using System;
using System.Collections.Generic;

namespace Bergamot.Extensions
{
    public static class IntegerExtensions
    {
        public static int Clamp(this int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
