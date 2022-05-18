using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;

using Scancode = SDL2.SDL.SDL_Scancode;
using Keys = SDL2.SDL.SDL_Keycode;

using cyUtility;
using System.Dynamic;

namespace cylib
{
    public enum FiredBy
    {
        BUTTON,
        MOUSE_BUTTON,
        AXIS,
        TRIGGER
    }

    #region Action Event Args
    public struct ActionEventArgs
    {
        public readonly string action;
        public readonly FiredBy cause;

        public readonly bool buttonDown; //button actions fire on both down and up
        public readonly float axisVal;
        public readonly float triggerVal;

        public readonly int mouseX;
        public readonly int mouseY;

        public readonly float relMouseX;
        public readonly float relMouseY;

        public ActionEventArgs(string action, FiredBy cause, bool buttonDown, float axisVal, float triggerVal)
        {
            this.action = action;
            this.cause = cause;
            this.buttonDown = buttonDown;
            this.axisVal = axisVal;
            this.triggerVal = triggerVal;

            mouseX = 0;
            mouseY = 0;
            relMouseX = 0;
            relMouseY = 0;
        }

        public ActionEventArgs(string action, FiredBy cause, bool buttonDown, int mouseX, int mouseY, float relMouseX, float relMouseY)
        {
            this.action = action;
            this.cause = cause;
            this.mouseX = mouseX;
            this.mouseY = mouseY;
            this.buttonDown = buttonDown;
            this.relMouseX = relMouseX;
            this.relMouseY = relMouseY;

            axisVal = 0;
            triggerVal = 0;
        }
    }
    #endregion

    public struct ActionInformation
    {
        //the underlying 'action' support information -- does not have keybind information, just what type of keybinds it *could* support
        public string Name { get; }
        internal bool SupportsButton { get; }
        internal bool SupportsAxis { get; }
        internal bool SupportsTrigger { get; }
        internal bool CaresAboutModifiers { get; }

        public ActionInformation(string name, bool supportsButton, bool supportsAxis, bool supportsTrigger, bool caresAboutModifiers)
        {
            this.Name = name;
            this.SupportsButton = supportsButton;
            this.SupportsAxis = supportsAxis;
            this.SupportsTrigger = supportsTrigger;
            this.CaresAboutModifiers = caresAboutModifiers;
        }
    }

    #region KeyMap
    public interface ActionMap
    {
        bool IsFired { get; set; }
        string Name { get; }
        string BindDisplay { get; }
    }

    public class KeyMap : ActionMap
    {
        internal ActionInformation Action { get; }
        internal bool CaresAboutModifiers { get { return Action.CaresAboutModifiers; } }
        public string Name { get { return Action.Name; } }
        internal bool RequiresShift { get; }
        internal bool RequiresCtrl { get; }
        internal bool RequiresAlt { get; }
        public bool IsFired { get; set; }
        public readonly Scancode key;

        public KeyMap(Scancode key, ActionInformation action, bool requiresShift, bool requiresCtrl, bool requiresAlt)
        {
            this.key = key;
            this.Action = action;
            this.RequiresShift = requiresShift;
            this.RequiresCtrl = requiresCtrl;
            this.RequiresAlt = requiresAlt;
            IsFired = false;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is KeyMap))
                return false;

            return Equals((KeyMap)obj);
        }

        public override int GetHashCode()
        {
            return Action.Name.GetHashCode();
        }

        public bool Equals(KeyMap o)
        {
            //This isn't *only* the name because we can have multiple things bound to the same action.
            //Like you can have jump bound to spacebar and mouse4, or something.

            if (Action.Name != o.Action.Name)
                return false;

            if (!CaresAboutModifiers)
                return true;

            return RequiresShift == o.RequiresShift && RequiresCtrl == o.RequiresCtrl && RequiresAlt == o.RequiresAlt;
        }

        public static bool operator ==(KeyMap a, KeyMap b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(KeyMap a, KeyMap b)
        {
            return !a.Equals(b);
        }

        public string BindDisplay
        {
            get
            {
                string toReturn = key.ToString();
                if (!CaresAboutModifiers)
                    return toReturn;
                if (RequiresShift)
                    toReturn += "+SHIFT";
                if (RequiresCtrl)
                    toReturn += "+CTRL";
                if (RequiresAlt)
                    toReturn += "+ALT";
                return toReturn;
            }
        }
    }

    public class PointerMap : ActionMap
    {
        internal ActionInformation Action { get; }
        public string Name { get { return Action.Name; } }
        public bool IsFired { get; set; }
        public readonly PointerButton button;

        public PointerMap(PointerButton button, ActionInformation action)
        {
            this.button = button;
            this.Action = action;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PointerMap))
                return false;

            return Equals((PointerMap)obj);
        }

        public override int GetHashCode()
        {
            return Action.Name.GetHashCode();
        }

        public bool Equals(PointerMap o)
        {
            return Action.Name == o.Action.Name;
        }

        public static bool operator ==(PointerMap a, PointerMap b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(PointerMap a, PointerMap b)
        {
            return !a.Equals(b);
        }

        public string BindDisplay
        {
            get
            {
                return button.ToString() + "_MOUSE";
            }
        }
    }
    #endregion

    public class ActionMapper
    {
        readonly Dictionary<string, ActionInformation> NameToAction; //Action Name -> Action Information (types of keys supported, etc)
        readonly Dictionary<Scancode, List<KeyMap>> KeyToAction; //Keyboard key -> List of possible actions (depending on modifiers)
        readonly Dictionary<PointerButton, PointerMap> PointerToAction; //Pointer button -> action

        /*
        Dictionary<ControllerButton, ActionType> controllerToAction;
        Dictionary<Axis, ActionType> axisToAction;
        Dictionary<Trigger, ActionType> triggerToAction;
        */

        public ActionMapper(IEnumerable<ActionInformation> SupportedActions, string BindingPath)
            : this()
        {
            foreach (var a in SupportedActions)
            {
                NameToAction.Add(a.Name, a);
            }
            LoadFromFile(BindingPath);
        }

        public ActionMapper()
        {
            NameToAction = new Dictionary<string, ActionInformation>();
            KeyToAction = new Dictionary<Scancode, List<KeyMap>>();
            PointerToAction = new Dictionary<PointerButton, PointerMap>();
            //controllerToAction = new Dictionary<ControllerButton, ActionType>();
            //axisToAction = new Dictionary<Axis, ActionType>();
            //triggerToAction = new Dictionary<Trigger, ActionType>();
        }

        /// <summary>
        /// Used for keybinding interfaces -- create a copy, apply all changes to the copy, and then call UpdateFromCopy
        /// </summary>
        public ActionMapper CreateCopy()
        {
            //we do hard internal copying here
            //skipping the add/remove methods in favor of just copying the internal data structure
            //mostly to keep any hooks on 'bindingChanged' methods clean
            var toRet = new ActionMapper();

            foreach (var e in NameToAction)
            {
                toRet.NameToAction.Add(e.Key, e.Value);
            }

            foreach (var e in KeyToAction)
            {
                var l = new List<KeyMap>();
                foreach (var i in e.Value)
                {
                    l.Add(i);
                }

                toRet.KeyToAction.Add(e.Key, l);
            }

            foreach (var e in PointerToAction)
            {
                toRet.PointerToAction.Add(e.Key, e.Value);
            }

            return toRet;
        }
        
        public void UpdateFromCopy(ActionMapper copy)
        {
            //assuming NameToAction can't change
            KeyToAction.Clear();
            foreach (var e in copy.KeyToAction)
            {
                var l = new List<KeyMap>();
                foreach (var i in e.Value)
                {
                    l.Add(i);
                }

                KeyToAction.Add(e.Key, l);
            }

            PointerToAction.Clear();
            foreach (var e in copy.PointerToAction)
            {
                PointerToAction.Add(e.Key, e.Value);
            }
        }

        #region Actions Get/Bind/Unbind
        public bool TryGetAction(KeyData key, out ActionMap args)
        {
            //general design for actions here --
            //actions are unique to a KeyData request, based on the ScanCode, shift, ctrl, and alt
            //we have a list of things per scancode because we *could* have multiple things bound to a button, but vary which action depending on modifiers
            //each action response also keeps track of if we've fired the OnDown for that action, because we never want to fire OnUp without first firing OnDown
            //this has the possibility of eating some action inputs when the user is tabbing in/out or something weird

            if (!KeyToAction.TryGetValue(key.s, out var m))
            {
                args = null;
                return false;
            }

            for (int i = 0; i < m.Count; i++)
            {
                KeyMap map = m[i];

                if (!map.CaresAboutModifiers || ((map.RequiresShift == key.shift) && (map.RequiresCtrl == key.ctrl) && (map.RequiresAlt == key.alt)))
                {
                    args = map;
                    return true;
                }
            }

            args = null;
            return false;
        }

        public bool TryGetAction(PointerButton button, out ActionMap args)
        {
            //pointer actions are simpler because they don't care about modifiers
            //theoretically may want to add modifier support? for MMO keybindspam games?
            //input handling systems don't typically support modifiers with pointer/controller buttons because they're separate inputs
            //sdl won't give me information about the keyboard during a mouse event, would have to track modifier states seperately
            if (!PointerToAction.TryGetValue(button, out var action))
            {
                args = null;
                return false;
            }

            args = action;
            return true;
        }

        /// <summary>
        /// Attempts to bind a key to an action.
        /// Returns true if the action succeeded, false if it didn't.
        /// 
        /// Ways this can fail:
        ///     1. Attempting to bind a key to an action that doesn't support keys.
        ///     2. Attempting to bind an action that doesn't exist.
        /// </summary>
        public bool AddKeyAction(Scancode s, bool shift, bool ctrl, bool alt, string actionName)
        {
            if (!NameToAction.TryGetValue(actionName, out var action))
                return false;

            if (!action.SupportsButton)
                return false;

            bool caresAboutMods = action.CaresAboutModifiers;
            List<KeyMap> m;
            KeyMap map = new KeyMap(s, action, shift, ctrl, alt);

            if (!KeyToAction.TryGetValue(s, out m))
            {
                m = new List<KeyMap>();
                m.Add(map);
                KeyToAction.Add(s, m);
                onBindingChange(map, true);
                return true;
            }

            //this key already has an action bound
            //if this action doesn't care about modifiers, unbind all other actions on this key
            //if the other action doesn't care about modifiers, unbind it
            //if all actions care about modifiers, unbind any action that has the same modifier set as this
            if (!caresAboutMods)
            {//nothing else matters, just unbind all of the other keys here
                while (m.Count != 0)
                {
                    UnbindKeyAction(m[0]);
                }

                m.Add(map);
                onBindingChange(map, true);
                return true;
            }

            if (m.Count == 1 && !m[0].CaresAboutModifiers)
            {//the action already bound here doesn't care about modifiers, so we have to unbind it
                //we're assuming we don't enter a state where there are more than one actions bound to this key, and one of them doesn't care about modifiers
                //that would be an invalid state
                UnbindKeyAction(m[0]);
                m.Add(map);
                onBindingChange(map, true);
                return true;
            }

            //none of the actions ignore modifiers, so check for any exact conflicts
            for (int i = 0; i < m.Count; i++)
            {
                if ((shift == m[i].RequiresShift) && (ctrl == m[i].RequiresCtrl) && (alt == m[i].RequiresAlt))
                {//we found an exact conflict, so remove this key
                    UnbindKeyAction(m[i]);

                    //there can't be two conflicts, so we're done here.
                    break;
                }
            }

            //either we didn't find any conflicts, or we did and removed it
            m.Add(map);
            onBindingChange(map, true);
            return true;
        }

        public void UnbindKeyAction(KeyMap m)
        {
            List<KeyMap> list;

            if (!KeyToAction.TryGetValue(m.key, out list))
            {
                Logger.WriteLine(LogType.POSSIBLE_ERROR, "Unbind Key was called, but we can't find the key binding: " + m.key);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == m)
                {
                    list.RemoveAt(i);
                    onBindingChange(m, false);
                    return;
                }
            }

            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Unbind Key was called, but we can't find the exact key map: " + m.key);
        }

        public bool AddPointerAction(PointerButton button, string actionName)
        {
            if (!NameToAction.TryGetValue(actionName, out var action))
                return false;

            if (!action.SupportsButton)
                return false;

            UnbindPointerAction(button);

            var map = new PointerMap(button, action);
            PointerToAction.Add(button, map);
            onBindingChange(map, true);
            return true;
        }

        public void UnbindPointerAction(PointerButton button)
        {
            if (PointerToAction.Remove(button, out var map))
            {
                onBindingChange(map, false);
            }
        }
        #endregion

        #region File IO
        public void WriteToFile(string path)
        {
            using (StreamWriter wr = new StreamWriter(new FileStream(path, FileMode.Create), Encoding.Unicode))
            {
                wr.WriteLine("#K = Key Bound to Action, Key ID, Shift, Ctrl, Alt, Action Name");
                foreach (var ent in KeyToAction)
                {
                    foreach (var map in ent.Value)
                    {
                        wr.WriteLine("K," + ent.Key.ToString() + "," + map.RequiresShift + "," + map.RequiresCtrl + "," + map.RequiresAlt + "," + map.Name);
                    }
                }

                wr.WriteLine();
                wr.WriteLine("#P = Pointer Button Bound to Action, Pointer Button ID, Action Name");
                foreach (var ent in PointerToAction)
                {
                    wr.WriteLine("P," + ent.Key.ToString() + "," + ent.Value.Name);
                }
            }
        }

        void LoadFromFile(string path)
        {
            using (StreamReader r = new StreamReader(new FileStream(path, FileMode.Open), Encoding.Unicode))
            {
                while (!r.EndOfStream)
                {
                    string line = r.ReadLine();

                    if (line.Length == 0 || line[0] == '#')
                        continue; //file comment or empty line

                    string[] parts = line.Split(',');

                    if (parts.Length == 0 || parts.Length == 1)
                    {
                        Logger.WriteLine(LogType.VERBOSE2, "Bindings config has an empty line: " + line);
                        continue;
                    }

                    for (int i = 0; i < parts.Length; i++)
                    {
                        parts[i] = parts[i].Trim();
                    }

                    if (parts[0] == "P")
                    {//pointer mapping
                        if (parts.Length != 3)
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Pointer binding config has incorrect number of parts: " + line);
                            continue;
                        }

                        PointerButton p;
                        string actionString = parts[2];

                        if (!Enum.TryParse(parts[1], out p))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't find pointer value in a pointer binding line: " + parts[1]);
                            continue;
                        }

                        if (!AddPointerAction(p, actionString))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Failed to bind pointer button to action: " + line);
                        }
                    }
                    else if (parts[0] == "K")
                    {//key mapping
                        if (parts.Length != 6)
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Key binding config has incorrect number of parts: " + line);
                            continue;
                        }

                        Scancode s;
                        bool shift, ctrl, alt;
                        string actionString = parts[5];

                        if (!Enum.TryParse(parts[1], out s))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't find key value in a key binding line: " + parts[1]);
                            continue;
                        }

                        if (!bool.TryParse(parts[2], out shift))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't read shiftRequired value in key binding: " + parts[2]);
                            continue;
                        }

                        if (!bool.TryParse(parts[3], out ctrl))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't read ctrlRequired value in key binding: " + parts[3]);
                            continue;
                        }

                        if (!bool.TryParse(parts[4], out alt))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't read altRequired value in key binding: " + parts[4]);
                            continue;
                        }

                        if (!AddKeyAction(s, shift, ctrl, alt, actionString))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Failed to bind button to action: " + line);
                        }
                    }
                    else
                    {
                        Logger.WriteLine(LogType.POSSIBLE_ERROR, "Unknown binding type in binding config: " + parts[0]);
                        continue;
                    }
                }
            }
        }
        #endregion

        #region Action to Cause methods
        /// <summary>
        /// Get a list of all bindings mapped to an Action.
        /// Generally slow, try to cache any results from this.
        /// 
        /// Other inputs should have analogous methdods -- call all of them that the action supports.
        /// </summary>
        public IEnumerable<ActionMap> GetActionBinds(string action)
        {
            //We're just looping through the entire key map, as we don't have a reverse acceleration structure.
            //This should be fast enough, given we only expect to call this ~once per run.
            foreach (var ent in KeyToAction.Values)
            {
                foreach (var map in ent)
                {
                    if (map.Name == action)
                    {
                        yield return map;
                    }
                }
            }

            foreach (var ent in PointerToAction.Values)
            {
                if (ent.Name == action)
                {
                    yield return ent;
                }
            }
        }
        #endregion

        private void onBindingChange(ActionMap bindData, bool isBound)
        {
            if (isBound)
            {
                Logger.WriteLine(LogType.VERBOSE, "Action binding added. Bind: " + bindData.BindDisplay + " Action: " + bindData.Name);
            }
            else
            {
                Logger.WriteLine(LogType.VERBOSE, "Action binding removed. Bind: " + bindData.BindDisplay + " Action: " + bindData.Name);
            }
        }
    }
}
