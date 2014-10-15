using CommandLine;
using CommandLine.Text;

namespace FavoImgs
{
    class Options
    {
        [Option("reset_path", Required = false,
            HelpText = "Reset default download path.")]
        public bool ResetDownloadPath { get; set; }

        [Option("exclude_rts", Required = false,
            HelpText = "Not contain native retweets")]
        public bool ExcludeRetweets { get; set; }

        [Option("path", Required = false,
            HelpText = "Specify download folder")]
        public string DownloadPath { get; set; }

        [Option("source", Required = false,
            HelpText = "Specify source location")]
        public string Source { get; set; }

        [Option("slug", Required = false,
            HelpText = "Identify a list by its slug")]
        public string Slug { get; set; }

        [ParserState]
        public IParserState ParserState { get; set; }

        [Option("screen_name", Required = false,
            HelpText = "The screen name of the user for whom to return results for")]
        public string ScreenName { get; set; }

        [HelpOption('h', "help")]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        public TweetSource TweetSource { get; set; }
    }
}
