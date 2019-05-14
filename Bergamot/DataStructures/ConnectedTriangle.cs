using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Bergamot.Extensions;

namespace Bergamot.DataStructures
{
	// 3 vertices and 3 adjacent triangles
	// vertices are clockwise ordered
	// triangles are ordered so that ith triangle connected
	// via edge that is opposite to ith vertex
	// ith orientation is index of this triangle in
	// triangles array of triangle that shares
	// edge that is opposite to ith vertex
	public class ConnectedTriangle
	{
		public PointF?[] Vertices = new PointF?[3];
		public ConnectedTriangle[] Triangles = new ConnectedTriangle[3];
		public int[] Orientations = new int[3];

		public ConnectedTriangle(PointF p1, PointF p2, PointF p3)
		{
			V1 = p1;
			V2 = p2;
			V3 = p3;
			Array.Sort(Vertices, (lhs, rhs) => {
				var r = lhs.Value.X.CompareTo(rhs.Value.X);
				return r != 0 ? r : -lhs.Value.Y.CompareTo(rhs.Value.X);
			});
			if (Area() < 0) {
				var tmp = V2;
				V2 = V3;
				V3 = tmp;
			}
			Debug.Assert(Area() >= 0, "Points are ordered counter-clockwise!");
		}

		public PointF? V1
		{
			get => Vertices[0];
			set => Vertices[0] = value;
		}

		public PointF? V2
		{
			get => Vertices[1];
			set => Vertices[1] = value;
		}

		public PointF? V3
		{
			get => Vertices[2];
			set => Vertices[2] = value;
		}

		public ConnectedTriangle T1
		{
			get => Triangles[0];
			set => Triangles[0] = value;
		}

		public ConnectedTriangle T2
		{
			get => Triangles[1];
			set => Triangles[1] = value;
		}

		public ConnectedTriangle T3
		{
			get => Triangles[2];
			set => Triangles[2] = value;
		}

		public int O1
		{
			get => Orientations[0];
			set => Orientations[0] = value;
		}

		public int O2
		{
			get => Orientations[1];
			set => Orientations[1] = value;
		}

		public int O3
		{
			get => Orientations[2];
			set => Orientations[2] = value;
		}

		public bool CircumcircleContains(PointF p)
		{
			PointF v1 = V1.Value, v2 = V2.Value, v3 = V3.Value;
			var n1 = (double)v1.Norm2();
			var n2 = (double)v2.Norm2();
			var n3 = (double)v3.Norm2();
			var a = (double)v1.X * v2.Y + (double)v2.X * v3.Y + (double)v3.X * v1.Y - ((double)v2.Y * v3.X + (double)v3.Y * v1.X + (double)v1.Y * v2.X);
			var b = n1 * v2.Y + n2 * v3.Y + n3 * v1.Y - (v2.Y * n3 + v3.Y * n1 + v1.Y * n2);
			var c = n1 * v2.X + n2 * v3.X + n3 * v1.X - (v2.X * n3 + v3.X * n1 + v1.X * n2);
			var d = n1 * v2.X * v3.Y + n2 * v3.X * v1.Y + n3 * v1.X * v2.Y - (v2.X * n3 * v1.Y + v3.X * n1 * v2.Y + v1.X * n2 * v3.Y);

			return (a * p.Norm2() - b * p.X + c * p.Y - d) * Math.Sign(a) < 0;
		}

		public float Area()
		{
			return (V2.Value.X - V1.Value.X) * (V3.Value.Y - V1.Value.Y) -
			       (V2.Value.Y - V1.Value.Y) * (V3.Value.X - V1.Value.X);
		}

		public bool Contains(PointF vertex)
		{
			return V1.Value.Equals(vertex) || V2.Value.Equals(vertex) || V3.Value.Equals(vertex);
		}

		public bool SelfCheck()
		{
			for (int i = 0; i < 3; i++) {
				if (Triangles[i] != null && !ReferenceEquals(Triangles[i].Triangles[Orientations[i]], this)) {
					return false;
				}
			}
			return true;
		}
	}
}
