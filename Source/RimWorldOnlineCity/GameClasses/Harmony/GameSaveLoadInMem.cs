using HarmonyLib;
using OCUnion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace RimWorldOnlineCity.GameClasses.Harmony
{
    [HarmonyPatch(typeof(ScribeLoader))]
    [HarmonyPatch("InitLoading")]
    internal class ScribeLoader_InitLoading_Patch
    {
		public static byte[] LoadData = null;
		public static bool Enable = false;

        [HarmonyPrefix]
        public static bool Prefix(ScribeLoader __instance, string filePath)
		{
			if (!Enable) return true;
			Loger.Log("ScribeLoader_InitLoading_Patch Start");
			if (Scribe.mode != 0)
			{
				Log.Error("Called InitLoading() but current mode is " + Scribe.mode);
				Scribe.ForceStop();
			}
			if (__instance.curParent != null)
			{
				Log.Error("Current parent is not null in InitLoading");
				__instance.curParent = null;
			}
			if (__instance.curPathRelToParent != null)
			{
				Log.Error("Current path relative to parent is not null in InitLoading");
				__instance.curPathRelToParent = null;
			}
			try
			{
				using (var input = new MemoryStream(LoadData))
				//using (StreamReader input = new StreamReader(filePath))
				{
					using (XmlTextReader reader = new XmlTextReader(input))
					{
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.Load(reader);
						__instance.curXmlParent = xmlDocument.DocumentElement;
					}
				}
				Scribe.mode = LoadSaveMode.LoadingVars;
			}
			catch (Exception ex)
			{
				Log.Error("Exception while init loading file: " + filePath + "\n" + ex);
				__instance.ForceStop();
				throw;
			}
			Loger.Log("ScribeLoader_InitLoading_Patch End");
			return false;
		}

	}

	[HarmonyPatch(typeof(ScribeLoader))]
	[HarmonyPatch("InitLoadingMetaHeaderOnly")]
	internal class ScribeLoader_InitLoadingMetaHeaderOnly_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(ScribeLoader __instance, string filePath)
		{
			if (!ScribeLoader_InitLoading_Patch.Enable) return true;
			Loger.Log("ScribeLoader_InitLoadingMetaHeaderOnly_Patch Start");

			if (Scribe.mode != 0)
			{
				Log.Error("Called InitLoadingMetaHeaderOnly() but current mode is " + Scribe.mode);
				Scribe.ForceStop();
			}
			try
			{
				using (var input = new MemoryStream(ScribeLoader_InitLoading_Patch.LoadData))
				//using (StreamReader input = new StreamReader(filePath))
				{
					using (XmlTextReader xmlTextReader = new XmlTextReader(input))
					{
						if (!ScribeMetaHeaderUtility.ReadToMetaElement(xmlTextReader))
						{
							return false;
						}
						using (XmlReader reader = xmlTextReader.ReadSubtree())
						{
							XmlDocument xmlDocument = new XmlDocument();
							xmlDocument.Load(reader);
							XmlElement xmlElement = xmlDocument.CreateElement("root");
							xmlElement.AppendChild(xmlDocument.DocumentElement);
							__instance.curXmlParent = xmlElement;
						}
					}
				}
				Scribe.mode = LoadSaveMode.LoadingVars;
			}
			catch (Exception ex)
			{
				Log.Error("Exception while init loading meta header: " + filePath + "\n" + ex);
				__instance.ForceStop();
				throw;
			}

			Loger.Log("ScribeLoader_InitLoadingMetaHeaderOnly_Patch End");
			return false;
		}

	}

	[HarmonyPatch(typeof(ScribeSaver))]
	[HarmonyPatch("InitSaving")]
	internal class ScribeSaver_InitSaving_Patch
	{
		public static MemoryStream SaveData;

		public static bool Enable = false;

		[HarmonyPrefix]
		public static bool Prefix(ScribeSaver __instance, string filePath, string documentElementName)
		{
			if (!Enable) return true;

			Loger.Log("ScribeSaver_InitSaving_Patch Start");
			var that = Traverse.Create(__instance);

			if (Scribe.mode != 0)
			{
				Log.Error("Called InitSaving() but current mode is " + Scribe.mode);
				Scribe.ForceStop();
			}
			if (that.Field("curPath").GetValue<string>() != null)
			{
				Log.Error("Current path is not null in InitSaving");
				that.Field("curPath").SetValue(null);
				that.Field("savedNodes").GetValue<HashSet<string>>().Clear();
				that.Field("nextListElementTemporaryId").SetValue(0);
			}
			try
			{
				Scribe.mode = LoadSaveMode.Saving;
				var saveStream = SaveData = new MemoryStream();
				//var saveStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
				File.WriteAllText(filePath, "Online save");
				that.Field("saveStream").SetValue(saveStream);

				XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
				xmlWriterSettings.Indent = true;
				xmlWriterSettings.IndentChars = "\t";
				var writer = XmlWriter.Create(saveStream, xmlWriterSettings);
				that.Field("writer").SetValue(writer);

				writer.WriteStartDocument();
				__instance.EnterNode(documentElementName);
			}
			catch (Exception ex)
			{
				Log.Error("Exception while init saving file: " + filePath + "\n" + ex);
				__instance.ForceStop();
				throw;
			}
			Loger.Log("ScribeSaver_InitSaving_Patch End");
			return false;
		}

	}
}
