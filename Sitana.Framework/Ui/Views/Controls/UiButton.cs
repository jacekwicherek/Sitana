﻿// SITANA - Copyright (C) The Sitana Team.
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Sitana.Framework.Input.TouchPad;
using Sitana.Framework.Ui.Controllers;
using Sitana.Framework.Ui.DefinitionFiles;
using Sitana.Framework.Xml;
using Sitana.Framework.Diagnostics;
using System;
using Sitana.Framework.Ui.Views.ButtonDrawables;
using Sitana.Framework.Cs;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Sitana.Framework.Ui.Views
{
    public class UiButton: UiView
    {
        public new static void Parse(XNode node, DefinitionFile file)
        {
            UiView.Parse(node, file);

            var parser = new DefinitionParser(node);

            file["Text"] = parser.ParseString("Text");
            file["Icon"] = parser.ParseResource<Texture2D>("Icon");
            file["Click"] = parser.ParseDelegate("Click");
            file["Enabled"] = parser.ParseBoolean("Enabled");

            file["PushSound"] = parser.ParseResource<SoundEffect>("PushSound");
            file["ReleaseSound"] = parser.ParseResource<SoundEffect>("ReleaseSound");
            file["ActionSound"] = parser.ParseResource<SoundEffect>("ActionSound");

            foreach (var cn in node.Nodes)
            {
                switch (cn.Tag)
                {
                    case "UiButton.Drawables":
                        ParseDrawables(cn, file, typeof(ButtonDrawable));
                        break;
                }
            }
        }

        protected UiButtonMode ButtonMode
        {
            set
            {
                _mode = value;
            }
        }

        public bool IsPushed {get; private set;}

        public SharedValue<bool> Enabled { get; set; }

        float _delayTime = 0.5f;
        float _waitForAction = 0;

        UiButtonMode _mode = UiButtonMode.Release;

        protected int _touchId = 0;

        private Dictionary<int, bool> _touches = new Dictionary<int, bool>();

        protected List<ButtonDrawable> _drawables = new List<ButtonDrawable>();

        protected SharedString _text;

        protected Rectangle _checkRect;

        public readonly SharedValue<Texture2D> Icon = new SharedValue<Texture2D>();

        private SoundEffect _pushSound;
        private SoundEffect _releaseSound;
        protected SoundEffect _actionSound;

        public SharedString Text
        {
            get
            {
                return _text;
            }

            set
            {
                _text = value;
            }
        }

        public virtual ButtonState ButtonState
        {
            get
            {
                return Enabled.Value ? (IsPushed ? ButtonState.Pushed : ButtonState.None) : ButtonState.Disabled;
            }
        }

        public UiButton()
        {
        }

        protected override void OnAdded()
        {
            SetPushed(false, false);
            OnPushedChanged();

            EnabledGestures = (GestureType.Down | GestureType.Up | GestureType.Move);
        }

        protected override void Update(float time)
        {
            base.Update(time);

            if( _waitForAction > 0 )
            {
                _waitForAction -= time;

                if ( _waitForAction <= 0 )
                {
                    DoAction();
                    _waitForAction = 0;
                }
            }
        }

        protected override void OnGesture(Gesture gesture)
        {
            if (!Enabled.Value)
            {
                return;
            }

            Rectangle bounds = ScreenBounds;

            switch(gesture.GestureType)
            {
                case GestureType.CapturedByOther:

                    if (_touchId == gesture.TouchId)
                    {
                        _touchId = 0;
                        SetPushed(false, true);
                    }
                    break;

                case GestureType.Down:

                    if (IsPointInsideView(gesture.Origin))
                    {
                        if ( _mode == UiButtonMode.Game)
                        {
                            if ( !_touches.ContainsKey(gesture.TouchId))
                            {
                                _touches.Add(gesture.TouchId, true);
                            }
                            break;
                        }

                        if (_touchId == 0)
                        {
                            _touchId = gesture.TouchId;

                            SetPushed(true, _mode != UiButtonMode.Press);
                            _checkRect = ScreenBounds;

                            gesture.Handled = true;

                            if (_mode == UiButtonMode.Press)
                            {
                                DoAction();
                            }
                            else if (_mode == UiButtonMode.Delayed)
                            {
                                _waitForAction = _delayTime;
                            }
                        }
                    }
                    break;

                case GestureType.Move:

                    if (_mode == UiButtonMode.Game)
                    {
                        if (IsPointInsideView(gesture.Position))
                        {
                            if (!_touches.ContainsKey(gesture.TouchId))
                            {
                                _touches.Add(gesture.TouchId, true);
                            }
                            SetPushed(true, true);
                        }
                        else
                        {
                            _touches.Remove(gesture.TouchId);

                            if (_touches.Count == 0 )
                            {
                                SetPushed(false, true);
                            }
                        }
                        break;
                    }
                    

                    if (_touchId == gesture.TouchId)
                    {
                        SetPushed(_checkRect.Contains(gesture.Position), true);
                    }
                    break;

                case GestureType.Up:

                    if (_mode == UiButtonMode.Game)
                    {
                        _touches.Remove(gesture.TouchId);

                        if (_touches.Count == 0)
                        {
                            SetPushed(false, true);
                        }
                        break;
                    }

                    if ( _touchId == gesture.TouchId)
                    {
                        if ( IsPushed && _mode == UiButtonMode.Release)
                        {
                            DoAction();
                        }

                        _touchId = 0;
                        SetPushed(false, false);
                    }
                    break;
            }
        }
        
        protected virtual void OnPushedChanged()
        {
            
        }

        void SetPushed(bool pushed, bool playSound)
        {
            if ( IsPushed != pushed)
            {
                if (playSound)
                {
                    SoundEffect sound = pushed ? _pushSound : _releaseSound;

                    if (sound != null)
                    {
                        sound.Play();
                    }
                }

                IsPushed = pushed;
                OnPushedChanged();
            }
        }

        protected override void Init(object controller, object binding, DefinitionFile definition)
        {
            base.Init(controller, binding, definition);

            DefinitionFileWithStyle file = new DefinitionFileWithStyle(definition, typeof(UiButton));

            Icon.Value = DefinitionResolver.Get<Texture2D>(Controller, Binding, file["Icon"], null);

            _text = DefinitionResolver.GetSharedString(Controller, Binding, file["Text"]);

            if (_text == null)
            {
                _text = new SharedString();
            }

            RegisterDelegate("Click", file["Click"]);

            List<DefinitionFile> drawableFiles = file["Drawables"] as List<DefinitionFile>;

            Enabled = DefinitionResolver.GetShared<bool>(Controller, Binding, file["Enabled"], true);

            if ( drawableFiles != null )
            {
                foreach (var def in drawableFiles)
                {
                    ButtonDrawable drawable = def.CreateInstance(Controller, Binding) as ButtonDrawable;

                    if (drawable != null)
                    {
                        _drawables.Add(drawable);
                    }
                }
            }

            _pushSound = DefinitionResolver.Get<SoundEffect>(Controller, Binding, file["PushSound"], null);
            _releaseSound = DefinitionResolver.Get<SoundEffect>(Controller, Binding, file["ReleaseSound"], null);
            _actionSound = DefinitionResolver.Get<SoundEffect>(Controller, Binding, file["ActionSound"], null);
        }

        protected override void Draw(ref Parameters.UiViewDrawParameters parameters)
        {
            float opacity = parameters.Opacity;

            if (opacity == 0)
            {
                return;
            }

            var batch = parameters.DrawBatch;

            var drawInfo = new DrawButtonInfo();
            drawInfo.Text = _text;
            drawInfo.ButtonState = ButtonState;

            drawInfo.Target = ScreenBounds;
            drawInfo.Opacity = opacity;
            drawInfo.EllapsedTime = parameters.EllapsedTime;
            drawInfo.Icon = Icon.Value;

            for (int idx = 0; idx < _drawables.Count; ++idx)
            {
                var drawable = _drawables[idx];
                drawable.Draw(batch, drawInfo);
            }
        }

        protected virtual void DoAction()
        {
            CallDelegate("Click");

            if (_actionSound != null)
            {
                _actionSound.Play();
            }
        }
    }
}
