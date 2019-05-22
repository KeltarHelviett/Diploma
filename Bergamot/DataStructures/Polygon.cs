using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Bergamot.Extensions;

namespace Bergamot.DataStructures
{
	public class Polygon
	{
		public PolygonEdge Start { get; set; }
		public PolygonEdge End { get; set; }

		public bool PushBack(PolygonEdge edge)
		{
			if (Start == null) {
				Start = edge;
				End = edge;
				return true;
			}
			if (End.V2 == edge.V1) {
				End.Next = edge;
				End = edge;
				return true;
			}
			return false;
		}

		public bool PushFront(PolygonEdge edge)
		{
			if (Start == null) {
				Start = edge;
				End = edge;
				return true;
			}
			if (edge.V2 == Start.V1) {
				edge.Next = Start;
				Start = edge;
				return true;
			}
			return false;
		}

		public bool TryClose()
		{
			if (End.V2 == Start.V1) {
				End.Next = Start;
				return true;
			}
			return false;
		}

		public List<ConnectedTriangle> Triangulate(PointF point)
		{
			return Start.Triangulate(point);
		}

		public (ConnectedTriangle, PolygonEdge) CreateTriangle(PolygonEdge e1, PolygonEdge e2)
		{
			var ct = new ConnectedTriangle(e1.V1, e2.V1, e2.V2);
			var i = (Array.IndexOf(ct.Vertices, e1.V1) + 2) % 3;
			ct.Triangles[i] = e1.Triangle;
			if (e1.Triangle != null) {
				e1.Triangle.Triangles[e1.Orientation] = ct;
				e1.Triangle.Orientations[e1.Orientation] = i;
				ct.Orientations[i] = e1.Orientation;
			}
			i = (Array.IndexOf(ct.Vertices, e2.V1) + 2) % 3;
			ct.Triangles[i] = e2.Triangle;
			if (e2.Triangle != null) {
				e2.Triangle.Triangles[e2.Orientation] = ct;
				e2.Triangle.Orientations[e2.Orientation] = i;
				ct.Orientations[i] = e2.Orientation;
			}
			i = Array.IndexOf(ct.Vertices, e2.V2) % 3;
			var edge = new PolygonEdge(i, ct) {
				Triangle = ct,
				Orientation = (i + 2) % 3,
			};
			return (ct, edge.Flip());
		}

		// if it's convex regarding to polygon
		private bool CanCreateTriangle(PolygonEdge e1, PolygonEdge e2) => e1.V2.Sub(e1.V1).Cross(e2.V2.Sub(e2.V1)) < 0;

		// Completely changes data structure
		public List<ConnectedTriangle> Triangulate()
		{
			//PolygonEdge prev = Start, current = Start.Next;
			//ConnectedTriangle t = null;
			// Do not change d
			var clone = Clone();
			var res = new List<ConnectedTriangle>();
			PolygonEdge prev = clone.Start, current = clone.Start.Next, prePrev = clone.End;
			var save = current.Next;
			while (!prev.Equals(current)) {
				if (!CanCreateTriangle(prev, current)) {
					var (ct, e) = CreateTriangle(prev, current);
					save = current.Next;
					Debug.Assert(ct.SelfCheck());
					res.Add(ct);
					prePrev.Next = e;
					e.Next = current.Next;
					Debug.Assert(prePrev.V2 == e.V1 && e.V2 == current.Next.V1);
					if (ReferenceEquals(prev, clone.Start)) {
						clone.Start = e;
					}
					prev = clone.Start;
					current = clone.Start.Next;
					prePrev = clone.End;
				} else {
					prePrev = prev;
					prev = current;
					current = current.Next;
				}
			}
			var fix = res[res.Count - 1];
			var i = Array.FindIndex(fix.Vertices, f => f != save.V1 && f != save.V2);
			Debug.Assert(i >= 0);
			fix.Triangles[i] = save.Triangle;
			if (save.Triangle != null) {
				save.Triangle.Triangles[save.Orientation] = fix;
				save.Triangle.Orientations[save.Orientation] = i;
				fix.Orientations[i] = save.Orientation;
			}
			Debug.Assert(fix.SelfCheck());
			return res;
		}

		public bool IsConvex()
		{
			if (!ReferenceEquals(Start, End.Next)) {
				return false;
			}
			PolygonEdge prev = Start, current = Start.Next;
			do {
				PointF v1 = prev.V2.Sub(prev.V1),
					v2 = current.V2.Sub(current.V1);
				if (v1.Cross(v2) < 0) {
					return false;
				}
			} while (!ReferenceEquals(current, Start.Next));
			return true;
		}

		public Polygon Clone()
		{
			var polygon = new Polygon();
			var current = Start;
			do {
				polygon.PushBack(current.Clone());
				current = current.Next;
			} while (!ReferenceEquals(current, Start) && current != null);
			return polygon;
		}
	}
}
