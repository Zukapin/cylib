using System;
using System.Numerics;
using System.Drawing;

namespace cylib
{
    /// <summary>
    /// It's a button! With text in the center.
    /// Super cool.
    /// </summary>
    public class BoxTextButton : Button, IDisposable
    {
        static readonly Color bgColor = Color.Black;
        Color baseColor = Color.White;
        Color highlightColor = Color.DarkBlue;
        Color mouseOverColor = Color.Blue;
        Color fontColor = Color.White;

        RoundedRectangle_2D rect;
        FontRenderer font;

        protected override void recalcPositions()
        {
            font.Pos = Position + Scale / 2;
            font.Scale = Scale.Y - rect.borderThickness * 2 - 10;

            font.Scale = Math.Min(font.Scale, font.Scale * (Scale.X - rect.borderThickness * 2 - 10) / font.getRenderWidth(font.Text));

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

        public Color BorderColor
        {
            get
            {
                return baseColor;
            }
            set
            {
                baseColor = value;
                rect.borderColor = value;
            }
        }

        public Color FontColor
        {
            get
            {
                return font.color;
            }
            set
            {
                font.color = value;
            }
        }

        public Color HighlightColor
        {
            get
            {
                return highlightColor;
            }
            set
            {
                highlightColor = value;
            }
        }

        public Color MouseoverColor
        {
            get
            {
                return mouseOverColor;
            }
            set
            {
                mouseOverColor = value;
            }
        }

        public override bool Enabled
        {
            set
            {
                base.Enabled = value;
                rect.enabled = _drawBackground && value;
                font.Enabled = value;
            }
        }

        public BoxTextButton(Renderer renderer, EventManager em, int priority, string text, float UIScaleX = -1, float UIScaleY = -1)
            : base(renderer, em, UIScaleX, UIScaleY)
        {
            rect = new RoundedRectangle_2D(renderer, em, priority);
            rect.borderColor = baseColor;
            rect.mainColor = bgColor;

            font = new FontRenderer(renderer, em, priority + 1, renderer.Assets.GetFont(Renderer.DefaultAssets.FONT_DEFAULT));
            font.color = fontColor;
            font.Anchor = FontAnchor.CENTER_CENTER;
            font.Text = text;

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
