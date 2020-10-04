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

        public readonly static ActionEventArgs None = new ActionEventArgs();

        public ActionEventArgs(string action, FiredBy cause, bool buttonDown, float axisVal, float triggerVal)
        {
            this.action = action;
            this.cause = cause;
            this.buttonDown = buttonDown;
            this.axisVal = axisVal;
            this.triggerVal = triggerVal;

            mouseX = 0;
            mouseY = 0;
        }

        public ActionEventArgs(string action, FiredBy cause, bool buttonDown, int mouseX, int mouseY)
        {
            this.action = action;
            this.cause = cause;
            this.mouseX = mouseX;
            this.mouseY = mouseY;
            this.buttonDown = buttonDown;

            axisVal = 0;
            triggerVal = 0;
        }
    }
    #endregion

    public class ActionInformation
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
    internal interface ActionMap
    {
        bool IsFired { get; set; }
        string Name { get; }
    }

    internal class KeyMap : ActionMap
    {
        ActionInformation Action { get; }
        internal bool CaresAboutModifiers { get { return Action.CaresAboutModifiers; } }
        public string Name { get { return Action.Name; } }
        internal bool RequiresShift { get; }
        internal bool RequiresCtrl { get; }
        internal bool RequiresAlt { get; }
        public bool IsFired { get; set; }

        public KeyMap(ActionInformation action, bool requiresShift, bool requiresCtrl, bool requiresAlt)
        {
            this.Action = action;
            this.RequiresShift = requiresShift;
            this.RequiresCtrl = requiresCtrl;
            this.RequiresAlt = requiresAlt;
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

        public string GetBindDisplay(Scancode s)
        {
            string toReturn = s.ToString();
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

    internal class PointerMap : ActionMap
    {
        ActionInformation Action { get; }
        public string Name { get { return Action.Name; } }
        public bool IsFired { get; set; }

        public PointerMap(ActionInformation action)
        {
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

        public string GetBindDisplay(PointerButton p)
        {
            string toReturn = p.ToString();
            return toReturn;
        }
    }
    #endregion

    class ActionMapper
    {
        readonly InputHandler Input;
        readonly Dictionary<string, ActionInformation> NameToAction;
        readonly Dictionary<Scancode, List<KeyMap>> KeyToAction;
        readonly Dictionary<PointerButton, PointerMap> PointerToAction;

        /*
        Dictionary<ControllerButton, ActionType> controllerToAction;
        Dictionary<Axis, ActionType> axisToAction;
        Dictionary<Trigger, ActionType> triggerToAction;
        */

        public ActionMapper(InputHandler input, string path)
        {
            this.Input = input;

            NameToAction = new Dictionary<string, ActionInformation>();

            KeyToAction = new Dictionary<Scancode, List<KeyMap>>();
            PointerToAction = new Dictionary<PointerButton, PointerMap>();
            //controllerToAction = new Dictionary<ControllerButton, ActionType>();
            //axisToAction = new Dictionary<Axis, ActionType>();
            //triggerToAction = new Dictionary<Trigger, ActionType>();

            LoadFromFile(path);
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
        /// </summary>
        bool AddKeyAction(Scancode s, bool shift, bool ctrl, bool alt, ActionInformation action)
        {
            if (!action.SupportsButton)
                return false;

            bool caresAboutMods = action.CaresAboutModifiers;
            List<KeyMap> m;
            KeyMap map = new KeyMap(action, shift, ctrl, alt);

            if (!KeyToAction.TryGetValue(s, out m))
            {
                m = new List<KeyMap>();
                m.Add(map);
                KeyToAction.Add(s, m);
                Input.onBindingChange(s, map, true);
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
                    UnbindKey(s, m[0]);
                }

                m.Add(map);
                Input.onBindingChange(s, map, true);
                return true;
            }

            if (m.Count == 1 && !m[0].CaresAboutModifiers)
            {//the action already bound here doesn't care about modifiers, so we have to unbind it
                //we're assuming we don't enter a state where there are more than one actions bound to this key, and one of them doesn't care about modifiers
                //that would be an invalid state
                UnbindKey(s, m[0]);
                m.Add(map);
                Input.onBindingChange(s, map, true);
                return true;
            }

            //none of the actions ignore modifiers, so check for any exact conflicts
            for (int i = 0; i < m.Count; i++)
            {
                if ((shift == m[i].RequiresShift) && (ctrl == m[i].RequiresCtrl) && (alt == m[i].RequiresAlt))
                {//we found an exact conflict, so remove this key
                    UnbindKey(s, m[i]);

                    //there can't be two conflicts, so we're done here.
                    break;
                }
            }

            //either we didn't find any conflicts, or we did and removed it
            m.Add(map);
            Input.onBindingChange(s, map, true);
            return true;
        }

        private void UnbindKey(Scancode s, KeyMap m)
        {
            List<KeyMap> list;

            if (!KeyToAction.TryGetValue(s, out list))
            {
                Logger.WriteLine(LogType.POSSIBLE_ERROR, "Unbind Key was called, but we can't find the key binding: " + s);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == m)
                {
                    list.RemoveAt(i);
                    Input.onBindingChange(s, m, false);
                    return;
                }
            }

            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Unbind Key was called, but we can't find the exact key map: " + s);
        }

        bool AddPointerAction(PointerButton button, ActionInformation action)
        {
            if (!action.SupportsButton)
                return false;

            if (PointerToAction.Remove(button, out var map))
            {
                Input.onBindingChange(button, map, false);
            }

            map = new PointerMap(action);
            PointerToAction.Add(button, map);
            Input.onBindingChange(button, map, true);
            return true;
        }
        #endregion

        #region File IO
        public void WriteToFile(string path)
        {
            using (StreamWriter wr = new StreamWriter(new FileStream(path, FileMode.Create), Encoding.Unicode))
            {
                foreach (var a in NameToAction.Values)
                {
                    wr.WriteLine("A," + a.Name + "," + a.SupportsButton + "," + a.SupportsTrigger + "," + a.SupportsAxis + "," + a.CaresAboutModifiers);
                }
                foreach (var ent in KeyToAction)
                {
                    foreach (var map in ent.Value)
                    {
                        wr.WriteLine("K," + ent.Key.ToString() + "," + map.RequiresShift + "," + map.RequiresCtrl + "," + map.RequiresAlt + "," + map.Name);
                    }
                }
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

                    if (parts[0] == "A")
                    {
                        if (parts.Length != 6)
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Key binding config has incorrect number of parts: " + line);
                            continue;
                        }

                        string action = parts[1];
                        bool button, trigger, axis, mods;

                        if (!bool.TryParse(parts[2], out button))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't read button supported value in action declaration: " + parts[2]);
                            continue;
                        }

                        if (!bool.TryParse(parts[3], out trigger))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't read trigger supported value in action declaration: " + parts[3]);
                            continue;
                        }

                        if (!bool.TryParse(parts[4], out axis))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't read axis supported value in action declaration: " + parts[4]);
                            continue;
                        }

                        if (!bool.TryParse(parts[5], out mods))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't read mods supported value in action declaration: " + parts[5]);
                            continue;
                        }

                        NameToAction.Add(action, new ActionInformation(action, button, axis, trigger, mods));
                    }
                    else if (parts[0] == "P")
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

                        if (!NameToAction.TryGetValue(actionString, out var action))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't find action declaration for key binding: " + actionString);
                            continue;
                        }

                        AddPointerAction(p, action);
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

                        if (!NameToAction.TryGetValue(actionString, out var action))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't find action declaration for key binding: " + actionString);
                            continue;
                        }

                        AddKeyAction(s, shift, ctrl, alt, action);
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
        /// Controller input should have analogous methdods -- call all of them that the action supports.
        /// </summary>
        public List<KeyMap> getActionBinds(string action)
        {
            List<KeyMap> toReturn = new List<KeyMap>();

            //We're just looping through the entire key map, as we don't have a reverse acceleration structure.
            //This should be fast enough, given we only expect to call this ~once per run.
            foreach (var ent in KeyToAction)
            {
                foreach (var map in ent.Value)
                {
                    if (map.Name == action)
                    {
                        toReturn.Add(map);
                    }
                }
            }

            return toReturn;
        }
        #endregion
    }
}
