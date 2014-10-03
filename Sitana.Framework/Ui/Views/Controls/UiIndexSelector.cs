﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitana.Framework.Xml;
using Sitana.Framework.Ui.DefinitionFiles;
using Sitana.Framework.Ui.Views.Parameters;
using Sitana.Framework.Ui.Controllers;
using Sitana.Framework.Diagnostics;
using Sitana.Framework.Ui.Views.ButtonDrawables;
using Microsoft.Xna.Framework;
using Sitana.Framework.Cs;
using Sitana.Framework.Input.TouchPad;

namespace Sitana.Framework.Ui.Views
{
    public class UiIndexSelector : UiButton
    {
        IIndexedElement _element;

        public new static void Parse(XNode node, DefinitionFile file)
        {
            UiButton.Parse(node, file);

            var parser = new DefinitionParser(node);

            file["Context"] = parser.ParseString("Context");
            file["Spacing"] = parser.ParseLength("Spacing");

            file["ElementWidth"] = parser.ParseLength("ElementWidth");
            file["ElementHeight"] = parser.ParseLength("ElementHeight");
        }

        private Length _spacing;
        private Length _elementWidth;
        private Length _elementHeight;
        private int _pushedIndex = -1;


        protected override void Draw(ref UiViewDrawParameters parameters)
        {
            float opacity = DisplayOpacity * parameters.Opacity;

            if (opacity == 0)
            {
                return;
            }

            
            int spacing = _spacing.Compute(Bounds.Height);

            Rectangle rect = GetFirstRect();

            int size = rect.Width;
            int selected = _element.SelectedIndex;

            int count = _element.Count;

            for (int idx = 0; idx < count; ++idx)
            {
                UiButton.State state = UiButton.State.Released;

                _element.GetText(_text, idx);

                if (idx == selected || idx == _pushedIndex)
                {
                    state = UiButton.State.Pushed;
                }

                for (int di = 0; di < _drawables.Count; ++di)
                {
                    _drawables[di].Draw(parameters.DrawBatch, rect, opacity, state, _text);
                }

                rect.X += size + spacing;
            }
        }

        Rectangle GetFirstRect()
        {
            Rectangle rect = ScreenBounds;

            int count = _element.Count;
            int spacing = _spacing.Compute(Bounds.Height);

            int width = _elementWidth.Compute(Bounds.Height);
            int height = _elementHeight.Compute(Bounds.Height);

            int posX = rect.Center.X - (width * count + spacing * (count-1)) / 2;
            int posY = rect.Center.Y - height / 2;

            rect = new Rectangle(posX, posY, width, height);

            return rect;
        }

        protected override void Init(object controller, object binding, DefinitionFile definition)
        {
            base.Init(controller, binding, definition);

            DefinitionFileWithStyle file = new DefinitionFileWithStyle(definition, typeof(UiLabel));

            string id = DefinitionResolver.GetString(Controller, Binding, file["Context"]);

            _element = Controller.Find(id) as IIndexedElement;

            _spacing = DefinitionResolver.Get<Length>(Controller, Binding, file["Spacing"], Length.Zero);

            _elementWidth = DefinitionResolver.Get<Length>(Controller, Binding, file["ElementWidth"], Length.Stretch);
            _elementHeight = DefinitionResolver.Get<Length>(Controller, Binding, file["ElementHeight"], Length.Stretch);
        }

        protected override void OnGesture(Gesture gesture)
        {
            base.OnGesture(gesture);

            _pushedIndex = -1;

            if (IsPushed && _touchId == gesture.TouchId)
            {
                int spacing = _spacing.Compute(Bounds.Height);
                int count = _element.Count;

                Rectangle rect = GetFirstRect();

                int size = rect.Width;

                Point point = gesture.Origin.ToPoint();

                for (int idx = 0; idx < count; ++idx)
                {
                    if (rect.Contains(point))
                    {
                        _pushedIndex = idx;
                    }
                    
                    rect.X += size + spacing;
                }
            }
        }

        protected override void DoAction()
        {
            if (_pushedIndex >= 0)
            {
                _element.SelectedIndex = _pushedIndex;
            }
        }

    }
}
