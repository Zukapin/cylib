using System;
using System.Numerics;
using Keys = SDL2.SDL.SDL_Keycode;
using System.Drawing;

namespace cylib
{
    public delegate void TextboxEnter();

    /// <summary>
    /// Textbox class, accepts typing on a single line in a box defined by position/scale.
    /// 
    /// Must call Update(dt) to get the flashing text cursor.
    /// 
    /// Fires 'onEnterPressed' and 'onTextChanged' events -- try not to change the text in an onTextChanged event because what are you doing.
    /// 
    /// There's a lot of fiddly crap with cursor positioning/input handling.
    /// </summary>
    public class Textbox : IDisposable
    {
        const float buffer = 10;
        const float textHeightScoot = -4;

        static readonly Color bgColor = Color.FromArgb(40, 40, 40);
        static readonly Color activeColor = Color.FromArgb(20, 20, 20);
        static readonly Color outlineColor = Color.White;

        static readonly Color fontColor = Color.White;
        static readonly Color cursorColor = Color.White;
        static readonly Color selColor = Color.FromArgb(80, 80, 255);

        Renderer renderer;
        EventManager em;
        InputHandler input;
        RoundedRectangle_2D bg;
        RoundedRectangle_2D selCursor;

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
                bg.position = pos;

                font.pos.X = pos.X + buffer;
                font.pos.Y = pos.Y + scale.Y / 2;

                font.boundsPos = new Vector2(pos.X + buffer, pos.Y);

                calcCursorPosition();
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
                bg.scale = value;
                scale = value;

                font.pos.Y = pos.Y + scale.Y / 2;
                font.scale = value.Y - buffer;
                font.boundsScale = value;
                font.boundsScale.X -= buffer * 2;

                calcCursorPosition();
            }
        }

        private bool _isTyping = false;
        private bool isTyping
        {
            get
            {
                return _isTyping;
            }
            set
            {
                if (value == _isTyping)
                    return;

                selCursor.enabled = value;
                _isTyping = value;

                if (value)
                {
                    bg.mainColor = activeColor;
                    em.addEventHandler((int)InterfacePriority.HIGHEST, onKeyChange);
                    input.StartTyping(OnText);
                }
                else
                {
                    bg.mainColor = bgColor;
                    em.removeEventHandler(onKeyChange);
                    input.StopTyping();
                }
            }
        }

        public string text
        {
            get
            {
                return font.text;
            }
            set
            {
                if (font.text == value)
                    return;

                font.text = value;
                calcCursorPosition();

                if (onTextChanged != null)
                    onTextChanged();
            }
        }

        FontRenderer font;

        int selPosition = 0;
        int selDist = 0;
        float selTimer = 0;
        bool mouseSelectingStuff = false;

        public event TextboxEnter onEnterPressed;
        public event TextboxEnter onTextChanged;

        float UIScaleX;
        float UIScaleY;

        public Textbox(Renderer renderer, EventManager em, InputHandler input, int priority, float UIScaleX = -1, float UIScaleY = -1)
        {
            this.renderer = renderer;
            this.em = em;
            this.input = input;

            bg = new RoundedRectangle_2D(renderer, em, priority);
            bg.radius = 4f;
            bg.mainColor = bgColor;
            bg.borderColor = outlineColor;
            bg.borderThickness = 1f;

            font = new FontRenderer(renderer, em, priority + 2, renderer.Assets.GetFont(Renderer.DefaultAssets.FONT_DEFAULT), UIScaleX, UIScaleY);
            font.anchor = FontAnchor.CENTER_LEFT;
            font.color = fontColor;

            selCursor = new RoundedRectangle_2D(renderer, em, priority + 1);
            selCursor.radius = 1;
            selCursor.mainColor = cursorColor;
            selCursor.borderThickness = 0;
            selCursor.enabled = false;

            em.addEventHandler((int)InterfacePriority.MEDIUM, onPointerEvent);
            em.addUpdateListener(priority, Update);

            this.UIScaleX = UIScaleX < 0 ? renderer.ResolutionWidth : UIScaleX;
            this.UIScaleY = UIScaleY < 0 ? renderer.ResolutionHeight : UIScaleY;
        }

        bool onPointerEvent(PointerEventArgs args)
        {
            float mouseX = args.aimDeltaX * UIScaleX;
            float mouseY = args.aimDeltaY * UIScaleY;

            if (args.type == PointerEventType.MOVE)
            {
                if (mouseSelectingStuff)
                {
                    int s = text.Length;

                    //find where the mouse is along the length of text
                    //if it's past the end, we set it to max
                    for (int i = 1; i < text.Length; i++)
                    {
                        float xpos = font.pos.X + font.offset.X + font.getRenderWidth(text.Substring(0, i));

                        if (mouseX < xpos)
                        {
                            s = i - 1;
                            break;
                        }
                    }

                    selDist = s - selPosition;

                    calcCursorPosition();
                    return true;
                }
                if (mouseX > pos.X && mouseX <= pos.X + scale.X
                    && mouseY > pos.Y && mouseY <= pos.Y + scale.Y)
                {//mouseOver
                    if (!isTyping)
                        bg.mainColor = activeColor;
                }
                else if (!isTyping)
                    bg.mainColor = bgColor;

                return false;
            }

            if (args.type == PointerEventType.BUTTON)
            {
                if (args.button == PointerButton.LEFT)
                {
                    if (args.isDown
                        && mouseX > pos.X && mouseX <= pos.X + scale.X
                        && mouseY > pos.Y && mouseY <= pos.Y + scale.Y)
                    {//mouse down
                        isTyping = true;
                        mouseSelectingStuff = true;
                        em.changePriority((int)InterfacePriority.HIGHEST, onPointerEvent);

                        selDist = 0;
                        selPosition = text.Length;

                        //find where the mouse is pointing
                        //if the mouse is pointing beyond all chars, just select the last pos
                        for (int i = 1; i < text.Length; i++)
                        {
                            float xpos = font.pos.X + font.offset.X + font.getRenderWidth(text.Substring(0, i));

                            if (mouseX < xpos)
                            {
                                selPosition = i - 1;
                                break;
                            }
                        }

                        calcCursorPosition();
                        return true;
                    }
                    else if (args.isDown && isTyping)
                    {//clicking down outside of the box
                        selDist = 0;
                        isTyping = false;

                        calcCursorPosition();
                    }
                    else if (!args.isDown && isTyping)
                    {
                        int s = text.Length;
                        mouseSelectingStuff = false;
                        em.changePriority((int)InterfacePriority.MEDIUM, onPointerEvent);

                        //figure out where the mouse is dragging the selection to
                        for (int i = 1; i < text.Length; i++)
                        {
                            float xpos = font.pos.X + font.offset.X + font.getRenderWidth(text.Substring(0, i));

                            if (mouseX < xpos)
                            {
                                s = i - 1;
                                break;
                            }
                        }

                        selDist = s - selPosition;

                        calcCursorPosition();
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        bool onKeyChange(KeyData key, bool isDown)
        {
            if (isDown && isTyping)
            {
                //we translate the given scankode to the keycode
                //scancodes are physical keys, keycodes are virtual -- if the user has a keyboard mode for a different language with a different layout
                //the 'virtual' keys are used for typing
                var k = key.k;
                if (k == Keys.SDLK_BACKSPACE)
                {
                    DoBackspace();
                    return true;
                }

                if (k == Keys.SDLK_DELETE || (k == Keys.SDLK_KP_DECIMAL && key.shift))
                {
                    if (selPosition == text.Length && selDist == 0)
                        return true;

                    int len = text.Length;
                    if (len == 0)
                        return true;

                    if (selDist == 0)
                        text = text.Remove(selPosition, 1);
                    else
                    {
                        text = text.Remove(Math.Min(selPosition, selPosition + selDist), Math.Abs(selDist));
                        selPosition = Math.Min(selPosition, selPosition + selDist);
                        selDist = 0;
                    }

                    calcCursorPosition();
                    return true;
                }

                if (k == Keys.SDLK_LEFT)
                {
                    if (key.shift)
                    {//we're now editting the selection box
                        if (key.ctrl)
                        {//select to the end of the closest space to the left
                            int s = selPosition + selDist;
                            selDist = 0;

                            for (int i = s - 2; i > 0; i--)
                            {
                                if (text[i] == ' ')
                                {
                                    selDist = i + 1;
                                    break;
                                }
                            }

                            selDist = selDist - selPosition;
                            calcCursorPosition();
                            return true;
                        }
                        selDist = Math.Max(selDist + selPosition - 1, 0) - selPosition;
                        calcCursorPosition();
                        return true;
                    }

                    if (key.ctrl)
                    {//go to the end of the closest space to the left
                        int s = selPosition + selDist;
                        selPosition = 0;

                        for (int i = s - 2; i > 0; i--)
                        {
                            if (text[i] == ' ')
                            {
                                selPosition = i + 1;
                                break;
                            }
                        }
                        selDist = 0;
                        calcCursorPosition();
                        return true;
                    }

                    //slide the cursor one to the left
                    if (selDist == 0)
                        selPosition = Math.Max(selPosition - 1, 0);
                    else
                        selPosition = Math.Min(selPosition, selPosition + selDist);

                    selDist = 0;
                    calcCursorPosition();
                    return true;
                }

                if (k == Keys.SDLK_RIGHT)
                {
                    if (key.shift)
                    {//editting the selection box
                        if (key.ctrl)
                        {//select past the next space to the right
                            int s = selPosition + selDist;
                            selDist = text.Length;

                            for (int i = s; i < text.Length; i++)
                            {
                                if (text[i] == ' ')
                                {
                                    selDist = i + 1;
                                    break;
                                }
                            }

                            selDist = selDist - selPosition;
                            calcCursorPosition();
                            return true;
                        }
                        selDist = Math.Min(selDist + selPosition + 1, text.Length) - selPosition;
                        calcCursorPosition();
                        return true;
                    }

                    if (key.ctrl)
                    {//go past the next space to the right
                        int s = selPosition + selDist;
                        selPosition = text.Length;

                        for (int i = s; i < text.Length; i++)
                        {
                            if (text[i] == ' ')
                            {
                                selPosition = i + 1;
                                break;
                            }
                        }

                        selDist = 0;
                        calcCursorPosition();
                        return true;
                    }

                    //increment cursor by one
                    if (selDist == 0)
                        selPosition = Math.Min(selPosition + 1, text.Length);
                    else
                        selPosition = Math.Max(selPosition, selPosition + selDist);

                    selDist = 0;
                    calcCursorPosition();
                    return true;
                }

                if (k == Keys.SDLK_UP || k == Keys.SDLK_HOME)
                {//jump to the beginning
                    if (key.shift)
                    {//select to the beginning
                        selDist = -selPosition;
                        calcCursorPosition();
                        return true;
                    }

                    selPosition = 0;
                    selDist = 0;
                    calcCursorPosition();
                    return true;
                }

                if (k == Keys.SDLK_DOWN || k == Keys.SDLK_END)
                {//jump to the end
                    if (key.shift)
                    {//select to the end
                        selDist = text.Length - selPosition;
                        calcCursorPosition();
                        return true;
                    }

                    selPosition = text.Length;
                    selDist = 0;
                    calcCursorPosition();
                    return true;
                }

                if (key.ctrl && k == Keys.SDLK_a)
                {//select all
                    selPosition = 0;
                    selDist = text.Length;
                    calcCursorPosition();
                    return true;
                }

                if (key.ctrl && k == Keys.SDLK_x)
                {//cut
                    if (selDist == 0)
                        return true;

                    int min = Math.Min(selPosition + selDist, selPosition);
                    int max = Math.Max(selPosition + selDist, selPosition);

                    SDL2.SDL.SDL_SetClipboardText(text.Substring(min, max - min));
                    DoBackspace();
                    return true;
                }

                if (key.ctrl && k == Keys.SDLK_c)
                {//copy
                    if (selDist == 0)
                        return true;

                    int min = Math.Min(selPosition + selDist, selPosition);
                    int max = Math.Max(selPosition + selDist, selPosition);

                    SDL2.SDL.SDL_SetClipboardText(text.Substring(min, max - min));
                    return true;
                }

                if (key.ctrl && k == Keys.SDLK_v)
                {//paste
                    if (SDL2.SDL.SDL_HasClipboardText() == SDL2.SDL.SDL_bool.SDL_FALSE)
                        return true;

                    string toAdd = SDL2.SDL.SDL_GetClipboardText();
                    OnText(toAdd);
                    return true;
                }

                if (k == Keys.SDLK_RETURN || k == Keys.SDLK_KP_ENTER || k == Keys.SDLK_RETURN2)
                {//enter, fire event
                    selDist = 0;

                    if (onEnterPressed != null)
                        onEnterPressed();

                    isTyping = false;
                    return true;
                }
            }

            //we consume all the key events here, if we're typing -- SDL will also fire OnText for normal keys, which we consume elsewhere
            return isTyping;
        }

        void OnText(string toAdd)
        {
            //so this text can be dirty -- unicode characters we don't support or whatever. trusting in C#'s string implementation to... deal with it
            if (selDist != 0)
            {
                //save the old selection, and edit it first, because it can get changed on the text set
                //we could bypass the auto-stuff on the text, but, whatever
                var oldSelPos = selPosition; 
                var oldSelDist = selDist;
                selPosition = Math.Min(selPosition, selPosition + selDist);
                selDist = 0;
                text = text.Remove(Math.Min(oldSelPos, oldSelPos + oldSelDist), Math.Abs(oldSelDist));
            }

            text = text.Insert(selPosition, toAdd);
            selPosition++;

            calcCursorPosition();
        }

        private void DoBackspace()
        {
            if (selPosition == 0 && selDist == 0)
                return;

            int len = text.Length;
            if (len == 0)
                return;

            if (selDist == 0)
                text = text.Remove(--selPosition, 1);
            else
            {
                text = text.Remove(Math.Min(selPosition, selPosition + selDist), Math.Abs(selDist));
                selPosition = Math.Min(selPosition, selPosition + selDist);
                selDist = 0;
            }

            calcCursorPosition();
        }

        /// <summary>
        /// This method is for calculating the position of the text cursor.
        /// Should be called whenever the text or selection updates in any way.
        /// </summary>
        private void calcCursorPosition()
        {
            if (scale.X <= buffer * 2 || scale.Y <= buffer * 2)
            {
                selCursor.enabled = false;
                return;
            }

            selTimer = 0;
            selCursor.enabled = isTyping;

            selPosition = Math.Max(Math.Min(selPosition, text.Length), 0);
            selDist = Math.Max(Math.Min(selPosition + selDist, text.Length), 0) - selPosition;

            float strWidth = font.getRenderWidth(text);
            if (font.pos.X + font.offset.X + strWidth < pos.X + scale.X - buffer && font.offset.X < 0)
            {//if we have room to move the text back to the right into view, do it
                //careful with this, easy to get in an infinite loop with the other text scrollers at the end
                font.offset.X -= font.pos.X + font.offset.X + strWidth - (pos.X + scale.X);
                font.offset.X = Math.Min(font.offset.X, 0);
            }

            selCursor.position.X = font.pos.X + font.offset.X;
            selCursor.position.Y = pos.Y + (scale.Y - font.scale) / 2;

            //the 'active' position, either the cursor position or the active selection position
            float activePos;
            if (selDist == 0)
            {//drawing the cursor,
                selCursor.scale = new Vector2(2, font.scale);

                selCursor.position.X += font.getRenderWidth(text.Substring(0, selPosition)) - 1;
                selCursor.mainColor = cursorColor;

                activePos = selCursor.position.X;
            }
            else
            {
                float distPos = font.getRenderWidth(text.Substring(0, selPosition + selDist));
                float curPos = font.getRenderWidth(text.Substring(0, selPosition));

                activePos = selCursor.position.X + distPos;

                float minDist = Math.Min(distPos, curPos) + selCursor.position.X;
                float maxDist = Math.Max(distPos, curPos) + selCursor.position.X;

                selCursor.position.X = minDist - 1;
                selCursor.scale.Y = font.scale;
                selCursor.scale.X = maxDist - 2;
                selCursor.enabled = true;
                selCursor.mainColor = selColor;

                selCursor.position.X = Math.Max(selCursor.position.X, pos.X + buffer);
                selCursor.scale.X = Math.Min(selCursor.scale.X - selCursor.position.X, pos.X + scale.X - selCursor.position.X - buffer);
            }

            if (activePos > pos.X + scale.X - buffer + 2)
            {//if our active position is past the end of the textbox, scroll the text to the left
                font.offset.X -= activePos - (pos.X + scale.X - buffer);
                calcCursorPosition();
            }
            else if (activePos < pos.X + buffer - 2)
            {//if our active position is before the textbox starts, scroll the text to the right
                font.offset.X -= activePos - (pos.X + buffer);
                calcCursorPosition();
            }
        }

        void Update(float dt)
        {
            if (isTyping && selDist == 0)
            {
                selTimer += dt;
                if (selTimer > 0.5f)
                {
                    selCursor.enabled = !selCursor.enabled;
                    selTimer = 0;
                }
            }

        }

        public void Dispose()
        {
            isTyping = false;

            bg.Dispose();
            selCursor.Dispose();
            font.Dispose();

            em.removeEventHandler(onPointerEvent);
            em.removeUpdateListener(Update);
        }
    }
}
