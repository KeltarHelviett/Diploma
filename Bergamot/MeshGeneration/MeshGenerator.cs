using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using Bergamot.DataStructures;
using Bergamot.Extensions;
using static Bergamot.NonConvexHullGeneration.NonConvexHull;

namespace Bergamot.MeshGeneration
{
	public static class MeshGenerator
	{
		enum SegmentsIntersectionResult
		{
			Collinear, None, Point,
		}

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

		//private static PointF Intersect(PointF a1, PointF a2, PointF b1, PointF b2)
		//{
		//	var denominator = (a1.X - a2.X) * (b1.Y - b2.Y) - (a1.Y - a2.Y) * (b1.X - b2.X);
		//	return Math.Abs(denominator) < 1e-4 ? Point.Empty : new PointF(
		//			(float)(((a1.X * a2.Y - a1.Y * a2.X) * (b1.X - b2.X) - (b1.X * b2.Y - b1.Y * b2.X) * (a1.X - a2.X)) /
		//			(double)denominator),
		//			(float)(((a1.X * a2.Y - a1.Y * a2.X) * (b1.Y - b2.Y) - (b1.X * b2.Y - b1.Y * b2.X) * (a1.Y - a2.Y)) /
		//			(double)denominator)
		//	);
		//}

		private static SegmentsIntersectionResult Intersect(PointF a1, PointF a2, PointF b1, PointF b2, out PointF intersection)
		{
			// p + tr = q + us
			PointF p = a1, q = b1, r = a2.Sub(a1), s = b2.Sub(b1);
			intersection = PointF.Empty;
			var denominator = r.Cross(s);
			var qsubp = q.Sub(p);
			var uNumerator = qsubp.Cross(r);
			if (Math.Abs(denominator) < 1e-4) {
				return Math.Abs(uNumerator) < 1e-4 ? SegmentsIntersectionResult.Collinear : SegmentsIntersectionResult.None;
			}
			var u = uNumerator / (double)denominator;
			var t = qsubp.Cross(s) / (double)denominator;
			intersection = t <= 1 && t >= 0 && u <= 1 && u >= 0 ? p.Add(r.Mul((float)t)) : intersection;
			return intersection.IsEmpty ? SegmentsIntersectionResult.None : SegmentsIntersectionResult.Point;
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
					Debug.Assert(polygon.PushBack(new PolygonEdge(i % 3, triangle)), "Failed to insert edge into polygon");
#else
					polygon.PushBack(new PolygonEdge(i, triangle))
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

		private static void DebugTriangulationStep(ICollection<ConnectedTriangle> triangles, Polygon polygon,
			Polygon polygon2, Segment segment, int step)
		{
			var black = new Pen(Color.Black);
			var green = new Pen(Color.Green);
			var blue = new Pen(Color.Blue);
			using (var image = new Bitmap(2600, 1630)) {
				using (var g = Graphics.FromImage(image)) {
					foreach (var triangle in triangles) {
						g.DrawPolygon(black, triangle.Vertices.Select(v => v.Value).ToArray());
					}
					var current = polygon?.Start;
					if (current != null) {
						do {
							g.DrawLine(green, current.V1, current.V2);
							current = current.Next;
						} while (current != null && !ReferenceEquals(current, polygon.Start));
					}
					current = polygon2?.Start;
					if (current != null) {
						do {
							g.DrawLine(green, current.V1, current.V2);
							current = current.Next;
						} while (current != null && !ReferenceEquals(current, polygon2.Start));
					}
					g.DrawLine(blue, segment.A, segment.B);
				}
				image.Save($@"D:\GIT\Samples\{step}.png");
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

		public /*private*/ static HashSet<ConnectedTriangle> BowyerWatson(List<PointF> points, List<ConnectedTriangle> supers)
		{
			var triangulation = new HashSet<ConnectedTriangle>();
			var badTriangles = new HashSet<ConnectedTriangle>();
			foreach (var super in supers) {
				triangulation.Add(super);
			}
			ConnectedTriangle lastAdded = triangulation.First();
			var j = 0;
			foreach (var point in points) {
				badTriangles.Clear();
				// find triangle that contains given point
				var t = FindTriangle(lastAdded, point);
				// construct contour polygon from all adjacent triangles
				// that does not satisfy Delaunay property
				var polygon = GetContourPolygon(t, point, badTriangles);
				var triangles = polygon.Triangulate(point);
				if (triangles == null) {
					lastAdded = badTriangles.First();
					foreach (var badTriangle in badTriangles) {
						badTriangle.Restore();
					}
					Debug.WriteLine($"Bad point {point}");
					continue;
				}
				foreach (var badTriangle in badTriangles) {
					badTriangle.Detach();
					triangulation.Remove(badTriangle);
				}
				lastAdded = triangles[0];
				foreach (var triangle in triangles) {
					triangulation.Add(triangle);
				}
				if (Options.Instance.RuntimeChecks) {
					//DebugTriangulationStep(point, triangulation, j++);
					//ShowPolygon(polygon, point);
				}
			}
			//DebugTriangulationStep(new PointF(0, 0), triangulation, j++);
			if (Options.Instance.RuntimeChecks) {
				//Debug.Assert(CheckDelaunayProperty(triangulation), "Triangulation doesn't satisfy Delaunay property!");
			}
			return triangulation;
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

		public static (ConnectedTriangle, Polygon, Polygon, int) FindStartTriangle(HashSet<ConnectedTriangle> triangulation,
			Segment segment, ConnectedTriangle searchStart = null)
		{
			var queue = new Queue<(ConnectedTriangle, int)>();
			queue.Enqueue((FindTriangle(searchStart ?? triangulation.First(), segment.A), -1));
			var hasEdge = false;
			// Vertex that is opposite to the edge of intersection
			int vertexIndex = -1;
			ConnectedTriangle found = null;
			// Find start triangle: the first one that has intersection with segment
			// not in the segment.A
			while (queue.Count > 0) {
				var (current, cameFrom) = queue.Dequeue();
				var i = Array.IndexOf(current.Vertices, segment.A);
				Debug.Assert(i >= 0);
				// Check if edge is already presented in the triangulation
				for (int j = -1; j <= 0; ++j) {
					PointF v1 = current.Vertices[(i + j).EuclideanMod(3)].Value,
						v2 = current.Vertices[(i + j + 1).EuclideanMod(3)].Value;
					if ((v1 == segment.A || v2 == segment.A) && (v1 == segment.B || v2 == segment.B)) {
						hasEdge = true;
					}
				}
				if (hasEdge) {
					break;
				}
				var r = Intersect(segment.A, segment.B, current.Vertices[(i + 1) % 3].Value,
					current.Vertices[(i + 2) % 3].Value, out var p);
				if (r == SegmentsIntersectionResult.Point) {
					vertexIndex = i;
					found = current;
					break;
				}
				var iplus1mod3 = (i + 1) % 3;
				if (iplus1mod3 != cameFrom && current.Triangles[iplus1mod3] != null) {
					queue.Enqueue((current.Triangles[iplus1mod3], current.Orientations[iplus1mod3]));
				}
				var iplus2mod3 = (i + 2) % 3;
				if (iplus2mod3 != cameFrom && current.Triangles[iplus2mod3] != null) {
					queue.Enqueue((current.Triangles[iplus2mod3], current.Orientations[iplus2mod3]));
				}
			}
			if (hasEdge) {
				Console.WriteLine($"Segment {{{segment.A}, {segment.B}}} is already presented");
				return (null, null, null, -1);
			}
			Debug.Assert(found != null);
			var polygon = new Polygon();
			polygon.PushBack(new PolygonEdge(vertexIndex, found));
			var polygon2 = new Polygon();
			polygon2.PushFront(new PolygonEdge((vertexIndex + 2) % 3, found));
			return (found, polygon, polygon2, vertexIndex);
		}

		// returns last triangle on the way so the next step can begin from the last end
		public static ConnectedTriangle Traverse(Polygon polygon, Polygon polygon2, Segment segment, ConnectedTriangle triangle,
			int cameFrom, HashSet<ConnectedTriangle> triangulation)
		{
			while (true) {
				var ok = false;
				// It's probably better to unroll this loop
				for (int j = -1; j <= 0; j++) {
					PointF v1 = triangle.Vertices[(cameFrom + j).EuclideanMod(3)].Value,
						v2 = triangle.Vertices[(cameFrom + j + 1) % 3].Value;
					var r = Intersect(segment.A, segment.B, v1, v2, out var p);
					if (r == SegmentsIntersectionResult.Point) {
						// Check if p is one of the vertices
						// than finish the polygon and split AB edge to Ap and pB
						// and repeat the same procedure for new edge
						// If p == B than complete polygon and stop traversing
						triangulation.Remove(triangle);
						if (p == segment.B) {
							polygon.PushBack(new PolygonEdge((cameFrom + 2) % 3, triangle));
							polygon2.PushFront(new PolygonEdge(cameFrom, triangle));
							polygon.PushBack(new PolygonEdge(segment.B, segment.A));
							polygon2.PushFront(new PolygonEdge(segment.A, segment.B));
							triangle.Detach();
							Debug.Assert(polygon.TryClose() && polygon2.TryClose());
							ConnectedTriangle last = null;
							ShowPolygon(polygon, segment.A);
							var tp = polygon.Triangulate();
							foreach (var t in tp) {
								triangulation.Add(t);
								if (t.Contains(segment.A) && t.Contains(segment.B)) {
									last = t;
								}
							}
							ConnectedTriangle last2 = null;
							var tp2 = polygon2.Triangulate();
							foreach (var t in tp2) {
								triangulation.Add(t);
								if (t.Contains(segment.A) && t.Contains(segment.B)) {
									last2 = t;
								}
							}
							Debug.Assert(last != null && last2 != null);
							Debug.Assert(last.Contains(segment.A) && last.Contains(segment.B) &&
										last2.Contains(segment.A) && last2.Contains(segment.B));
							var i = (Array.IndexOf(last.Vertices, segment.A) + 1) % 3;
							var k = (Array.IndexOf(last2.Vertices, segment.B) + 1) % 3;
							last2.Orientations[k] = i;
							last.Orientations[i] = k;
							last2.Triangles[k] = last;
							last.Triangles[i] = last2;
							Debug.Assert(last2.SelfCheck() && last.SelfCheck());
							//foreach (var t in tp) {
							//	foreach (var tt in t.Triangles) {
							//		if (tt == null) {
							//			Console.WriteLine("Warn");
							//		}
							//	}
							//}
							return last;
						}
						for (int i = 0; i < 3; i++) {
							if (p == triangle.Vertices[i]) {
								polygon.PushBack(new PolygonEdge((cameFrom + 2) % 3, triangle));
								polygon2.PushFront(new PolygonEdge(cameFrom, triangle));
								polygon.PushBack(new PolygonEdge(triangle.Vertices[i].Value, segment.A));
								polygon2.PushFront(new PolygonEdge(segment.A, triangle.Vertices[i].Value));
								polygon.TryClose();
								Debug.Assert(polygon.TryClose());
								ShowPolygon(polygon, segment.A);
								ConnectedTriangle last = null, last2 = null;
								foreach (var t in polygon.Triangulate()) {
									triangulation.Add(t);
								}
								foreach (var t in polygon2.Triangulate()) {
									triangulation.Add(t);
								}
								Debug.Assert(last.Contains(segment.A) && last.Contains(segment.B) &&
								             last2.Contains(segment.A) && last2.Contains(segment.B));
								var l = (Array.IndexOf(last.Vertices, segment.A) + 1) % 3;
								var k = (Array.IndexOf(last2.Vertices, triangle.Vertices[i].Value) + 1) % 3;
								last2.Orientations[k] = l;
								last.Orientations[l] = k;
								last2.Triangles[k] = last;
								last.Triangles[l] = last2;
								Debug.Assert(last2.SelfCheck() && last.SelfCheck());
								var s = new Segment(new Point((int)p.X, (int)p.Y), segment.B);
								var (start, newPolygon, newPolygon2, vi) = FindStartTriangle(triangulation, s);
								if (newPolygon != null) {
									var res = Traverse(newPolygon, newPolygon2, s, start.Triangles[vi], start.Orientations[vi],
										triangulation);
									start.Detach();
									triangulation.Remove(start);
									return res;
								}
								return triangulation.First();
							}
						}
						// Otherwise we have intersection with edge
						// we complete the polygon and move to the adjacent triangle
						if ((cameFrom + 2) % 3 != (cameFrom + j).EuclideanMod(3)) {
							polygon.PushBack(new PolygonEdge((cameFrom + 2) % 3, triangle));
						}
						if (cameFrom != (cameFrom + j).EuclideanMod(3)) {
							polygon2.PushFront(new PolygonEdge(cameFrom, triangle));
						}
						(triangle, cameFrom) = (triangle.Triangles[(cameFrom + j + 2) % 3],
							triangle.Orientations[(cameFrom + j + 2) % 3]);
						ok = true;
						break;
					}
				}
				Debug.Assert(ok);
			}
		}

		public static ICollection<ConnectedTriangle> ConstrainedDelaunay(Bitmap bitmap, List<Segment> hull)
		{
			var supers = SuperSquare(bitmap);
			var triangulation = BowyerWatson(hull.Select(s => new PointF(s.A.X, s.A.Y)).ToList(), supers);
			// Find any triangle that contains that first segment A point
			var current = triangulation.First();
			var i = 0;
			foreach (var segment in hull) {
				var (start, polygon, polygon2, vertexIndex) = FindStartTriangle(triangulation, segment, current);
				if (polygon != null) {
					current = Traverse(polygon, polygon2, segment, start.Triangles[vertexIndex], start.Orientations[vertexIndex],
						triangulation);
					start.Detach();
					triangulation.Remove(start);
				}
				DebugTriangulationStep(triangulation, polygon, polygon2, segment, i++);
			}
			triangulation.RemoveWhere(t => {
				var res = supers.Any(s => s.Contains(t.V1.Value) || s.Contains(t.V2.Value) || s.Contains(t.V3.Value));
				if (res) {
					t.Detach();
				}
				return res;
			});
			return triangulation;
		}

		public static ICollection<ConnectedTriangle> ConstrainedDelaunay(List<PointF> hull, List<ConnectedTriangle> supers, List<Segment> constrains)
		{
			var triangulation = BowyerWatson(hull, supers);
			var i = 0;
			// Find any triangle that contains that first segment A point
			var current = triangulation.First();
			foreach (var segment in constrains) {
				var (start, polygon, polygon2, vertexIndex) = FindStartTriangle(triangulation, segment, current);
				if (polygon != null) {
					current = Traverse(polygon, polygon2, segment, start.Triangles[vertexIndex], start.Orientations[vertexIndex],
						triangulation);
					start.Detach();
					triangulation.Remove(start);
				}
				DebugTriangulationStep(triangulation, polygon, polygon2, segment, i++);
			}
			return triangulation;
		}
	}
}
