using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;

namespace IDEBuildExtensions
{
    public static class Extensions
    {
        /// <summary>
        /// Collection of attributes by which a configuration file property may be recognised as unique.
        /// </summary>
        internal static string[] UniqueAttributes = new string[]
        {
            "name", "key", // classical identificators
            "verb", "path" // HTTP Request, pair
        };


        public static bool HasUniqueAttribute(this XmlNode node)
        {
            if (node.Attributes != null && node.Attributes.Count > 0)
            {
                foreach (string uniqueAttribute in UniqueAttributes)
                {
                    if (node.Attributes[uniqueAttribute] != null)
                        return true;
                }
            }

            return false;
        }


        public static XmlNode SelectOrCreate(this XmlDocument xmlDocument, string xPath)
        {
            if (String.IsNullOrEmpty(xPath))
                return null;
            return SelectOrCreate(xmlDocument, xmlDocument as XmlNode, xPath.Trim('/').Split('/'));
        }

        public static XmlNode SelectOrCreate(this XmlDocument xmlDocument, XmlNode parentNode, string xPath)
        {
            if (parentNode == null || String.IsNullOrEmpty(xPath))
                return null;
            return SelectOrCreate(xmlDocument, parentNode, xPath.Trim('/').Split('/'));
        }

        internal static XmlNode SelectOrCreate(XmlDocument xmlDocument, XmlNode parentNode, string[] path)
        {
            string nextNodeInXPath = path.FirstOrDefault();
            if (String.IsNullOrEmpty(nextNodeInXPath))
                return parentNode;

            XmlNode node = parentNode.SelectSingleNode(nextNodeInXPath);
            if (node == null)
                node = parentNode.AppendChild(xmlDocument.CreateElement(nextNodeInXPath));


            return SelectOrCreate(xmlDocument, node, path.Skip(1).ToArray());
        }


        /// <summary>
        /// Get XPath to this node from the root node of this one.
        /// </summary>
        /// <returns>XPath string or empty one.</returns>
        public static string ToXPath(this XmlNode node)
        {
            Stack<string> nodeHierarchy = new Stack<string>();

            for (XmlNode xmlNode = node;
                 xmlNode != null && xmlNode.ParentNode != null
                 && !(Object.ReferenceEquals(node, xmlNode) && nodeHierarchy.Count != 0);
                 xmlNode = xmlNode.ParentNode)
            {
                nodeHierarchy.Push(xmlNode.ToXSubpath());
            }

            return (nodeHierarchy.Count == 0) ? String.Empty : String.Format("//{0}", String.Join("/", nodeHierarchy));
        }

        /// <summary>
        /// Get XPath to this node from a given ancestor node.
        /// </summary>
        /// <param name="fromAncestor"></param>
        /// <returns>XPath string or empty one.</returns>
        public static string ToXPath(this XmlNode node, XmlNode fromAncestor)
        {
            Stack<string> nodeHierarchy = new Stack<string>();

            for (XmlNode xmlNode = node;
                 xmlNode != null && xmlNode.ParentNode != null && !Object.ReferenceEquals(xmlNode, fromAncestor)
                 && !(Object.ReferenceEquals(node, xmlNode) && nodeHierarchy.Count != 0);
                 xmlNode = xmlNode.ParentNode)
            {
                nodeHierarchy.Push(xmlNode.ToXSubpath());
            }

            return (nodeHierarchy.Count == 0) ? String.Empty : String.Format("/{0}", String.Join("/", nodeHierarchy));
        }

        /// <summary>
        /// Get this node's partial XPath.
        /// </summary>
        /// <returns>Partial XPath string for this node.</returns>
        public static string ToXSubpath(this XmlNode node)
        {
            List<string> detectedUniqueAttributes = new List<string>();

            if (node.Attributes != null && node.Attributes.Count > 0)
            {
                foreach (string uniqueAttribute in UniqueAttributes)
                {
                    if (node.Attributes[uniqueAttribute] != null)
                    {
                        detectedUniqueAttributes.Add(String.Format("@{0}='{1}'", uniqueAttribute, node.Attributes[uniqueAttribute].Value));
                    }
                }
            }

            if (detectedUniqueAttributes.Count == 0)
                return node.Name;
            else
                return String.Format("{0}[{1}]", node.Name, String.Join(" and ", detectedUniqueAttributes.ToArray()));
        }
    }
}
