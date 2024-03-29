﻿using System;
using System.Numerics;
using System.Drawing;

namespace cylib
{
    public delegate void SliderEvent(float value);

    /// <summary>
    /// Slider, for selecting a value between a min and max.
    /// 
    /// The text/sliderpoint will go over the bounds set by position/scale.
    /// The point will go over by a set amount, about scale.X / 2. The text will go over by a variable amount -- deal with it later.
    /// 
    /// Currently only supports floats. If you want int/exp/somethingweird, create a new one and redefine the sliderPos set method.
    /// Currently only supports horizontal sliders.
    /// </summary>
    public class Slider : IDisposable
    {
        static readonly Color bgColor = Color.Black;
        static readonly Color baseColor = Color.White;
        static readonly Color highlightColor = Color.LightBlue;
        static readonly Color mouseoverColor = Color.Blue;
        static readonly Color fontColor = Color.White;

        Renderer renderer;
        EventManager em;
        RoundedRectangle_2D bar;
        RoundedRectangle_2D point;
        FontRenderer font;

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
                recalcSizes();
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
                recalcSizes();
            }
        }

        private void recalcSizes()
        {
            point.scale = new Vector2(scale.Y, scale.Y);
            point.radius = scale.Y / 1.2f;

            point.position.X = pos.X + scale.X * sliderPos;
            point.position.Y = pos.Y;

            bar.scale.X = scale.X;
            bar.scale.Y = scale.Y / 3f;
            bar.radius = bar.scale.Y / 2f;

            bar.position.X = pos.X;
            bar.position.Y = pos.Y + (scale.Y - bar.scale.Y) / 2f;

            font.Pos.X = pos.X + scale.X + point.scale.X / 2;
            font.Pos.Y = pos.Y + scale.Y / 2;
            font.Scale = scale.Y * 0.8f;
        }

        bool _isDragging = false;
        bool isDragging
        {
            get
            {
                return _isDragging;
            }
            set
            {
                if (_isDragging == value)
                    return;

                _isDragging = value;

                if (value)
                {
                    em.changePriority((int)InterfacePriority.HIGHEST, onPointerEvent);
                }
                else
                {
                    em.changePriority((int)InterfacePriority.MEDIUM, onPointerEvent);
                }
            }
        }

        float _sliderPos = 0.5f;
        float sliderPos
        {
            get
            {
                return _sliderPos;
            }
            set
            {
                _sliderPos = Math.Max(Math.Min(value, 1), 0);
                point.position.X = pos.X + scale.X * _sliderPos - point.scale.X / 2;

                float val = _sliderPos * (maxValue - minValue) + minValue;
                font.Text = val.ToString("G3");

                if (onValueChanged != null)
                    onValueChanged(val);
            }
        }

        float minValue;
        float maxValue;

        public SliderEvent onValueChanged;

        float UIScaleX;
        float UIScaleY;

        public Slider(Renderer renderer, EventManager em, int priority, float minValue, float startValue, float maxValue, float UIScaleX = -1, float UIScaleY = -1)
        {
            this.renderer = renderer;
            this.em = em;
            this.minValue = minValue;
            this.maxValue = maxValue;

            bar = new RoundedRectangle_2D(renderer, em, priority);
            bar.borderColor = baseColor;
            bar.mainColor = bgColor;

            point = new RoundedRectangle_2D(renderer, em, priority + 1);
            point.borderColor = baseColor;
            point.mainColor = bgColor;

            font = new FontRenderer(renderer, em, priority, renderer.Assets.GetFont(Renderer.DefaultAssets.FONT_DEFAULT));
            font.color = fontColor;
            font.Anchor = FontAnchor.CENTER_LEFT;

            em.addEventHandler((int)InterfacePriority.MEDIUM, onPointerEvent);

            sliderPos = (startValue - minValue) / (maxValue - minValue);

            this.UIScaleX = UIScaleX < 0 ? renderer.ResolutionWidth : UIScaleX;
            this.UIScaleY = UIScaleY < 0 ? renderer.ResolutionHeight : UIScaleY;
        }

        bool onPointerEvent(PointerEventArgs args)
        {
            float mouseX = args.aimDeltaX * UIScaleX;
            float mouseY = args.aimDeltaY * UIScaleY;

            if (args.type == PointerEventType.MOVE)
            {
                if (isDragging)
                {
                    sliderPos = (mouseX - pos.X) / scale.X;
                    return true;
                }
                else
                {
                    if (mouseX > pos.X - point.scale.X / 2 && mouseX <= pos.X + scale.X + point.scale.X / 2
                        && mouseY > pos.Y && mouseY <= pos.Y + scale.Y)
                    {//mouseOver
                        bar.borderColor = mouseoverColor;
                    }
                    else
                        bar.borderColor = baseColor;
                }

                return false;
            }

            if (args.type == PointerEventType.BUTTON)
            {
                if (args.button == PointerButton.LEFT)
                {
                    if (args.isDown
                        && mouseX > pos.X - point.scale.X / 2 && mouseX <= pos.X + scale.X + point.scale.X / 2
                        && mouseY > pos.Y && mouseY <= pos.Y + scale.Y)
                    {
                        isDragging = true;
                        sliderPos = (mouseX - pos.X) / scale.X;

                        point.borderColor = mouseoverColor;
                        point.mainColor = highlightColor;

                        return true;
                    }
                    else if (!args.isDown && isDragging)
                    {
                        isDragging = false;
                        sliderPos = (mouseX - pos.X) / scale.X;

                        point.borderColor = baseColor;
                        point.mainColor = bgColor;

                        return true;
                    }
                    else
                        isDragging = false;
                }

                return false;
            }

            return false;
        }

        public void Dispose()
        {
            em.removeEventHandler(onPointerEvent);
            bar.Dispose();
            point.Dispose();
            font.Dispose();
        }
    }
}
