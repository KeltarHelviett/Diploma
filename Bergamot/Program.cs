using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Bergamot.DataStructures;
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
                var boundaries = options.Boundaries == BoundariesType.All
                    ? NonConvexHull.GetBoundaries(image)
                    : NonConvexHull.GetExtremumPoints(image, options.FiniteDifferenceType);
                var hull = NonConvexHull.GetNonConvexHull(image, boundaries);
                ShowHull(image, hull, new Pen(Color.DarkSalmon, 1));
                image.Save(options.Output ?? $"{Path.Combine(Path.GetDirectoryName(options.Filename) ?? "", "out_" + Path.GetFileName(options.Filename))}");
            });
        }
    }
}
