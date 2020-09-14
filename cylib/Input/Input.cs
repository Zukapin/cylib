using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDL2;
using Scancode = SDL2.SDL.SDL_Scancode;
using Keys = SDL2.SDL.SDL_Keycode;

using log;
using System.Runtime.InteropServices;

namespace cylib
{
    public delegate bool OnPointerChange(PointerEventArgs args);
    public delegate bool OnFocusChange(bool hasFocus); //window focus, for when to eat/release mouse
    public delegate bool OnAxisMove(Axis axis, float val);
    public delegate bool OnTriggerMove(Trigger trig, float val);
    public delegate bool OnKeyChange(KeyData data, bool isDown);
    public delegate bool OnAction(ActionEventArgs args);
    public delegate void OnTextInput(string text);

    /// <summary>
    /// Anything that interfaces with human input should have a priority.
    /// Any input is given through the list of interfaces, in order of priority, until the input has been handled.
    /// </summary>
    public enum InterfacePriority : int
    {
        HIGHEST = 0,
        HIGH = 1024,
        MEDIUM = 2048,
        LOW = 3072,
        LOWEST = 4096
    }

    public enum PointerButton : uint
    {
        NONE,
        LEFT = SDL.SDL_BUTTON_LEFT,
        MIDDLE = SDL.SDL_BUTTON_MIDDLE,
        RIGHT = SDL.SDL_BUTTON_RIGHT,
        XBUTTON1 = SDL.SDL_BUTTON_X1,
        XBUTTON2 = SDL.SDL_BUTTON_X2
    }

    public enum Axis
    {
        X,
        Y,
        RX,
        RY
    }

    public enum Trigger
    {
        LEFT,
        RIGHT
    }

    public enum ControllerButton
    {
        X,
        Y,
        B,
        A,
        L3,
        R3,
        L2,
        R2,
        START,
        SELECT,
        GUIDE,
        DPAD_LEFT,
        DPAD_RIGHT,
        DPAD_UP,
        DPAD_DOWN
    }

    public enum PointerEventType
    {
        MOVE,
        BUTTON,
        MOUSEWHEEL,
        AIM
    }

    public struct KeyData
    {
        public readonly Scancode s;
        public readonly Keys k;
        public readonly bool shift;
        public readonly bool ctrl;
        public readonly bool alt;

        public KeyData(Scancode s, Keys k, bool shift, bool ctrl, bool alt)
        {
            this.s = s;
            this.k = k;
            this.shift = shift;
            this.ctrl = ctrl;
            this.alt = alt;
        }
    }

    public struct PointerEventArgs
    {
        /// <summary>
        /// The type of event.
        /// </summary>
        public readonly PointerEventType type;

        /// <summary>
        /// X Position of the pointer during the event.
        /// </summary>
        public readonly int x;

        /// <summary>
        /// Y Position of the pointer during the event.
        /// </summary>
        public readonly int y;

        /// <summary>
        /// The button involved during a 'BUTTON' event.
        /// </summary>
        public readonly PointerButton button;

        /// <summary>
        /// Whether the button is down or up, for 'BUTTON' events.
        /// </summary>
        public readonly bool isDown;

        /// <summary>
        /// Signed number of detents the mousewheel was changed by in the horizontal direction, for 'MOUSEWHEEL' events.
        /// Used for phones and iOS stuff, generally?
        /// </summary>
        public readonly int wheelX;

        /// <summary>
        /// Signed number of detents the mousewheel was changed by in the vertical direction, for 'MOUSEWHEEL' events.
        /// This will be the usual mouse stuff.
        /// </summary>
        public readonly int wheelY;

        /// <summary>
        /// The amount the pointer moved horizontally during an aim event, as a percentage of the screen.
        /// </summary>
        public readonly float aimDeltaX;

        /// <summary>
        /// The amount the pointer moved vertically during an aim event, as a percentage of the screen.
        /// </summary>
        public readonly float aimDeltaY;

        /// <summary>
        /// If the window had focus when this event was fired.
        /// </summary>
        public readonly bool windowInFocus;

        public PointerEventArgs(PointerEventType type, int x, int y, PointerButton button, bool isDown, int deltaX, int deltaY, float aimX, float aimY, bool windowInFocus)
        {
            this.type = type;
            this.x = x;
            this.y = y;
            this.button = button;
            this.isDown = isDown;
            this.wheelX = deltaX;
            this.wheelY = deltaY;
            this.aimDeltaX = aimX;
            this.aimDeltaY = aimY;
            this.windowInFocus = windowInFocus;
        }
    }

    /// <summary>
    /// Input maps raw input from devices into application actions.
    /// </summary>
    public class InputHandler
    {
        GameStage stage;
        ActionMapper map;
        public EventManager events; //should be set by stage during constructor, then updated however

        private OnTextInput activeTextbox;

        public InputHandler(GameStage stage, string actionFile)
        {
            this.stage = stage;
            map = new ActionMapper(this, actionFile);
            SDL.SDL_StopTextInput(); //for some reason sdl defaults text input to on
        }

        int prevMouseX, prevMouseY;
        public void EnterFPVMode()
        {
            if (SDL.SDL_GetRelativeMouseMode() == SDL.SDL_bool.SDL_TRUE)
                return;
            SDL.SDL_GetMouseState(out prevMouseX, out prevMouseY);
            SDL.SDL_SetRelativeMouseMode(SDL.SDL_bool.SDL_TRUE);
        }

        public void LeaveFPVMode()
        {
            if (SDL.SDL_GetRelativeMouseMode() == SDL.SDL_bool.SDL_FALSE)
                return;
            SDL.SDL_SetRelativeMouseMode(SDL.SDL_bool.SDL_FALSE);
            SDL.SDL_WarpMouseInWindow(stage.renderer.window.Handle, prevMouseX, prevMouseY);
        }

        public void ConstrainMouseToWindow()
        {
            SDL.SDL_SetWindowGrab(stage.renderer.window.Handle, SDL.SDL_bool.SDL_TRUE);
        }

        public void StopConstrainingMouseToWindow()
        {
            SDL.SDL_SetWindowGrab(stage.renderer.window.Handle, SDL.SDL_bool.SDL_FALSE);
        }

        internal void StartTyping(OnTextInput callback)
        {
            activeTextbox = callback;
            SDL.SDL_StartTextInput();
        }

        internal void StopTyping()
        {
            activeTextbox = null;
            SDL.SDL_StopTextInput();
        }

        public void Update()
        {
            while (SDL.SDL_PollEvent(out SDL.SDL_Event ev) != 0)
            {
                switch (ev.type)
                {
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        onKeyChange(ev.key.keysym.scancode, ev.key.keysym.sym, true,
                            (ev.key.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != 0,
                            (ev.key.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != 0,
                            (ev.key.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != 0);
                        break;
                    case SDL.SDL_EventType.SDL_KEYUP:
                        onKeyChange(ev.key.keysym.scancode, ev.key.keysym.sym, false,
                            (ev.key.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != 0,
                            (ev.key.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != 0,
                            (ev.key.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != 0);
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEMOTION:
                        if (SDL.SDL_GetRelativeMouseMode() == SDL.SDL_bool.SDL_FALSE)
                            onPointerMovement(ev.motion.x, ev.motion.y, true);
                        else
                            onPointerAim(ev.motion.xrel / (float)stage.renderer.ResolutionWidth, ev.motion.yrel / (float)stage.renderer.ResolutionHeight, true);
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                        if (SDL.SDL_GetRelativeMouseMode() == SDL.SDL_bool.SDL_FALSE)
                        {
                            SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
                            int dx = ev.wheel.x;
                            int dy = ev.wheel.y;

                            if (ev.wheel.direction == (uint)SDL.SDL_MouseWheelDirection.SDL_MOUSEWHEEL_FLIPPED)
                            {
                                dx *= -1;
                                dy *= -1;
                            }

                            onPointerMousewheel(mouseX, mouseY, dx, dy, true);
                        }
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                        if (SDL.SDL_GetRelativeMouseMode() == SDL.SDL_bool.SDL_FALSE)
                            onPointerButton((PointerButton)ev.button.button, ev.button.x, ev.button.y, true, true, true);
                        else
                            onPointerButton((PointerButton)ev.button.button, ev.button.x, ev.button.y, true, true, false);
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                        if (SDL.SDL_GetRelativeMouseMode() == SDL.SDL_bool.SDL_FALSE)
                            onPointerButton((PointerButton)ev.button.button, ev.button.x, ev.button.y, false, true, true);
                        else
                            onPointerButton((PointerButton)ev.button.button, ev.button.x, ev.button.y, false, true, false);
                        break;
                    case SDL.SDL_EventType.SDL_QUIT:
                        stage.Exit();
                        break;
                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        switch (ev.window.windowEvent)
                        {
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                                break;
                            default:
                                Logger.WriteLine(LogType.VERBOSE, "Missing handling for window event: " + ev.window.windowEvent);
                                break;
                        }
                        break;
                    case SDL.SDL_EventType.SDL_TEXTINPUT:
                        unsafe
                        {
                            var text = StringFromNativeUtf8(ev.text.text);
                            if (activeTextbox != null)
                                activeTextbox(text);
                            else
                                Logger.WriteLine(LogType.DEBUG, "TextInput event while our textbox callback is null? " + text);
                        }
                        break;
                    case SDL.SDL_EventType.SDL_TEXTEDITING:
                        unsafe
                        {
                            var text = StringFromNativeUtf8(ev.edit.text);
                            Logger.WriteLine(LogType.DEBUG, "Got text edit input: " + text + " " + ev.edit.start + " " + ev.edit.length);
                        }
                        break;
                    case SDL.SDL_EventType.SDL_AUDIODEVICEADDED:
                        break;
                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                        break;
                    case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                        break;
                    default:
                        Logger.WriteLine(LogType.DEBUG, "Missing handling for event: " + ev.type);
                        break;
                }
            }
        }

        private static unsafe string StringFromNativeUtf8(byte* nativeUtf8)
        {
            int len = 0;
            while (nativeUtf8[len] != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy((IntPtr)nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        /*
        private void onLostFocus(object sender, EventArgs e)
        {
            Logger.WriteLine(LogType.VERBOSE, "Form lost focus...");
            //mouse.LostFocus();
            foreach (OnFocusChange d in events.focusChangeList)
            {
                if (d(false))
                    return;
            }
        }

        private void onGainedFocus(object sender, EventArgs e)
        {
            Logger.WriteLine(LogType.VERBOSE, "Form gained focus...");
            //mouse.GainedFocus();
            foreach (OnFocusChange d in events.focusChangeList)
            {
                if (d(true))
                    return;
            }
        }
        */

        private void onPointerMovement(int posX, int posY, bool focus)
        {
            PointerEventArgs args = new PointerEventArgs(PointerEventType.MOVE, posX, posY, PointerButton.NONE, false, 0, 0, 0, 0, focus);
            foreach (OnPointerChange e in events.pointerChangeList)
            {
                if (e(args))
                    return;
            }
        }

        private void onPointerMousewheel(int posX, int posY, int deltaX, int deltaY, bool focus)
        {
            PointerEventArgs args = new PointerEventArgs(PointerEventType.MOUSEWHEEL, posX, posY, PointerButton.NONE, false, deltaX, deltaY, 0, 0, focus);
            foreach (OnPointerChange e in events.pointerChangeList)
            {
                if (e(args))
                    return;
            }
        }

        private void onPointerButton(PointerButton button, int posX, int posY, bool isDown, bool focus, bool mouseVisible)
        {
            PointerEventArgs args = new PointerEventArgs(PointerEventType.BUTTON, posX, posY, button, isDown, 0, 0, 0, 0, focus);
            bool hasAction = map.TryGetAction(button, out var actionMap);
            if (hasAction && actionMap.IsFired == isDown)
                hasAction = false;

            ActionEventArgs actionArgs = new ActionEventArgs();
            if (hasAction)
                actionArgs = new ActionEventArgs(actionMap.Name, FiredBy.MOUSE_BUTTON, isDown, posX, posY);

            if (!hasAction && mouseVisible)
            {
                foreach (OnPointerChange e in events.pointerChangeList)
                {
                    if (e(args))
                        return;
                }
            }
            else if (hasAction && mouseVisible)
            {
                foreach (Pair<OnPointerChange, OnAction> p in events.PointerActionList(actionMap.Name))
                {
                    if (p.hasVal1)
                    {
                        if (p.val1(args))
                            return;
                    }
                    else
                    {
                        if (p.val2(actionArgs))
                        {
                            actionMap.IsFired = isDown;
                            return;
                        }
                    }
                }
            }
            else if (hasAction && !mouseVisible)
            {
                foreach (var a in events.ActionList(actionMap.Name))
                {
                    if (a(actionArgs))
                    {
                        actionMap.IsFired = isDown;
                        return;
                    }
                }
            }
        }

        private void onPointerAim(float dX, float dY, bool focus)
        {
            PointerEventArgs args = new PointerEventArgs(PointerEventType.AIM, 0, 0, PointerButton.NONE, false, 0, 0, dX, dY, focus);
            foreach (OnPointerChange e in events.pointerChangeList)
            {
                if (e(args))
                    return;
            }
        }

        private void onKeyChange(Scancode s, Keys k, bool isDown, bool shift, bool ctrl, bool alt)
        {
            KeyData key = new KeyData(s, k, shift, ctrl, alt);

            bool hasAction = map.TryGetAction(key, out var actionMap);
            if (hasAction && actionMap.IsFired == isDown)
                hasAction = false;

            if (!hasAction)
            {
                foreach (OnKeyChange e in events.keyChangeList)
                {
                    if (e(key, isDown))
                        return;
                }

                return;
            }
            else
            {
                var actionArgs = new ActionEventArgs(actionMap.Name, FiredBy.BUTTON, isDown, 0, 0);
                foreach (Pair<OnKeyChange, OnAction> p in events.KeyActionList(actionMap.Name))
                {
                    if (p.hasVal1)
                    {
                        if (p.val1(key, isDown))
                            return;
                    }
                    else
                    {
                        if (p.val2(actionArgs))
                        {
                            actionMap.IsFired = isDown;
                            return;
                        }
                    }
                }
            }
        }

        internal void onBindingChange(Scancode k, KeyMap keyData, bool isBound)
        {
            //if the action was fired but not released, might want to call the release event
            //... can that even happen?
            //otherwise I'm not sure why this exists
            if (isBound)
            {
                Logger.WriteLine(LogType.VERBOSE, "Key binding added. Bind: " + keyData.GetBindDisplay(k) + " Action: " + keyData.Name);
            }
            else
            {
                Logger.WriteLine(LogType.VERBOSE, "Key binding removed. Bind: " + keyData.GetBindDisplay(k) + " Action: " + keyData.Name);
            }
        }

        internal void onBindingChange(PointerButton k, PointerMap pointerData, bool isBound)
        {
            if (isBound)
            {
                Logger.WriteLine(LogType.VERBOSE, "Pointer binding added. Bind: " + pointerData.GetBindDisplay(k) + " Action: " + pointerData.Name);
            }
            else
            {
                Logger.WriteLine(LogType.VERBOSE, "Pointer binding removed. Bind: " + pointerData.GetBindDisplay(k) + " Action: " + pointerData.Name);
            }
        }
    }
}
