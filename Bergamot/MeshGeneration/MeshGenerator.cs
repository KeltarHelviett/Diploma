using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Bergamot.DataStructures;
using Bergamot.Extensions;
using static Bergamot.NonConvexHullGeneration.NonConvexHull;

namespace Bergamot.MeshGeneration
{
	public static class MeshGenerator
	{
		// Returns true if segments collinear or intersects
		// But when they intersects with endpoints false is returned
		private static bool Intersect(Segment lhs, Segment rhs)
		{
			// p + tr = q + us
			Point p = lhs.A, q = rhs.A, r = lhs.B.Sub(lhs.A), s = rhs.B.Sub(rhs.A);
			var denominator = r.Cross(s);
			var qsubp = q.Sub(p);
			var uNumerator = qsubp.Cross(r);
			if (denominator == 0) {
				return uNumerator == 0; // true if collinear false if parallel
			}
			var u = uNumerator / (double)denominator;
			var t = qsubp.Cross(s) / (double)denominator;
			if ((Math.Abs(t) < 1e-4 || Math.Abs(t - 1.0) < 1e-4) && (Math.Abs(u) < 1e-4 || Math.Abs(u - 1.0) < 1e-4)) {
				return false;
			}
			return t <= 1 && t >= 0 && u <= 1 && u >= 0;
		}

		public static void BadTriangulation(Bitmap image, List<Segment> hull)
		{
			var rnd = new Random(123123);
			var segments = new List<Segment>();
			using (var g = Graphics.FromImage(image)) {
				for (int i = 0; i < hull.Count; i++) {
					var pen = new Pen(Color.FromArgb(rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255)), 1f);
					var current = hull[i];
					var prev = hull[(i - 1).EuclideanMod(hull.Count)];
					var j = (i + 2) % hull.Count;
					while (j != i) {
						var s = hull[j];
						if (IsOnNormalDirection(current.A, current.B, s.A) && IsOnNormalDirection(prev.A, prev.B, s.A)) {
							var targetSegment = new Segment(current.A, s.A);
							if (!hull.Any(segment => Intersect(segment, targetSegment)) && !segments.Any(segment => Intersect(segment, targetSegment))) {
								g.DrawLine(pen, current.A, s.A);
								segments.Add(new Segment(current.A, s.A));
							}
						}
						j = (j + 1).EuclideanMod(hull.Count);
					}
				}
			}
		}

		private static void DebugTriangulationStep(PointF vertex, ICollection<ConnectedTriangle> triangles, int step)
		{
			var red = new Pen(Color.Red, 2);
			var black = new Pen(Color.Black);
			using (var image = new Bitmap(2600, 1630)) {
				using (var g = Graphics.FromImage(image)) {
					foreach (var triangle in triangles) {
						g.DrawPolygon(black, triangle.Vertices.Select(v => v.Value).ToArray());
					}
					g.DrawEllipse(red, vertex.X, vertex.Y, 2, 2);
				}
				image.Save($@"D:\GIT\Samples\{step}.png");
			}
		}

		public static ConnectedTriangle FindTriangle(ConnectedTriangle start, PointF point)
		{
			ConnectedTriangle prev = null;
			while (prev != start) {
				prev = start;
				for (int i = 0; i < 3; i++) {
					var line = start.Vertices[(i + 1) % 3].Value.Sub(start.Vertices[i].Value);
					var vec = point.Sub(start.Vertices[i].Value);
					var vec2 = start.Vertices[(i + 2) % 3].Value.Sub(start.Vertices[i].Value);
					if (Math.Sign(line.Cross(vec)) * Math.Sign(line.Cross(vec2)) < 0 && start.Triangles[(i + 2) % 3] != null) {
						start = start.Triangles[(i + 2) % 3];
						break;
					}
				}
			}
			return start;
		}

		public static void ClockwiseTrace(ConnectedTriangle triangle, int cameFrom, Polygon polygon, PointF point, ICollection<ConnectedTriangle> used)
		{
			used.Add(triangle);
			for (int i = (cameFrom - 2).EuclideanMod(3); i < (cameFrom - 2).EuclideanMod(3) + 3; i++) {
				var t = triangle.Triangles[(i + 2) % 3];
				if (t != null && used.Contains(t)) {
					continue;
				}
				if (t != null && t.CircumcircleContains(point)) {
					ClockwiseTrace(t, triangle.Orientations[(i + 2) % 3], polygon, point, used);
				} else {
#if DEBUG
					Debug.Assert(polygon.InsertEdge(new PolygonEdge(i % 3, triangle)), "Failed to insert edge into polygon");
#else
					polygon.InsertEdge(new PolygonEdge(i, triangle))
#endif
				}
			}
		}

		public static void ShowPolygon(Polygon polygon, PointF point)
		{
			using (var image = new Bitmap(2600, 1630)) {
				var current = polygon.Start;
				var red = new Pen(Color.DarkRed);
				var green = new Pen(Color.Green);
				using (var g = Graphics.FromImage(image)) {
					do {
						g.DrawLine(green, current.V1, current.V2);
						current = current.Next;
					} while (current != null && current != polygon.Start);
					g.DrawEllipse(red, point.X, point.Y, 2, 2);
				}
				image.Save($@"D:\GIT\Samples\p.png");
			}
		}

		public static Polygon GetContourPolygon(ConnectedTriangle start, PointF point, ICollection<ConnectedTriangle> badTriangles)
		{
			var polygon = new Polygon();
			ClockwiseTrace(start, 2, polygon, point, badTriangles);
#if DEBUG
			Debug.Assert(polygon.TryClose(), "Polygon isn't closed");
#else
			polygon.TryClose();
#endif
			return polygon;
		}

		private static HashSet<ConnectedTriangle> BowyerWatson(List<PointF> points, List<ConnectedTriangle> supers)
		{
			var triangles = new HashSet<ConnectedTriangle>();
			var badTriangles = new HashSet<ConnectedTriangle>();
			foreach (var super in supers) {
				triangles.Add(super);
			}
			ConnectedTriangle lastAdded = triangles.First();
			var j = 0;
			foreach (var point in points) {
				badTriangles.Clear();
				// find triangle that contains given point
				var t = FindTriangle(lastAdded, point);
				// construct contour polygon from all adjacent triangles
				// that does not satisfy Delaunay property
				var polygon = GetContourPolygon(t, point, badTriangles);
				foreach (var badTriangle in badTriangles) {
					triangles.Remove(badTriangle);
				}
				lastAdded = polygon.Triangulate(point, triangles);
				if (Options.Instance.RuntimeChecks) {
					DebugTriangulationStep(point, triangles, j++);
					ShowPolygon(polygon, point);
				}
			}
			triangles.RemoveWhere(t => supers.Any(s => s.Contains(t.V1.Value) || s.Contains(t.V2.Value) || s.Contains(t.V3.Value)));
			if (Options.Instance.RuntimeChecks) {
				Debug.Assert(CheckDelaunayProperty(triangles), "Triangulation doesn't satisfy Delaunay property!");
			}
			return triangles;
		}

		private static bool CheckDelaunayProperty(ICollection<ConnectedTriangle> triangulation)
		{
			foreach (var triangle in triangulation) {
				foreach (var other in triangulation) {
					if (triangle != other) {
						foreach (var vertex in other.Vertices) {
							if (
								Array.IndexOf(triangle.Vertices, vertex) < 0 && vertex.HasValue &&
								triangle.CircumcircleContains(vertex.Value)
							) {
								return false;
							}
						}
					}
				}
			}
			return true;
		}

		public static List<ConnectedTriangle> SuperSquare(Bitmap bitmap)
		{
			var t1 = new ConnectedTriangle(new PointF(0, bitmap.Height), new PointF(0, 0), new PointF(bitmap.Width, 0));
			var t2 = new ConnectedTriangle(new PointF(0, bitmap.Height), new PointF(bitmap.Width, 0),
				new PointF(bitmap.Width, bitmap.Height));
			t2.T3 = t1;
			t2.O3 = 1;
			t1.T2 = t2;
			t1.O2 = 2;
			Debug.Assert(t2.SelfCheck() && t1.SelfCheck());
			return new List<ConnectedTriangle> { t1, t2, };
		}

		public static List<ConnectedTriangle> SuperSquare(List<Point> points)
		{
			PointF min = points[0], max = points[0];
			foreach (var point in points) {
				min.X = Math.Min(min.X, point.X);
				min.Y = Math.Min(min.Y, point.Y);
				max.X = Math.Max(max.X, point.X);
				max.Y = Math.Max(max.Y, point.Y);
			}
			return new List<ConnectedTriangle> {
				new ConnectedTriangle(new PointF(min.X, max.Y), new PointF(min.X, min.Y), new PointF(max.X, min.Y)),
				new ConnectedTriangle(new PointF(min.X, max.Y), new PointF(max.X, min.Y), new PointF(max.X, max.Y)),
			};
		}

		public static ICollection<ConnectedTriangle> Delaunay(List<Point> points) => 
			BowyerWatson(points.Select(p => new PointF(p.X, p.Y)).ToList(), SuperSquare(points));

		public static ICollection<ConnectedTriangle> Delaunay(Bitmap bitmap, List<Point> points) => 
			BowyerWatson(points.Select(p => new PointF(p.X, p.Y)).ToList(), SuperSquare(bitmap));
	}
}
