﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Verse;

namespace OCUnion
{
    public class GameXMLUtils
    {
        public static bool FromXmlIsActive = false;
        public StringBuilder OutXml;
        private string rootElementName = "data";
        private static object SuncObj = new Object();

        public GameXMLUtils()
        {
            Scribe.ForceStop();
            if (ScribeMetaHeaderUtility.loadedGameVersion == null) ScribeMetaHeaderUtility.loadedGameVersion = "";
        }

        public string ToXml(IExposable t)
        {
            lock (SuncObj)
            {
                var dataXML = Scribe.saver.DebugOutputFor(t);
                XmlDocument xmlDocument = new XmlDocument();
                try
                {
                    xmlDocument.LoadXml(dataXML);
                }
                catch (Exception ex)
                {
                    var log = $"ExceptionXML ToXml<{t.GetType().Name}>: " + ex.Message + Environment.NewLine + "----------" + Environment.NewLine + dataXML + Environment.NewLine + "----------";
                    Loger.Log(log);
                    Loger.TransLog(log);

                    Thread.Sleep(1);
                    dataXML = Scribe.saver.DebugOutputFor(t);
                    xmlDocument = new XmlDocument();
                    try
                    {
                        xmlDocument.LoadXml(dataXML);
                    }
                    catch (Exception ex2)
                    {
                        log = $"ExceptionXML ToXml<{t.GetType().Name}>: " + ex2.Message + Environment.NewLine + "----------" + Environment.NewLine + dataXML + Environment.NewLine + "----------";
                        Loger.Log(log);
                        Loger.TransLog("ExceptionXML ToXml: " + ex2.Message);
                    }
                }

                return @"<?xml version=""1.0"" encoding=""utf-8""?>
<" + rootElementName + @"> 
"
                    + dataXML
                    + @"
</" + rootElementName + ">";
            }
        }

        public T FromXml<T>(string dataXML)
            where T : new()
        {
            lock (SuncObj)
            {
                XmlDocument xmlDocument = new XmlDocument();
                try
                {
                    xmlDocument.LoadXml(dataXML);
                }
                catch (Exception ex)
                {
                    var log = $"ExceptionXML FromXml<{typeof(T).Name}>: " + ex.Message + Environment.NewLine + "----------" + Environment.NewLine + dataXML + Environment.NewLine + "----------";
                    Loger.Log(log);
                    Loger.TransLog(log);
                    //throw;

                    xmlDocument.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<data> 
</data>");
                }
                Scribe.loader.curXmlParent = xmlDocument.DocumentElement;
                Scribe.mode = LoadSaveMode.LoadingVars;
                try
                {
                    /*
                    bool flag = typeof(T).IsValueType || typeof(Name).IsAssignableFrom(typeof(T));
                    if (!flag)
                    {
                        Scribe.loader.crossRefs.RegisterForCrossRefResolve(exposable);
                    }*/
                    FromXmlIsActive = true;
                    Scribe.EnterNode(rootElementName);
                    var thing = new T();
                    Scribe_Deep.Look<T>(ref thing, "saveable", new object[0]);

                    // Scribe.loader.crossRefs.ResolveAllCrossReferences()

                    return thing;
                }
                finally
                {
                    try
                    {
                        //Finish()
                        Scribe.loader.FinalizeLoading();
                    }
                    finally
                    {
                        FromXmlIsActive = false;
                    }
                }
            }
        }

        public void StartFromXml(string filePath)
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                using (XmlTextReader xmlTextReader = new XmlTextReader(streamReader))
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(xmlTextReader);
                    Scribe.loader.curXmlParent = xmlDocument.DocumentElement;
                }
            }
            Scribe.mode = LoadSaveMode.LoadingVars;
        }

        public T Test<T>()
            where T : new()
        {
            Scribe.EnterNode(rootElementName);
            var thing = new T();
            Scribe_Deep.Look<T>(ref thing, "saveable", new object[0]);
            return thing;
        }

        public void Finish()
        {
            Scribe.loader.FinalizeLoading();
        }

        public static string GetByTag(string xml, string tagName, string afterText = null)
        {
            int after = string.IsNullOrEmpty(afterText) ? 0 : xml.IndexOf(afterText);
            if (after < 0) after = 0;

            var tagNameB = "<" + tagName + ">";
            int pos = xml.IndexOf(tagNameB, after);
            if (pos < 0) return null;
            pos += tagNameB.Length;

            var tagNameE = "</" + tagName + ">";
            int posE = xml.IndexOf(tagNameE, pos);
            if (posE < 0) return null;

            return xml.Substring(pos, posE - pos);
        }

        public static string ReplaceByTag(string xml, string tagName, string newValue, string afterText = null)
        {
            int after = string.IsNullOrEmpty(afterText) ? 0 : xml.IndexOf(afterText);
            if (after < 0) after = 0;

            var tagNameB = "<" + tagName + ">";
            int pos = xml.IndexOf(tagNameB, after);
            if (pos < 0) return xml;
            pos += tagNameB.Length;
            
            var tagNameE = "</" + tagName + ">";
            int posE = xml.IndexOf(tagNameE, pos);
            if (posE < 0) return xml;

            return xml.Substring(0, pos) + newValue + xml.Substring(posE);
        }

        public static string ReplaceByTag(string xml, string tagName 
            , Func<string, string> getNewValue)
        {
            var tagNameB = "<" + tagName + ">";
            int pos = xml.IndexOf(tagNameB);
            if (pos < 0) return xml;
            pos += tagNameB.Length;

            var tagNameE = "</" + tagName + ">";
            int posE = xml.IndexOf(tagNameE, pos);
            if (posE < 0) return xml;

            var newValue = getNewValue(xml.Substring(pos, posE - pos));
            if (newValue == null) return xml;

            return xml.Substring(0, pos) + newValue + xml.Substring(posE);
        }
    }
}
