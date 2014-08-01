using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using CommandLine.Parsing;

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
