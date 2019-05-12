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
		public PolygonEdge Prev;
		public int Orientation;

		public PolygonEdge(int i, ConnectedTriangle t)
		{
			V1 = t.Vertices[i].Value;
			V2 = t.Vertices[(i + 1) % 3].Value;
			Orientation = t.Orientations[(i + 2) % 3];
			Triangle = t.Triangles[(i + 2) % 3];
		}

		public bool RemoveIfContains(PolygonEdge edge, out PolygonEdge start)
		{
			var current = this;
			start = this;
			do {
				if (edge.Equals(current)) {
					start = current != this ? current : current.Prev ?? current.Next;
					if (current.Prev != null) {
						current.Prev.Next = current.Next;
					}
					current.Next = null;
					current.Prev = null;
					return true;
				}
				current = current.Next;
			} while (current != null && current != this);
			return false;
		}

		public bool ConnectEdge(PolygonEdge edge)
		{
			return (V2 == edge.V1 ? Next = edge : null) != null;
		}

		public bool TryClosePolygon()
		{
			var current = this;
			PolygonEdge prev = null;
			do {
				prev = current;
				current = current.Next;
				if (current != null && prev != null) {
					Debug.Assert(prev.V2 == current.V1, "Inconsistent polygon");
				}
			} while (current != null && current != this);

			if (prev.V2 == V1) {
				prev.Next = current;
				return true;
			}
			return false;
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
			}
			t.Triangles[(i + 2) % 3] = prev;
			if (prev != null) {
				var j = Array.IndexOf(prev.Vertices, vertex);
				prev.Triangles[(j + 1) % 3] = t;
				prev.Orientations[(j + 1) % 3] = (i + 2) % 3;
				t.Orientations[(i + 2) % 3] = (j + 1) % 3;
			}
			return t;
		}

		public void Triangulate(PointF vertex, ICollection<ConnectedTriangle> triangulation)
		{
			PolygonEdge current = this;
			ConnectedTriangle prevT = null, fixme = null;
			do {
				prevT = current.CreateTriangle(vertex, prevT);
				triangulation.Add(prevT);
				fixme = fixme ?? prevT;
				current = current.Next;
			} while (current != this);
			var i = Array.IndexOf(fixme.Vertices, vertex);
			var j = Array.IndexOf(prevT.Vertices, vertex);
			prevT.Triangles[(j + 1) % 3] = fixme;
			prevT.Orientations[(j + 1) % 3] = (i + 2) % 3;
			fixme.Orientations[(i + 2) % 3] = (j + 1) % 3;
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
	}
}
