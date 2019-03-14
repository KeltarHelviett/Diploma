using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                    boundaryPoints.Add(next.Clamp(0, image.Width - 1, 0, image.Height - 1));
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

        private static (List<Segment>, List<int>) UpdateHull(Bitmap image, List<Point> boundaries, List<Segment> segments, List<int> startIndexes)
        {
            var updatedSegments = new List<Segment>();
            var updatedIndexes = new List<int>();
            int cur = 0;
            int merge = 1;
            int j = 0;
            while (merge < segments.Count) {
                if (!SegmentIntersectBoundaries(boundaries, startIndexes[cur], startIndexes[merge])) {
                    segments[cur] = new Segment(segments[cur].A, segments[merge++].A);
                } else {
                    updatedSegments.Add(segments[cur]);
                    updatedIndexes.Add(startIndexes[cur]);
                    cur = merge - 1;
                }
            }
            updatedSegments.Add(segments[cur]);
            updatedIndexes.Add(startIndexes[cur]);
            cur = merge - 1;
            while (cur < segments.Count) {
                updatedSegments.Add(segments[cur]);
                updatedIndexes.Add(startIndexes[cur++]);
            }
            for (int i = 1; i < updatedSegments.Count; i++) {
                Debug.Assert(updatedSegments[i].A == updatedSegments[i - 1].B);
            }
            Debug.Assert(updatedSegments[updatedSegments.Count - 1].B == updatedSegments[0].A);
            return (updatedSegments, updatedIndexes);
        }

        public static List<Point> GetExtremumPoints(Bitmap image)
        {
            var cloud = GetBoundaries(image);
            var res = new List<Point>();
            for (int i = 0; i < cloud.Count - 1; ++i) {
                var current = cloud[i];
                var next = cloud[i + 1];
                var fx = current.X == next.X
                    ? 0
                    : (image.GetPixel(next.X, next.Y).Diff(image.GetPixel(current.X, current.Y))) / Math.Abs(next.X - current.X);
                var fy = current.Y == next.Y
                    ? 0
                    : (image.GetPixel(next.X, next.Y).Diff(image.GetPixel(current.X, current.Y))) / Math.Abs(next.Y - current.Y);
                if (Math.Abs(fx) < 1 && Math.Abs(fy) < 1) {
                    res.Add(next);
                }
            }
            return res;
        }

        public static List<Segment> GetNonConvexHull(Bitmap image)
        {
            var segments = new List<Segment>();
            var segmentStartIndexes = new List<int>();
            var cloud = GetBoundaries(image).Distinct().ToList();
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
            segments.Add(new Segment(segments[segments.Count - 1].B, segments[0].A));
            segmentStartIndexes.Add(cloud.Count - 1);
            Debug.Assert(segments[segments.Count - 1].B == segments[0].A);
            int prev;
            do {
                prev = segments.Count;
                (segments, segmentStartIndexes) = UpdateHull(image, cloud, segments, segmentStartIndexes);
            } while (prev != segments.Count);
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
                if (A * p.X + B * p.Y + C <= -1) {
                    return true;
                }
            }
            return false;
        }
    }
}
