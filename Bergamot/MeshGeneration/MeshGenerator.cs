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
	}
}
