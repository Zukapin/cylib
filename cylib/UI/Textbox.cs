using System;
using System.Numerics;
using Keys = SDL2.SDL.SDL_Keycode;
using Color = SharpDX.Color;

namespace cylib
{
    /// <summary>
    /// Used in textbox class to map from a keycode to a set of render-supported characters.
    /// Probably doesn't work on non-US keyboards.
    /// </summary>
    public static class KeyToCharMapper
    {
        public static string supportedChars = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ`1234567890-=[];',./\\~!@#$%^&*()_+{}:\"<>?|";
        public static char getChar(Keys key, bool isShiftDown)
        {
            if ((int)key > 96 && (int)key < 123)
            {
                if (isShiftDown)
                    return (char)(key - 32);
                else
                    return (char)(key);
            }

            switch (key)
            {
                case Keys.SDLK_SPACE:
                    return ' ';
                case Keys.SDLK_KP_0:
                    return '0';
                case Keys.SDLK_KP_1:
                    return '1';
                case Keys.SDLK_KP_2:
                    return '2';
                case Keys.SDLK_KP_3:
                    return '3';
                case Keys.SDLK_KP_4:
                    return '4';
                case Keys.SDLK_KP_5:
                    return '5';
                case Keys.SDLK_KP_6:
                    return '6';
                case Keys.SDLK_KP_7:
                    return '7';
                case Keys.SDLK_KP_8:
                    return '8';
                case Keys.SDLK_KP_9:
                    return '9';
                case Keys.SDLK_KP_MULTIPLY:
                    return '*';
                case Keys.SDLK_KP_DIVIDE:
                    return '/';
                case Keys.SDLK_KP_PLUS:
                    return '+';
                case Keys.SDLK_KP_MINUS:
                    return '-';
                case Keys.SDLK_KP_DECIMAL:
                    return '.';
                case Keys.SDLK_1:
                    if (isShiftDown)
                        return '!';
                    return '1';
                case Keys.SDLK_2:
                    if (isShiftDown)
                        return '@';
                    return '2';
                case Keys.SDLK_3:
                    if (isShiftDown)
                        return '#';
                    return '3';
                case Keys.SDLK_4:
                    if (isShiftDown)
                        return '$';
                    return '4';
                case Keys.SDLK_5:
                    if (isShiftDown)
                        return '%';
                    return '5';
                case Keys.SDLK_6:
                    if (isShiftDown)
                        return '^';
                    return '6';
                case Keys.SDLK_7:
                    if (isShiftDown)
                        return '&';
                    return '7';
                case Keys.SDLK_8:
                    if (isShiftDown)
                        return '*';
                    return '8';
                case Keys.SDLK_9:
                    if (isShiftDown)
                        return '(';
                    return '9';
                case Keys.SDLK_0:
                    if (isShiftDown)
                        return ')';
                    return '0';
                case Keys.SDLK_MINUS:
                    if (isShiftDown)
                        return '_';
                    return '-';
                case Keys.SDLK_PLUS:
                    if (isShiftDown)
                        return '+';
                    return '=';
                case Keys.SDLK_LEFTBRACKET:
                    if (isShiftDown)
                        return '{';
                    return '[';
                case Keys.SDLK_RIGHTBRACKET:
                    if (isShiftDown)
                        return '}';
                    return ']';
                case Keys.SDLK_SEMICOLON:
                    if (isShiftDown)
                        return ':';
                    return ';';
                case Keys.SDLK_QUOTE:
                    if (isShiftDown)
                        return '|';
                    return '\'';
                case Keys.SDLK_COMMA:
                    if (isShiftDown)
                        return '<';
                    return ',';
                case Keys.SDLK_KP_PERIOD:
                    if (isShiftDown)
                        return '>';
                    return '.';
                case Keys.SDLK_SLASH:
                    if (isShiftDown)
                        return '?';
                    return '/';
                case Keys.SDLK_BACKSLASH:
                    if (isShiftDown)
                        return '|';
                    return '\\';
                case Keys.SDLK_BACKQUOTE:
                    if (isShiftDown)
                        return '~';
                    return '`';
                default:
                    //Logger.WriteLine(LogType.DEBUG, "Missing char case: " + key);
                    return (char)0;
            }
        }
    }

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

        static readonly Color bgColor = new Color(40, 40, 40);
        static readonly Color activeColor = new Color(20, 20, 20);
        static readonly Color outlineColor = Color.White;

        static readonly Color fontColor = Color.White;
        static readonly Color cursorColor = Color.White;
        static readonly Color selColor = new Color(80, 80, 255);

        Renderer renderer;
        EventManager em;
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
                }
                else
                {
                    bg.mainColor = bgColor;
                    em.removeEventHandler(onKeyChange);
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

        public Textbox(Renderer renderer, EventManager em, int priority)
        {
            this.renderer = renderer;
            this.em = em;

            bg = new RoundedRectangle_2D(renderer, em, priority);
            bg.radius = 4f;
            bg.mainColor = bgColor;
            bg.borderColor = outlineColor;
            bg.borderThickness = 1f;

            font = new FontRenderer(renderer, em, priority + 2, renderer.Assets.getAsset(FontAssets.SEGOEUI_SDF_128));
            font.anchor = FontAnchor.CENTER_LEFT;
            font.color = fontColor;

            selCursor = new RoundedRectangle_2D(renderer, em, priority + 1);
            selCursor.radius = 1;
            selCursor.mainColor = cursorColor;
            selCursor.borderThickness = 0;
            selCursor.enabled = false;

            em.addEventHandler((int)InterfacePriority.MEDIUM, onPointerEvent);
            em.addUpdateListener(priority, Update);
        }

        bool onPointerEvent(PointerEventArgs args)
        {
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

                        if (args.x < xpos)
                        {
                            s = i - 1;
                            break;
                        }
                    }

                    selDist = s - selPosition;

                    calcCursorPosition();
                    return true;
                }
                if (args.x > pos.X && args.x <= pos.X + scale.X
                    && args.y > pos.Y && args.y <= pos.Y + scale.Y)
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
                        && args.x > pos.X && args.x <= pos.X + scale.X
                        && args.y > pos.Y && args.y <= pos.Y + scale.Y)
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

                            if (args.x < xpos)
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

                            if (args.x < xpos)
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
                if (key.k == Keys.SDLK_BACKSPACE)
                {
                    DoBackspace();
                    return true;
                }

                if (key.k == Keys.SDLK_DELETE || (key.k == Keys.SDLK_KP_DECIMAL && key.shift))
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

                if (key.k == Keys.SDLK_LEFT)
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

                if (key.k == Keys.SDLK_RIGHT)
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

                if (key.k == Keys.SDLK_UP || key.k == Keys.SDLK_HOME)
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

                if (key.k == Keys.SDLK_DOWN || key.k == Keys.SDLK_END)
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

                if (key.ctrl && key.k == Keys.SDLK_a)
                {//select all
                    selPosition = 0;
                    selDist = text.Length;
                    calcCursorPosition();
                    return true;
                }

                if (key.ctrl && key.k == Keys.SDLK_x)
                {//cut
                    if (selDist == 0)
                        return true;

                    int min = Math.Min(selPosition + selDist, selPosition);
                    int max = Math.Max(selPosition + selDist, selPosition);

                    SDL2.SDL.SDL_SetClipboardText(text.Substring(min, max - min));
                    DoBackspace();
                    return true;
                }

                if (key.ctrl && key.k == Keys.SDLK_c)
                {//copy
                    if (selDist == 0)
                        return true;

                    int min = Math.Min(selPosition + selDist, selPosition);
                    int max = Math.Max(selPosition + selDist, selPosition);

                    SDL2.SDL.SDL_SetClipboardText(text.Substring(min, max - min));
                    return true;
                }

                if (key.ctrl && key.k == Keys.SDLK_v)
                {//paste
                    if (SDL2.SDL.SDL_HasClipboardText() == SDL2.SDL.SDL_bool.SDL_FALSE)
                        return true;

                    string toAdd = SDL2.SDL.SDL_GetClipboardText();

                    for (int i = 0; i < toAdd.Length; i++)
                    {
                        if (KeyToCharMapper.supportedChars.Contains(toAdd[i]))
                            addChar(toAdd[i]);
                    }
                    return true;
                }

                if (key.k == Keys.SDLK_RETURN || key.k == Keys.SDLK_KP_ENTER || key.k == Keys.SDLK_RETURN2)
                {//enter, fire event
                    selDist = 0;

                    if (onEnterPressed != null)
                        onEnterPressed();

                    return true;
                }

                char c = KeyToCharMapper.getChar(key.k, key.shift);
                if (c != 0)
                {//add char
                    addChar(c);
                    return true;
                }
            }
            return false;
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

        private void addChar(char c)
        {
            if (selDist != 0)
            {
                text = text.Remove(Math.Min(selPosition, selPosition + selDist), Math.Abs(selDist));
                selPosition = Math.Min(selPosition, selPosition + selDist);
                selDist = 0;
            }

            text = text.Insert(selPosition, c.ToString());
            selPosition++;

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
