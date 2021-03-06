﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Bergamot.DataStructures;
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

		public static void ShowMesh(Bitmap image, IEnumerable<ConnectedTriangle> triangles, Pen pen)
		{
			using (var g = Graphics.FromImage(image)) {
				foreach (var triangle in triangles) {
					g.DrawPolygon(pen, new [] { triangle.V1.Value, triangle.V2.Value, triangle.V3.Value, });
				}
			}
		}

		public static void Test()
		{
			var points = new[] {
				new PointF(100f, 50f), new PointF(140f, 25f), new PointF(155f, 100f),
				new PointF(190f, 30f), new PointF(220f, 45f),
			};
			var super = new ConnectedTriangle(new PointF(0f, 0f), new PointF(300f, 0f), new PointF(150f, 150f));
			using (var image = new Bitmap(300, 300)) {
				var triangulation = MeshGenerator.ConstrainedDelaunay(points.ToList(), new List<ConnectedTriangle>() {
					super
				}, new List<Segment> {
					new Segment(new Point(100, 50), new Point(190, 30)),
					new Segment(new Point(190, 30), new Point(220, 45)), new Segment(new Point(220, 45), new Point(100, 50))
				});
				ShowMesh(image, triangulation, new Pen(Color.Brown, 1));
				image.Save($@"D:\GIT\Samples\test.png");
			}
		}

		static void Main(string[] args)
		{
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
				ShowHull(image, hull, new Pen(Color.DarkSalmon, 2));
				if (options.ShowBoundaries) {
					ShowBoundaries(image, boundaries, Color.BlueViolet);
				}
				if (options.ShowSegmentEndpoints) {
					ShowSegmentEndpoints(image, hull, Color.Chartreuse);
				}
				if (options.Triangulate) {
					//var triangles = MeshGenerator.Delaunay(image, hull.Select(s => s.A).ToList());
					var triangles = MeshGenerator.ConstrainedDelaunay(image, hull);
					ShowMesh(image, triangles, new Pen(Color.Firebrick, 1f));
				}
				image.Save(options.Output ?? $"{Path.Combine(Path.GetDirectoryName(options.Filename) ?? "", "out_" + Path.GetFileName(options.Filename))}");
			});
		}
	}
}
