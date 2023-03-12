using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows.Threading;
using System.Windows.Media;
using System.Numerics;

namespace JAISW
{
    public class MusicPlayer
    {

        private MediaPlayer _player;
        private string _folder;

        private string _levelSong;
        private string _levelCompleteSong;
        private string _victorySong;

        private bool _noLoop;

        private DispatcherTimer newSongTimer;

        public MusicPlayer(string musicFolder, int level)
        {

            _player = new MediaPlayer();
            _folder = musicFolder;

            if (!_folder.EndsWith("\\"))
            {
                _folder += "\\";
            }

            _levelSong = _folder + "LevelSong.mp3";
            _levelCompleteSong = _folder + "LevelComplete.mp3";
            _victorySong = _folder + "Victory.mp3";

            if (level == 2)
            {
                _levelSong = _folder + "Level2Song.mp3";
            }
            else if (level == 3)
            {
                _levelSong = _folder + "Level3Song.mp3";
            }

            _player.MediaEnded += _player_MediaEnded;

        }

        public void PlaySong()
        {

            _noLoop = false;
            _player.Stop();
            _player.Open(new Uri(_levelSong));
            _player.Play();

        }

        public void PlayLevelCompleteSong()
        {
            _noLoop = true;
            _player.Stop();
            _player.Open(new Uri(_levelCompleteSong));
            _player.Play();
        }

        public void PlayVictorySong()
        {
            _noLoop = true;
            _player.Stop();
            _player.Open(new Uri(_victorySong));
            _player.Play();
        }

        private void _player_MediaEnded(object? sender, EventArgs e)
        {
            if (!_noLoop)
            {
                newSongTimer = new DispatcherTimer();
                newSongTimer.Interval = new TimeSpan(0, 0, 5);
                newSongTimer.Tick += NewSongTimer_Tick;
                newSongTimer.Start();
            }
        }

        private void NewSongTimer_Tick(object? sender, EventArgs e)
        {
            newSongTimer.Stop();
            PlaySong();
        }
    }
}
