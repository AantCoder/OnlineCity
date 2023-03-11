using OpenCvSharp;
using Sidekick.Sidekick.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUITest
{
    public class LoaderImage : ILoaderImage
    {
        private string FileNameMask;

        private ConcurrentDictionary<string, OCVImage> Base = new ConcurrentDictionary<string, OCVImage>();

        public LoaderImage(string fileNameMask)
        {
            FileNameMask = fileNameMask;
        }

        public OCVImage Get(string name)
            => Base.GetOrAdd(name.ToLower(), n => OCVImage.CreateFromFile(string.Format(FileNameMask, n)));

        public void Dispose()
        {
            foreach (var image in Base.Values) image.Dispose();
        }
    }
}
