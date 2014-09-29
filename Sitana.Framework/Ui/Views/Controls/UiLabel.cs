﻿/// This file is a part of the EBATIANOS.ESSENTIALS class library.
/// (C)2013-2014 Sebastian Sejud. All rights reserved.
///
/// THIS SOURCE FILE IS THE PROPERTY OF SEBASTIAN SEJUD AND IS NOT TO BE 
/// RE-DISTRIBUTED BY ANY MEANS WHATSOEVER WITHOUT THE EXPRESSED WRITTEN 
/// CONSENT OF SEBASTIAN SEJUD.
/// 
/// THIS SOURCE CODE CAN ONLY BE USED UNDER THE TERMS AND CONDITIONS OUTLINED
/// IN THE EBATIANOS.ESSENTIALS LICENSE AGREEMENT. SEBASTIAN SEJUD GRANTS
/// TO YOU (ONE SOFTWARE DEVELOPER) THE LIMITED RIGHT TO USE THIS SOFTWARE 
/// ON A SINGLE COMPUTER.
///
/// CONTACT INFORMATION:
/// essentials@sejud.com
/// sejud.com/essentials-library
/// 
///---------------------------------------------------------------------------
using Sitana.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Text;
using Sitana.Framework.Ui.Views.Parameters;
using Microsoft.Xna.Framework.Graphics;
using Sitana.Framework.Content;
using Sitana.Framework.Ui.DefinitionFiles;
using Sitana.Framework.Essentials.Ui.DefinitionFiles;
using System;
using Sitana.Framework.Ui.Controllers;
using Sitana.Framework.Cs;
using Sitana.Framework.Xml;

namespace Sitana.Framework.Ui.Views
{
    public class UiLabel: UiView
    {
        public new static void Parse(XNode node, DefinitionFile file)
        {
            UiView.Parse(node, file);

            var parser = new DefinitionParser(node);

            file["Text"] = parser.ParseString("Text");
            file["Font"] = parser.Value("Font");
            file["FontSize"] = parser.ParseInt("FontSize");

            file["TextColor"] = parser.ParseColor("TextColor");
        }

        public SharedString Text { get; private set; }
        public ColorWrapper TextColor { get; private set; }

        public string FontName
        {
            get
            {
                return _fontName;
            }

            set
            {
                _fontName = value;
                _font = null;
            }
        }

        public int FontSize
        {
            get
            {
                return _fontSize;
            }

            set
            {
                _fontSize = value;
                _font = null;
            }
        }


        int _fontSize = 0;
        string _fontName = null;

        SpriteFont _font;

        protected override void Draw(ref UiViewDrawParameters parameters)
        {
            if (DisplayOpacity == 0 || TextColor.Value.A == 0)
            {
                return;
            }

            base.Draw(ref parameters);

            if (_font == null )
            {
                _font = FontManager.Instance.FindFont(FontName, FontSize);
            }

            parameters.DrawBatch.Font = _font;
            parameters.DrawBatch.DrawText(Text, ScreenBounds, TextAlign.Center | TextAlign.Middle, TextColor.Value * DisplayOpacity);
        }

        protected override void Init(UiController controller, object binding, DefinitionFile definition)
        {
            base.Init(ref controller, binding, definition);

            DefinitionFileWithStyle file = new DefinitionFileWithStyle(definition, typeof(UiLabel));

            FontName = file["Font"] as string;
            FontSize = DefinitionResolver.Get<int>(controller, binding, file["FontSize"], 0);

            Text = DefinitionResolver.GetSharedString(controller, binding, file["Text"]);
            TextColor = DefinitionResolver.GetColorWrapper(controller, binding, file["TextColor"]) ?? new ColorWrapper(Color.White);
        }
    }
}
