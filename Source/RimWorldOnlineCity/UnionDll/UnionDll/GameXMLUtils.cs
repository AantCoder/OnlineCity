using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Verse;

namespace OCUnion
{
    public class GameXMLUtils
    {
        public StringBuilder OutXml;
        private string rootElementName = "data";

        public GameXMLUtils()
        {
            Scribe.ForceStop();
            if (ScribeMetaHeaderUtility.loadedGameVersion == null) ScribeMetaHeaderUtility.loadedGameVersion = "";
        }

        public string ToXml(IExposable t)
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<" + rootElementName + @"> 
"
                + Scribe.saver.DebugOutputFor(t)
                + @"
</" + rootElementName + ">";
        }

        public T FromXml<T>(string dataXML)
            where T : new()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(dataXML);
            Scribe.loader.curXmlParent = xmlDocument.DocumentElement;
            Scribe.mode = LoadSaveMode.LoadingVars;
            try
            {
                Scribe.EnterNode(rootElementName);
                var thing = new T();
                Scribe_Deep.Look<T>(ref thing, "saveable", new object[0]);
                return thing;
            }
            finally
            {
                //Finish()
                Scribe.loader.FinalizeLoading();
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

    }
}
