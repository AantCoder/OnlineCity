using OCUnion;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    internal class TestClass
    {
        public Texture2D Exec()
        {
            try
            {
                var steamAccountID = SteamUser.GetSteamID().GetAccountID().m_AccountID;
                var language = LanguageDatabase.activeLanguage.folderName;
                Loger.Log($"TestClass Run steamAccountID={steamAccountID} language={language}");
                var request = MakeRequest("Полная girl Повар со светлыми волосами до плеч в зеленой одежде age 12"
                    //мысли использовать: woman/girl вместо female.  "beautiful portrait of a human"
                    //test6 "Полная girl Повар со светлыми волосами до плеч age 12" //"beautiful portrait of a human"  
                    //test5 "girl Повар, age 12, полное телосложение, блондинка, волосы средней длинны" //"beautiful full length of a human"  
                    //test4 "girl Повар, age 12, худое телосложение" //"beautiful portrait full length of a human"  "beautiful portrait of a human"
                    //test3 "female, 51, human appearance, against the background of something \"charming killer\", likes close combat, crop production, communication" //"beautiful portrait of a man"
                    //test2 "Женщина Убийца, возраст 18" //"beautiful portrait of a man"
                    //test1 "Wildlife Ranger Age: 44. Shooting" //"beautiful portrait of a man"
                    , "beautiful portrait of a human"
                    , steamAccountID, language);
                /*
                green — 3enéHbin
                blue — cuHun, ronyoon
                yellow — *énTEIK
                red — KpacHbin
                gray — cepbii
                orange — opaHKeBbIi
                black — 4épHbin
                violet — PuoneToBbInK
                pink — posoBbli
                purple — cupeHesoin
                brown — Kopu4Hesbin
                white — Obenbii
                */

                using (var response = request.GetResponse())
                {
                    using (var rsDataStream = response.GetResponseStream())
                    {
                        return ProcessResponse(rsDataStream, response.ContentType);
                    }
                }
            }
            catch (Exception e)
            {
                Loger.Log("TestClass Exception:" + e.Message);
                return null;
            }
        }

        private static WebRequest MakeRequest(string artDescription, string thingDescription, uint steamAccountID,
            string language)
        {
            var serverUrl = "https://boriselec.com/rimworld-art/generate";
            var request = WebRequest.Create(serverUrl);
            request.Method = "POST";
            var postData = artDescription + ';' + thingDescription + ';' + steamAccountID + ';' + language;
            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "text/plain";
            request.ContentLength = byteArray.Length;
            using (var rqDataStream = request.GetRequestStream())
            {
                rqDataStream.Write(byteArray, 0, byteArray.Length);
                rqDataStream.Close();
            }
            return request;
        }

        private static Texture2D ProcessResponse(Stream response, string contentType)
        {
            if (response == null)
            {
                return null;
            }

            switch (contentType)
            {
                case "text":
                case "text/plain":
                    using (var reader = new StreamReader(response))
                    {
                        var responseFromServer = reader.ReadToEnd();
                        Loger.Log("TestClass responseFromServer:" + responseFromServer);
                        return null;
                    }
                case "image/png":
                    using (var ms = new MemoryStream())
                    {
                        response.CopyTo(ms);
                        var array = ms.ToArray();
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.Alpha8, true);
                        tex.LoadImage(array);
                        tex.Apply();
                        Loger.Log("TestClass Image " + array?.Length);
                        return tex;
                    }
                default:
                    return null;
            }
        }
    }
}
