using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Bergamot.DataStructures;
using Bergamot.Extensions;
using Bergamot.MeshGeneration;
using CommandLine;
using Bergamot.NonConvexHullGeneration;

namespace Bergamot
{
	class Program
	{
		public static void ShowHull(Bitmap image, List<Segment> hull, Pen pen)
		{
			using (var g = Graphics.FromImage(image)) {
				foreach (var segment in hull) {
					g.DrawLine(pen, segment.A, segment.B);
				}
			}
		}

		public static void ShowBoundaries(Bitmap image, List<Point> boundaries, Color color)
		{
			foreach (var point in boundaries) {
				image.SetPixel(point.X, point.Y, color);
			}
		}

		public static void ShowSegmentEndpoints(Bitmap image, List<Segment> segments, Color color)
		{
			foreach (var segment in segments) {
				image.SetPixel(segment.A.X, segment.A.Y, color);
				image.SetPixel(segment.B.X, segment.B.Y, color);
			}
		}

		public static void ShowMesh(Bitmap image, ICollection<ConnectedTriangle> triangles, Pen pen)
		{
			using (var g = Graphics.FromImage(image)) {
				foreach (var triangle in triangles) {
					g.DrawPolygon(pen, new [] { triangle.V1.Value, triangle.V2.Value, triangle.V3.Value, });
				}
			}
		}

		public static List<Segment> TestUpdateHull2(Bitmap image, List<Segment> hull)
		{
			var boundaries = hull.Select(s => s.A).ToList();
			int prev;
			using (Bitmap kek = (Bitmap)image.Clone()) {
				ShowHull(kek, hull, new Pen(Color.DarkSalmon));
				ShowSegmentEndpoints(kek, hull, Color.Aquamarine);
				kek.Save($@"D:\GIT\{boundaries.Count}.png");
			}
			do {
				prev = boundaries.Count;
				using (Bitmap kek = (Bitmap)image.Clone()) {
					boundaries = NonConvexHull.UpdateHull2(image, boundaries);
					var segments = boundaries.Select((point, i) => new Segment(point, boundaries[(i + 1) % boundaries.Count])).ToList();
					ShowHull(kek, segments, new Pen(Color.DarkSalmon, 2));
					ShowSegmentEndpoints(kek, segments, Color.Aquamarine);
					kek.Save($@"D:\GIT\{prev}-{boundaries.Count}.png");
				}
			} while (prev != boundaries.Count);
			return boundaries.Select((point, i) => new Segment(point, boundaries[(i + 1) % boundaries.Count])).ToList();
		}

		static void Main(string[] args)
		{
			//Point A = new Point(844, 1384), B = new Point(841, 1389), C = new Point(838, 1389), D = new Point(837, 1388);
			//PointF v1 = B.Sub(A), v2 = C.Sub(D);
			////var a = v1.Cross(v2);
			//NonConvexHull.Check(new Bitmap(10000, 10000), A, B, C, D);
			//return;
			Parser.Default.ParseArguments<Options>(args).WithParsed(options => {
				Options.Instance = options;
				if (!File.Exists(options.Filename)) {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"File {options.Filename} does not exists");
					return;
				}
				var image = new Bitmap(options.Filename);
				List<Point> boundaries;
				switch (options.ContourTracing) {
					case ContourTracingAlgorithm.SquareTracing:
						boundaries = NonConvexHull.SquareTrace(image);
						break;
					case ContourTracingAlgorithm.MoorNeighbor:
						boundaries = NonConvexHull.MoorNeighborTracing(image);
						break;
					case ContourTracingAlgorithm.TheoPavlidis:
						boundaries = NonConvexHull.TheoPavlidisTrace(image);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				boundaries = options.Boundaries == BoundariesType.All
					? boundaries
					: NonConvexHull.GetExtremumPoints(image, boundaries, options.FiniteDifferenceType);
				var hull = NonConvexHull.GetNonConvexHull(image, boundaries);
				hull = TestUpdateHull2(image, hull);
				ShowHull(image, hull, new Pen(Color.DarkSalmon, 1));
				if (options.ShowBoundaries) {
					ShowBoundaries(image, boundaries, Color.BlueViolet);
				}
				if (options.ShowSegmentEndpoints) {
					ShowSegmentEndpoints(image, hull, Color.Chartreuse);
				}
				if (options.Triangulate) {
					var triangles = MeshGenerator.Delaunay(image, hull.Select(s => s.A).ToList());
					ShowMesh(image, triangles, new Pen(Color.Firebrick, 1f));
				}
				image.Save(options.Output ?? $"{Path.Combine(Path.GetDirectoryName(options.Filename) ?? "", "out_" + Path.GetFileName(options.Filename))}");
			});
		}
	}
}
