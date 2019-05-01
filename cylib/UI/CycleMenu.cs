using System;
using System.Numerics;
using Color = SharpDX.Color;

using log;

namespace cylib
{
    public delegate void CycleEvent(int selected);

    /// <summary>
    /// It's a button! With text in the center.
    /// Super cool.
    /// </summary>
    public class CycleMenu : IDisposable
    {
        static readonly Color bgColor = Color.Black;
        static readonly Color baseColor = Color.White;
        static readonly Color highlightColor = Color.DarkBlue;
        static readonly Color mouseoverColor = Color.Blue;
        static readonly Color fontColor = Color.White;

        Renderer renderer;
        EventManager em;
        RoundedRectangle_2D rect;

        private Vector2 pos;
        public Vector2 Position
        {
            get
            {
                return pos;
            }
            set
            {
                pos = value;
                recalcPos();
            }
        }

        private Vector2 scale;
        public Vector2 Scale
        {
            get
            {
                return scale;
            }
            set
            {
                scale = value;
                recalcPos();
            }
        }

        private void recalcPos()
        {
            font.pos.X = pos.X + scale.X / 2f;
            font.pos.Y = pos.Y + scale.Y / 2f;
            font.scale = scale.Y - rect.borderThickness * 2 - 10;

            rect.position = pos;
            rect.scale = scale;

            leftButton.Scale = new Vector2(scale.Y, scale.Y);
            leftButton.Position = pos;

            rightButton.Scale = new Vector2(scale.Y, scale.Y);
            rightButton.Position = new Vector2(pos.X + scale.X - rightButton.Scale.X, pos.Y);
        }

        int _sel;
        public int Selection
        {
            get
            {
                return _sel;
            }
            set
            {
                if (_sel == value)
                    return;

                if (Selection < 0 || Selection >= labels.Length)
                {
                    Logger.WriteLine(LogType.ERROR, "Setting CycleMenu selection to an invalid number: " + Selection + " " + labels.Length);
                    return;
                }

                _sel = value;
                font.text = labels[_sel];

                if (onSelectionChange != null)
                    onSelectionChange(_sel);
            }
        }

        public event CycleEvent onSelectionChange;

        FontRenderer font;
        string[] labels;

        BoxTextButton leftButton;
        BoxTextButton rightButton;

        public CycleMenu(Renderer renderer, EventManager em, int priority, string[] labels, int initial)
        {
            this.renderer = renderer;
            this.em = em;
            this.labels = labels;

            rect = new RoundedRectangle_2D(renderer, em, priority);
            rect.borderColor = baseColor;
            rect.mainColor = bgColor;

            font = new FontRenderer(renderer, em, priority + 2, renderer.Assets.getAsset(FontAssets.SEGOEUI_SDF_128));
            font.color = fontColor;
            font.anchor = FontAnchor.CENTER_CENTER;

            leftButton = new BoxTextButton(renderer, em, priority + 1, "<");
            leftButton.drawBackground = false;

            rightButton = new BoxTextButton(renderer, em, priority + 1, ">");
            rightButton.drawBackground = false;

            leftButton.onClick += onLeftClick;
            rightButton.onClick += onRightClick;

            Selection = initial;
        }

        void onLeftClick(Button obj)
        {
            Selection = (Selection - 1 + labels.Length) % labels.Length;
        }

        void onRightClick(Button obj)
        {
            Selection = (Selection + 1) % labels.Length;
        }

        public void Dispose()
        {
            rect.Dispose();
            font.Dispose();
            leftButton.Dispose();
            rightButton.Dispose();
        }
    }
}
