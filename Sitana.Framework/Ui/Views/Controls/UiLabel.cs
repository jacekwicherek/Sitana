﻿// SITANA - Copyright (C) The Sitana Team.
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Sitana.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Text;
using Sitana.Framework.Ui.Views.Parameters;
using Microsoft.Xna.Framework.Graphics;
using Sitana.Framework.Content;
using Sitana.Framework.Ui.DefinitionFiles;
using System;
using Sitana.Framework.Ui.Controllers;
using Sitana.Framework.Cs;
using Sitana.Framework.Xml;
using Sitana.Framework.Ui.Core;

namespace Sitana.Framework.Ui.Views
{
    public class UiLabel: UiView
    {
        public new static void Parse(XNode node, DefinitionFile file)
        {
            UiView.Parse(node, file);

            var parser = new DefinitionParser(node);

            file["Text"] = parser.ParseString("Text");
            file["Font"] = parser.ValueOrNull("Font");
            file["FontSize"] = parser.ParseInt("FontSize");
            file["FontSpacing"] = parser.ParseInt("FontSpacing");
            file["LineHeight"] = parser.ParseInt("LineHeight");

            file["TextColor"] = parser.ParseColor("TextColor");
            file["HorizontalContentAlignment"] = parser.ParseEnum<HorizontalContentAlignment>("HorizontalContentAlignment");
            file["VerticalContentAlignment"] = parser.ParseEnum<VerticalContentAlignment>("VerticalContentAlignment");

            file["AutoSizeUpdate"] = parser.ParseBoolean("AutoSizeUpdate");
            file["TextRotation"] = parser.ParseEnum<TextRotation>("TextRotation");

            file["TextMargin"] = parser.ParseMargin("TextMargin");
        }

        public static ColorWrapper DefaultTextColor = new ColorWrapper();

        public SharedString Text{ get; private set; }
        public ColorWrapper TextColor { get; protected set; }

        protected TextRotation _rotation;

        public string FontName
        {
            get
            {
                return _fontName;
            }

            set
            {
                _fontName = value;
                _fontFace = null;
            }
        }
        
        public virtual int FontSize {get;set;}
        public int FontSpacing {get; set;}
        public int LineHeight { get; set; }

        string _fontName;
        protected Margin _textMargin;

        protected FontFace _fontFace = null;
        public TextAlign TextAlign {get;set;}

        protected override void Draw(ref UiViewDrawParameters parameters)
        {
            float opacity = parameters.Opacity;

            if (opacity == 0 || TextColor.Value.A == 0)
            {
                return;
            }

            base.Draw(ref parameters);

            if (_fontFace == null)
            {
                _fontFace = FontManager.Instance.FindFont(FontName);
            }

            float scale;
            UniversalFont font = _fontFace.Find(FontSize, out scale);

            Rectangle bounds = ScreenBounds;

            if(Text.Length > 0)
            {
                bounds =  _textMargin.ComputeRect(bounds);
            }

            parameters.DrawBatch.DrawText(font, Text, bounds, TextAlign, TextColor.Value * opacity, (float)FontSpacing / 1000.0f, (float)LineHeight / 100.0f, scale, _rotation);
        }

        public override Point ComputeSize(int width, int height)
        {
            Point size = base.ComputeSize(width, height);

            Vector2 sizeInPixels = new Vector2(-1,-1);

            if (PositionParameters.Width.IsAuto)
            {
                sizeInPixels = CalculateSizeInPixels();
                size.X = (int)Math.Ceiling(sizeInPixels.X);
            }

            if (PositionParameters.Height.IsAuto)
            {
                if (sizeInPixels.Y < 0)
                {
                    sizeInPixels = CalculateSizeInPixels();
                }
                size.Y = (int)Math.Ceiling(sizeInPixels.Y);
            }

            return size;
        }

        private Vector2 CalculateSizeInPixels()
        {
            if (_fontFace == null)
            {
                _fontFace = FontManager.Instance.FindFont(FontName);
            }

            float scale;
            UniversalFont font = _fontFace.Find(FontSize, out scale);

            Vector2 size;

            lock (Text)
            {
                size = font.MeasureString(Text.StringBuilder, (float)FontSpacing / 1000.0f, (float)LineHeight / 100.0f);
            }

            switch(_rotation)
            {
                case TextRotation.Rotate270:
                case TextRotation.Rotate90:
                    size = new Vector2(size.Y, size.X);
                    break;
            }

            Vector2 marginIfText = Vector2.Zero;

            if(Text.Length > 0)
            {
                marginIfText = new Vector2(_textMargin.Width, _textMargin.Height);
            }

            return size * scale + marginIfText;
        }

        public Point CalculateSize()
        {
            Vector2 size = CalculateSizeInPixels();
            return (size / (float)UiUnit.Unit).ToPoint();
        }

        protected override bool Init(object controller, object binding, DefinitionFile definition)
        {
            if (!base.Init(controller, binding, definition))
            {
                return false;
            }

            DefinitionFileWithStyle file = new DefinitionFileWithStyle(definition, typeof(UiLabel));

            FontName = file["Font"] as string;
            FontSize = DefinitionResolver.Get<int>(Controller, Binding, file["FontSize"], 0);
            FontSpacing = DefinitionResolver.Get<int>(Controller, Binding, file["FontSpacing"], 0);
            LineHeight = DefinitionResolver.Get<int>(Controller, Binding, file["LineHeight"], 100);

            _textMargin = DefinitionResolver.Get<Margin>(Controller, Binding, file["TextMargin"], Margin.None);

            _rotation = DefinitionResolver.Get<TextRotation>(Controller, Binding, file["TextRotation"], TextRotation.None);

            Text = DefinitionResolver.GetSharedString(Controller, Binding, file["Text"]);

            if (Text == null)
            {
                return false;
            }

            TextColor = DefinitionResolver.GetColorWrapper(Controller, Binding, file["TextColor"]) ?? DefaultTextColor;

            HorizontalContentAlignment horzAlign = DefinitionResolver.Get<HorizontalContentAlignment>(Controller, Binding, file["HorizontalContentAlignment"], HorizontalContentAlignment.Auto);
            VerticalContentAlignment vertAlign = DefinitionResolver.Get<VerticalContentAlignment>(Controller, Binding, file["VerticalContentAlignment"], VerticalContentAlignment.Auto);

            if (horzAlign == HorizontalContentAlignment.Auto)
            {
                horzAlign = UiHelper.ContentAlignFromAlignment(PositionParameters.HorizontalAlignment);
            }

            if (vertAlign == VerticalContentAlignment.Auto)
            {
                vertAlign = UiHelper.ContentAlignFromAlignment(PositionParameters.VerticalAlignment);
            }

            TextAlign = UiHelper.TextAlignFromContentAlignment(horzAlign, vertAlign);

            if(DefinitionResolver.Get<bool>(Controller, Binding, file["AutoSizeUpdate"], false))
            {
                Text.ValueChanged += Text_ValueChanged;
            }

            return true;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            Text.ValueChanged -= Text_ValueChanged;
        }

        protected void Text_ValueChanged()
        {
            if(Parent!=null)
            {
                Parent.RecalcLayout();
            }
        }
    }
}
