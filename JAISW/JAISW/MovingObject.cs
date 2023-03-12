using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static JAISW.Globals;
namespace JAISW
{
    public class MovingObject
    {

        public int X;
        public int Y;
        public int Height;
        public int ScreenX;
        public int AltFrame;
        public int Speed;
        public System.Windows.Controls.Image Sprite;
        public bool IsJumping;
        public bool IsDropping;
        public int JumpSpeed;
        public bool IsBrush;

        public void Jump()
        {
            if (IsDropping || IsJumping)
            {
                return;
            }

            IsJumping = true;
            JumpSpeed = 34;

        }

        public void ContinueJump(List<WorldBlock> blocks)
        {

            if (!IsJumping)
            {
                return;
            }

            bool hitsHead = false;
            foreach (WorldBlock b in blocks)
            {
                if (!b.Vanishes || b.VanishOpacity > 0)
                {
                    if (b.X + BLOCK_WIDTH > ScreenX && b.X + BLOCK_WIDTH < ScreenX + JORD_WIDTH * 2)
                    {
                        if (b.Y < Y && b.Y + BLOCK_HEIGHT > Y - JumpSpeed)
                        {
                            JumpSpeed = Y - (b.Y + BLOCK_HEIGHT);
                            hitsHead = true;
                            break;
                        }
                    }
                }
            }

            Y -= JumpSpeed;
            Canvas.SetTop(Sprite, Y);
            JumpSpeed -= 3;
            if (JumpSpeed <= 0 || hitsHead)
            {
                JumpSpeed = 0;
                IsJumping = false;
                IsDropping = true;
            }

        }

        public void ContinueDrop(List<WorldBlock> blocks)
        {

            if (!IsDropping)
            {
                return;
            }

            if (JumpSpeed < 34)
            {
                JumpSpeed += 4;
            }

            bool hitsFeet = false;
            int landedY = 0;
            foreach (WorldBlock b in blocks)
            {
                if (!b.Vanishes || b.VanishOpacity > 0)
                {
                    if (b.X + BLOCK_WIDTH > ScreenX && b.X + BLOCK_WIDTH < ScreenX + JORD_WIDTH * 2)
                    {
                        //if (b.Y > Y + Height && b.Y < Y + Height + JumpSpeed)
                        if (b.Y > Y && b.Y < Y + Height + JumpSpeed)
                        {
                            JumpSpeed = (b.Y - Height - Y);
                            hitsFeet = true;
                            landedY = b.Y - (Height + 1);
                            break;
                        }
                    }
                }
            }

            Y += JumpSpeed;

            if (hitsFeet)
            {
                JumpSpeed = 0;
                IsDropping = false;
                Y = landedY;
                Canvas.SetTop(Sprite, Y);
            }
            else
            {
                Canvas.SetTop(Sprite, Y);
            }

        }

    }
}
