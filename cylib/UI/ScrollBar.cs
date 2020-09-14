using System;
using System.Numerics;
using System.Drawing;

using log;

namespace cylib
{
    public delegate void ScrollEvent(float value);

    /// <summary>
    /// Generic scrollbar to scroll things with. Super cool.
    /// 
    /// To use, set position/scale of the place where the scroll bar itself should be rendered.
    /// Also set the 'percentOfScreen' variable, with the % of the screen viewable at one time, compared to the total height of what you want to render.
    /// So if you're rendering a list that's 3,000 pixels tall, and you only have 550 pixels of screen space, set percentOfScreen to 550 / 3,000.
    /// 
    /// Currently only supports vertical scrollbars. Should be trivial to add in horizonal and swap the X/Y crap.
    /// </summary>
    public class ScrollBar : IDisposable
    {
        static readonly Color bgColor = Color.Black;
        static readonly Color pointColor = Color.LightBlue;
        static readonly Color baseColor = Color.White;
        static readonly Color highlightColor = Color.Blue;
        static readonly Color mouseoverColor = Color.Blue;
        static readonly Color fontColor = Color.White;

        Renderer renderer;
        EventManager em;
        RoundedRectangle_2D bar;
        RoundedRectangle_2D point;

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

        /// <summary>
        /// Scroll position and scale is the bounding box that the scrollbar will use to determine if it should consume a mousewheel event.
        /// </summary>
        public Vector2 scrollPos = new Vector2(0, 0);

        /// <summary>
        /// Scroll position and scale is the bounding box that the scrollbar will use to determine if it should consume a mousewheel event.
        /// </summary>
        public Vector2 scrollScale = new Vector2(0, 0);

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
            point.scale = new Vector2(scale.X, Math.Min(Math.Max(scale.X * 3, percentOfScreen * scale.Y), scale.Y * 0.33f));
            point.radius = point.scale.X / 2f;

            point.position.X = pos.X;
            point.position.Y = pos.Y + sliderPos * (scale.Y - point.scale.Y);

            bar.scale.X = scale.X;
            bar.scale.Y = scale.Y;
            bar.radius = bar.scale.X / 2f;

            bar.position.X = pos.X + (scale.X - bar.scale.X) / 2f;
            bar.position.Y = pos.Y;
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
                    em.changePriority(selectedPriority, onPointerEvent);
                }
                else
                {
                    em.changePriority(defaultPriority, onPointerEvent);
                }
            }
        }

        float _sliderPos = 0.5f;
        public float sliderPos
        {
            get
            {
                return _sliderPos;
            }
            set
            {
                _sliderPos = Math.Max(Math.Min(value, 1), 0);

                recalcSizes();

                if (onValueChanged != null)
                    onValueChanged(_sliderPos);
            }
        }

        float _perc;

        /// <summary>
        /// Set this to the percentage of the render target you can see, compared to total view area.
        /// See class notes for more information.
        /// </summary>
        public float percentOfScreen
        {
            get
            {
                return _perc;
            }
            set
            {
                float v = Math.Min(Math.Max(value, 0), 1);

                if (float.IsNaN(v))
                    return;

                if (v == _perc)
                    return;

                _perc = v;

                percBigEnough = value < 1;

                recalcSizes();

                if (onValueChanged != null)
                {
                    if (_isEnabled)
                        onValueChanged(_sliderPos);
                    else
                        onValueChanged(0);
                }
            }
        }

        //composite bool for saying if we're viewable or not
        bool _isEnabled = true;
        //do we even need the scroll bar
        bool _percBigEnough = true;
        bool percBigEnough
        {
            get
            {
                return _percBigEnough;
            }
            set
            {
                _percBigEnough = value;
                calcEnabled();
            }
        }
        //what the UI is set to
        bool _isUserEnabled = true;
        public bool enabled
        {
            get
            {
                return _isUserEnabled;
            }
            set
            {
                _isUserEnabled = value;
                calcEnabled();
            }
        }

        public bool isVisible
        {
            get
            {
                return _isEnabled;
            }
        }

        private void calcEnabled()
        {
            bool toSet = _percBigEnough && _isUserEnabled;
            if (toSet == _isEnabled)
                return;

            _isEnabled = _percBigEnough && _isUserEnabled;

            bar.enabled = _isEnabled;
            point.enabled = _isEnabled;
        }

        int _defaultPriority = (int)InterfacePriority.MEDIUM;
        public int defaultPriority
        {
            get
            {
                return _defaultPriority;
            }
            set
            {
                _defaultPriority = value;

                if (!isDragging)
                {
                    em.changePriority(defaultPriority, onPointerEvent);
                }
            }
        }
        int _selectedPriority = (int)InterfacePriority.HIGHEST;
        public int selectedPriority
        {
            get
            {
                return _selectedPriority;
            }
            set
            {
                _selectedPriority = value;

                if (isDragging)
                {
                    em.changePriority(selectedPriority, onPointerEvent);
                }
            }
        }

        public SliderEvent onValueChanged;

        public ScrollBar(Renderer renderer, EventManager em, int priority)
        {
            this.renderer = renderer;
            this.em = em;

            bar = new RoundedRectangle_2D(renderer, em, priority);
            bar.borderColor = baseColor;
            bar.mainColor = bgColor;

            point = new RoundedRectangle_2D(renderer, em, priority + 1);
            point.borderColor = baseColor;
            point.mainColor = pointColor;

            em.addEventHandler(defaultPriority, onPointerEvent);

            sliderPos = 0;
        }

        bool onPointerEvent(PointerEventArgs args)
        {
            if (!_isEnabled)
                return false;

            if (args.type == PointerEventType.MOVE)
            {
                if (isDragging)
                {
                    sliderPos = (args.y - pos.Y - point.scale.Y / 2) / (scale.Y - point.scale.Y);
                    return true;
                }
                else
                {
                    if (args.x > pos.X && args.x <= pos.X + scale.X
                        && args.y > pos.Y && args.y <= pos.Y + scale.Y)
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
                        && args.x > pos.X - point.scale.X / 2 && args.x <= pos.X + scale.X + point.scale.X / 2
                        && args.y > pos.Y && args.y <= pos.Y + scale.Y)
                    {
                        isDragging = true;
                        sliderPos = (args.y - pos.Y - point.scale.Y / 2) / (scale.Y - point.scale.Y);

                        point.borderColor = mouseoverColor;
                        point.mainColor = highlightColor;

                        return true;
                    }
                    else if (!args.isDown && isDragging)
                    {
                        isDragging = false;
                        sliderPos = (args.y - pos.Y - point.scale.Y / 2) / (scale.Y - point.scale.Y);

                        point.borderColor = baseColor;
                        point.mainColor = pointColor;

                        return true;
                    }
                    else
                        isDragging = false;
                }

                return false;
            }

            if (args.type == PointerEventType.MOUSEWHEEL)
            {
                if (args.x > scrollPos.X && args.x <= scrollPos.X + scrollScale.X
                    && args.y > scrollPos.Y && args.y <= scrollPos.Y + scrollScale.Y)
                {
                    sliderPos += args.wheelY * percentOfScreen * -0.3f;
                    return true;
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
        }
    }
}
