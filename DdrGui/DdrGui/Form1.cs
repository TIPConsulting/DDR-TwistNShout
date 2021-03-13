﻿using ArdNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TIPC.Core.Tools;
using TIPC.Core.Tools.Extensions;
using TIPC.Core.Tools.Threading;

namespace DdrGui
{
    public partial class Form1 : Form
    {
        readonly IArdNetSystem _ardSystem;
        readonly SerialPort _gamepadPort;
        readonly CancellationTokenSource _tokenSrc = new();
        readonly PictureBox[] _targetArrows;
        readonly CancelThread<object> _newNoteThread;
        readonly CancelThread<object> _tickThread;
        readonly object _scrollingArrowLock = new();
        readonly List<PictureBox> _scrollingArrows = new();
        readonly int _tickSpeed = 100;
        readonly int _tickSize;
        readonly int _tickTopTolerance = 2;
        readonly int _tickBtmTolerance = 4;
        readonly QueueThread _feedbackThread;
        readonly QueueThread<ArrowDir> _userInputHandlerThread;
        readonly CancelThread<object> _gamepadThread;

        public Form1(IArdNetSystem ArdSystem, SerialPort GamepadPort)
        {
            this._ardSystem = ArdSystem;
            this._gamepadPort = GamepadPort;

            InitializeComponent();
            tbl_TargetZone.LocationChanged += OnSizeChangedHandler;
            tbl_TargetZone.SizeChanged += OnSizeChangedHandler;
            img_TargetLeft.ImageLocation = Path.Combine(Resources.ResourcePaths.Images, "ArrowLeft.png");
            img_TargetUp.ImageLocation = Path.Combine(Resources.ResourcePaths.Images, "ArrowUp.png");
            img_TargetRight.ImageLocation = Path.Combine(Resources.ResourcePaths.Images, "ArrowRight.png");
            img_TargetDown.ImageLocation = Path.Combine(Resources.ResourcePaths.Images, "ArrowDown.png");
            img_TargetSpin.ImageLocation = Path.Combine(Resources.ResourcePaths.Images, "ArrowSpin.png");
            _targetArrows = new[]
            {
                img_TargetLeft,
                img_TargetUp,
                img_TargetSpin,
                img_TargetDown,
                img_TargetRight,
            };

            this.WindowState = FormWindowState.Maximized;
            _tickSize = (int)(Math.Abs(this.Top - tbl_TargetZone.Top) / 50.0);

            _tickThread = new CancelThread<object>(AdvanceTokenWorker, _tokenSrc.Token);
            _tickThread.IsBackground = true;
            _tickThread.Start();

            _feedbackThread = new QueueThread();
            _feedbackThread.IsBackground = true;
            _feedbackThread.Start();

            _userInputHandlerThread = new QueueThread<ArrowDir>(InputQueueHandler);
            _userInputHandlerThread.IsBackground = true;
            _userInputHandlerThread.Start();

            _gamepadThread = new CancelThread<object>(GamepadInputHandler, _tokenSrc.Token);
            _gamepadThread.IsBackground = true;
            _gamepadThread.Start();

            _newNoteThread = new CancelThread<object>(GenerateTokenWorker, _tokenSrc.Token);
            _newNoteThread.IsBackground = true;
            _newNoteThread.Start();

            hz_Top.Top = tbl_TargetZone.Top - _tickSize * _tickTopTolerance;
            hz_Top.BackColor = Color.Black;
            hz_Top.Left = 0;
            hz_Top.Width = this.Size.Width;

            hz_Bottom.Top = tbl_TargetZone.Top + _tickSize * _tickBtmTolerance + (int)(img_TargetLeft.Size.Height * .75);
            hz_Bottom.BackColor = Color.Black;
            hz_Bottom.Left = 0;
            hz_Bottom.Width = this.Size.Width;

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _tokenSrc.Cancel();
            base.OnFormClosing(e);
        }


        void OnSizeChangedHandler(object sender, EventArgs e) => OnSizeChanged(e);
        protected override void OnSizeChanged(EventArgs e)
        {
            hz_Top.Top = tbl_TargetZone.Top - _tickSize * _tickTopTolerance;
            hz_Top.Left = 0;
            hz_Top.Width = this.Size.Width;

            hz_Bottom.Top = tbl_TargetZone.Top + (_tickSize * _tickBtmTolerance) + (int)(img_TargetLeft.Size.Height * .75);
            hz_Bottom.Left = 0;
            hz_Bottom.Width = this.Size.Width;
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            var dir = keyData switch
            {
                Keys.Left => ArrowDir.Left,
                Keys.Up => ArrowDir.Up,
                Keys.Space => ArrowDir.Spin,
                Keys.Down => ArrowDir.Down,
                Keys.Right => ArrowDir.Right,
                _ => ArrowDir.MAX
            };
            if (dir != ArrowDir.MAX)
            {
                _ = _userInputHandlerThread.Enqueue(dir);
            }

            return base.ProcessDialogKey(keyData);
        }


        private void GenerateTokenWorker(object State, CancellationToken Token)
        {
            PictureBox generatePicture(ArrowDir dir)
            {
                PictureBox pic = new();
                var targetPic = _targetArrows[(int)dir];
                var state = new ArrowState(targetPic, dir);
                pic.Tag = state;
                pic.Top = 0;
                pic.ImageLocation = targetPic.ImageLocation;
                pic.SizeMode = PictureBoxSizeMode.Zoom;
                pic.BackColor = Color.Transparent;
                var size = (int)(targetPic.Size.Height * .75);
                pic.Size = new Size(size, size);
                pic.Left = (int)(this.Width / 5.0) * (int)dir + (int)(targetPic.Width / 2.0) - (int)(pic.Size.Width / 2.0);
                pic.BorderStyle = BorderStyle.FixedSingle;
                return pic;
            }

            var lastDir = -1;

            while (!Token.IsCancellationRequested && !IsDisposed)
            {
                int dirVal;

                do
                {
                    dirVal = Thread.CurrentThread.LocalRandom().Next(0, (int)ArrowDir.MAX);
                }
                while (dirVal == lastDir);
                lastDir = dirVal;
                var pic = generatePicture((ArrowDir)dirVal);
                this.AutoInvoke(x =>
                {
                    lock (_scrollingArrowLock)
                    {
                        _scrollingArrows.Add(pic);
                        x.Controls.Add(pic);
                        pic.BringToFront();
                    }
                });
                var waitTime = Thread.CurrentThread.LocalRandom().Next(0, 1500);
                var bonusWait = dirVal == (int)ArrowDir.Spin ? 750 : 0;
                _ = Token.WaitHandle.WaitOne(500 + waitTime + bonusWait);
            }
        }

        private void AdvanceTokenWorker(object State, CancellationToken Token)
        {
            List<int> deleteList = new();

            while (!Token.IsCancellationRequested && !IsDisposed)
            {
                if (_scrollingArrows.Count == 0)
                {
                    goto loopTerminal;
                }

                try
                {
                    this.AutoInvoke(x =>
                    {
                        lock (_scrollingArrowLock)
                        {
                            for (int i = 0; i < _scrollingArrows.Count; ++i)
                            {
                                var pic = _scrollingArrows[i];
                                var state = (ArrowState)pic.Tag;
                                pic.Top += _tickSize;
                                if (state.IsHit || pic.Top > (tbl_TargetZone.Top + _tickSize * _tickBtmTolerance))
                                {
                                    deleteList.Add(i);
                                }
                            }

                            if (deleteList.Count > 0)
                            {
                                for (int i = 0; i < deleteList.Count; ++i)
                                {
                                    var dIdx = deleteList[i] + i;
                                    var pic = _scrollingArrows[i];
                                    var state = (ArrowState)pic.Tag;
                                    x.Controls.Remove(pic);
                                    _scrollingArrows.RemoveAt(dIdx);
                                    if (!state.IsHit)
                                    {
                                        _ = _feedbackThread.Enqueue(() => NoteMissed());
                                    }
                                }
                                deleteList.Clear();
                            }
                        }
                    });
                }
                catch (ObjectDisposedException ex)
                {
                    //noop
                }

            loopTerminal:
                _ = Token.WaitHandle.WaitOne(_tickSpeed);
            }
        }


        private void GamepadInputHandler(object State, CancellationToken Token)
        {
            try
            {
                while (!Token.IsCancellationRequested && !IsDisposed)
                {

                    var input = _gamepadPort.ReadLine();
                    var dir = input.Trim() switch
                    {
                        "T8" => ArrowDir.Left,
                        "T7" => ArrowDir.Up,
                        "T6" => ArrowDir.Down,
                        "T5" => ArrowDir.Right,
                        _ => ArrowDir.MAX
                    };
                    if (dir == ArrowDir.MAX)
                    {
                        continue;
                    }

                    MessageBox.Show(dir.ToString());
                    _ = _userInputHandlerThread.Enqueue(dir);
                }
            }
            catch (OperationCanceledException)
            {
                //noop
            }
        }

        private void InputQueueHandler(ArrowDir dir)
        {
            bool didHit = false;

            try
            {
                this.AutoInvoke(x =>
                {
                    lock (_scrollingArrowLock)
                    {

                        for (int i = 0; i < _scrollingArrows.Count; ++i)
                        {
                            var pic = _scrollingArrows[i];
                            var state = (ArrowState)pic.Tag;
                            if (state.Direction != dir)
                            {
                                continue;
                            }

                            if (pic.Top > (tbl_TargetZone.Top - _tickSize * _tickTopTolerance) && pic.Top <= (tbl_TargetZone.Top + _tickSize * _tickBtmTolerance))
                            {
                                state.IsHit = true;
                                didHit = true;
                                break;
                            }
                        }
                    }
                });
            }
            catch (ObjectDisposedException ex)
            {
                //noop
            }

            if (!didHit)
            {
                _ = _feedbackThread.Enqueue(() => NoteMissed());
            }
        }


        private void NoteMissed()
        {

        }
    }


    public class ArrowState
    {
        public PictureBox TargetPicture { get; }
        public ArrowDir Direction { get; }
        public bool IsHit { get; set; }

        public ArrowState(PictureBox TargetPicture, ArrowDir Direction)
        {
            this.TargetPicture = TargetPicture;
            this.Direction = Direction;
        }
    }

    public enum ArrowDir
    {
        Left,
        Up,
        Spin,
        Down,
        Right,
        MAX
    }

}
