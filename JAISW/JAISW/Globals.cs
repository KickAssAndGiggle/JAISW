using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAISW
{
    public static class Globals
    {

        public const int JORD_HEIGHT = 128;
        public const int JORD_WIDTH = 64;

        public const int BLOCK_HEIGHT = 64;
        public const int BLOCK_WIDTH = 64;

        public struct WorldBlock : IComparable<WorldBlock>
        {
            public int X;
            public int Y;
            public bool Vanishes;
            public int VanishFrame;
            public double VanishOpacity;
            public System.Windows.Controls.Image Sprite;
            public int CompareTo(WorldBlock other)
            {
                return Y.CompareTo(other.Y);
            }
        }

    }
}
