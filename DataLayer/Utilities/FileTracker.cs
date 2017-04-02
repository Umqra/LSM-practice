using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DataLayer.Utilities;

namespace DataLayer.Utilities
{
    public class FileTracker : IFileTracker
    {
        private readonly DirectoryInfo workingDirectory;
        private string formatString;
        private Regex parsingRegex;
        public FileTracker(string formatString, DirectoryInfo workingDirectory)
        {
            this.workingDirectory = workingDirectory;
            ParseFormatString(formatString);
        }

        private void ParseFormatString(string format)
        {
            var captureRegex = new Regex("{.*}");
            if (captureRegex.IsMatch(format) && captureRegex.Matches(format).Count == 1)
            {
                var convertedToRegex = $"^{captureRegex.Replace(format, "(.*)")}$";
                formatString = format;
                parsingRegex = new Regex(convertedToRegex);
            }
            else
                throw new ArgumentException($"Expected format string with exactly one substitution group {{}}, but got '{format}'");
        }

        public IEnumerable<FileData> Files => workingDirectory
            .GetFiles()
            .Where(f => parsingRegex.IsMatch(f.Name))
            .Select(file => new FileData(file.FullName));
        public FileData CreateNewFile()
        {
            var maxId = int.MinValue;
            foreach (var file in Files)
            {
                var match = parsingRegex.Match(file.Path);
                maxId = Math.Max(maxId, int.Parse(match.Groups[0].Value));
            }
            return new FileData(Path.Combine(workingDirectory.FullName, string.Format(formatString, maxId + 1)));
        }
    }
}