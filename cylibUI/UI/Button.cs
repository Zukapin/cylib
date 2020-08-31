using System;
using System.Numerics;
using cylib;

namespace cylibUI
{
    public delegate void ButterInteractionEvent(Button obj, bool active);
    public delegate void ButtonClickEvent(Button obj);

    /// <summary>
    /// Button class that implements all the button-y basics.
    /// Doesn't implement any graphics, use/imlement a subclass for that.
    /// </summary>
    public abstract class Button : IDisposable
    {
        Renderer renderer;
        EventManager em;

        /// <summary>
        /// User-set data. Useful for differentiating between buttons during a click event.
        /// </summary>
        public object dat = null;

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
                recalcPositions();
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
                recalcPositions();
            }
        }

        protected abstract void recalcPositions();

        bool _isPressed = false;
        bool isPressed
        {
            get
            {
                return _isPressed;
            }
            set
            {
                if (_isPressed == value)
                    return;

                _isPressed = value;

                if (value)
                {
                    em.changePriority((int)InterfacePriority.HIGHEST, onPointerEvent);
                }
                else
                {
                    em.changePriority((int)InterfacePriority.MEDIUM, onPointerEvent);
                }

                if (onPressed != null)
                    onPressed(this, _isPressed);
            }
        }

        bool _isMouseOver = false;
        protected bool isMouseOver
        {
            get
            {
                return _isMouseOver;
            }
            set
            {
                if (_isMouseOver == value)
                    return;

                _isMouseOver = value;

                if (onMouseOver != null)
                    onMouseOver(this, _isMouseOver);
            }
        }

        private bool _isEnabled = true;
        public virtual bool enabled
        {
            set
            {
                _isEnabled = value;
            }
        }

        public event ButterInteractionEvent onMouseOver;

        /// <summary>
        /// Fires when the button is pressed down on. Use onClick for a confirmed click.
        /// </summary>
        public event ButterInteractionEvent onPressed;
        public event ButtonClickEvent onClick;

        public Button(Renderer renderer, EventManager em)
        {
            this.renderer = renderer;
            this.em = em;

            em.addEventHandler((int)InterfacePriority.MEDIUM, onPointerEvent);
        }

        bool onPointerEvent(PointerEventArgs args)
        {
            if (!_isEnabled)
                return false;

            if (args.type == PointerEventType.MOVE)
            {
                if (isPressed)
                    return true;

                isMouseOver = (args.x > pos.X && args.x <= pos.X + scale.X
                    && args.y > pos.Y && args.y <= pos.Y + scale.Y);

                return false;
            }

            if (args.type == PointerEventType.BUTTON)
            {
                if (args.button == PointerButton.LEFT)
                {
                    if (args.isDown
                        && args.x > pos.X && args.x <= pos.X + scale.X
                        && args.y > pos.Y && args.y <= pos.Y + scale.Y)
                    {
                        isPressed = true;
                        return true;
                    }
                    else if (!args.isDown && isPressed)
                    {
                        if (args.x > pos.X && args.x <= pos.X + scale.X
                            && args.y > pos.Y && args.y <= pos.Y + scale.Y)
                        {
                            if (onClick != null)
                                onClick(this);
                        }

                        isPressed = false;
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        public virtual void Dispose()
        {
            em.removeEventHandler(onPointerEvent);
        }
    }
}
