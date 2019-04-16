using System;
using System.Collections.Generic;
using System.Drawing;
using Bergamot.Extensions;

namespace Bergamot.DataStructures
{
	public struct Triangle : IEquatable<Triangle>
	{
		public PointF[] Vertices;
		public Edge[] Edges;

		public bool CircumcircleContains(PointF p)
		{
			var n1 = V1.Norm2();
			var n2 = V2.Norm2();
			var n3 = V3.Norm2();
			var a = V1.X * V2.Y + V2.X * V3.Y + V3.X * V1.Y - (V2.Y * V3.X + V3.Y * V1.X + V1.Y * V2.X);
			var b = n1 * V2.Y + n2 * V3.Y + n3 * V1.Y - (V2.Y * n3 + V3.Y * n1 + V1.Y * n2);
			var c = n1 * V2.X + n2 * V3.X + n3 * V1.X - (V2.X * n3 + V3.X * n1 + V1.X * n2);
			var d = n1 * V2.X * V3.Y + n2 * V3.X * V1.Y + n3 * V1.X * V2.Y - (V2.X * n3 * V1.Y + V3.X * n1 * V2.Y + V1.X * n2 * V3.Y);

			return (a * p.Norm2() - b * p.X + c * p.Y - d) * Math.Sign(a) < 0;
		}

		public bool Contains(PointF vertex)
		{
			return V1.Equals(vertex) || V2.Equals(vertex) || V3.Equals(vertex);
		}

		public PointF V1
		{
			get => Vertices[0];
			set => Vertices[0] = value;
		}

		public PointF V2
		{
			get => Vertices[1];
			set => Vertices[1] = value;
		}

		public PointF V3
		{
			get => Vertices[2];
			set => Vertices[2] = value;
		}

		public Edge E1
		{
			get => Edges[0];
			set => Edges[0] = value;
		}

		public Edge E2
		{
			get => Edges[1];
			set => Edges[1] = value;
		}

		public Edge E3
		{
			get => Edges[2];
			set => Edges[2] = value;
		}

		public Triangle(PointF p1, PointF p2, PointF p3)
		{
			Vertices = new PointF[3] { p1, p2, p3 };
			Edges = new Edge[3] { new Edge(p1, p2), new Edge(p2, p3), new Edge(p3, p1) };
		}

		public bool Equals(Triangle other) => 
			Vertices[0] == other.Vertices[0] && Vertices[1] == other.Vertices[1] && Vertices[2] == other.Vertices[2];

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Triangle other && Equals(other);
		}

		public override int GetHashCode()
		{
			var hashCode = 203343529;
			hashCode = hashCode * -1521134295 + V1.GetHashCode();
			hashCode = hashCode * -1521134295 + V2.GetHashCode();
			hashCode = hashCode * -1521134295 + V3.GetHashCode();
			hashCode = hashCode * -1521134295 + E1.GetHashCode();
			hashCode = hashCode * -1521134295 + E2.GetHashCode();
			hashCode = hashCode * -1521134295 + E3.GetHashCode();
			return hashCode;
		}
	}
}
