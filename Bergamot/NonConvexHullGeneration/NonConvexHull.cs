using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Bergamot.DataStructures;
using Bergamot.Extensions;

public enum FiniteDifferenceType
{
    Left, Right, Central
}

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
            var used = new HashSet<Point> {start};
            while (next != start) {
                if (!image.TryGetPixel(next.X, next.Y, out var c) || c.A == 0) {
                    nextStep = GoRight(nextStep);
                    next = next.Add(nextStep);
                } else {
                    if (!used.Contains(next = next.Clamp(0, image.Width - 1, 0, image.Height - 1)) && !IsIsolated(next, image)) {
                        boundaryPoints.Add(next);
                        used.Add(next);
                    }
                    nextStep = GoLeft(nextStep);
                    next = next.Add(nextStep);
                }
            }
            return boundaryPoints;
        }

        public static List<Point> TheoPavlidisTrace(Bitmap image)
        {
            var start = GetStartPoint(image);
            Point current = start, next;
            var boundaries = new List<Point>(image.Height * 2 + image.Width + 2) { start };
            var used = new HashSet<Point>();
            var direction = new Point(0, -1);
            var rotationTimes = 0;
            do {
                var ahead = current.Add(direction);
                if (image.TryGetPixel(next = ahead.Add(GoLeft(direction)), out var c) && c.A != 0) {
                    current = next;
                    direction = GoLeft(direction);
                } else if (image.TryGetPixel(ahead.X, ahead.Y, out c) && c.A != 0) {
                    current = ahead;
                } else if (image.TryGetPixel(next = current.Add(GoRight(direction)).Add(GoLeft(GoRight(direction))), out c) && c.A != 0) {
                    current = next;
                    direction = GoLeft(GoRight(direction));
                } else if (++rotationTimes == 4) {
                    return boundaries;
                } else {
                    direction = GoRight(direction);
                    continue;
                }
                if (!used.Contains(current) && !IsIsolated(current, image)) {
                    boundaries.Add(current);
                    used.Add(current);
                }
                rotationTimes = 0;
            } while (current != start);
            return boundaries;
        }

        private static readonly Point[,] nextNeighbor = new Point[3,3] {
            {new Point(1, 0), new Point(0, 0), new Point(0, 1)},
            {new Point(2, 0), new Point(int.MinValue, int.MinValue), new Point(0, 2)},
            {new Point(2, 1), new Point(2, 2), new Point(1, 2)}
        };

        public static List<Point> MoorNeighborTracing(Bitmap image)
        {
            Point NextNeighbor(Point p, Point n)
            {
                var offset = n.Sub(p);
                return p.Add(nextNeighbor[offset.X + 1, offset.Y + 1].Sub(new Point(1, 1)));
            }
            var start = GetStartPoint(image);
            var current = start;
            var boundaries = new List<Point>(image.Height * 2 + image.Width + 2) { start };
            var neighbor = NextNeighbor(current, current.Add(new Point(0, 1)));
            var prev = neighbor;
            while (neighbor != start) {
                if (image.TryGetPixel(neighbor, out var c) && c.A != 0) {
                    boundaries.Add(neighbor);
                    current = neighbor;
                    neighbor = prev;
                } else {
                    prev = neighbor;
                    neighbor = NextNeighbor(current, neighbor);
                }
            }
            return boundaries;
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

        public static List<Point> GetExtremumPoints(Bitmap image, FiniteDifferenceType fdt = FiniteDifferenceType.Right)
        {
            var cloud = GetBoundaries(image);
            var res = new List<Point>();
            var (start, end, diff) = GetExtremumPointsHelper(image, cloud, fdt);
            for (int i = start; i < end; ++i) {
                var (fx, fy) = diff(i);
                if (Math.Abs(fx) <= 1 && Math.Abs(fy) <= 1) {
                    res.Add(cloud[i]);
                }
            }
            return res;
        }

        private static (int, int, Func<int, (int, int)>) GetExtremumPointsHelper(Bitmap image, List<Point> points, FiniteDifferenceType fdt)
        {
            var start = fdt == FiniteDifferenceType.Right ? 0 : 1;
            var end = fdt == FiniteDifferenceType.Left ? points.Count - 1 : points.Count - 2;
            Func<int, (int, int)> diff;
            switch (fdt) {
                case FiniteDifferenceType.Right:
                    diff = i => {
                        Point p1 = points[i], p2 = points[i + 1];
                        return (
                            p1.X == p2.X ? 0 : image.GetPixel(p2.X, p2.Y).Diff(image.GetPixel(p1.X, p1.Y)) / Math.Abs(p2.X - p1.X),
                            p1.Y == p2.Y ? 0 : image.GetPixel(p2.Y, p2.Y).Diff(image.GetPixel(p1.Y, p1.Y)) / Math.Abs(p2.Y - p1.Y)
                        );
                    };
                    break;
                case FiniteDifferenceType.Left:
                    diff = i => {
                        Point p1 = points[i - 1], p2 = points[i];
                        return (
                            p1.X == p2.X ? 0 : image.GetPixel(p2.X, p2.Y).Diff(image.GetPixel(p1.X, p1.Y)) / Math.Abs(p2.X - p1.X),
                            p1.Y == p2.Y ? 0 : image.GetPixel(p2.Y, p2.Y).Diff(image.GetPixel(p1.Y, p1.Y)) / Math.Abs(p2.Y - p1.Y)
                        );
                    };
                    break;
                case FiniteDifferenceType.Central:
                    diff = i => {
                        Point p1 = points[i - 1], p2 = points[i + 1];
                        return (
                            p1.X == p2.X ? 0 : image.GetPixel(p2.X, p2.Y).Diff(image.GetPixel(p1.X, p1.Y)) / (2 * Math.Abs(p2.X - p1.X)),
                            p1.Y == p2.Y ? 0 : image.GetPixel(p2.Y, p2.Y).Diff(image.GetPixel(p1.Y, p1.Y)) / (2 * Math.Abs(p2.Y - p1.Y))
                        );
                    };
                    break;
                default:
                    diff = null;
                    break;
            }
            return (start, end, diff);
        }

        public static List<Segment> GetNonConvexHull(Bitmap image, List<Point> boundaries)
        {
            var segments = new List<Segment>();
            var segmentStartIndexes = new List<int>();
            int pivotIndex = 0;
            for (int i = 1; i < boundaries.Count; ++i) {
                if (
                    segments.Count > 0 &&
                    !SegmentIntersectBoundaries(boundaries, segmentStartIndexes[segmentStartIndexes.Count - 1], i)
                ) {
                    segments[segments.Count - 1] = new Segment(segments[segments.Count - 1].A, boundaries[i]);
                    pivotIndex = i;
                } else if (!SegmentIntersectBoundaries(boundaries, pivotIndex, i)) {
                    segments.Add(new Segment(boundaries[pivotIndex], boundaries[i]));
                    segmentStartIndexes.Add(pivotIndex);
                    pivotIndex = i;
                }
            }
            segments.Add(new Segment(segments[segments.Count - 1].B, segments[0].A));
            segmentStartIndexes.Add(boundaries.Count - 1);
            Debug.Assert(segments[segments.Count - 1].B == segments[0].A);
            int prev;
            do {
                prev = segments.Count;
                (segments, segmentStartIndexes) = UpdateHull(image, boundaries, segments, segmentStartIndexes);
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

        public static bool BresenhamSegmentIntersectsSprite(Bitmap image, Point p1, Point p2)
        {
            int w = p2.X - p1.X;
            int h = p2.Y - p1.Y;
            int dy2 = 0;
            var dx1 = w < 0 ? -1 : w > 0 ? 1 : 0;
            var dy1 = h < 0 ? -1 : h > 0 ? 1 : 0;
            var dx2 = w < 0 ? -1 : w > 0 ? 1 : 0;
            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);
            if (longest <= shortest) {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                dy2 = h < 0 ? -1 : h > 0 ? 1 : 0;
                dx2 = 0;
            }
            int numerator = longest >> 1;
            var cur = p1;
            for (int i = 0; i <= longest; i++) {
                if (image.GetPixel(cur.X, cur.Y).A != 0 && cur != p1 && cur != p2) {
                    return true;
                }
                numerator += shortest;
                if (numerator >= longest) {
                    numerator -= longest;
                    cur.X += dx1;
                    cur.Y += dy1;
                } else {
                    cur.X += dx2;
                    cur.Y += dy2;
                }
            }
            return false;
        }

        private static bool IsIsolated(Point p, Bitmap image)
        {
            return (!image.TryGetPixel(p.X + 1, p.Y - 1, out var c) || c.A == 0 ? 1 : 0) +
                   (!image.TryGetPixel(p.X + 1, p.Y, out c) || c.A == 0 ? 1 : 0) +
                   (!image.TryGetPixel(p.X + 1, p.Y + 1, out c) || c.A == 0 ? 1 : 0) +
                   (!image.TryGetPixel(p.X, p.Y - 1, out c) || c.A == 0 ? 1 : 0) +
                   (!image.TryGetPixel(p.X, p.Y + 1, out c) || c.A == 0 ? 1 : 0) +
                   (!image.TryGetPixel(p.X - 1, p.Y - 1, out c) || c.A == 0 ? 1 : 0) +
                   (!image.TryGetPixel(p.X - 1, p.Y, out c) || c.A == 0 ? 1 : 0) +
                   (!image.TryGetPixel(p.X - 1, p.Y + 1, out c) || c.A == 0 ? 1 : 0) >= 5;
        }
    }
}
