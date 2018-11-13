using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Web.Publishing.Tasks;
using System.IO; // for: File, FileStream, StreamReader, StreamWriter, FileMode, FileAccess

// link: https://blogs.msdn.microsoft.com/debuggingtoolbox/2008/04/02/comparing-regex-replace-string-replace-and-stringbuilder-replace-which-has-better-performance/
// better performance: String.Replace > StringBuilder.Replace > RegEx.Replace

// System.IO.File.Replace(SourceFile, TargetFile, String.Format("{0}.backup", TargetFile), false);

namespace IDEBuildExtensions
{
    public class ReplacePhrase : Microsoft.Build.Utilities.Task
    {
        public const string TaskName = "ReplacePhrase"; // nameof()


        [Microsoft.Build.Framework.Required]
        public string Phrase { get; set; }

        public string Prefix { get; set; }

        [Microsoft.Build.Framework.Output]
        public string ReplaceWith { get; set; }

        [Microsoft.Build.Framework.Required]
        public string SourceFile { get; set; }

        [Microsoft.Build.Framework.Output]
        [Microsoft.Build.Framework.Required]
        public string TargetFile { get; set; }


        public override bool Execute()
        {
            bool canExecute = true;

            if (String.IsNullOrWhiteSpace(Phrase))
            {
                Log.LogError("Build task has encountered a fatal error, the phrase to be replace is incorrect \"{0}\"", Phrase);
                canExecute = false;
            }

            if (String.IsNullOrWhiteSpace(SourceFile) || !File.Exists(SourceFile))
            {
                Log.LogError("Build task has encountered a fatal error, the source file path in incorrect \"{0}\"", SourceFile);
                canExecute = false;
            }

            if (FilePath.Validate(TargetFile) == false)
            {
                Log.LogError("Build task has encountered a fatal error, the target file path in incorrect \"{0}\"", TargetFile);
                canExecute = false;
            }

            if (canExecute == false)
                return false;


            string completePhrase =
                String.IsNullOrWhiteSpace(Prefix) ? String.Format("{{{0}}}", Phrase) : String.Format("{{{0}:{1}}}", Prefix, Phrase);

            if (String.Compare(SourceFile, TargetFile) != 0)
            {
                /* dedicated to manipulating large files  */
                try
                {
                    FileStream sourceFileStream = new FileStream(SourceFile, FileMode.Open, FileAccess.Read);
                    FileStream targetFileStream = new FileStream(TargetFile, FileMode.OpenOrCreate, FileAccess.Write);

                    targetFileStream.SetLength(0);

                    Log.LogMessage("Processing file \"{0}\"", SourceFile);
                    Log.LogMessage("Attempting to replace string \"{0}\" with \"{1}\".", completePhrase, ReplaceWith);


                    using (var streamReader = new StreamReader(sourceFileStream, Encoding.UTF8))
                    using (var streamWriter = new StreamWriter(targetFileStream, Encoding.UTF8))
                    {
                        string workingString = null;

                        while ((workingString = streamReader.ReadLine()) != null)
                        {
                            workingString = workingString.Replace(completePhrase, ReplaceWith);
                            streamWriter.WriteLine(workingString);
                        }
                        streamWriter.Write(streamReader.ReadToEnd());
                    }


                    sourceFileStream.Close();
                    targetFileStream.Close();

                    Log.LogMessage("Saving changes to file \"{0}\"", TargetFile);
                }
                catch (Exception ex)
                {
                    Log.LogError("Processing failed: \"{0}\".", ex.Message);
                    return false;
                }
            }
            else
            {
                /* designed for small config files */
                string backupFile = String.Format("{0}.backup", TargetFile);
                try
                {
                    File.Copy(TargetFile, backupFile, true);

                    Log.LogMessage("Attempting to replace string \"{0}\" with \"{1}\".", completePhrase, ReplaceWith);

                    string workingString = File.ReadAllText(TargetFile);
                    workingString = workingString.Replace(completePhrase, ReplaceWith);
                    File.WriteAllText(TargetFile, workingString);

                    Log.LogMessage("Saving changes to source file \"{0}\"", TargetFile);

                    File.Delete(backupFile);
                }
                catch (Exception ex)
                {
                    Log.LogError("Processing failed: \"{0}\".", ex.Message);

                    try
                    {
                        if (File.Exists(backupFile))
                        {
                            File.Replace(backupFile, TargetFile, null, false);
                            File.Delete(backupFile);
                        }
                    }
                    catch (Exception inner)
                    {
                        Log.LogError("Reverting to the original file failed: \"{0}\".", inner.Message);
                    }

                    return false;
                }
            }


            return true;
        }
    }
}
