using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Web.Publishing.Tasks;
using System.Xml;

// System.IO.File.Exists("../../../Metadata/CustomerService.config")

namespace IDEBuildExtensions
{
    public class MergeServiceMetadata : Microsoft.Build.Utilities.Task
    {
        public const string TaskName = "MergeServiceMetadata"; // nameof()


        [Microsoft.Build.Framework.Required]
        public Microsoft.Build.Framework.ITaskItem[] MetadataFiles { get; set; }

        [Microsoft.Build.Framework.Output]
        public string ReportFile { get; set; }

        [Microsoft.Build.Framework.Output]
        [Microsoft.Build.Framework.Required]
        public string TargetFile { get; set; }


        public virtual void AppendEntryToReport(string metadataFile, IList<string> included, IList<string> excluded, ref StringBuilder report)
        {
            if (report == null)
                report = new StringBuilder();

            report.AppendFormat("Metadata file: \"{0}\"\r\n\r\nincluded:\r\n{1}\r\n\r\n", metadataFile, String.Join("\r\n", included));
            if (excluded.Count != 0)
                report.AppendFormat("excluded:\r\n{0}\r\n\r\n", String.Join("\r\n", excluded));
        }

        public virtual void AppendFooterToReport(string targetFile, IList<string> integrated, ref StringBuilder report)
        {
            if (report == null)
                report = new StringBuilder();

            report.AppendFormat("\r\n\r\nTarget file: \"{0}\"\r\n\r\n", targetFile);
            if (integrated != null && integrated.Count > 0)
                report.AppendFormat("\r\n{0}\r\n\r\n", String.Join("\r\n", integrated));
        }

        public bool ReplaceMetadata(Dictionary<string, XmlNode> metadata, ref XmlDocument document, out IList<string> integrated)
        {
            integrated = null;
            if (metadata == null || document == null)
                return false;


            List<string> integratedNodes = new List<string>(metadata.Count);
            XmlNode node;
            string pathToParentNode, xPath;
            int index;

            foreach (var entry in metadata)
            {
                xPath = entry.Key;
                index = xPath.LastIndexOf('/');

                if (index >= 0)
                {
                    pathToParentNode = xPath.Substring(0, index);

                    node = document.SelectOrCreate(pathToParentNode);
                    node.AppendChild(document.ImportNode(entry.Value, true));

                    integratedNodes.Add(entry.Key);
                }
            }

            integratedNodes.Sort();
            integrated = integratedNodes;

            return true;
        }

        public bool RetrieveMetadataFromFile(XmlDocument document,
                                             ref Dictionary<string, XmlNode> metadata,
                                             out IList<string> included,
                                             out IList<string> excluded)
        {
            if (metadata == null)
                metadata = new Dictionary<string, XmlNode>();

            included = new List<string>();
            excluded = new List<string>();


            XmlNode bindings = document.SelectSingleNode("//configuration/system.serviceModel/bindings");
            XmlNode client = document.SelectSingleNode("//configuration/system.serviceModel/client");
            string xPath;

            if (bindings == null || client == null)
                return false;


            foreach (XmlNode groupingNode in bindings)
            {
                if (groupingNode.HasChildNodes)
                {
                    foreach (XmlNode binding in groupingNode)
                    {
                        if (binding.HasUniqueAttribute())
                        {
                            /* //configuration/system.serviceModel/client/<BindingTypeGroup>/binding[@name=''] */
                            xPath = binding.ToXPath();

                            if (metadata.ContainsKey(xPath))
                            {
                                excluded.Add(xPath);
                            }
                            else
                            {
                                metadata.Add(xPath, binding.CloneNode(true)); // non cloneable nodes?
                                included.Add(xPath);
                            }
                        }
                    }
                }
            }

            foreach (XmlNode node in client)
            {
                if (node.HasUniqueAttribute())
                {
                    /* //configuration/system.serviceModel/client/endpoint[@name=''] */
                    xPath = node.ToXPath();

                    if (metadata.ContainsKey(xPath))
                    {
                        excluded.Add(xPath);
                    }
                    else
                    {
                        metadata.Add(xPath, node.CloneNode(true)); // non cloneable nodes?
                        included.Add(xPath);
                    }
                }
            }

            return true;
        }


        public override bool Execute()
        {
            if (FilePath.Validate(TargetFile) == false)
            {
                Log.LogError("Build task has encountered a fatal error, the target file path in incorrect \"{0}\"", TargetFile);
                return false;
            }

            bool isResportRequierd = FilePath.Validate(ReportFile);
            StringBuilder report = new StringBuilder();


            XmlDocument metadataDocument;
            XmlDocument targetDocument = new XmlDocument();
            Dictionary<string, XmlNode> metadata = null;
            IList<string> included, excluded;


            // target file

            if (System.IO.File.Exists(TargetFile))
            {
                try
                {
                    System.IO.FileStream fileStream = new System.IO.FileStream(TargetFile, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    targetDocument.Load(fileStream);
                    fileStream.Close();

                    // metadata initialization always occurs
                    RetrieveMetadataFromFile(targetDocument, ref metadata, out included, out excluded);

                    XmlNode system_serviceModel = targetDocument.SelectOrCreate("//configuration/system.serviceModel");
                    if (system_serviceModel != null)
                        system_serviceModel.RemoveAll();


                    Log.LogMessage("Target file pre-processing.");
                }
                catch (Exception ex)
                {
                    Log.LogError("Target file pre-processing failed: \"{0}\".", ex.Message);
                    return false;
                }
            }
            else
            {
                Log.LogMessage("Target file was not found.");

                // preparing XmlDocument
                targetDocument.AppendChild(targetDocument.CreateXmlDeclaration("1.0", "UTF-8", null));
                targetDocument.SelectOrCreate("//configuration/system.serviceModel");
            }


            // processing metadate

            string metadataFile;
            foreach (ITaskItem taskItem in MetadataFiles)
            {
                metadataFile = taskItem.ItemSpec;

                Log.LogMessage("Metadata file: \"{0}\".", metadataFile);

                if (String.IsNullOrWhiteSpace(metadataFile) || !System.IO.File.Exists(metadataFile))
                {
                    Log.LogMessage("Processing failed: \"The path to configuration file is invalid.\".");
                    continue;
                }

                try
                {
                    metadataDocument = new XmlDocument();

                    System.IO.FileStream fileStream = new System.IO.FileStream(metadataFile, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    metadataDocument.Load(fileStream);
                    fileStream.Close();


                    if (RetrieveMetadataFromFile(metadataDocument, ref metadata, out included, out excluded) == true)
                    {
                        //Log.LogMessage("Processing succeeded: {0} included, {1} excluded.", included.Count, excluded.Count);

                        if (isResportRequierd)
                            AppendEntryToReport(metadataFile, included, excluded, ref report);
                    }
                    else
                    {
                        Log.LogMessage("Processing failed: \"Configuration file is invalid.\".");
                    }
                }
                catch (Exception ex)
                {
                    Log.LogError("Processing failed: \"{0}\".", ex.Message);
                }
            }

            // metadata integration

            Log.LogMessage("Target file: \"{0}\".", TargetFile);
            IList<string> integrated;

            if (!ReplaceMetadata(metadata, ref targetDocument, out integrated))
            {
                string errorMessage = String.Format("Build task has encountered a fatal error, metadata integration failed.");

                Log.LogError(errorMessage);
                return false;
            }

            // save configuration file

            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.IndentChars = "    ";

                using (XmlWriter writer = XmlTextWriter.Create(TargetFile, settings))
                {
                    targetDocument.Save(writer);
                }

                Log.LogMessage("Metadata integration complete.");
            }
            catch (Exception ex)
            {
                Log.LogError("Metadata integration failed: \"{0}\".", ex.Message);
                return false;
            }

            // save report file

            if (isResportRequierd)
            {
                AppendFooterToReport(TargetFile, integrated, ref report);

                try
                {
                    System.IO.FileStream fileStream =
                        System.IO.File.Open(ReportFile, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);

                    using (var streamWriter = new System.IO.StreamWriter(fileStream, Encoding.UTF8))
                    {
                        streamWriter.Write(report);
                    }

                    fileStream.Close();

                    Log.LogMessage("Creating a report file: \"{0}\".", ReportFile);
                }
                catch (Exception ex)
                {
                    Log.LogError("Creating a report file failed: \"{0}\".", ex.Message);
                }
            }


            return !Log.HasLoggedErrors;
        }
    }
}
