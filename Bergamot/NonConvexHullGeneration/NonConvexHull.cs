using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bergamot.DataStructures;
using Bergamot.Extensions;

namespace Bergamot.NonConvexHullGeneration
{
    public static class NonConvexHull
    {
        public static List<Point> GetBoundaries(Bitmap image)
        {
            return SquareTrace(image);
        }

        public static List<Point> SquareTrace(Bitmap image)
        {
            var start = GetStartPoint(image);
            var boundaryPoints = new List<Point> { start };
            Point nextStep = GoLeft(new Point(1, 0));
            Point next = start.Add(nextStep);
            while (next != start) {
                if (
                    (next.X < 0 || next.Y < 0 || next.X >= image.Width || next.Y >= image.Height) ||
                    image.GetPixel(next.X, next.Y).A == 0
                ) {
                    nextStep = GoRight(nextStep);
                    next = next.Add(nextStep);
                } else {
                    boundaryPoints.Add(next);
                    nextStep = GoLeft(nextStep);
                    next = next.Add(nextStep);
                }
            }
            return boundaryPoints;
        }

        private static Point GoLeft(Point p) => new Point(p.Y, -p.X);
        private static Point GoRight(Point p) => new Point(-p.Y, p.X);

        public static Point GetStartPoint(Bitmap bitmap)
        {
            for (int y = bitmap.Height - 1; y >= 0; --y) {
                for (int x = 0; x < bitmap.Width; x++) {
                    if (bitmap.GetPixel(x, y).A != 0) {
                        return new Point(x, y);
                    }
                }
            }
            return new Point(-1, -1);
        }

        public static List<Segment> GetNonConvexHull(Bitmap image)
        {
            var segments = new List<Segment>();
            var segmentStartIndexes = new List<int>();
            var cloud = GetBoundaries(image).Distinct().ToList();
            var pivot = cloud[0];
            int pivotIndex = 0;
            for (int i = 2; i < cloud.Count; ++i) {
                if (
                    segments.Count > 0 &&
                    !SegmentIntersectBoundaries(cloud, segmentStartIndexes[segmentStartIndexes.Count - 1], i)
                 ) {
                    segments[segments.Count - 1] = new Segment(segments[segments.Count - 1].A, cloud[i]);
                    pivotIndex = i;
                } else if (!SegmentIntersectBoundaries(cloud, pivotIndex, i)) {
                    segments.Add(new Segment(cloud[pivotIndex], cloud[i]));
                    segmentStartIndexes.Add(pivotIndex);
                    pivotIndex = i;
                }
            }
            return segments;
        }

        public static bool SegmentIntersectBoundaries(List<Point> boundaries, int startIndex, int endIndex)
        {
            Point start = boundaries[startIndex], end = boundaries[endIndex];
            float
                A = start.Y - end.Y,
                B = end.X - start.X,
                C = start.X * end.Y - end.X * start.Y;
            for (int i = startIndex + 1; i < endIndex; ++i) {
                var p = boundaries[i];
                if (A * p.X + B * p.Y + C < 0) {
                    return true;
                }
            }
            return false;
        }
    }
}
