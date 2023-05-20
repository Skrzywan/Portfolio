using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;


/// <summary>
/// Backup file creator. Thought this will be faster but, including post creation file importing, boost is negligible
/// </summary>
public class OctreeCreatorUtils
{
	const string ASSET_TEMPLATE_PATH = "./OctNodeTemplate.txt";
	public static void CreateNewAsset(OctreeNode node, string path)
	{
		string fileTemplate = string.Empty;
		UnityEngine.Debug.Log(path);
		FileInfo fileInfo = new FileInfo(path);
		fileInfo.Directory.Create();
		string formatedFile = GetFormatedFile(fileTemplate, node);
		File.WriteAllText(fileInfo.FullName, formatedFile);
	}

	private static string GetFormatedFile(string fileTemplate, OctreeNode node)
	{
		string octreeNodeScriptGUID = string.Empty;
		var allGuids = AssetDatabase.FindAssets($"{nameof(OctreeNode)} t:script");
		octreeNodeScriptGUID = allGuids.FirstOrDefault();
		string center = GetFormatedCenter(node);
		string extents = GetFormatedExtents(node);
		fileTemplate = InsertDataIntoTemplate(octreeNodeScriptGUID, node.name, center, extents, node.level.ToString());

		return fileTemplate;
	}

	private static string InsertDataIntoTemplate(string octreeNodeScriptGUID, string name, string center, string extents, string level)
	{
		string retVal = string.Empty;
		retVal = $"%YAML 1.1\r\n%TAG !u! tag:unity3d.com,2011:\r\n--- !u!114 &11400000\r\nMonoBehaviour:\r\n  m_ObjectHideFlags: 0\r\n  m_CorrespondingSourceObject: {{fileID: 0}}\r\n  m_PrefabInstance: {{fileID: 0}}\r\n  m_PrefabAsset: {{fileID: 0}}\r\n  m_GameObject: {{fileID: 0}}\r\n  m_Enabled: 1\r\n  m_EditorHideFlags: 0\r\n  m_Script: {{fileID: 11500000, guid: {octreeNodeScriptGUID}, type: 3}}\r\n  m_Name: {name}\r\n  m_EditorClassIdentifier: \r\n  bounds:\r\n    m_Center: {center}\r\n    m_Extent: {extents}\r\n  verts: []\r\n  childrenReferences: []\r\n  level: {level}\r\n";
		return retVal;
	}

	private static string GetFormatedExtents(OctreeNode node)
	{

		//string formatedExt = node.bounds.extents.ToString();
		string formatedX = node.bounds.extents.x.ToString("G", new CultureInfo("en-US"));
		string formatedY = node.bounds.extents.y.ToString("G", new CultureInfo("en-US"));
		string formatedZ = node.bounds.extents.z.ToString("G", new CultureInfo("en-US"));
		string formatedExt = $"{{x: {formatedX}, y: {formatedY}, z: {formatedZ}}}";
		return formatedExt;
	}

	private static string GetFormatedCenter(OctreeNode node)
	{
		//string formatedCenter = node.bounds.center.ToString();
		string formatedX = node.bounds.center.x.ToString("G", new CultureInfo("en-US"));
		string formatedY = node.bounds.center.y.ToString("G", new CultureInfo("en-US"));
		string formatedZ = node.bounds.center.z.ToString("G", new CultureInfo("en-US"));
		string formatedCenter = $"{{x: {formatedX}, y: {formatedY}, z: {formatedZ}}}";
		return formatedCenter;
	}
}
