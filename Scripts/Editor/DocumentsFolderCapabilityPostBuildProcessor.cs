// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

#if UNITY_WSA || WINDOWS_UWP
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ARETT.Editor
{

	/// <summary>
	/// Unity post build processor which adds the documents folder capability to the build
	/// </summary>
	public class DocumentsFolderCapabilityPostBuildProcessor
	{
		[PostProcessBuild]
		public static void DocumentsFolderCapability(BuildTarget buildTarget, string pathToBuiltProject)
		{
			if (buildTarget == BuildTarget.WSAPlayer)
			{
				// Get AppxManifest
				string manifestFilePath = GetManifestFilePath(pathToBuiltProject);
				if (manifestFilePath == null)
				{
					throw new FileNotFoundException("Unable to find manifest file");
				}

				// Load XML data from manifest
				XElement rootElement = XElement.Load(manifestFilePath);

				// uap namespace
				XNamespace uapNamespace = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
				XNamespace defaultNamespace = rootElement.GetDefaultNamespace();

				//----
				// Add the documents folder capability

				// If the capabilities container tag is missing, make sure it gets added.
				XElement capabilitiesNode = AddXElement(rootElement, defaultNamespace, "Capabilities");

				// Add the document library capability
				AddXElementWithAttribute(capabilitiesNode, uapNamespace, "Capability", "Name", "documentsLibrary", true);


				//----
				// Add the supported files so we can actually access the folder

				// Nodes to the file types
				XElement applicationElement = AddXElement(AddXElement(rootElement, defaultNamespace, "Applications"), defaultNamespace, "Application");
				XElement extensionsNode = AddXElement(applicationElement, defaultNamespace, "Extensions");
				XElement extensionNode = AddXElementWithAttribute(extensionsNode, uapNamespace, "Extension", "Category", "windows.fileTypeAssociation");
				XElement fileTypeAssociationNode = AddXElementWithAttribute(extensionNode, uapNamespace, "FileTypeAssociation", "Name", "eye_tracking_data");
				XElement supportedFileTypesNode = AddXElement(fileTypeAssociationNode, uapNamespace, "SupportedFileTypes");

				// File types
				AddXElementWithAttributeAndValue(supportedFileTypesNode, uapNamespace, "FileType", "ContentType", "text/csv", ".csv");
				AddXElementWithAttributeAndValue(supportedFileTypesNode, uapNamespace, "FileType", "ContentType", "application/json", ".txt");
				AddXElementWithAttributeAndValue(supportedFileTypesNode, uapNamespace, "FileType", "ContentType", "text/plain", ".tmp");

				// Add display name
				AddXElementWithValue(fileTypeAssociationNode, uapNamespace, "DisplayName", "Eye Tracking Data");


				//----
				// Save the new XML data as new manifest
				rootElement.Save(manifestFilePath);

				Debug.Log("[DocumentsFolderCapability] Added capability to save to documents folder!");
			}
		}

		/// <summary>
		/// Add an element to the given base node
		/// </summary>
		private static XElement AddXElement(XElement baseNode, XNamespace elementNamespace, string elementName)
		{
			XElement capabilitiesNode = baseNode.Element(elementNamespace + elementName);
			if (capabilitiesNode == null)
			{
				capabilitiesNode = new XElement(elementNamespace + elementName);
				baseNode.Add(capabilitiesNode);
				//Debug.Log("[DocumentsFolderCapability] Added (" + elementNamespace + "):" + elementName + " element");
			}
			else
			{
				//Debug.Log("[DocumentsFolderCapability] (" + elementNamespace + "):" + elementName + " element already existing!");
			}

			return capabilitiesNode;
		}

		/// <summary>
		/// Add an element with an attribute to the given base node
		/// </summary>
		private static XElement AddXElementWithAttribute(XElement baseNode, XNamespace elementNamespace, string elementName, string attributeName, string attributeValue, bool first = false)
		{
			XElement extensionElement = baseNode.Elements(elementNamespace + elementName).FirstOrDefault(element => element.Attribute(attributeName)?.Value == attributeValue);
			if (extensionElement == null)
			{
				extensionElement = new XElement(elementNamespace + elementName, new XAttribute(attributeName, attributeValue));
				if (first)
				{
					baseNode.AddFirst(extensionElement);
				}
				else
				{
					baseNode.Add(extensionElement);
				}
				//Debug.Log("[DocumentsFolderCapability] Added (" + elementNamespace + "):" + elementName + " element with attribute " + attributeName + " and attributeValue + " + attributeValue);
			}
			else
			{
				//Debug.Log("[DocumentsFolderCapability] (" + elementNamespace + "):" + elementName + " element with attribute " + attributeName + " and attributeValue + " + attributeValue + " already existing!");
			}

			return extensionElement;
		}

		/// <summary>
		/// Add an element with an attribute and a value to the given base node
		/// </summary>
		private static XElement AddXElementWithAttributeAndValue(XElement baseNode, XNamespace elementNamespace, string elementName, string attributeName, string attributeValue, string value)
		{
			XElement extensionElement = baseNode.Elements(elementNamespace + elementName).FirstOrDefault(element => element.Attribute(attributeName)?.Value == attributeValue);
			if (extensionElement == null)
			{
				extensionElement = new XElement(elementNamespace + elementName, new XAttribute(attributeName, attributeValue))
				{
					Value = value
				};
				baseNode.Add(extensionElement);
				//Debug.Log("[DocumentsFolderCapability] Added (" + elementNamespace + "):" + elementName + " element with attribute " + attributeName + ", attributeValue + " + attributeValue + " and value " + value);
			}
			else
			{
				//Debug.Log("[DocumentsFolderCapability] (" + elementNamespace + "):" + elementName + " element with attribute " + attributeName + " and attributeValue + " + attributeValue + " already existing!");
			}

			return extensionElement;
		}

		/// <summary>
		/// Add an element with an value to the given base node
		/// </summary>
		private static XElement AddXElementWithValue(XElement baseNode, XNamespace elementNamespace, string elementName, string value)
		{
			XElement extensionElement = baseNode.Element(elementNamespace + elementName);
			if (extensionElement == null)
			{
				extensionElement = new XElement(elementNamespace + elementName)
				{
					Value = value
				};
				baseNode.Add(extensionElement);
				//Debug.Log("[DocumentsFolderCapability] Added (" + elementNamespace + "):" + elementName + " element with value " + value);
			}
			else
			{
				//Debug.Log("[DocumentsFolderCapability] (" + elementNamespace + "):" + elementName + " element already existing!");
			}

			return extensionElement;
		}

		/// <summary>
		/// Gets the AppX manifest path in the project output directory.
		/// </summary>
		private static string GetManifestFilePath(string pathToBuiltProject)
		{
			var fullPathOutputDirectory = Path.GetFullPath(pathToBuiltProject);
			//Debug.Log($"Searching for appx manifest in {fullPathOutputDirectory}...");

			// Find the manifest, assume the one we want is the first one
			string[] manifests = Directory.GetFiles(fullPathOutputDirectory, "Package.appxmanifest", SearchOption.AllDirectories);

			if (manifests.Length == 0)
			{
				Debug.LogError($"[DocumentsFolderCapability] Unable to find Package.appxmanifest file for build (in path - {fullPathOutputDirectory})");
				return null;
			}

			if (manifests.Length > 1)
			{
				Debug.LogWarning("[DocumentsFolderCapability] Found more than one appxmanifest in the target build folder! Using the first one.");
			}

			return manifests[0];
		}
	}
}
#endif
