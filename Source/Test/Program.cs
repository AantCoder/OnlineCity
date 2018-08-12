using OCServer;
using OCUnion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Transfer;
using Util;

namespace Test
{
    class Program
    {
        [Serializable]
        public class T1
        {
            public int I;
            public object S;
        }
              

        static void Main(/*string[] args*/)
        {
            var m = new byte[0];
            /*
            var args = "12''3  4'56 '78 90' aa";
            //var args = "' 12 '' 34 '56 '78 90'";
            var sm = Service.SplitBySpace(args);
            */

            Console.ReadKey();
        }
    }
}
