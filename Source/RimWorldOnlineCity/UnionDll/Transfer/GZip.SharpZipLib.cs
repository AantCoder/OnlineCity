using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using ICSharpCode.SharpZipLib.Core;

namespace Util
{
    public static partial class GZip
    {
        static GZip()
        {
            //ZipStrings.UseUnicode = true;
            ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
        }

        // Compresses the supplied memory stream, naming it as zipEntryName, into a zip,
        // which is returned as a memory stream or a byte array.
        //
        private static MemoryStream CreateToStream(Stream memStreamIn, string zipEntryName)
        {
            MemoryStream outputMemStream = new MemoryStream();
            ZipOutputStream zipStream = new ZipOutputStream(outputMemStream);

            zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

            ZipEntry newEntry = new ZipEntry(zipEntryName);
            newEntry.DateTime = DateTime.Now;

            zipStream.PutNextEntry(newEntry);

            StreamUtils.Copy(memStreamIn, zipStream, new byte[4096]);
            zipStream.CloseEntry();

            zipStream.IsStreamOwner = false;    // False stops the Close also Closing the underlying stream.
            zipStream.Close();          // Must finish the ZipOutputStream before using outputMemStream.

            outputMemStream.Position = 0;
            return outputMemStream;
        }

        private static MemoryStream UnpackFromStream(Stream zipStream)
        {
            MemoryStream outputMemStream = new MemoryStream();

            ZipInputStream zipInputStream = new ZipInputStream(zipStream);
            ZipEntry zipEntry = zipInputStream.GetNextEntry();

            byte[] buffer = new byte[4096];     // 4K is optimum

            StreamUtils.Copy(zipInputStream, outputMemStream, buffer);
            
            return outputMemStream;
        }
    }
}
