using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Controls;

namespace JAISW
{
    public class ImageManager
    {

        public Bitmap MainTitle;

        public Bitmap ParallaxBackground;
        public Bitmap ParallaxMiddle;
        public Bitmap ParallaxForeground;
        public Bitmap Sky;

        public Bitmap JordStand;
        public Bitmap JordWalkOne;
        public Bitmap JordWalkTwo;
        public Bitmap JordCrouch;
        public Bitmap JordDead;

        public Bitmap StormBlock;
        public Bitmap VanishBlock;

        public Bitmap Wasp1;
        public Bitmap Wasp2;

        public Bitmap Spider1;
        public Bitmap Spider2;

        public Bitmap Cricket1;
        public Bitmap Cricket2;

        public Bitmap Tea1;
        public Bitmap Tea2;
        public Bitmap Tea3;

        public Bitmap Cheese1;
        public Bitmap Cheese2;
        public Bitmap Cheese3;

        public Bitmap Idle1;
        public Bitmap Idle2;
        public Bitmap Idle3;
        public Bitmap Idle4;
        public Bitmap Idle5;
        public Bitmap Idle6;
        public Bitmap Idle7;
        public Bitmap Idle8;

        public Bitmap FinishLine;
        public Bitmap LevelComplete;

        public Bitmap TitleCard1;
        public Bitmap TitleCard2;
        public Bitmap TitleCard3;
        public Bitmap VictoryCard;

        public Bitmap Boss;
        public Bitmap Brush;


        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);


        public ImageManager(string imageFolder) 
        { 
        
            if (!imageFolder.EndsWith("\\"))
            {
                imageFolder += "\\";
            }

            MainTitle = (Bitmap)Bitmap.FromFile(imageFolder + "Title.png");

            ParallaxBackground = (Bitmap)Bitmap.FromFile(imageFolder + "ParallaxBack.png");
            ParallaxMiddle = (Bitmap)Bitmap.FromFile(imageFolder + "ParallaxMiddle.png");
            ParallaxForeground = (Bitmap)Bitmap.FromFile(imageFolder + "ParallaxFront.png");
            Sky = (Bitmap)Bitmap.FromFile(imageFolder + "Sky.png");

            JordStand = (Bitmap)Bitmap.FromFile(imageFolder + "JordStand.png");
            JordWalkOne = (Bitmap)Bitmap.FromFile(imageFolder + "JordWalk1.png");
            JordWalkTwo = (Bitmap)Bitmap.FromFile(imageFolder + "JordWalk2.png");
            JordCrouch = (Bitmap)Bitmap.FromFile(imageFolder + "JordCrouch.png");
            JordDead = (Bitmap)Bitmap.FromFile(imageFolder + "Dead.png");

            Wasp1 = (Bitmap)Bitmap.FromFile(imageFolder + "Wasp1.png");
            Wasp2 = (Bitmap)Bitmap.FromFile(imageFolder + "Wasp2.png");

            Spider1 = (Bitmap)Bitmap.FromFile(imageFolder + "Spider1.png");
            Spider2 = (Bitmap)Bitmap.FromFile(imageFolder + "Spider2.png");

            Cricket1 = (Bitmap)Bitmap.FromFile(imageFolder + "Cricket1.png");
            Cricket2 = (Bitmap)Bitmap.FromFile(imageFolder + "Cricket2.png");

            Tea1 = (Bitmap)Bitmap.FromFile(imageFolder + "Tea1.png");
            Tea2 = (Bitmap)Bitmap.FromFile(imageFolder + "Tea2.png");
            Tea3 = (Bitmap)Bitmap.FromFile(imageFolder + "Tea3.png");

            Cheese1 = (Bitmap)Bitmap.FromFile(imageFolder + "Cheese1.png");
            Cheese2 = (Bitmap)Bitmap.FromFile(imageFolder + "Cheese2.png");
            Cheese3 = (Bitmap)Bitmap.FromFile(imageFolder + "Cheese3.png");

            StormBlock = (Bitmap)Bitmap.FromFile(imageFolder + "StormBlock.png");
            VanishBlock = (Bitmap)Bitmap.FromFile(imageFolder + "VanishBlock.png");
            FinishLine = (Bitmap)Bitmap.FromFile(imageFolder + "Finish.png");

            Idle1 = (Bitmap)Bitmap.FromFile(imageFolder + "Idle1.png");
            Idle2 = (Bitmap)Bitmap.FromFile(imageFolder + "Idle2.png");
            Idle3 = (Bitmap)Bitmap.FromFile(imageFolder + "Idle3.png");
            Idle4 = (Bitmap)Bitmap.FromFile(imageFolder + "Idle4.png");
            Idle5 = (Bitmap)Bitmap.FromFile(imageFolder + "Idle5.png");
            Idle6 = (Bitmap)Bitmap.FromFile(imageFolder + "Idle6.png");
            Idle7 = (Bitmap)Bitmap.FromFile(imageFolder + "Idle7.png");
            Idle8 = (Bitmap)Bitmap.FromFile(imageFolder + "Idle8.png");

            LevelComplete = (Bitmap)Bitmap.FromFile(imageFolder + "LevelComplete.png");
            TitleCard1 = (Bitmap)Bitmap.FromFile(imageFolder + "Level1.png");
            TitleCard2 = (Bitmap)Bitmap.FromFile(imageFolder + "Level2.png");
            TitleCard3 = (Bitmap)Bitmap.FromFile(imageFolder + "Level3.png");

            Boss = (Bitmap)Bitmap.FromFile(imageFolder + "Boss.png");
            Brush = (Bitmap)Bitmap.FromFile(imageFolder + "Brush.png");

            VictoryCard = (Bitmap)Bitmap.FromFile(imageFolder + "Victory.png");

        }


        public ImageSource ImageSourceForBitmap(Bitmap BMP)
        {
            var Handle = BMP.GetHbitmap();
            try
            {
                ImageSource NewSource = Imaging.CreateBitmapSourceFromHBitmap(Handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(Handle);
                return NewSource;
            }
            catch
            {
                DeleteObject(Handle);
                return null;
            }
        }

    }
}
