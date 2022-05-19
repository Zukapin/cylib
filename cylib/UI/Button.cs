using System;
using System.Numerics;

namespace cylib
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

        private float UIScaleX;
        private float UIScaleY;

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
        public virtual bool Enabled
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
        public event ButtonClickEvent OnClick;

        public Button(Renderer renderer, EventManager em, float UIScaleX = -1, float UIScaleY = -1)
        {
            this.renderer = renderer;
            this.em = em;

            em.addEventHandler((int)InterfacePriority.MEDIUM, onPointerEvent);

            this.UIScaleX = UIScaleX < 0 ? renderer.ResolutionWidth : UIScaleX;
            this.UIScaleY = UIScaleY < 0 ? renderer.ResolutionHeight : UIScaleY;
        }

        bool onPointerEvent(PointerEventArgs args)
        {
            if (!_isEnabled)
                return false;

            float mouseX = args.aimDeltaX * UIScaleX;
            float mouseY = args.aimDeltaY * UIScaleY;

            if (args.type == PointerEventType.MOVE)
            {
                if (isPressed)
                    return true;

                isMouseOver = (mouseX > pos.X && mouseX <= pos.X + scale.X
                    && mouseY > pos.Y && mouseY <= pos.Y + scale.Y);

                return false;
            }

            if (args.type == PointerEventType.BUTTON)
            {
                if (args.button == PointerButton.LEFT)
                {
                    if (args.isDown
                        && mouseX > pos.X && mouseX <= pos.X + scale.X
                        && mouseY > pos.Y && mouseY <= pos.Y + scale.Y)
                    {
                        isPressed = true;
                        return true;
                    }
                    else if (!args.isDown && isPressed)
                    {
                        isPressed = false;

                        if (mouseX > pos.X && mouseX <= pos.X + scale.X
                            && mouseY > pos.Y && mouseY <= pos.Y + scale.Y)
                        {
                            if (OnClick != null)
                                OnClick(this);
                        }

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
