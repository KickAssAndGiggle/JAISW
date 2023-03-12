using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAISW
{
    public class SinWaveFlier : MovingObject
    {

        public Bitmap SpriteImg;
        public bool Active;

        public int HorizY;
        public int HorizX;

        public int SinFrame;
        public bool YOnly;

        public void CalculatePosition()
        {

            double angle = 270 + (3.14 / 2.0);
            double dist = 80 * (Math.Sin(SinFrame * 3));

            if (YOnly)
            {
                X = HorizX;
                Y = Convert.ToInt32(HorizY + Math.Sin(angle) + dist);
            }
            else
            {
                X = Convert.ToInt32(HorizX + Math.Cos(angle) + dist);
                Y = Convert.ToInt32(HorizY + Math.Sin(angle) + dist);
            }

        }

        //public void CalculatePosition()
        //{

        //    double angle = 270 + (3.14 / 2.0);
        //    double dist = 80 * (Math.Sin(SinFrame * 3));



        //}

    }
}
