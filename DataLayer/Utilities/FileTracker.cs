using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataLayer.Utilities
{
    public class FileTracker : IFileTracker
    {
        private readonly DirectoryInfoBase workingDirectory;
        private string formatString;
        private Regex parsingRegex;
        public FileTracker(string formatString, DirectoryInfoBase workingDirectory)
        {
            this.workingDirectory = workingDirectory;
            ParseFormatString(formatString);
        }

        private void ParseFormatString(string format)
        {
            var captureRegex = new Regex(@"\{0\}");
            if (captureRegex.IsMatch(format) && captureRegex.Matches(format).Count == 1)
            {
                var convertedToRegex = $"^{captureRegex.Replace(format, "(.*)")}$";
                formatString = format;
                parsingRegex = new Regex(convertedToRegex);
            }
            else
                throw new ArgumentException($"Expected format string with exactly one substitution group {{}}, but got '{format}'");
        }

        public IEnumerable<FileInfoBase> Files => workingDirectory
            .GetFiles()
            .Where(f => parsingRegex.IsMatch(f.Name));
        public FileInfoBase CreateNewFile()
        {
            var maxId = 0;
            foreach (var file in Files)
            {
                var match = parsingRegex.Match(file.Name);
                maxId = Math.Max(maxId, int.Parse(match.Groups[1].Value));
            }
            return new FileInfo(Path.Combine(workingDirectory.FullName, string.Format(formatString, maxId + 1)));
        }
    }
}