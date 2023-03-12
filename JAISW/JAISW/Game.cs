using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Xml;
using static JAISW.Globals;
namespace JAISW
{
    public class Game
    {

        private enum Action
        {
            Standing = 0,
            Running = 1,
            Jumping = 2
        }

        private enum Reasons
        {
            LevelComplete = 0,
            GameComplete = 1
        }

        private struct AirPoint
        {
            public int Row;
            public int Column;
            public bool Vanisher;
        }

        string _appFolder = AppDomain.CurrentDomain.BaseDirectory;

        private Canvas _canv;
        private Window _window;
        private ImageManager _img;
        private MusicPlayer _music;
        private Jord _jord;
        private List<UIElement> _controls = new();

        private System.Windows.Controls.Image _prlBgOne;
        private System.Windows.Controls.Image _prlMidOne;
        private System.Windows.Controls.Image _prlFgOne;
        private System.Windows.Controls.Image _prlBgTwo;
        private System.Windows.Controls.Image _prlMidTwo;
        private System.Windows.Controls.Image _prlFgTwo;

        private System.Windows.Controls.Image _sky;

        private bool _rightPressed;
        private bool _leftPressed;
        private bool _downPressed;
        private bool _hasBoss;
        private System.Windows.Controls.Image _boss;
        private int _bossX;
        private bool _bossActive = false;
        private int _bossFrame = 0;
        private int _brushes = 0;

        private Dispatcher _disp;

        private bool _isTitle;
        private bool _isLevel;
        private bool _isDeadScreen;
        private bool _isVictory;
        private int _level = 0;
        private int _deathCount;
        private bool _stopLoop = false;

        private List<WorldBlock> _blocks = new();

        private List<HorizontalFlier> _horizontalFliers = new();
        private List<Crawler> _crawlers = new();
        private List<Jumper> _jumpers = new();
        private List<Dropper> _droppers= new();
        private List<SinWaveFlier> _sinWaveFliers = new();

        private int _finishX;
        private System.Windows.Controls.Image _finImg;
        private System.Windows.Controls.Image _lvCompImg;

        private bool _idleShow = false;
        private int _idleFrames = 0;
        private System.Windows.Controls.Image _idleImage;

        private bool _lockInput = false;

        private DispatcherTimer _levelCompleteTimer;
        private DispatcherTimer _victoryTimer;
        private DispatcherTimer _fadeToBlackTimer;
        private DispatcherTimer _deadTimer;
        private Reasons _fadeToBlackReason;

        private Label _brushLabel;

        private TextBlock _fader;
        private double _faderOpacity = 0.0;


        public Game(Canvas c, Window w)
        {

            _canv = c;
            _window = w;

            if (!_appFolder.EndsWith("\\"))
            {
                _appFolder += "\\";
            }
            
            string imgFolder = _appFolder + "IMG\\";
            _img = new(imgFolder);

            _disp = _canv.Dispatcher;
            _window.KeyDown += _window_KeyDown;
            _window.KeyUp += _window_KeyUp;

            _level = 1;
            TitleScreen();
        }

        private void TitleScreen()
        {

            _isTitle = true;
            CreateImage(_img.MainTitle, 0, 0);

        }

        private void ResetOnDeath()
        {
            _deathCount += 1;
            _stopLoop = false;
            InitGame(true);
        }


        private void InitGame(bool afterADeath = false)
        {

            _jord = new Jord()
            {
                X = 370,
                Y = 404,
                ScreenX = 370,
                Height = JORD_HEIGHT
            };

            _sky = CreateImage(_img.Sky, 0, 0);

            _prlBgOne = CreateImage(_img.ParallaxBackground, 0, 0);
            _prlBgTwo = CreateImage(_img.ParallaxBackground, 0, 800);

            _prlMidOne = CreateImage(_img.ParallaxMiddle, 0, 0);
            _prlMidTwo = CreateImage(_img.ParallaxMiddle, 0, 800);

            _prlFgOne = CreateImage(_img.ParallaxForeground, 0, 0);
            _prlFgTwo = CreateImage(_img.ParallaxForeground, 0, 1600);

            _jord.Sprite = CreateImage(_img.JordStand, _jord.Y, _jord.ScreenX);
            _jord.Sprite.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

            Label deadLabel = new Label()
            {
                Background = new SolidColorBrush(new System.Windows.Media.Color() { A = 200, B = 0, G = 0, R = 0 }),
                Foreground = System.Windows.Media.Brushes.White,
                Content = "Deaths so far: " + _deathCount.ToString(),
                FontFamily = new System.Windows.Media.FontFamily("Arial"),
                FontWeight = FontWeights.Bold,
                FontSize = 18
            };
            Canvas.SetTop(deadLabel, 3);
            Canvas.SetLeft(deadLabel, 635);
            _canv.Children.Add(deadLabel);

            _leftPressed = false;
            _rightPressed = false;
            _downPressed = false;

            _crawlers.Clear();
            _horizontalFliers.Clear();
            _jumpers.Clear();
            _droppers.Clear();
            _sinWaveFliers.Clear();
            _blocks.Clear();
            
            GenerateWorldBlocksAndEnemies(_level, afterADeath);

            _stopLoop = false;
            _lockInput = false;

            if (!afterADeath)
            {
                _music.PlaySong();
            }

            Thread gameThread = new Thread(new ThreadStart(GameLoop));
            gameThread.Start();

        }


        private void GenerateWorldBlocksAndEnemies(int level, bool afterDeath = false)
        {

            string levelFolder = _appFolder + "Levels\\";
            string[] lines = File.ReadAllLines(levelFolder + level.ToString() + ".txt");

            _bossActive = false;
            _bossFrame = 0;
            _brushes = 0;

            List<int> floorGaps = new();
            List<AirPoint> airPoints = new();
            
            for (int nn = 0; nn < lines.Length; nn++)
            {
                string line = lines[nn];
                if (line.Contains("FLOORGAP="))
                {
                    floorGaps.Add(Convert.ToInt32(line.Split('=')[1]));
                }
                else if (line.Contains("FLOAT="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    AirPoint ap = new AirPoint()
                    {
                        Column= Convert.ToInt32(vals[0]),
                        Row= Convert.ToInt32(vals[1])
                    };
                    airPoints.Add(ap);
                }
                else if (line.Contains("VANISH="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    AirPoint ap = new AirPoint()
                    {
                        Column = Convert.ToInt32(vals[0]),
                        Row = Convert.ToInt32(vals[1]),
                        Vanisher = true
                    };
                    airPoints.Add(ap);
                }
                else if (line.Contains("WASP="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    HorizontalFlier hf = new()
                    {
                        X = (Convert.ToInt32(vals[0]) * 64),
                        Y = 599 - (Convert.ToInt32(vals[1]) * 64),
                        Image1 = _img.Wasp1,
                        Image2 = _img.Wasp2
                    };                    
                    _horizontalFliers.Add(hf);
                }
                else if (line.Contains("SPIDER="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    Crawler spid = new()
                    {
                        X = (Convert.ToInt32(vals[0]) * 64),
                        Y = 599 - (Convert.ToInt32(vals[1]) * 64),
                        Image1 = _img.Spider1,
                        Image2 = _img.Spider2
                    };
                    _crawlers.Add(spid);
                }
                else if (line.Contains("CRICKET="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    Jumper cricket = new()
                    {
                        X = (Convert.ToInt32(vals[0]) * 64),
                        Y = 599 - (Convert.ToInt32(vals[1]) * 64),
                        Image1 = _img.Cricket1,
                        Image2 = _img.Cricket2,
                        Height = 64
                    };
                    _jumpers.Add(cricket);
                }
                else if (line.Contains("TEA1="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    HorizontalFlier hf = new()
                    {
                        X = (Convert.ToInt32(vals[0]) * 64),
                        Y = 599 - (Convert.ToInt32(vals[1]) * 64),
                        Image1 = _img.Tea1,
                        Image2 = _img.Tea1
                    };
                    _horizontalFliers.Add(hf);
                }
                else if (line.Contains("TEA2="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    Dropper d = new()
                    {
                        X = (Convert.ToInt32(vals[0]) * 64),
                        Y = -72,
                        SpriteImg = _img.Tea2,
                        Diagonal = vals[1].ToUpper().Trim() == "DIA"
                    };
                    _droppers.Add(d);
                }
                else if (line.Contains("TEA3="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    SinWaveFlier s = new()
                    {
                        X = (Convert.ToInt32(vals[0]) * 64),
                        Y = 599 - (Convert.ToInt32(vals[1]) * 64),
                        HorizX = (Convert.ToInt32(vals[0]) * 64),
                        HorizY = 599 - (Convert.ToInt32(vals[1]) * 64),
                        SpriteImg = _img.Tea3,
                        YOnly = vals[2].Trim().ToUpper() == "2D"
                    };
                    _sinWaveFliers.Add(s);
                }
                else if (line.Contains("CHEESE1="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    HorizontalFlier hf = new()
                    {
                        X = (Convert.ToInt32(vals[0]) * 64),
                        Y = 599 - (Convert.ToInt32(vals[1]) * 64),
                        Image1 = _img.Cheese1,
                        Image2 = _img.Cheese1
                    };
                    _horizontalFliers.Add(hf);
                }
                else if (line.Contains("CHEESE2="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    Dropper d = new()
                    {
                        X = (Convert.ToInt32(vals[0]) * 64),
                        Y = -72,
                        SpriteImg = _img.Cheese2,
                        Diagonal = vals[1].ToUpper().Trim() == "DIA"
                    };
                    _droppers.Add(d);
                }
                else if (line.Contains("CHEESE3="))
                {
                    string coOrd = line.Split("=")[1];
                    string[] vals = coOrd.Split(',');
                    SinWaveFlier s = new()
                    {
                        X = (Convert.ToInt32(vals[0]) * 64),
                        Y = 599 - (Convert.ToInt32(vals[1]) * 64),
                        HorizX = (Convert.ToInt32(vals[0]) * 64),
                        HorizY = 599 - (Convert.ToInt32(vals[1]) * 64),
                        SpriteImg = _img.Cheese3,
                        YOnly = vals[2].Trim().ToUpper() == "2D"
                    };
                    _sinWaveFliers.Add(s);
                }
                else if (line.Contains("BOSS="))
                {
                    _hasBoss = true;
                    _finishX = 1000000;
                    _bossX = Convert.ToInt32(line.Split('=')[1]) * 64;
                    _boss = CreateImage(_img.Boss, 360, _finishX * 64);
                }
                else if (line.Contains("FINISH="))
                {
                    _finishX = Convert.ToInt32(line.Split('=')[1]) * 64;
                    _finImg = CreateImage(_img.FinishLine, 360, _finishX * 64);
                }
            }

            for (int xx = 0; xx < 199; xx++)
            {
                if (!floorGaps.Contains(xx))
                {
                    WorldBlock b = new WorldBlock();
                    b.X = (xx * BLOCK_WIDTH);
                    b.Y = 599 - BLOCK_HEIGHT;
                    b.Sprite = CreateImage(_img.StormBlock, b.Y, b.X);
                    _blocks.Add(b);
                }
            }

            foreach (AirPoint ap in airPoints)
            {
                WorldBlock sb = new WorldBlock();
                sb.Y = (599 - (BLOCK_HEIGHT * ap.Row));
                sb.X = (ap.Column * BLOCK_WIDTH);
                if (ap.Vanisher)
                {
                    sb.Sprite = CreateImage(_img.VanishBlock, sb.Y, sb.X);
                    sb.Vanishes = true;
                    sb.VanishFrame = 0;
                    sb.VanishOpacity = 1.0;
                }
                else
                {
                    sb.Sprite = CreateImage(_img.StormBlock, sb.Y, sb.X);
                }
                _blocks.Add(sb);
            }

            WorldBlock[] finalBlocks = _blocks.ToArray();
            Array.Sort(finalBlocks);
            _blocks.Clear();
            _blocks.AddRange(finalBlocks);

            foreach (HorizontalFlier hf in _horizontalFliers)
            {
                hf.Sprite = CreateImage(hf.Image1, hf.Y, hf.X);
                hf.Active = hf.X < 850;
            }

            foreach (Crawler c in _crawlers)
            {
                c.Sprite = CreateImage(c.Image1, c.Y, c.X);
                c.Active = c.X < 850;
            }

            foreach (Jumper j in _jumpers)
            {
                j.Sprite = CreateImage(j.Image1, j.Y, j.X);
                j.Active = j.X < 850;
                j.ScreenX = j.X;
                for (int nn = 0; nn < finalBlocks.Length; nn++)
                {
                    if (finalBlocks[nn].X == j.X && finalBlocks[nn].Y == j.Y + 64)
                    {
                        j.BlockIndex = nn;
                        break;
                    }
                }
            }

            foreach (Dropper d in _droppers)
            {
                d.Sprite = CreateImage(d.SpriteImg, d.Y, d.X);
                if (d.Diagonal)
                {
                    d.Active = d.X < 750;
                }
                else
                {
                    d.Active = d.X < 520;
                }
            }

            foreach (SinWaveFlier s in _sinWaveFliers)
            {
                s.Sprite = CreateImage(s.SpriteImg, s.Y, s.X);
                s.Active = s.X < 850;
            }

            if (!afterDeath)
            {
                string musFolder = _appFolder + "Music\\";
                _music = new(musFolder, level);
            }

        }


        private void GameLoop()
        {

            int tickCount = Environment.TickCount;

            do
            {
                Thread.Sleep(0);
                int newTickCount = Environment.TickCount;
                if (newTickCount - tickCount > 20)
                {
                    tickCount = newTickCount;
                    try
                    {
                        _disp.Invoke(Process);
                    }
                    catch (Exception ex)
                    {
                        break;
                    }
                }

            } while (!_stopLoop);


        }


        private void Process()
        {

            if (_lockInput)
            {
                return;
            }

            if (_rightPressed && _jord.Speed < 100)
            {
                _jord.Speed += 10;
                if (_jord.Speed > 100)
                {
                    _jord.Speed = 100;
                }
            }
            else if (_leftPressed && _jord.Speed > -100)
            {
                _jord.Speed -= 10;
                if (_jord.Speed < -100)
                {
                    _jord.Speed = -100;
                }
            }
            else if (!_rightPressed && _jord.Speed > 0)
            {
                _jord.Speed -= 10;
                if (_jord.Speed < 0)
                {
                    _jord.Speed = 0;
                }
            }
            else if (!_leftPressed && _jord.Speed < 0)
            {
                _jord.Speed += 10;
                if (_jord.Speed > 0)
                {
                    _jord.Speed = 0;
                }
            }

            _idleShow = false;
            if (_jord.IsJumping)
            {
                _jord.ContinueJump(_blocks);
                if (_idleImage != null)
                {
                    _canv.Children.Remove(_idleImage);
                    _idleImage = null;
                    _idleFrames = 0;
                }
            }
            else if (_jord.IsDropping)
            {
                _jord.ContinueDrop(_blocks);
            }
            else
            {
                if (_downPressed && !_jord.IsCrouching)
                {
                    _jord.IsCrouching = true;
                    _jord.Sprite.Source = _img.ImageSourceForBitmap(_img.JordCrouch);
                    _jord.Y += 64;
                    _jord.Speed = 0;
                    Canvas.SetTop(_jord.Sprite, _jord.Y);
                }
                else if (!_downPressed && _jord.IsCrouching)
                {
                    _jord.IsCrouching = false;
                    _jord.Sprite.Source = _img.ImageSourceForBitmap(_img.JordStand);
                    _jord.Y -= 64;
                    Canvas.SetTop(_jord.Sprite, _jord.Y);
                }
                else if (_downPressed && _jord.IsCrouching)
                {
                    //do nothing
                }
                else if (_jord.Speed != 0)
                {
                    _jord.AltFrame += 1;
                    if (_jord.AltFrame >= 10)
                    {
                        _jord.AltFrame = 0;
                    }
                    _jord.Sprite.Source = _jord.AltFrame < 5 ? _img.ImageSourceForBitmap(_img.JordWalkOne) : _img.ImageSourceForBitmap(_img.JordWalkTwo);
                    FaceJordRightWay();
                }
                else
                {
                    _jord.Sprite.Source = _img.ImageSourceForBitmap(_img.JordStand);
                }
                if (!_rightPressed && !_leftPressed && !_downPressed)
                {
                    _idleFrames += 1;
                }
                else
                {
                    _idleFrames = 0;
                }
                if (_idleFrames > 200 && _jord.Y > 300)
                {
                    _idleShow = true;
                    if (_idleImage == null)
                    {
                        Random rnd = new Random(Environment.TickCount);
                        int r = rnd.Next(8);
                        if (r == 0)
                        {
                            _idleImage = CreateImage(_img.Idle1, _jord.Y - 180, _jord.ScreenX + 30);
                        }
                        else if (r == 1)
                        {
                            _idleImage = CreateImage(_img.Idle2, _jord.Y - 180, _jord.ScreenX + 30);
                        }
                        else if (r == 2)
                        {
                            _idleImage = CreateImage(_img.Idle3, _jord.Y - 180, _jord.ScreenX + 30);
                        }
                        else if (r == 3)
                        {
                            _idleImage = CreateImage(_img.Idle4, _jord.Y - 180, _jord.ScreenX + 30);
                        }
                        else if (r == 4)
                        {
                            _idleImage = CreateImage(_img.Idle5, _jord.Y - 180, _jord.ScreenX + 30);
                        }
                        else if (r == 5)
                        {
                            _idleImage = CreateImage(_img.Idle6, _jord.Y - 180, _jord.ScreenX + 30);
                        }
                        else if (r == 6)
                        {
                            _idleImage = CreateImage(_img.Idle7, _jord.Y - 180, _jord.ScreenX + 30);
                        }
                        else if (r == 7)
                        {
                            _idleImage = CreateImage(_img.Idle8, _jord.Y - 180, _jord.ScreenX + 30);
                        }
                    }
                }
                if (!_idleShow && _idleImage != null)
                {
                    _canv.Children.Remove(_idleImage);
                    _idleImage = null;
                }
            }

            AdvanceVanishingBlocks();

            bool jordMovedHoriz = false;
            if (_jord.Speed > 0)
            {
                if (_jord.ScreenX >= 370 && !_bossActive)
                {

                    if (!_hasBoss)
                    {
                        _finishX -= (_jord.Speed / 10);
                        Canvas.SetLeft(_finImg, _finishX);
                    }
                    else
                    {
                        _bossX -= (_jord.Speed / 10);
                        Canvas.SetLeft(_boss, _bossX);
                        if (_bossX <= 608)
                        {
                            _bossActive = true;
                            _brushLabel = new Label()
                            {
                                Background = new SolidColorBrush(new System.Windows.Media.Color() { A = 200, B = 0, G = 0, R = 0 }),
                                Foreground = System.Windows.Media.Brushes.White,
                                Content = "Toilet brushes: " + _brushes.ToString() + " / 5",
                                FontFamily = new System.Windows.Media.FontFamily("Arial"),
                                FontWeight = FontWeights.Bold,
                                FontSize = 18
                            };
                            Canvas.SetTop(_brushLabel, 3);
                            Canvas.SetLeft(_brushLabel, 3);
                            _canv.Children.Add(_brushLabel);
                        }
                    }

                    if (_jord.ScreenX > _finishX + 64)
                    {
                        _lockInput = true;
                        _lvCompImg = CreateImage(_img.LevelComplete, 120, 106);
                        _music.PlayLevelCompleteSong();
                        _levelCompleteTimer = new DispatcherTimer();
                        _levelCompleteTimer.Tick += _levelCompleteTimer_Tick;
                        _levelCompleteTimer.Interval = new TimeSpan(0, 0, 3);
                        _levelCompleteTimer.Start();
                        return;
                    }

                    if (DoesJordHitWorldBlockForwards())
                    {
                        _jord.Speed = 0;
                    }
                    else
                    {

                        jordMovedHoriz = true;
                        _jord.X += (_jord.Speed / 10);

                        double bgOneLeft = Canvas.GetLeft(_prlBgOne);
                        bgOneLeft -= (_jord.Speed / 30);
                        if (bgOneLeft < -800)
                        {
                            bgOneLeft += 1600;
                        }
                        double bgTwoLeft = Canvas.GetLeft(_prlBgTwo);
                        bgTwoLeft -= (_jord.Speed / 30);
                        if (bgTwoLeft < -800)
                        {
                            bgTwoLeft += 1600;
                        }
                        Canvas.SetLeft(_prlBgOne, bgOneLeft);
                        Canvas.SetLeft(_prlBgTwo, bgTwoLeft);

                        double midOneLeft = Canvas.GetLeft(_prlMidOne);
                        midOneLeft -= (_jord.Speed / 15);
                        if (midOneLeft < -800)
                        {
                            midOneLeft += 1600;
                        }
                        double midTwoLeft = Canvas.GetLeft(_prlMidTwo);
                        midTwoLeft -= (_jord.Speed / 15);
                        if (midTwoLeft < -800)
                        {
                            midTwoLeft += 1600;
                        }
                        Canvas.SetLeft(_prlMidOne, midOneLeft);
                        Canvas.SetLeft(_prlMidTwo, midTwoLeft);

                        double fgOneLeft = Canvas.GetLeft(_prlFgOne);
                        fgOneLeft -= (_jord.Speed / 10);
                        if (fgOneLeft < -1600)
                        {
                            fgOneLeft += 3200;
                        }
                        double fgTwoLeft = Canvas.GetLeft(_prlFgTwo);
                        fgTwoLeft -= (_jord.Speed / 10);
                        if (fgTwoLeft < -1600)
                        {
                            fgTwoLeft += 3200;
                        }
                        Canvas.SetLeft(_prlFgOne, fgOneLeft);
                        Canvas.SetLeft(_prlFgTwo, fgTwoLeft);

                        ScrollWorldBlocks();

                    }

                }
                else
                {
                    if (DoesJordHitWorldBlockForwards())
                    {
                        _jord.Speed = 0;
                    }
                    else
                    {

                        jordMovedHoriz = true;

                        _jord.ScreenX += (_jord.Speed / 10);
                        double jordSX = Canvas.GetLeft(_jord.Sprite);
                        jordSX += (_jord.Speed / 10);
                        Canvas.SetLeft(_jord.Sprite, jordSX);
                    }
                }
            }
            else if (_jord.Speed < 0)
            {

                if (DoesJordHitWorldBlockBackwards())
                {
                    _jord.Speed = 0;
                }
                else
                {
                    jordMovedHoriz = true;
                    _jord.ScreenX += (_jord.Speed / 10);
                    double jordSX = Canvas.GetLeft(_jord.Sprite);
                    jordSX += (_jord.Speed / 10);
                    if (jordSX < 0)
                    {
                        jordSX = 0;
                        _jord.ScreenX = 0;
                        _jord.Speed = 0;
                    }
                    Canvas.SetLeft(_jord.Sprite, jordSX);
                }
            }

            if (!_jord.IsJumping && !_jord.IsDropping && !_jord.IsCrouching)
            {
                _jord.IsDropping = DoesJordFall();
            }

            MoveHorizontalFliers(_jord.Speed);
            MoveCrawlers(_jord.Speed);
            MoveJumpers(_jord.Speed);
            MoveDroppers(_jord.Speed);
            MoveSineWaveFliers(_jord.Speed);

            if (_bossActive)
            {
                _bossFrame += 1;
                if (_brushes <= 1)
                { 
                    if (_bossFrame % 50 == 0)
                    {
                        if (_bossFrame % 350 != 0)
                        {
                            GenerateBossCheese();
                        }
                        else
                        {
                            GenerateBrush();
                        }
                    }
                }
                else if (_brushes > 1 && _brushes < 4)
                {
                    if (_bossFrame % 45 == 0)
                    {
                        if (_bossFrame % 450 != 0)
                        {
                            GenerateBossCheese();
                        }
                        else
                        {
                            GenerateBrush();
                        }
                    }
                }
                else
                {
                    if (_bossFrame % 40 == 0)
                    {
                        if (_bossFrame % 600 != 0)
                        {
                            GenerateBossCheese();
                        }
                        else
                        {
                            GenerateBrush();
                        }
                    }
                }
            }


            DoesJordDieOrCollectBrush();

            if (_brushes >= 5)
            {
                _lockInput = true;
                _lvCompImg = CreateImage(_img.LevelComplete, 120, 106);
                _music.PlayLevelCompleteSong();
                _victoryTimer = new DispatcherTimer();
                _victoryTimer.Tick += _victoryTimer_Tick; ;
                _victoryTimer.Interval = new TimeSpan(0, 0, 3);
                _victoryTimer.Start();
            }

        }

        private void _victoryTimer_Tick(object? sender, EventArgs e)
        {
            _victoryTimer.Stop();
            _fadeToBlackReason = Reasons.GameComplete;

            _fadeToBlackTimer = new();
            _fadeToBlackTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _fadeToBlackTimer.Tick += _fadeToBlackTimer_Tick;

            CreateFader(false);
            _fadeToBlackTimer.Start();
        }

        private void GenerateBrush()
        {

            Random rnd = new(Environment.TickCount);
            int rndNum = rnd.Next(2, 7);
            HorizontalFlier hf = new()
            {
                X = 800,
                Y = 599 - ((rndNum) * 64),
                Image1 = _img.Brush,
                Image2 = _img.Brush,
            };
            hf.Sprite = CreateImage(_img.Brush, hf.Y, hf.X);
            hf.Active = true;
            hf.IsBrush = true;
            _horizontalFliers.Add(hf);

        }


        private void GenerateBossCheese()
        {

            Random rnd = new(Environment.TickCount);
            int rndNum = rnd.Next(0, 100);
            if (rndNum < 50)
            {
                for (int nn = 0; nn < 5; nn++)
                {
                    HorizontalFlier hf = new()
                    {
                        X = 800,
                        Y = 599 - ((nn + 3) * 64),
                        Image1 = _img.Cheese1,
                        Image2 = _img.Cheese1,
                    };
                    hf.Sprite = CreateImage(_img.Cheese1, hf.Y, hf.X);
                    hf.Active = true;
                    _horizontalFliers.Add(hf);
                }
            }
            else
            {
                for (int nn = 0; nn < 2; nn++)
                {
                    HorizontalFlier hf = new()
                    {
                        X = 800,
                        Y = 599 - ((nn + 2) * 64),
                        Image1 = _img.Cheese1,
                        Image2 = _img.Cheese1,
                    };
                    hf.Sprite = CreateImage(_img.Cheese1, hf.Y, hf.X);
                    hf.Active = true;
                    _horizontalFliers.Add(hf);
                }
            }

        }


        private void DoesJordDieOrCollectBrush()
        {
            List<MovingObject> all = new();
            all.AddRange(_crawlers);
            all.AddRange(_jumpers);
            all.AddRange(_horizontalFliers);
            all.AddRange(_droppers);
            all.AddRange(_sinWaveFliers);

            bool hesDead = false;
            foreach (MovingObject obj in all)
            {
                double centreX = Canvas.GetLeft(obj.Sprite) + 32;
                double centreY = Canvas.GetTop(obj.Sprite) + 32;
                if (centreX > _jord.ScreenX && centreX < _jord.ScreenX + 64)
                {
                    if (!_jord.IsCrouching)
                    {
                        if (centreY > _jord.Y && centreY < _jord.Y + 128)
                        {
                            if (!obj.IsBrush)
                            {
                                hesDead = true;
                                break;
                            }
                            else
                            {
                                if (obj.Sprite.Visibility == Visibility.Visible)
                                {
                                    _brushes += 1;
                                    _brushLabel.Content = "Toilet brushes: " + _brushes.ToString() + " / 5";
                                    obj.Sprite.Visibility = Visibility.Collapsed;
                                    _bossFrame = 0;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (centreY > _jord.Y && centreY < _jord.Y + 64)
                        {
                            if (!obj.IsBrush)
                            {
                                hesDead = true;
                                break;
                            }
                            else
                            {
                                if (obj.Sprite.Visibility == Visibility.Visible)
                                {
                                    _brushes += 1;
                                    _brushLabel.Content = "Toilet brushes: " + _brushes.ToString() + " / 5";
                                    obj.Sprite.Visibility = Visibility.Collapsed;
                                    _bossFrame = 0;
                                }
                            }
                        }
                    }
                }
            }

            if (_jord.Y > 600)
            {
                hesDead = true;
            }

            if (hesDead)
            {
                if (!_jord.IsCrouching)
                {
                    _jord.Y += 64;
                }
                _jord.Sprite.Source = _img.ImageSourceForBitmap(_img.JordDead);
                Canvas.SetTop(_jord.Sprite, _jord.Y);
                _lockInput = true;
                _stopLoop = true;                

                _deadTimer = new();
                _deadTimer.Interval = new TimeSpan(0, 0, 0, 0, 1100);
                _deadTimer.Tick += _deadTimer_Tick;
                _deadTimer.Start();

            }

        }

        private void _deadTimer_Tick(object? sender, EventArgs e)
        {

            _deadTimer.Stop();

            TextBlock tb = new()
            {
                Width = 600,
                Height = 400,
                FontFamily = new System.Windows.Media.FontFamily("Arial"),
                FontWeight = FontWeights.Black,
                Foreground = System.Windows.Media.Brushes.White,
                Background = new SolidColorBrush(new System.Windows.Media.Color() { A = 210, B = 0, G = 0, R = 0 }),
                TextAlignment = TextAlignment.Center,
                Text = Environment.NewLine + "Europe's toughest" + Environment.NewLine + "man...falls???",
                FontSize = 48,                
            };
            Canvas.SetTop(tb, 100);
            Canvas.SetLeft(tb, 100);
            _canv.Children.Add(tb);

            System.Windows.Controls.Image dead = CreateImage(_img.JordDead, 318, 368);

            Label deadLabel = new Label()
            {
                Background = new SolidColorBrush(new System.Windows.Media.Color() { A = 0, B = 0, G = 0, R = 0 }),
                Foreground = System.Windows.Media.Brushes.White,
                Content = "Press any key to give him another chance (not recommended...)",
                FontFamily = new System.Windows.Media.FontFamily("Arial"),
                FontWeight = FontWeights.Bold,
                FontSize = 16
            };
            Canvas.SetTop(deadLabel, 440);
            Canvas.SetLeft(deadLabel, 155);
            _canv.Children.Add(deadLabel);

            _isDeadScreen= true;

        }

        private void _levelCompleteTimer_Tick(object? sender, EventArgs e)
        {

            _levelCompleteTimer.Stop();
            _fadeToBlackReason = Reasons.LevelComplete;

            _fadeToBlackTimer = new();
            _fadeToBlackTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _fadeToBlackTimer.Tick += _fadeToBlackTimer_Tick;

            CreateFader(false);
            _fadeToBlackTimer.Start();


        }

        private void CreateFader(bool fadeIn)
        {

            if (_fader != null)
            {
                _canv.Children.Remove(_fader);
            }

            _fader = new TextBlock()
            {
                Width = 800,
                Height = 600,
                Background = new SolidColorBrush(Colors.Black),
                Opacity = fadeIn ? 1 : 0
            };

            _canv.Children.Add(_fader);
            Canvas.SetLeft(_fader, 0);
            Canvas.SetTop(_fader, 0);

            _faderOpacity = fadeIn ? 1 : 0;

        }

        private void _fadeToBlackTimer_Tick(object? sender, EventArgs e)
        {

            _faderOpacity += 0.1;

            if (_faderOpacity >= 1.1)
            {
                _fadeToBlackTimer.Stop();
                if (_fadeToBlackReason == Reasons.LevelComplete)
                {
                    _level += 1;
                    _stopLoop = true;
                    ClearCanvas();
                    _fader = null;
                    LevelScreen();
                    return;
                }
                else if (_fadeToBlackReason == Reasons.GameComplete)
                {
                    ClearCanvas();
                    _isVictory = true;
                    _stopLoop= true;
                    CreateImage(_img.VictoryCard, 0, 0);
                    _music.PlayVictorySong();
                    Label dl = new Label()
                    {
                        Background = new SolidColorBrush(new System.Windows.Media.Color() { A = 0, B = 0, G = 0, R = 0 }),
                        Foreground = System.Windows.Media.Brushes.DarkRed,
                        Content = "Deaths: " + _deathCount.ToString(),
                        FontFamily = new System.Windows.Media.FontFamily("Arial"),
                        FontWeight = FontWeights.Bold,
                        FontSize = 22
                    };
                    Canvas.SetTop(dl, 557);
                    Canvas.SetLeft(dl, 3);
                    _canv.Children.Add(dl);
                }

            }

            _fader.Opacity = _faderOpacity;

        }


        private void MoveSineWaveFliers(int speed)
        {
            SinWaveFlier[] sins = _sinWaveFliers.ToArray();
            _sinWaveFliers.Clear();
            for (int nn = 0; nn < sins.Length; nn++)
            {
                if (sins[nn].X > -128)
                {
                    if (!sins[nn].Active)
                    {
                        if (speed > 0 && _jord.ScreenX >= 370)
                        {
                            sins[nn].X -= (speed / 10);
                            sins[nn].HorizX = sins[nn].X;
                            Canvas.SetLeft(sins[nn].Sprite, sins[nn].X);
                            if (sins[nn].X < 850)
                            {
                                sins[nn].Active = true;
                            }
                        }
                    }
                    else
                    {
                        if (sins[nn].SinFrame % 2 == 0)
                        {
                            sins[nn].HorizX -= 8;
                        }

                        if (speed > 0 && _jord.ScreenX >= 370)
                        {
                            sins[nn].HorizX -= (speed / 10);
                            sins[nn].X -= (speed / 10);
                        }

                        sins[nn].SinFrame += 1;
                        if (sins[nn].SinFrame % 2 == 0)
                        {
                            sins[nn].CalculatePosition();
                        }
                        Canvas.SetTop(sins[nn].Sprite, sins[nn].Y);
                        Canvas.SetLeft(sins[nn].Sprite, sins[nn].X);
                    }
                }
                _sinWaveFliers.Add(sins[nn]);
            }
        }


        private void MoveDroppers(int speed)
        {

            Dropper[] droppers = _droppers.ToArray();
            _droppers.Clear();
            for (int nn = 0; nn < droppers.Length; nn++)
            {
                if (droppers[nn].X > -64 && droppers[nn].Y < 900)
                {
                    if (!droppers[nn].Active)
                    {
                        if (speed > 0 && _jord.ScreenX >= 370)
                        {
                            droppers[nn].X -= (speed / 10);
                            droppers[nn].ScreenX = droppers[nn].X;
                            Canvas.SetLeft(droppers[nn].Sprite, droppers[nn].X);
                            if (droppers[nn].Diagonal)
                            {
                                if (droppers[nn].X < 750)
                                {
                                    droppers[nn].Active = true;
                                }
                            }
                            else
                            {
                                if (droppers[nn].X < 520)
                                {
                                    droppers[nn].Active = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (speed > 0 && _jord.ScreenX >= 370)
                        {
                            droppers[nn].X -= (speed / 10);
                        }
                        droppers[nn].Y += 14;
                        if (droppers[nn].Diagonal)
                        {
                            droppers[nn].X -= 10;
                        }
                        Canvas.SetTop(droppers[nn].Sprite, droppers[nn].Y);
                        Canvas.SetLeft(droppers[nn].Sprite, droppers[nn].X);
                    }
                    _droppers.Add(droppers[nn]);
                }
            }

        }


        private void MoveJumpers(int speed)
        {

            Jumper[] jumpers = _jumpers.ToArray();
            _jumpers.Clear();
            for (int nn = 0; nn < jumpers.Length; nn++)
            {
                if (jumpers[nn].X > -64)
                {
                    if (!jumpers[nn].Active)
                    {
                        if (speed > 0 && _jord.ScreenX >= 370)
                        {
                            jumpers[nn].X -= (speed / 10);
                            jumpers[nn].ScreenX = jumpers[nn].X;
                            Canvas.SetLeft(jumpers[nn].Sprite, jumpers[nn].X);
                            if (jumpers[nn].X < 850)
                            {
                                jumpers[nn].Active = true;
                            }
                        }
                    }
                    else
                    {
                        //if (speed > 0 && _jord.ScreenX >= 370)
                        //{
                        //    jumpers[nn].X -= (speed / 10);
                        //    jumpers[nn].ScreenX = jumpers[nn].X;
                        //}
                        jumpers[nn].X = _blocks[jumpers[nn].BlockIndex].X;
                        jumpers[nn].ScreenX = jumpers[nn].X;
                        Canvas.SetLeft(jumpers[nn].Sprite, _blocks[jumpers[nn].BlockIndex].X);
                        if (jumpers[nn].IsJumping)
                        {
                            jumpers[nn].ContinueJump(_blocks);
                        }
                        else if (jumpers[nn].IsDropping)
                        {
                            jumpers[nn].ContinueDrop(_blocks);
                            if (!jumpers[nn].IsDropping)
                            {
                                jumpers[nn].Sprite.Source = _img.ImageSourceForBitmap(jumpers[nn].Image1);
                                jumpers[nn].AltFrame = 0;
                            }
                        }
                        else
                        {
                            jumpers[nn].AltFrame += 1;
                            if (jumpers[nn].AltFrame == 50)
                            {
                                jumpers[nn].Jump();
                                jumpers[nn].Sprite.Source = _img.ImageSourceForBitmap(jumpers[nn].Image2);
                            }
                        }
                    }
                    _jumpers.Add(jumpers[nn]);
                }
            }

        }

        private void MoveCrawlers(int speed)
        {

            Crawler[] crawlers = _crawlers.ToArray();
            _crawlers.Clear();
            for (int nn = 0; nn < crawlers.Length; nn++)
            {
                if (crawlers[nn].X > -64)
                {
                    if (!crawlers[nn].Active)
                    {
                        if (speed > 0 && _jord.ScreenX >= 370)
                        {
                            crawlers[nn].X -= (speed / 10);
                            Canvas.SetLeft(crawlers[nn].Sprite, crawlers[nn].X);
                            if (crawlers[nn].X < 850)
                            {
                                crawlers[nn].Active = true;
                            }
                        }
                    }
                    else
                    {
                        if (crawlers[nn].Right)
                        {
                            crawlers[nn].X += 2;
                        }
                        else
                        {
                            crawlers[nn].X -= 2;
                        }
                        if (speed > 0 && _jord.ScreenX >= 370)
                        {
                            crawlers[nn].X -= (speed / 10);
                        }

                        bool feetOnBlock = false;
                        foreach (Globals.WorldBlock b in _blocks)
                        {
                            //if (b.X >= crawlers[nn].X - 32 && b.X + 64 < crawlers[nn].X + 96)
                            if (Math.Floor((decimal)b.X / 64) == Math.Floor((decimal)crawlers[nn].X / 64)
                                || Math.Ceiling(((decimal)b.X + 64) / 64) == Math.Ceiling((decimal)crawlers[nn].X / 64))
                            {
                                if (b.Y == crawlers[nn].Y + 64)
                                {
                                    feetOnBlock = true;
                                    break;
                                }
                            }
                        }

                        if (!feetOnBlock)
                        {
                            crawlers[nn].Right = !crawlers[nn].Right;
                            if (crawlers[nn].Right)
                            {
                                crawlers[nn].X += 32;
                            }
                            else
                            {
                                crawlers[nn].X -= 32;
                            }
                        }

                        foreach (Globals.WorldBlock b in _blocks)
                        {
                            if (b.Y == crawlers[nn].Y)
                            {
                                if (crawlers[nn].Right && Math.Abs(b.X - crawlers[nn].X) < 16)
                                {
                                    crawlers[nn].Right = false;
                                    crawlers[nn].X -= 16;
                                    break;
                                }
                                else if (!crawlers[nn].Right && Math.Abs((b.X + 64) - crawlers[nn].X) < 16)
                                {
                                    crawlers[nn].Right = true;
                                    crawlers[nn].X += 16;
                                    break;
                                }
                            }
                        }

                        crawlers[nn].AltFrame += 1;
                        if (crawlers[nn].AltFrame >= 10)
                        {
                            crawlers[nn].AltFrame = 0;
                        }
                        Canvas.SetLeft(crawlers[nn].Sprite, crawlers[nn].X);
                        if (crawlers[nn].AltFrame < 5)
                        {
                            crawlers[nn].Sprite.Source = _img.ImageSourceForBitmap(crawlers[nn].Image1);
                        }
                        else
                        {
                            crawlers[nn].Sprite.Source = _img.ImageSourceForBitmap(crawlers[nn].Image2);
                        }
                        if (crawlers[nn].Right)
                        {
                            ScaleTransform flipTrans = new ScaleTransform();
                            flipTrans.ScaleX = -1;
                            crawlers[nn].Sprite.RenderTransform = flipTrans;
                        }
                        else
                        {
                            crawlers[nn].Sprite.RenderTransform = null;
                        }
                    }
                    _crawlers.Add(crawlers[nn]);
                }
            }

        }

        private void MoveHorizontalFliers(int speed)
        {

            HorizontalFlier[] fliers = _horizontalFliers.ToArray();
            _horizontalFliers.Clear();
            for (int nn = 0; nn < fliers.Length; nn++)
            {
                if (fliers[nn].X > -64)
                {
                    if (!fliers[nn].Active)
                    {
                        if (speed > 0 && _jord.ScreenX >= 370 && !_bossActive)
                        { 
                            fliers[nn].X -= (speed / 10);
                            Canvas.SetLeft(fliers[nn].Sprite, fliers[nn].X);
                            if (fliers[nn].X < 850)
                            {
                                fliers[nn].Active = true;
                            }
                        }
                    }
                    else
                    {
                        fliers[nn].X -= 7;
                        if (speed > 0 && _jord.ScreenX >= 370 && !_bossActive)
                        {
                            fliers[nn].X -= (speed / 10);
                        }
                        fliers[nn].AltFrame += 1;
                        if (fliers[nn].AltFrame >= 10)
                        {
                            fliers[nn].AltFrame = 0;
                        }
                        Canvas.SetLeft(fliers[nn].Sprite, fliers[nn].X);
                        if (fliers[nn].AltFrame < 5)
                        {
                            fliers[nn].Sprite.Source = _img.ImageSourceForBitmap(fliers[nn].Image1);
                        }
                        else
                        {
                            fliers[nn].Sprite.Source = _img.ImageSourceForBitmap(fliers[nn].Image2);
                        }
                    }
                    _horizontalFliers.Add(fliers[nn]);
                }
            }

        }


        private void FaceJordRightWay()
        {
            if (_jord.Speed < 0)
            {
                ScaleTransform flipTrans = new ScaleTransform();
                flipTrans.ScaleX = -1;
                _jord.Sprite.RenderTransform = flipTrans;
            }
            else if (_jord.Speed > 0)
            {
                _jord.Sprite.RenderTransform = null;
            }
        }


        private bool DoesJordFall()
        {
            bool feetOnBlock = false;
            foreach (Globals.WorldBlock b in _blocks)
            {
                if (!b.Vanishes || b.VanishOpacity > 0)
                { 
                    if (b.X + BLOCK_WIDTH > _jord.ScreenX && b.X + BLOCK_WIDTH < _jord.ScreenX + JORD_WIDTH * 2)
                    {
                        if (b.Y == _jord.Y + _jord.Height + 1)
                        {
                            feetOnBlock = true;
                            break;
                        }
                    }
                }
            }
            return !feetOnBlock;
        }


        private bool DoesJordHitWorldBlockForwards()
        {

            foreach (WorldBlock b in _blocks)
            {
                if (b.X > _jord.ScreenX)
                {
                    if (_jord.ScreenX + JORD_WIDTH + (_jord.Speed / 10) > b.X)
                    {
                        if (b.Y >= _jord.Y && b.Y <= _jord.Y + JORD_HEIGHT)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;

        }

        private bool DoesJordHitWorldBlockBackwards()
        {

            foreach (WorldBlock b in _blocks)
            {
                if (b.X + BLOCK_WIDTH < _jord.ScreenX)
                {
                    if (_jord.ScreenX + (_jord.Speed / 10) < b.X + (BLOCK_WIDTH + 1))
                    {
                        if (b.Y >= _jord.Y && b.Y <= _jord.Y + JORD_HEIGHT)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        private void AdvanceVanishingBlocks()
        {

            WorldBlock[] b = _blocks.ToArray();
            //Array.Sort(b);

            _blocks.Clear();

            for (int nn = 0; nn < b.Length; nn++)
            {
                if (b[nn].Vanishes)
                {
                    b[nn].VanishFrame += 1;
                    if (b[nn].VanishFrame >= 90)
                    {
                        b[nn].VanishFrame = 0;
                        b[nn].VanishOpacity = 1;
                    }
                    else
                    {
                        if (b[nn].VanishOpacity > 0 && b[nn].VanishFrame % 3 == 0)
                        {
                            b[nn].VanishOpacity -= 0.05;
                        }
                    }
                    b[nn].Sprite.Opacity = b[nn].VanishOpacity;
                }
                _blocks.Add(b[nn]);
            }
        }


        private void ScrollWorldBlocks()
        {

            WorldBlock[] b = _blocks.ToArray();
            //Array.Sort(b);

            _blocks.Clear();

            for (int nn = 0; nn < b.Length; nn++)
            {
                if (b[nn].X > - 100)
                {
                    b[nn].X -= (_jord.Speed / 10);
                    Canvas.SetLeft(b[nn].Sprite, b[nn].X);                    
                }
                _blocks.Add(b[nn]);
            }

        }


        private void ClearCanvas()
        {
            foreach (UIElement c in _controls)
            {
                _canv.Children.Remove(c);
            }
            _controls.Clear();
        }


        private void LevelScreen()
        {
            _isLevel = true;
            if (_level == 1)
            {
                CreateImage(_img.TitleCard1, 0, 0);
            }
            else if (_level == 2)
            {
                CreateImage(_img.TitleCard2, 0, 0);
            }
            else if (_level == 3)
            {
                CreateImage(_img.TitleCard3, 0, 0);
            }
        }


        private void _window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {

            if (_isVictory)
            {
                _window.Close();
                return;
            }

            if (_isTitle)
            {
                _isTitle = false;
                ClearCanvas();
                LevelScreen();
            }
            else if (_isLevel)
            {
                _isLevel = false;
                ClearCanvas();
                InitGame();
            }
            else if (_isDeadScreen)
            {
                _isDeadScreen= false;
                ClearCanvas();
                ResetOnDeath();

            }

            if (e.Key == System.Windows.Input.Key.A)
            {
                _leftPressed = false;
            }
            else if (e.Key == System.Windows.Input.Key.D)
            {
                _rightPressed = false;
            }
            else if (e.Key == System.Windows.Input.Key.S)
            {
                _downPressed = false;
            }
        }

        private void _window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            
            if (_isTitle || _isLevel)
            {
                return;
            }

            if (e.Key == System.Windows.Input.Key.A)
            {
                if (!_downPressed)
                {
                    _leftPressed = true;
                    _rightPressed = false;
                }
            }
            else if (e.Key == System.Windows.Input.Key.D)
            {
                if (!_downPressed)
                { 
                    _rightPressed = true;
                    _leftPressed = false;
                }
            }
            else if (e.Key == System.Windows.Input.Key.W)
            {
                if (!_downPressed)
                {
                    _jord.Jump();
                }
            }
            else if (e.Key == System.Windows.Input.Key.S)
            {
                if (!_jord.IsJumping && !_jord.IsDropping)
                {
                    _downPressed = true;
                    _leftPressed = false;
                    _rightPressed = false;
                }
            }

        }

        private System.Windows.Controls.Image CreateImage(Bitmap src, int top, int left)
        {

            System.Windows.Controls.Image ret = new()
            {
                Source = _img.ImageSourceForBitmap(src)
            };

            Canvas.SetLeft(ret, left);
            Canvas.SetTop(ret, top);

            _canv.Children.Add(ret);
            _controls.Add(ret);
            return ret;

        }




    }
}
