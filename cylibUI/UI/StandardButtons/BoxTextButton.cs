﻿using System;
using System.Numerics;
using cylib;

using Color = SharpDX.Color;

namespace cylibUI
{
    /// <summary>
    /// It's a button! With text in the center.
    /// Super cool.
    /// </summary>
    public class BoxTextButton : Button, IDisposable
    {
        static readonly Color bgColor = Color.Black;
        static readonly Color baseColor = Color.White;
        static readonly Color highlightColor = Color.DarkBlue;
        static readonly Color mouseOverColor = Color.Blue;
        static readonly Color fontColor = Color.White;

        RoundedRectangle_2D rect;
        FontRenderer font;

        protected override void recalcPositions()
        {
            font.pos = Position + Scale / 2;
            font.scale = Scale.Y - rect.borderThickness * 2 - 10;

            font.scale = Math.Min(font.scale, font.scale * (Scale.X - rect.borderThickness * 2 - 10) / font.getRenderWidth(font.text));

            rect.position = Position;
            rect.scale = Scale;
        }

        private bool _drawBackground = true;
        public bool drawBackground
        {
            get
            {
                return _drawBackground;
            }
            set
            {
                _drawBackground = value;
                rect.enabled = value;
            }
        }

        public Color MainColor
        {
            get
            {
                return rect.mainColor;
            }
            set
            {
                rect.mainColor = value;
            }
        }

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                rect.enabled = _drawBackground && value;
                font.enabled = value;
            }
        }

        public BoxTextButton(Renderer renderer, EventManager em, int priority, string text)
            : base(renderer, em)
        {
            rect = new RoundedRectangle_2D(renderer, em, priority);
            rect.borderColor = baseColor;
            rect.mainColor = bgColor;

            font = new FontRenderer(renderer, em, priority + 1, renderer.Assets.getAsset(FontAssets.SEGOEUI_SDF_128));
            font.color = fontColor;
            font.anchor = FontAnchor.CENTER_CENTER;
            font.text = text;

            onMouseOver += mouseOver;
            onPressed += pressed;
        }

        private void pressed(Button obj, bool active)
        {
            if (active)
                rect.borderColor = highlightColor;
            else if (isMouseOver)
                rect.borderColor = mouseOverColor;
            else
                rect.borderColor = baseColor;
        }

        private void mouseOver(Button obj, bool active)
        {
            if (active)
                rect.borderColor = mouseOverColor;
            else
                rect.borderColor = baseColor;
        }

        public override void Dispose()
        {
            base.Dispose();
            rect.Dispose();
            font.Dispose();
        }
    }
}