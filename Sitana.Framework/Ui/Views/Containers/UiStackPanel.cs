﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitana.Framework.Xml;
using Sitana.Framework.Ui.DefinitionFiles;
using Microsoft.Xna.Framework;
using Sitana.Framework.Cs;
using Sitana.Framework.Ui.Views.Parameters;
using Sitana.Framework.Ui.Core;

namespace Sitana.Framework.Ui.Views
{
    public class UiStackPanel : UiContainer
    {
        public new static void Parse(XNode node, DefinitionFile file)
        {
            UiContainer.Parse(node, file);

            DefinitionParser parser = new DefinitionParser(node);

            file["Mode"] = parser.ParseEnum<Mode>("Mode");
            file["Spacing"] = parser.ParseLength("Spacing", false);
            file["Padding"] = parser.ParseLength("Padding", false);
            file["NotifyParentOnResize"] = parser.ParseBoolean("NotifyParentOnResize");

            file["ExpandTime"] = parser.ParseInt("ExpandTime");
            file["Expanded"] = parser.ParseBoolean("Expanded");

            file["Wrap"] = parser.ParseBoolean("Wrap");

            file["HorizontalContentAlignment"] = parser.ParseEnum<HorizontalContentAlignment>("HorizontalContentAlignment");
            file["VerticalContentAlignment"] = parser.ParseEnum<VerticalContentAlignment>("VerticalContentAlignment");
        }

        public enum Mode
        {
            Horizontal,
            Vertical
        }

        HorizontalContentAlignment _horizontalContentAlignment;
        VerticalContentAlignment _verticalContentAlignment;

        bool _vertical = false;
        bool _updateBounds = true;
        bool _recalculateLayout = true;
        bool _notifyParentOnResize = true;
        bool _wrap = false;

        double _expandSpeed;
        double _expandedValue;

        int _currentWrapPos;
        int _currentWrapMax;

        SharedValue<bool> _expanded;

        Length _spacing;
        Length _padding;

        int _currentSize;

        Point? _internalSize = null;

        List<UiView> _tempChildren = new List<UiView>();

        int _rest;
        int _restParts;

        public int Spacing
        {
            get
            {
                return _spacing.Compute(0);
            }
        }

        public int Padding
        {
            get
            {
                return _padding.Compute();
            }

            set
            {
                _padding = new Length(value);
                RecalcLayout();
            }
        }

        public Mode StackMode
        {
            get
            {
                return _vertical ? Mode.Vertical : Mode.Horizontal;
            }

            set
            {
                _vertical = value == Mode.Vertical;

                RecalcLayout();
                OnChildrenModified();
            }
        }

        protected override void Draw(ref Parameters.UiViewDrawParameters parameters)
        {
            float opacity = parameters.Opacity;

            if (opacity == 0)
            {
                return;
            }

            DrawBackground(ref parameters);

            UiViewDrawParameters drawParams = parameters;
            drawParams.Opacity = opacity;

            if (_clipChildren || _expandedValue < 1)
            {
                parameters.DrawBatch.PushClip(ScreenBounds);
            }

            Rectangle bound = new Rectangle(0, 0, Bounds.Width, Bounds.Height);

            for (int idx = 0; idx < _children.Count; ++idx)
            {
                var child = _children[idx];

                if (child.Bounds.Intersects(bound))
                {
                    child.ViewDraw(ref drawParams);
                }
            }

            if (_clipChildren || _expandedValue < 1)
            {
                parameters.DrawBatch.PopClip();
            }
        }

        protected override void Update(float time)
        {
            if (_updateBounds)
            {
                UpdateBounds();
                _updateBounds = false;
            }

            if (_recalculateLayout)
            {
                _recalculateLayout = false;
                RecalcLayout();
            }

            base.Update(time);

            double desiredValue = _expanded.Value ? 1 : 0;
            bool update = false;

            if (_expandedValue < desiredValue)
            {
                _expandedValue += time * _expandSpeed;
                _expandedValue = Math.Min(1, _expandedValue);

                update = true;
            }
            else if (_expandedValue > desiredValue)
            {
                _expandedValue -= time * _expandSpeed;
                _expandedValue = Math.Max(0, _expandedValue);
                update = true;
            }

            if (update)
            {
                Parent.RecalcLayout();
            }
        }

        void UpdateBounds()
        {
            if (_notifyParentOnResize && Parent != null)
            {
                Parent.RecalcLayout();
            }
        }

        public override void Add(UiView view)
        {
            if (!_children.Contains(view))
            {
                _children.Add(view);
                view.Parent = this;
                view.RegisterView();

                if (_added)
                {
                    view.ViewAdded();
                }

                OnChildrenModified();

                _recalculateLayout = true;
            }
        }

        public Point ComputeSizeInternal(int width, int height)
        {
            if (_recalculateLayout)
            {
                RecalcLayout();
                _recalculateLayout = false;
            }

            Point value = base.ComputeSize(width, height);

            int size = _padding.Compute();
            int sizeAlt = 0;

            for (int idx = 0; idx < _children.Count; ++idx)
            {
                var child = _children[idx];

                if (child.DisplayVisibility > 0)
                {
                    if ( size > _padding.Compute())
                    {
                        size += _spacing.Compute();
                    }

                    size += _vertical ? child.Bounds.Height + child.Margin.Height : child.Bounds.Width + child.Margin.Width;
                    sizeAlt = Math.Max(sizeAlt, _vertical ? child.Bounds.Width + child.Margin.Width : child.Bounds.Height + child.Margin.Height);
                }
            }

            size += _padding.Compute();

            if (_vertical)
            {
                if (PositionParameters.Height.IsAuto)
                {
                    value.Y = size;
                }

                if(PositionParameters.Width.IsAuto)
                {
                    value.X = sizeAlt;
                }
            }
            else
            {
                if (PositionParameters.Width.IsAuto)
                {
                    value.X = size;
                }

                if (PositionParameters.Height.IsAuto)
                {
                    value.Y = sizeAlt;
                }
            }

            return value;
        }

        protected override Rectangle CalculateChildBounds(UiView view)
        {
            Rectangle childBounds = Bounds;

            int index = _tempChildren.IndexOf(view);

            if ( index < 0)
            {
                return new Rectangle(100000, 10000, 100, 100);
            }

            if(index == 0)
            {
                _currentWrapMax = 0;
                _currentWrapPos = Padding;

                _currentSize = 0;
            }

            PositionParameters parameters = _tempChildren[index].PositionParameters;

            int width = Bounds.Width;
            int height = Bounds.Height;

            if (_vertical)
            {
                Point size = view.ComputeSize(width, height);

                if(view.PositionParameters.Height.IsRest)
                {
					int restParts = Math.Max(_restParts, 1);

					size.Y = _rest * view.PositionParameters.Height.Rest / restParts;
					size.Y = Math.Max(view.MinSize.Y, size.Y);
                }

                int posX = Padding + parameters.Margin.Left;

                if (!_wrap)
                {
                    switch (parameters.HorizontalAlignment)
                    {
                        case HorizontalAlignment.Center:
                            posX = (width - size.X) / 2;
                            break;

                        case HorizontalAlignment.Right:
                            posX = width - Padding - parameters.Margin.Right - size.X;
                            break;

                        case HorizontalAlignment.Stretch:
                            size.X = width - Padding * 2 - parameters.Margin.Width;
                            break;
                    }
                }

                int pos = Padding;

                if (index > 0)
                {
                    pos = _tempChildren[index - 1].Bounds.Bottom + _tempChildren[index - 1].PositionParameters.Margin.Bottom + Spacing;

                    if (_wrap)
                    {
                        posX = _currentWrapPos + view.Margin.Left;
                        
                        _currentSize = Math.Max(pos, _currentSize);

                        if (pos + size.Y > Math.Ceiling(Bounds.Height+UiUnit.Unit))
                        {

                            pos = Padding;

                            _currentWrapPos = _currentWrapMax + Spacing;

                            int right = _currentWrapPos;
                            posX = right + parameters.Margin.Left;
                        }
                    }
                }

                childBounds.X = posX;
                childBounds.Width = size.X;

                childBounds.Y = pos + parameters.Margin.Top;
                childBounds.Height = size.Y;

                _currentWrapMax = Math.Max(_currentWrapMax, childBounds.Right + view.Margin.Right);
                _currentSize = Math.Max(childBounds.Bottom + Padding, _currentSize);
            }
            else
            {
                Point size = view.ComputeSize(width, height);

                if (view.PositionParameters.Width.IsRest)
                {
					int restParts = Math.Max(_restParts, 1);

					size.X = _rest * view.PositionParameters.Width.Rest / restParts;
					size.X = Math.Max(view.MinSize.X, size.X);

                }

                int posY = Padding + parameters.Margin.Top;

                if (!_wrap)
                {
                    switch (parameters.VerticalAlignment)
                    {
                        case VerticalAlignment.Center:
                            posY = (height - size.Y) / 2;
                            break;

                        case VerticalAlignment.Bottom:
                            posY = height - Padding - parameters.Margin.Bottom - size.Y;
                            break;

                        case VerticalAlignment.Stretch:
                            size.Y = height - Padding * 2 - parameters.Margin.Height;
                            break;
                    }
                }

                int pos = Padding;
                
                if (index > 0)
                {
                    pos = _tempChildren[index - 1].Bounds.Right + _tempChildren[index - 1].PositionParameters.Margin.Right + Spacing;

                    if (_wrap)
                    {
                        posY = _currentWrapPos + view.Margin.Top;

                        if (pos + size.X > Math.Ceiling(Bounds.Width + UiUnit.Unit))
                        {

                            pos = Padding;

                            _currentWrapPos = _currentWrapMax + Spacing;

                            int bottom = _currentWrapPos;
                            posY = bottom + parameters.Margin.Top;
                        }
                    }
                }

                childBounds.X = pos + parameters.Margin.Left;
                childBounds.Width = size.X;

                childBounds.Y = posY;
                childBounds.Height = size.Y;

                _currentWrapMax = Math.Max(_currentWrapMax, childBounds.Bottom + view.Margin.Bottom);
                _currentSize = Math.Max(childBounds.Right + Padding, _currentSize);
            }

            _updateBounds |= view.Bounds != childBounds;
            
            return childBounds;
        }

        protected override void OnChildrenModified()
        {
            if (_children.Count == 0)
            {
                _minSizeFromChildren = Point.Zero;
                return;
            }

            int minSizeX = 0;
            int minSizeY = 0;

            if (_vertical)
            {
                for (int idx = 0; idx < _children.Count; ++idx)
                {
                    minSizeX = Math.Max(minSizeX, _children[idx].MinSize.X);
                    minSizeY += _children[idx].MinSize.Y;
                }
            }
            else
            {
                for (int idx = 0; idx < _children.Count; ++idx)
                {
                    minSizeY = Math.Max(minSizeX, _children[idx].MinSize.Y);
                    minSizeX += _children[idx].MinSize.X;
                }
            }

            _minSizeFromChildren = new Point(minSizeX, minSizeY);
        }

        public override void RecalcLayout()
        {
            if(_internalSize != null)
            {
                return;
            }

            int width = Bounds.Width;
            int height = Bounds.Height;

            bool computeRest = false;
            _rest = 0;
            _restParts = 0;

            for (int idx = 0; idx < _children.Count; ++idx)
            {
                var child = _children[idx];
                computeRest |= _vertical ? child.PositionParameters.Height.IsRest : child.PositionParameters.Width.IsRest;

                if(computeRest)
                {
                    break;
                }
            }

            _tempChildren.Clear();
            for(int idx = 0; idx < _children.Count; ++idx)
            {
                var child = _children[idx];

                if (child.DisplayVisibility > 0)
                {
                    _tempChildren.Add(child);

                    if (computeRest)
                    {
                        if (_vertical)
                        {
                            if (!child.PositionParameters.Height.IsRest)
                            {
                                _rest += child.ComputeSize(width, height).Y;
                            }
                            else
                            {
                                _restParts += child.PositionParameters.Height.Rest;
                            }

                            _rest += child.PositionParameters.Margin.Height;
                        }
                        else
                        {
                            if (!child.PositionParameters.Width.IsRest)
                            {
                                _rest += child.ComputeSize(width, height).X;
                            }
                            else
                            {
                                _restParts += child.PositionParameters.Width.Rest;
                            }

                            _rest += child.PositionParameters.Margin.Width;
                        }
                    }
                }
            }

            if (computeRest)
            {
                if (_vertical)
                {
                    _rest = Bounds.Height - _rest;
                }
                else
                {
                    _rest = Bounds.Width - _rest;
                }
            }

            for (int idx = 0; idx < _children.Count; ++idx)
            {
                var child = _children[idx];
                child.Bounds = CalculateChildBounds(child);
            }

            if (_wrap)
            {
                Point move = Point.Zero;

                if (_vertical)
                {
                    switch (_verticalContentAlignment)
                    {
                        case VerticalContentAlignment.Center:
                            move.Y = (Bounds.Height - _currentSize) / 2;
                            break;

                        case VerticalContentAlignment.Bottom:
                            move.Y = (Bounds.Height - _currentSize);
                            break;
                    }
                }
                else
                {
                    switch (_horizontalContentAlignment)
                    {
                        case HorizontalContentAlignment.Center:
                            move.X = (Bounds.Width - _currentSize) / 2;
                            break;

                        case HorizontalContentAlignment.Right:
                            move.X = (Bounds.Width - _currentSize);
                            break;
                    }
                }

                if (move != Point.Zero)
                {
                    for (int idx = 0; idx < _children.Count; ++idx)
                    {
                        var child = _children[idx];
                        child.Bounds = new Rectangle(child.Bounds.X + move.X, child.Bounds.Y + move.Y, child.Bounds.Width, child.Bounds.Height);
                    }
                }

                bool calculate = (_vertical && PositionParameters.Width.IsAuto) || (!_vertical && PositionParameters.Height.IsAuto);

                if (calculate)
                {
                    int size = 0;

                    for (int idx = 0; idx < _children.Count; ++idx)
                    {
                        var child = _children[idx];

                        if (_vertical)
                        {
                            size = Math.Max(child.Bounds.Right + child.Margin.Right, size);
                        }
                        else
                        {
                            size = Math.Max(child.Bounds.Bottom + child.Margin.Bottom, size);
                        }
                    }

                    if (_vertical)
                    {
                        _bounds.Width = size;
                    }
                    else
                    {
                        _bounds.Height = size;
                    }

                    _internalSize = new Point(_bounds.Width, _bounds.Height);
                    Parent.RecalcLayout(this);
                }
            }

            _internalSize = null;
        }

        protected override bool Init(object controller, object binding, DefinitionFile definition)
        {
            if (!base.Init(controller, binding, definition))
            {
                return false;
            }

            DefinitionFileWithStyle file = new DefinitionFileWithStyle(definition, typeof(UiStackPanel));

            StackMode = DefinitionResolver.Get<Mode>(Controller, Binding, file["Mode"], Mode.Vertical);
            _spacing = DefinitionResolver.Get<Length>(Controller, Binding, file["Spacing"], Length.Zero);
            _padding = DefinitionResolver.Get<Length>(Controller, Binding, file["Padding"], Length.Zero);
            _notifyParentOnResize = DefinitionResolver.Get<bool>(Controller, Binding, file["NotifyParentOnResize"], true);

            _wrap = DefinitionResolver.Get<bool>(Controller, Binding, file["Wrap"], false);

            _horizontalContentAlignment = DefinitionResolver.Get<HorizontalContentAlignment>(Controller, Binding, file["HorizontalContentAlignment"], HorizontalContentAlignment.Left);
            _verticalContentAlignment = DefinitionResolver.Get<VerticalContentAlignment>(Controller, Binding, file["VerticalContentAlignment"], VerticalContentAlignment.Top);

            _expanded = DefinitionResolver.GetShared<bool>(Controller, Binding, file["Expanded"], true);

            _expandSpeed = DefinitionResolver.Get<int>(Controller, Binding, file["ExpandTime"], 0);
            _expandedValue = _expanded.Value ? 1 : 0;

            if (_expandSpeed > 0)
            {
                _expandSpeed = 1000 / _expandSpeed;
            }
            else
            {
                _expandSpeed = 10000;
            }

            InitChildren(Controller, Binding, definition);

            return true;
        }

        public override Point ComputeSize(int width, int height)
        {
            if(_internalSize != null)
            {
                return _internalSize.Value;
            }

            Point size = Point.Zero;
            Point exSize = ComputeSizeInternal(width, height);

            if(_vertical)
            {
                size.X = exSize.X;
            }
            else
            {
                size.Y = exSize.Y;
            }

            return new Point((int)(size.X * (1 - _expandedValue) + exSize.X * _expandedValue), (int)(size.Y * (1 - _expandedValue) + exSize.Y * _expandedValue));
        }
    }
}
