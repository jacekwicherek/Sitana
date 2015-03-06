﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitana.Framework.Ui.DefinitionFiles;
using Sitana.Framework.Xml;
using Sitana.Framework.Ui.Controllers;
using Microsoft.Xna.Framework;
using Sitana.Framework.Graphics;
using Sitana.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Sitana.Framework.Cs;

namespace Sitana.Framework.Ui.Views.ButtonDrawables
{
    public class Text : ButtonDrawable
    {
        public new static void Parse(XNode node, DefinitionFile file)
        {
            ButtonDrawable.Parse(node, file);

            var parser = new DefinitionParser(node);

            file["Font"] = parser.Value("Font");
            file["FontSize"] = parser.ParseInt("FontSize");
            file["FontSpacing"] = parser.ParseInt("FontSpacing");
            file["LineHeight"] = parser.ParseInt("LineHeight");
            file["HorizontalContentAlignment"] = parser.ParseEnum<HorizontalContentAlignment>("HorizontalContentAlignment");
            file["VerticalContentAlignment"] = parser.ParseEnum<VerticalContentAlignment>("VerticalContentAlignment");
            file["Text"] = parser.ParseString("Text");
        }

        protected string _font;
        protected int _fontSize;
        protected int _fontSpacing;
        protected TextAlign _textAlign;
        protected SharedString _text;
        protected int _lineHeight;

        private FontFace _fontFace = null;

        protected override void Init(UiController controller, object binding, DefinitionFile definition)
        {
            base.Init(controller, binding, definition);

            DefinitionFileWithStyle file = new DefinitionFileWithStyle(definition, typeof(Text));

            _font = DefinitionResolver.GetString(controller, binding, file["Font"]);
            _fontSize = DefinitionResolver.Get<int>(controller, binding, file["FontSize"], 0);
            _fontSpacing = DefinitionResolver.Get<int>(controller, binding, file["FontSpacing"], 0);
            _lineHeight = DefinitionResolver.Get<int>(controller, binding, file["LineHeight"], 0);
            HorizontalContentAlignment horzAlign = DefinitionResolver.Get<HorizontalContentAlignment>(controller, binding, file["HorizontalContentAlignment"], HorizontalContentAlignment.Center);
            VerticalContentAlignment vertAlign = DefinitionResolver.Get<VerticalContentAlignment>(controller, binding, file["VerticalContentAlignment"], VerticalContentAlignment.Center);

            _textAlign = UiHelper.TextAlignFromContentAlignment(horzAlign, vertAlign);
            _text = DefinitionResolver.GetSharedString(controller, binding, file["Text"]);
        }

        public override void Draw(AdvancedDrawBatch drawBatch, DrawButtonInfo info)
        {
            Update(info.EllapsedTime, info.ButtonState);

            SharedString str = _text != null ? _text : info.Text;

            if (_fontFace == null)
            {
                _fontFace = FontManager.Instance.FindFont(_font);
            }

            float scale;
            UniversalFont font = _fontFace.Find(_fontSize, out scale);

            Color color = ColorFromState * info.Opacity * Opacity;

            Rectangle target = _margin.ComputeRect(info.Target);

            drawBatch.DrawText(font, str, target, _textAlign, color, (float)_fontSpacing / 1000.0f, (float)_lineHeight / 100.0f, scale);
        }

        public override object OnAction(DrawButtonInfo info, params object[] parameters)
        {
            return null;
        }
    }
}
