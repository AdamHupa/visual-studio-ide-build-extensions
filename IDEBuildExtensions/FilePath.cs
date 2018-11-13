using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO; // for: Directory, Path
using System.Text.RegularExpressions; // for: Regex

// link: https://docs.microsoft.com/en-gb/windows/desktop/FileIO/naming-a-file
// link: https://en.wikipedia.org/wiki/Path_(computing)

namespace IDEBuildExtensions
{
    // PathFileExistsW(path) - include "Shlwapi.h" and add reference: C:\Program Files (x86)\Windows Kits\8.1\Lib\winv6.3\um\x86\ShLwApi.lib

    public static class FilePath
    {
        private static readonly char[] _invalidFileNameChars;
        private static readonly char[] _invalidPathChars;

        private static readonly Regex _invalidFileName =
            new Regex(@"(^\s)|([\s.]$)|(^(AUX|COM[1-9]|CON|LPT[1-9]|NUL|PRN)$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _invalidRelativePath =
            new Regex(@"((^|\\|/)[\s.])|(\\\\|//|\\/|/\\)|([\s.]($|\\|/))" +
                      @"|(^|\\|/)(AUX|COM[1-9]|CON|LPT[1-9]|NUL|PRN)($|\\|/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _prefixLongNamingConvention =
            new Regex(@"^(\\\\\?\\)", RegexOptions.Compiled);

        private static readonly Regex _pathPrefix =
            new Regex(@"^((\\\\\?\\)?[a-zA-Z]:\\)|(\\\\\?\\Volume\{[0-9a-fA-F]{8,}-[0-9a-fA-F]{4,}-[0-9a-fA-F]{4,}-[0-9a-fA-F]{4,}-[0-9a-fA-F]{12,}\}\\)|(\\\\)", RegexOptions.Compiled);


        static FilePath()
        {
            // illegal characters: 0x01-0x1F + "\"*/:<>?\\|"

            _invalidFileNameChars = Path.GetInvalidFileNameChars();
            Array.Sort(_invalidFileNameChars);

            var list = Path.GetInvalidPathChars().ToList();
            list.AddRange("*:?");
            list.Sort();
            _invalidPathChars = list.ToArray(); // only excluding '/' and '\\'
        }


        public static bool Validate(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
                return false;

            bool longNamingConvention = _prefixLongNamingConvention.IsMatch(path);
            if (longNamingConvention && path.Length > 32767)
                return false;


            Match match = _pathPrefix.Match(path);

            int firstAfterPrefix = match.Length;
            int lastBackslash = path.LastIndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });


            if (firstAfterPrefix > path.Length || firstAfterPrefix == lastBackslash + 2)
                return false;

            if (firstAfterPrefix == lastBackslash + 1)
            {
                if (firstAfterPrefix == path.Length)
                    return true;

                if (path.Length > 260)
                    return false;

                return ValidateFileName(path.Substring(lastBackslash + 1)); // validating file name
            }

            if (lastBackslash + 1 == path.Length)
            {
                /* this method assumes that all paths end with a backslash which is not a prerequisite in Universal Naming Convention
                   so the allowed directory path string should be smaller by 1 character to accommodate the ending backslash */
                if (path.Length > 247 + 1)
                    return false;

                return ValidateRelativePath(path.Substring(firstAfterPrefix, lastBackslash - firstAfterPrefix)); // validating path
            }
            else
            {
                if (path.Length > 260)
                    return false;

                return ValidateRelativePath(path.Substring(firstAfterPrefix, lastBackslash - firstAfterPrefix))
                       && ValidateFileName(path.Substring(lastBackslash + 1)); // validating path and file name
            }
        }

        public static bool ValidateFileName(string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName) || fileName.Split(_invalidFileNameChars).Length > 1)
                return false;

            if (_invalidFileName.IsMatch(fileName))
                return false;

            return true;
        }

        public static bool ValidateRelativePath(string relativePath)
        {
            if (String.IsNullOrWhiteSpace(relativePath) || relativePath.Split(_invalidPathChars).Length > 1)
                return false;

            if (_invalidRelativePath.IsMatch(relativePath))
                return false;

            return true;
        }
    }
}
