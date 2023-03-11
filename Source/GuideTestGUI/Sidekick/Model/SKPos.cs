using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Sidekick.Sidekick.Model
{
    public class SKPos
    {
        public int X;
        public int Y;

        public SKPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Point Point
        {
            get { return new Point(X, Y); }
        }

        public override string ToString()
        {
            return "(" + X.ToString() + "," + Y.ToString() + ")";
        }

        public long GetHash()
        {
            return X << 32 + Y;
        }
    }
}
