using System;
using System.Drawing;

namespace Bergamot.DataStructures
{
	public struct Edge : IEquatable<Edge>
	{
		public PointF A, B;

		public Edge(PointF a, PointF b)
		{
			A = a;
			B = b;
		}

		public bool Equals(Edge other)
		{
			return A.Equals(other.A) && B.Equals(other.B) || B.Equals(other.A) && A.Equals(other.B);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Edge other && Equals(other);
		}

		public override int GetHashCode()
		{
			return A.GetHashCode() ^ B.GetHashCode();
		}
	}
}
