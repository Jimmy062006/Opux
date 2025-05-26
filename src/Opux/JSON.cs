﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace JSONStuff
{
	public static class JSON
	{
		public static string XmlToJSON(string xml)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);

			return XmlToJSON(doc);
		}
		public static string XmlToJSON(XmlDocument xmlDoc)
		{
			StringBuilder sbJSON = new StringBuilder();
			sbJSON.Append("{ ");
			XmlToJSONnode(sbJSON, xmlDoc.DocumentElement, true);
			sbJSON.Append("}");
			return sbJSON.ToString();
		}

		//  XmlToJSONnode:  Output an XmlElement, possibly as part of a higher array
		private static void XmlToJSONnode(StringBuilder sbJSON, XmlElement node, bool showNodeName)
		{
			if (showNodeName)
				sbJSON.Append("\"" + SafeJSON(node.Name) + "\": ");
			sbJSON.Append("{");
			// Build a sorted list of key-value pairs
			//  where   key is case-sensitive nodeName
			//          value is an ArrayList of string or XmlElement
			//  so that we know whether the nodeName is an array or not.
			SortedList<string, object> childNodeNames = new SortedList<string, object>();

			//  Add in all node attributes
			if (node.Attributes != null)
				foreach (XmlAttribute attr in node.Attributes)
					StoreChildNode(childNodeNames, attr.Name, attr.InnerText);

			//  Add in all nodes
			foreach (XmlNode cnode in node.ChildNodes)
			{
				if (cnode is XmlText)
					StoreChildNode(childNodeNames, "value", cnode.InnerText);
				else if (cnode is XmlElement)
					StoreChildNode(childNodeNames, cnode.Name, cnode);
				else if (cnode is XmlCDataSection)
					StoreChildNode(childNodeNames, cnode.Name, cnode.InnerText);
			}

			// Now output all stored info
			foreach (string childname in childNodeNames.Keys)
			{
				List<object> alChild = (List<object>)childNodeNames[childname];
				if (alChild.Count == 1)
					OutputNode(childname, alChild[0], sbJSON, true);
				else
				{
					sbJSON.Append(" \"" + SafeJSON(childname) + "\": [ ");
					foreach (object child in alChild)
						OutputNode(childname, child, sbJSON, false);
					sbJSON.Remove(sbJSON.Length - 2, 2);
					sbJSON.Append(" ], ");
				}
			}
			sbJSON.Remove(sbJSON.Length - 2, 2);
			sbJSON.Append(" }");
		}

		//  StoreChildNode: Store data associated with each nodeName
		//                  so that we know whether the nodeName is an array or not.
		private static void StoreChildNode(SortedList<string, object> childNodeNames, string nodeName, object nodeValue)
		{
			// Pre-process contraction of XmlElement-s
			if (nodeValue is XmlElement)
			{
				// Convert  <aa></aa> into "aa":null
				//          <aa>xx</aa> into "aa":"xx"
				XmlNode cnode = (XmlNode)nodeValue;
				if (cnode.Attributes.Count == 0)
				{
					XmlNodeList children = cnode.ChildNodes;
					if (children.Count == 0)
						nodeValue = null;
					else if (children.Count == 1 && (children[0] is XmlText text))
						nodeValue = text.InnerText;
				}
			}
			// Add nodeValue to ArrayList associated with each nodeName
			// If nodeName doesn't exist then add it
			List<object> valuesAL;

			if (childNodeNames.ContainsKey(nodeName))
			{
				valuesAL = (List<object>)childNodeNames[nodeName];
			}
			else
			{
				valuesAL = new List<object>();
				childNodeNames[nodeName] = valuesAL;
			}
			valuesAL.Add(nodeValue);
		}

		private static void OutputNode(string childname, object alChild, StringBuilder sbJSON, bool showNodeName)
		{
			if (alChild == null)
			{
				if (showNodeName)
					sbJSON.Append("\"" + SafeJSON(childname) + "\": ");
				sbJSON.Append("null");
			}
			else if (alChild is string @string)
			{
				if (showNodeName)
					sbJSON.Append("\"" + SafeJSON(childname) + "\": ");
				string sChild = @string;
				sChild = sChild.Trim();
				sbJSON.Append("\"" + SafeJSON(sChild) + "\"");
			}
			else
				XmlToJSONnode(sbJSON, (XmlElement)alChild, showNodeName);
			sbJSON.Append(", ");
		}

		// Make a string safe for JSON
		private static string SafeJSON(string sIn)
		{
			StringBuilder sbOut = new StringBuilder(sIn.Length);
			foreach (char ch in sIn)
			{
				if (Char.IsControl(ch) || ch == '\'')
				{
					int ich = (int)ch;
					sbOut.Append(@"\u" + ich.ToString("x4"));
					continue;
				}
				else if (ch == '\"' || ch == '\\' || ch == '/')
				{
					sbOut.Append('\\');
				}
				sbOut.Append(ch);
			}
			return sbOut.ToString();
		}
	}
}
