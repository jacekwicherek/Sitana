﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System.Text;
using Microsoft.Xna.Framework;

using Sitana.Framework.Cs;
using Sitana.Framework;
using Sitana.Framework.Graphics;

#if MACOSX
using MonoMac.Foundation;
#elif IOS
using Foundation;
#else
using System.Diagnostics;
#endif

namespace Sitana.Framework.Diagnostics
{
    public class PerformanceProfiler: Singleton<PerformanceProfiler>
    {
        class Counter
        {
            private List<float> _times = new List<float>();
            private int        _historyCount = 20;

            public float Value
            {
                get
                {
                    float value = _value;

                    for(int idx = 0; idx < _times.Count; ++idx)
                    {
                        value = Math.Max(value, _times[idx]);
                    }

                    return value;
                }
            }

            public bool Enabled;

            float _value;

            public int MaxFill = 50;

            double _begin;

            #if IOS || MACOSX

            public void Begin()
            {
                _begin = NSDate.Now.SecondsSinceReferenceDate;
            }

            public void End()
            {
                float time = (float)(NSDate.Now.SecondsSinceReferenceDate - _begin);
                AddTime(time);
            }

            #elif ANDROID

            public void Begin()
            {
				_begin = 0;
            }


            public void End()
            {
				float time = (float)(_begin);
				AddTime(time);
            }
            #else

            public void Begin()
            {
                _begin = StopWatch.Elapsed.TotalSeconds;
                _value = 0;
            }

            public void End()
            {
                float time = (float)(StopWatch.Elapsed.TotalSeconds - _begin);
                AddTime(time);
            }
            #endif

            private void AddTime(float time)
            {
                _value += time;
            }

            public Counter(int historyCount)
            {
                _historyCount = historyCount;
                Enabled = true;
            }

            public void Reset()
            {
                if (!float.IsNaN(Value))
                {
                    _times.Add(_value);
                }

                while (_times.Count > _historyCount)
                {
                    _times.RemoveAt(0);
                }

                _value = float.NaN;
            }
        }

        private List<float> _lastFrames = new List<float>();
        private float _fps = 0;
        private float _minFps = 0;
        private float _maxFrame = 0;

        private int _historyCount = 20;

        private float _targetFrameTime = 1f / 60f;
        private float _minFrameTime = 1f / 50f;

        private StringBuilder _stringBuilder = new StringBuilder();

        public bool ShowFps { private get; set; }
        public bool ShowMinFps { private get; set; }

        public bool Enabled = false;

        public Align Position = Align.Top;

        private SevenSegmentDisplay _display;

        private int _height;
        private int _right;
        

    #if !IOS && !MACOSX && !ANDROID
        internal static Stopwatch StopWatch = new Stopwatch();
    #endif

        List<Counter> _counters = new List<Counter>();

        public PerformanceProfiler()
        {
            ShowFps = true;
            ShowMinFps = true;

            #if !IOS && !MACOSX && !ANDROID
                StopWatch.Start();
            #endif
        }

        public int Initialize(int targetFps, int minFps)
        {
            _targetFrameTime = 1f / (float)targetFps;
            _minFrameTime = 1f / (float)minFps;

            int height = 0;

            _display = new SevenSegmentDisplay(0, out _height);

            _height = _height + Math.Max(2, _height / 5);

            Enabled = true;

            return height;
        }

        public int AddCounter(int historyCount)
        {
            Counter counter = new Counter(historyCount);
            _counters.Add(counter);

            return _counters.Count - 1;
        }

        public void EnableCounter(int id, bool enable)
        {
            Counter counter = _counters[id];
            counter.Enabled = enable;
        }

        public void BeginCounter(int id)
        {
            if (Enabled)
            {
                Counter counter = _counters[id];

                if (counter.Enabled)
                {
                    counter.Begin();
                }
            }
        }

        public void EndCounter(int id)
        {
            if (Enabled)
            {
                Counter counter = _counters[id];

                if (counter.Enabled)
                {
                    counter.End();
                }
            }
        }

        public void Update(TimeSpan timeSpan)
        {
            if (!Enabled)
            {
                return;
            }

            _lastFrames.Add((float)timeSpan.TotalSeconds);

            while (_lastFrames.Count > _historyCount)
            {
                _lastFrames.RemoveAt(0);
            }

            _fps = 0;
            _minFps = 1000;
            _maxFrame = 0;

            for (int idx = 0; idx < _lastFrames.Count; ++idx)
            {
                float frameTime = _lastFrames[idx];
                _fps += frameTime;

                if (frameTime > 0)
                {
                    _minFps = Math.Min(_minFps, 1.0f / frameTime);
                    _maxFrame = Math.Max(_maxFrame, frameTime);
                }
            }

            _fps /= (float)_lastFrames.Count;

            _fps = 1.0f / _fps;

            for ( int idx =0; idx < _counters.Count; ++idx )
            {
                _counters[idx].Reset();
            }
        }

        public void Draw(AdvancedDrawBatch batch)
        {
            if (!Enabled)
            {
                return;
            }

            Color normalColor = Color.LightBlue;
            Color errorColor = Color.Red;
            Color backgroundColor = Color.Black;

            int offset = Math.Max(1, _height / 10);

            _display.Reset(new Point(offset, offset));

            if (_right > 0)
            {
                Matrix matrix;

                matrix = Matrix.CreateRotationZ(MathHelper.PiOver2) * Matrix.CreateTranslation(new Vector3(_right, 0, 0));
                batch.PushTransform(matrix);
            }

            batch.BeginPrimitive(PrimitiveType.TriangleList, null);
            batch.PushVertex(new Vector2(0, 0), backgroundColor);
            batch.PushVertex(new Vector2(0, _height), backgroundColor);
            batch.PushVertex(new Vector2(6000, 0), backgroundColor);

            batch.PushVertex(new Vector2(0, _height), backgroundColor);
            batch.PushVertex(new Vector2(6000, 0), backgroundColor);
            batch.PushVertex(new Vector2(6000, _height), backgroundColor);

            int lastMaxFrameFill = (int)(100 * _maxFrame / (_targetFrameTime)) - 100;
            Color color;

            lastMaxFrameFill = Math.Min(99, lastMaxFrameFill);

            _stringBuilder.Clear();
            _stringBuilder.AppendFormat("{0,-2} ", (int)_minFps);
            color = (_maxFrame > _minFrameTime) ? errorColor : normalColor;
            _display.Draw(batch, _stringBuilder, color);

            _stringBuilder.Clear();
            _stringBuilder.AppendFormat("{0,-2} ", (int)_fps);
            color = _fps < (1 / _minFrameTime) ? errorColor : normalColor;
            _display.Draw(batch, _stringBuilder, color);

            _stringBuilder.Clear();
            _stringBuilder.AppendFormat("{0,-2} ", Math.Max(0, lastMaxFrameFill));
            color = (_maxFrame > _minFrameTime) ? errorColor : normalColor;
            _display.Draw(batch, _stringBuilder, color);

            for (int idx = 0; idx < _counters.Count; ++idx)
            {
                var counter = _counters[idx];

                float frameTime = counter.Value;
                int lastUpdatePercent = float.IsNaN(frameTime) ? -1 : (int)(100 * frameTime / (_targetFrameTime));
                lastUpdatePercent = Math.Min(99, lastUpdatePercent);

                _stringBuilder.Clear();

                if (lastUpdatePercent < 0)
                {
                    _stringBuilder.AppendFormat("{1}{0,-2} ", "-", (char)(idx+'A'));
                }
                else
                {
                    _stringBuilder.AppendFormat("{1}{0,-2} ", lastUpdatePercent, (char)(idx + 'A'));
                }

                color = (lastUpdatePercent > counter.MaxFill) ? errorColor : normalColor;
                _display.Draw(batch, _stringBuilder, color);
            }

            if (_right > 0)
            {
                batch.PopTransform();
            }
        }

        public void ComputeContentRect(ref Rectangle rect)
        {
            if (Enabled)
            {
                _right = rect.Width > rect.Height ? rect.Width : 0;

                if (_right > 0)
                {
                    rect.Width -= _height;
                }
                else
                {
                    rect.Y += _height;
                    rect.Height -= _height;
                }
            }
        }
    }
}

