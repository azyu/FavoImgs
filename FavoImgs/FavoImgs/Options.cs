using CommandLine;
using CommandLine.Text;

namespace FavoImgs
{
    class Options
    {
        [Option('c', "continue", Required = false,
            HelpText = "Continue download from the oldest favorite tweet.")]
        public bool Continue { get; set; }

        [Option("reset_path", Required = false,
            HelpText = "Reset default download path.")]
        public bool ResetDownloadPath { get; set; }

        [Option('a', "all", Required = false,
            HelpText = "Get them all!")]
        public bool GetThemAll { get; set; }

        [Option("path", Required = false,
            HelpText = "Specify download folder")]
        public string DownloadPath { get; set; }

        [Option("source", Required = false,
            HelpText = "Specify source location")]
        public string Source { get; set; }

        [Option("source_slug", Required = false,
            HelpText = "Identify a list by its slug")]
        public string SourceSlug { get; set; }

        [ParserState]
        public IParserState ParserState { get; set; }

        [HelpOption('h', "help")]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));

        }
    }
}
