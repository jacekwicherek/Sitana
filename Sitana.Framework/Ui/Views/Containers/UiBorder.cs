﻿// SITANA - Copyright (C) The Sitana Team.
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Sitana.Framework.Ui.DefinitionFiles;
using Sitana.Framework.Xml;
using Sitana.Framework.Ui.Controllers;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sitana.Framework.Ui.Views
{
    public class UiBorder : UiContainer
    {
        public new static void Parse(XNode node, DefinitionFile file)
        {
            UiContainer.Parse(node, file);
        }

        protected override bool Init(object controller, object binding, DefinitionFile definition)
        {
            if (!base.Init(controller, binding, definition))
            {
                return false;
            }
            InitChildren(Controller, Binding, definition);
            return true;
        }

        protected override Rectangle CalculateChildBounds(UiView view)
        {
            Point size = view.ComputeSize(Bounds.Width, Bounds.Height);
            PositionParameters pos = view.PositionParameters;

            Rectangle childRect = new Rectangle(0, 0, size.X, size.Y);

            int posX = pos.X.Compute(Bounds.Width);
            int posY = pos.Y.Compute(Bounds.Height);

            switch(pos.HorizontalAlignment)
            {
            case HorizontalAlignment.Center:
                childRect.X = posX - size.X / 2;
                break;

            case HorizontalAlignment.Left:
                childRect.X = posX;
                break;

            case HorizontalAlignment.Right:
                childRect.X = posX - size.X;
                break;

            case HorizontalAlignment.Stretch:
                childRect.X = 0;
                childRect.Width = Bounds.Width;
                break;
            }

            switch(pos.VerticalAlignment)
            {
            case VerticalAlignment.Center:
                childRect.Y = posY - size.Y / 2;
                break;

            case VerticalAlignment.Top:
                childRect.Y = posY;
                break;

            case VerticalAlignment.Bottom:
                childRect.Y = posY - size.Y;
                break;

            case VerticalAlignment.Stretch:
                childRect.Y = 0;
                childRect.Height = Bounds.Height;
                break;
            }

            pos.Margin.RepairRect(ref childRect, Bounds.Width, Bounds.Height);
            return childRect;
        }


    }
}
