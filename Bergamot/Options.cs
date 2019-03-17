using CommandLine;

namespace Bergamot
{
    public enum BoundariesType
    {
        All,
        Extremums,
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
    }
}
