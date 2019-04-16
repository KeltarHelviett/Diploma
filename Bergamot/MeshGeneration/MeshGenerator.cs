using System;
using System.Collections.Generic;
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

		private static HashSet<Triangle> BowyerWatson(List<PointF> points, List<Triangle> supers)
		{
			var triangles = new HashSet<Triangle>();
			var polygon = new HashSet<Edge>();
			var badTriangles = new HashSet<Triangle>();
			foreach (var super in supers) {
				triangles.Add(super);
			}
			int i = 0;
			foreach (var point in points) {
				badTriangles.Clear();
				polygon.Clear();
				foreach (var triangle in triangles) {
					if (triangle.CircumcircleContains(point)) {
						badTriangles.Add(triangle);
						foreach (var e in triangle.Edges) {
							if (!polygon.Contains(e)) {
								polygon.Add(e);
							} else {
								polygon.Remove(e);
							}
						}
					}
				}
				foreach (var badTriangle in badTriangles) {
					triangles.Remove(badTriangle);
				}
				foreach (var edge in polygon) {
					triangles.Add(new Triangle(edge.A, edge.B, point));
				}
			}
			triangles.RemoveWhere(t => supers.Any(s => s.Contains(t.V1) || s.Contains(t.V2) || s.Contains(t.V3)));
			return triangles;
		}

		public static List<Triangle> SuperSquare(Bitmap bitmap) => new List<Triangle> {
			new Triangle(new PointF(0, 0), new PointF(bitmap.Width, 0), new PointF(0, bitmap.Height)),
			new Triangle(new PointF(bitmap.Width, bitmap.Height), new PointF(bitmap.Width, 0), new PointF(0, bitmap.Height)),
		};

		public static List<Triangle> SuperSquare(List<Point> points)
		{
			PointF min = points[0], max = points[0];
			foreach (var point in points) {
				min.X = Math.Min(min.X, point.X);
				min.Y = Math.Min(min.Y, point.Y);
				max.X = Math.Max(max.X, point.X);
				max.Y = Math.Max(max.Y, point.Y);
			}
			return new List<Triangle> {
				new Triangle(new PointF(min.X, min.Y), new PointF(max.X, min.Y), new PointF(min.X, max.Y)),
				new Triangle(new PointF(max.X, max.Y), new PointF(max.X, min.Y), new PointF(min.X, max.Y)),
			};
		}

		public static ICollection<Triangle> Delaunay(List<Point> points) => 
			BowyerWatson(points.Select(p => new PointF(p.X, p.Y)).ToList(), SuperSquare(points));

		public static ICollection<Triangle> Delaunay(Bitmap bitmap, List<Point> points) => 
			BowyerWatson(points.Select(p => new PointF(p.X, p.Y)).ToList(), SuperSquare(bitmap));
	}
}
