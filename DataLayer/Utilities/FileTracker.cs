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
        public DirectoryInfoBase WorkingDirectory { get; }
        private readonly IFileInfoFactory fileFactory;
        private string formatString;
        private Regex parsingRegex;
        public FileTracker(string formatString, DirectoryInfoBase workingDirectory, IFileInfoFactory fileFactory)
        {
            this.WorkingDirectory = workingDirectory;
            this.fileFactory = fileFactory;
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

        public IEnumerable<FileInfoBase> Files
        {
            get
            {
                //TODO: lock with IEnumerable
                lock (WorkingDirectory)
                {
                    return WorkingDirectory
                        .GetFiles()
                        .Where(f => parsingRegex.IsMatch(f.Name));
                }
            }
        }

        public FileInfoBase CreateNewFile()
        {
            //TODO: too big lock-area?
            lock (WorkingDirectory)
            {
                var maxId = 0;
                foreach (var file in Files)
                {
                    var match = parsingRegex.Match(file.Name);
                    maxId = Math.Max(maxId, int.Parse(match.Groups[1].Value));
                }
                var newFile =
                    fileFactory.FromFileName(Path.Combine(WorkingDirectory.FullName,
                        string.Format(formatString, maxId + 1)));
                newFile.Create().Dispose();
                return newFile;
            }
        }
    }
}