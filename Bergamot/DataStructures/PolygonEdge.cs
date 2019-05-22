using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace Bergamot.DataStructures
{
	public class PolygonEdge : IEquatable<PolygonEdge>
	{
		// Triangle that shares edge with polygon
		public ConnectedTriangle Triangle;
		// Endpoints
		public PointF V1, V2;
		public PolygonEdge Next;
		public int Orientation;

		public PolygonEdge(PointF v1, PointF v2)
		{
			V1 = v1;
			V2 = v2;
		}

		public PolygonEdge(int i, ConnectedTriangle t)
		{
			V1 = t.Vertices[i].Value;
			V2 = t.Vertices[(i + 1) % 3].Value;
			Orientation = t.Orientations[(i + 2) % 3];
			Triangle = t.Triangles[(i + 2) % 3];
		}

		// prev is a triangle that is build on counter clockwise edge connected to this
		public ConnectedTriangle CreateTriangle(PointF vertex, ConnectedTriangle prev)
		{
			var t = new ConnectedTriangle(vertex, V1, V2);
			var i = Array.IndexOf(t.Vertices, vertex);
			t.Triangles[i] = Triangle;
			if (Triangle != null) {
				Triangle.Triangles[Orientation] = t;
				t.Orientations[i] = Orientation;
				Triangle.Orientations[Orientation] = i;
				Debug.Assert(Triangle.SelfCheck());
			}
			t.Triangles[(i + 2) % 3] = prev;
			if (prev != null) {
				var j = Array.IndexOf(prev.Vertices, vertex);
				prev.Triangles[(j + 1) % 3] = t;
				prev.Orientations[(j + 1) % 3] = (i + 2) % 3;
				t.Orientations[(i + 2) % 3] = (j + 1) % 3;
				Debug.Assert(prev.SelfCheck());
			}
			Debug.Assert(t.SelfCheck());
			return t;
		}

		public List<ConnectedTriangle> Triangulate(PointF vertex)
		{
			PolygonEdge current = this;
			ConnectedTriangle prevT = null, fixme = null;
			var triangles = new List<ConnectedTriangle>(3);
			do {
				prevT = current.CreateTriangle(vertex, prevT);
				if (prevT.Area() == 0) {
					return null;
				}
				triangles.Add(prevT);
				fixme = fixme ?? prevT;
				current = current.Next;
			} while (!ReferenceEquals(current, this));
			var i = Array.IndexOf(fixme.Vertices, vertex);
			var j = Array.IndexOf(prevT.Vertices, vertex);
			prevT.Triangles[(j + 1) % 3] = fixme;
			prevT.Orientations[(j + 1) % 3] = (i + 2) % 3;
			fixme.Orientations[(i + 2) % 3] = (j + 1) % 3;
			fixme.Triangles[(i + 2) % 3] = prevT;
			Debug.Assert(prevT.SelfCheck());
			Debug.Assert(fixme.SelfCheck());
			return triangles;
		}

		public bool Equals(PolygonEdge other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return V1.Equals(other.V1) && V2.Equals(other.V2) || V2.Equals(other.V1) && V1.Equals(other.V2);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((PolygonEdge) obj);
		}

		public override int GetHashCode()
		{
			unchecked {
				return (V1.X.GetHashCode() ^ V2.X.GetHashCode()) + (V1.Y.GetHashCode() ^ V2.Y.GetHashCode());
			}
		}

		public PolygonEdge Flip()
		{
			var tmp = V1;
			V1 = V2;
			V2 = tmp;
			return this;
		}

		public PolygonEdge Clone()
		{
			return new PolygonEdge(V1, V2) { Next = Next, Orientation = Orientation, Triangle = Triangle };
		}
	}
}
