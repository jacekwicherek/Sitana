﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitana.Framework.Content;

namespace Sitana.Framework.Graphics
{
    public partial class AdvancedDrawBatch
    {
        PrimitiveBatch _primitiveBatch;
        SpriteBatch _spriteBatch;
        BasicEffect _basicEffect;

        static RasterizerState _rasterizerScissors = new RasterizerState() { CullMode = CullMode.None, ScissorTestEnable = true };
        static RasterizerState _rasterizerNoScissors = new RasterizerState() { CullMode = CullMode.None, ScissorTestEnable = false };

        RasterizerState _rasterizerState = _rasterizerNoScissors;

        SamplerState _samplerState = SamplerState.LinearClamp;

        public GraphicsDevice GraphicsDevice {get; private set;}

        BlendState _blendState = BlendState.AlphaBlend;

        bool _primitiveBatchStarted = false;
        bool _spriteBatchStarted = false;

        Texture2D _texture = null;
        PrimitiveType _primitiveType = PrimitiveType.TriangleList;

        SpriteFont _font = null;

        NinePatchImage _ninePatchImage;

        Stack<Rectangle?> _scissors = new Stack<Rectangle?>();

        Stack<Matrix> _transforms = new Stack<Matrix>();

        Matrix _transform = Matrix.Identity;

        public NinePatchImage NinePatchImage
        {
            get
            {
                return _ninePatchImage;
            }

            set
            {
                _ninePatchImage = value;

                if (value != null)
                {
                    Texture = _ninePatchImage.Texture;
                }
                else
                {
                    Texture = null;
                }
            }
        }

        public SpriteFont Font
        {
            get
            {
                return _font;
            }

            set
            {
                if (_font != value)
                {
                    Flush();
                }

                _font = value;
            }
        }

        public BlendState BlendState
        {
            get
            {
                return _blendState;
            }

            set
            {
                if (_blendState != value)
                {
                    Flush();
                    _blendState = value;
                }
            }
        }

        public SamplerState SamplerState
        {
            get
            {
                return _samplerState;
            }

            set
            {
                if (_samplerState != value)
                {
                    Flush();
                    _samplerState = value;
                }
            }
        }

        private Rectangle? ScissorRectangle
        {
            get
            {
                if (_rasterizerState.ScissorTestEnable)
                {
                    return GraphicsDevice.ScissorRectangle;
                }

                return null;
            }

            set
            {
                if (value.HasValue)
                {
                    if (!_rasterizerState.ScissorTestEnable)
                    {
                        Flush();
                    }

                    if (GraphicsDevice.ScissorRectangle != value.Value)
                    {
                        Flush();
                        GraphicsDevice.ScissorRectangle = GraphicsHelper.IntersectRectangle(value.Value, GraphicsDevice.Viewport.Bounds);
                    }

                    _rasterizerState = _rasterizerScissors;
                }
                else
                {
                    if (_rasterizerState.ScissorTestEnable)
                    {
                        Flush();
                        _rasterizerState = _rasterizerNoScissors;
                    }
                }
            }
        }

        public PrimitiveType PrimitiveType
        {
            get
            {
                return _primitiveType;
            }

            set
            {
                if (value != _primitiveType)
                {
                    Flush();
                }

                _primitiveType = value;
            }
        }

        public Texture2D Texture
        {
            get
            {
                return _texture;
            }

            set
            {
                if (value != _texture)
                {
                    Flush();
                }

                _texture = value;
            }
        }

        public void Reset()
        {
            _scissors.Clear();
            ScissorRectangle = null;
            _transforms.Clear();
            _transform = Matrix.Identity;
        }

        public void PushClip(Rectangle? rect)
        {
            _scissors.Push(ScissorRectangle);
            ScissorRectangle = rect;
        }

        public void PopClip()
        {
            if (_scissors.Count > 0)
            {
                ScissorRectangle = _scissors.Pop();
            }
            else
            {
                ScissorRectangle = null;
            }
        }

        public void PushTransform(Matrix transform)
        {
            _transforms.Push(_transform);
            _transform = transform;

            Flush();
        }

        public void PopTransform()
        {
            if (_transforms.Count > 0)
            {
                _transform = _transforms.Pop();
            }
            else
            {
                _transform = Matrix.Identity;
            }

            Flush();
        }

        public AdvancedDrawBatch(GraphicsDevice device)
        {
            GraphicsDevice = device;
            _primitiveBatch = new PrimitiveBatch(device);
            _spriteBatch = new SpriteBatch(device);
            _basicEffect = new BasicEffect(device);

            
        }

        public void Flush()
        {
            if (_primitiveBatchStarted)
            {
                _primitiveBatch.End();
                _primitiveBatchStarted = false;
            }

            if (_spriteBatchStarted)
            {
                _spriteBatch.End();
                _spriteBatchStarted = false;
            }
        }

        void SpriteBatchIsNeeded()
        {
            if (_primitiveBatchStarted)
            {
                Flush();
            }

            if (!_spriteBatchStarted)
            {
                Matrix projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, 1);
                Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);

                _basicEffect.TextureEnabled = true;
                _basicEffect.VertexColorEnabled = true;

                _basicEffect.Alpha = 1;
                _basicEffect.Projection = halfPixelOffset * projection;
                _basicEffect.View = Matrix.Identity;
                _basicEffect.VertexColorEnabled = true;
                _basicEffect.World = _transform;

                _spriteBatch.Begin(SpriteSortMode.Deferred, _blendState, _samplerState, DepthStencilState.None, _rasterizerState, _basicEffect);
                _spriteBatchStarted = true;
            }
        }

        void PrimitiveBatchNeeded()
        {
            if (_spriteBatchStarted)
            {
                Flush();
            }

            if (!_primitiveBatchStarted)
            {
                _primitiveBatch.Transform = _transform;
                _primitiveBatch.Begin(_primitiveType, _rasterizerState, _samplerState, _texture);
                GraphicsDevice.BlendState = _blendState;

                _primitiveBatchStarted = true;
            }
        }

        public void PushVertex(Vector2 vertex, Color color)
        {
            PrimitiveBatchNeeded();

            if (_texture != null)
            {
                _primitiveBatch.AddVertex(vertex, color, Vector2.Zero);
            }
            else
            {
                _primitiveBatch.AddVertex(vertex, color);
            }
        }

        public void PushVertex(Vector2 vertex, Color color, Vector2 texCoord)
        {
            PrimitiveBatchNeeded();

            if (_texture != null)
            {
                _primitiveBatch.AddVertex(vertex, color, texCoord);
            }
            else
            {
                _primitiveBatch.AddVertex(vertex, color);
            }
        }

        public void PushVertex(Vector2 vertex, Color color, Point texCoord)
        {
            PrimitiveBatchNeeded();

            if (_texture != null)
            {
                float width = _texture.Width > 1 ? _texture.Width - 1 : 1;
                float height = _texture.Height > 1 ? _texture.Height - 1 : 1;

                _primitiveBatch.AddVertex(vertex, color, texCoord.ToVector2() / new Vector2(width, height));
            }
            else
            {
                _primitiveBatch.AddVertex(vertex, color);
            }
        }
    }
}
