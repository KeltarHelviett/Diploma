using CommandLine;

namespace Bergamot
{
	public enum BoundariesType
	{
		All,
		Extremums,
	}

	public enum ContourTracingAlgorithm
	{
		SquareTracing,
		MoorNeighbor,
		TheoPavlidis,
	}

	public class Options
	{
		public static Options Instance { get; set; }

		[Option("fin-diff", HelpText = "Specify the way to calculate derivative numerically (Left, Right, Central)", Default = global::FiniteDifferenceType.Right)]
		public FiniteDifferenceType FiniteDifferenceType { get; set; }

		[Option('b', HelpText = "Type of boundaries (All or Extremum points)", Default = BoundariesType.All)]
		public BoundariesType Boundaries { get; set; }

		[Value(0, Required = true)]
		public string Filename { get; set; }

		[Option('o', "output")]
		public string Output { get; set; }

		[Option("show-boundaries", Default = false, HelpText = "Show boundaries that were constructed by the algorithm")]
		public bool ShowBoundaries { get; set; }

		[Option("show-segment-endpoints", Default = false, HelpText = "Show hull's segments endpoints")]
		public bool ShowSegmentEndpoints { get; set; }

		[Option("contour-tracing", HelpText = "Contour tracing algorithm (SquareTracing, MoorNeighbor, TheoPavlidis)", Default = ContourTracingAlgorithm.SquareTracing)]
		public ContourTracingAlgorithm ContourTracing { get; set; }

		[Option('t', "triangulate", Default = false, HelpText = "Show triangulation")]
		public bool Triangulate { get; set; }

		[Option('c', "check", Default = false, HelpText = "Runtime checks and assertions")]
		public bool RuntimeChecks { get; set; }
	}
}
