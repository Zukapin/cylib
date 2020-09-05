using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;

using Scancode = SDL2.SDL.SDL_Scancode;
using Keys = SDL2.SDL.SDL_Keycode;

using log;

namespace cylib
{
    /// <summary>
    /// Every ActionType should then have the properties we care about added to the Action class. (If it's a button/axis/trigger action, etc)
    /// </summary>
    public enum ActionType
    {
        ESCAPE,
    }

    public enum FiredBy
    {
        BUTTON,
        AXIS,
        TRIGGER
    }

    #region Action Event Args
    public struct ActionEventArgs
    {
        public readonly ActionType action;
        public readonly FiredBy cause;

        public readonly bool buttonDown; //button actions fire on both down and up
        public readonly float axisVal;
        public readonly float triggerVal;

        public readonly static ActionEventArgs None = new ActionEventArgs();

        public ActionEventArgs(ActionType action, FiredBy cause, bool buttonDown, float axisVal, float triggerVal)
        {
            this.action = action;
            this.cause = cause;
            this.buttonDown = buttonDown;
            this.axisVal = axisVal;
            this.triggerVal = triggerVal;
        }
    }
    #endregion

    static class Action
    { //support, 'button', 'axis', 'trigger' event types
        #region Button Support
        /// <summary>
        /// This should return true if the action can be bound to a button.
        /// </summary>
        public static bool ActionSupportsButton(ActionType type)
        {
            switch (type)
            {
                case ActionType.ESCAPE:
                    return true;
            }

            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't find if an action supports buttons: " + type);
            return false;
        }
        #endregion

        #region Axis Support
        /// <summary>
        /// This should return true if the action can be bound to an axis.
        /// </summary>
        public static bool ActionSupportsAxis(ActionType type)
        {
            return false;
        }
        #endregion

        #region Trigger Support
        /// <summary>
        /// This should return true if the action can be bound to a trigger.
        /// </summary>
        public static bool ActionSupportsTrigger(ActionType type)
        {
            return false;
        }
        #endregion

        #region Cares About Modifiers
        /// <summary>
        /// This should return true if we want to use modifiers with this action, false if it should be the only action bound to a key.
        /// 
        /// For key binds, some actions may allow 'shift+Key' to be distinct from 'Key' alone.
        /// Other actions may want any press of 'Key', regardless of modifiers, to always fire the action.
        /// Compare 'W' in CS:GO, where it 'goes forward' regardless of shift/ctrl, to EVE where every key only fires on correct modifier combos.
        /// 
        /// Some keys can't have modifiers (mouse buttons), so this doesn't really affect those.
        /// </summary>
        public static bool ActionCaresAboutModifiers(ActionType type)
        {
            switch (type)
            {
                case ActionType.ESCAPE:
                    return false;
            }

            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't find if an action cares about modifiers: " + type);
            return true;
        }
        #endregion
    }

    #region KeyMap
    public class KeyMap
    {
        public readonly ActionType action;
        public readonly bool caresAboutModifiers;
        public readonly bool requiresShift;
        public readonly bool requiresCtrl;
        public readonly bool requiresAlt;
        public bool isDown = false;

        public KeyMap(ActionType action, bool caresAboutModifiers, bool requiresShift, bool requiresCtrl, bool requiresAlt)
        {
            this.action = action;
            this.caresAboutModifiers = caresAboutModifiers;
            this.requiresShift = requiresShift;
            this.requiresCtrl = requiresCtrl;
            this.requiresAlt = requiresAlt;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is KeyMap))
                return false;

            return Equals((KeyMap)obj);
        }

        public override int GetHashCode()
        {
            int toReturn = (int)action;
            toReturn = 31 * toReturn + (caresAboutModifiers ? 10 : 3);

            if (caresAboutModifiers)
            {
                toReturn = 31 * toReturn + (requiresShift ? 10 : 3);
                toReturn = 31 * toReturn + (requiresCtrl ? 10 : 3);
                toReturn = 31 * toReturn + (requiresAlt ? 10 : 3);
            }

            return toReturn;
        }

        public bool Equals(KeyMap o)
        {
            if (action != o.action)
                return false;

            if (caresAboutModifiers != o.caresAboutModifiers)
                return false;

            if (!caresAboutModifiers)
                return true;

            return requiresShift == o.requiresShift && requiresCtrl == o.requiresCtrl && requiresAlt == o.requiresAlt;
        }

        public static bool operator ==(KeyMap a, KeyMap b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(KeyMap a, KeyMap b)
        {
            return !a.Equals(b);
        }

        public string getDisplay(Scancode s)
        {
            string toReturn = s.ToString();
            if (!caresAboutModifiers)
                return toReturn;
            if (requiresShift)
                toReturn += "+SHIFT";
            if (requiresCtrl)
                toReturn += "+CTRL";
            if (requiresAlt)
                toReturn += "+ALT";
            return toReturn;
        }
    }
    #endregion

    class ActionMapper
    {
        InputHandler input;
        Dictionary<Scancode, List<KeyMap>> keyToAction;

        /*
        Dictionary<ControllerButton, ActionType> controllerToAction;
        Dictionary<Axis, ActionType> axisToAction;
        Dictionary<Trigger, ActionType> triggerToAction;
        */

        public ActionMapper(InputHandler input, string path)
        {
            this.input = input;

            keyToAction = new Dictionary<Scancode, List<KeyMap>>();
            //controllerToAction = new Dictionary<ControllerButton, ActionType>();
            //axisToAction = new Dictionary<Axis, ActionType>();
            //triggerToAction = new Dictionary<Trigger, ActionType>();

            LoadFromFile(path);
        }

        #region Key Get/Bind/Unbind
        public bool tryGetAction(KeyData key, bool isDown, out ActionEventArgs args)
        {
            List<KeyMap> m;

            if (!keyToAction.TryGetValue(key.s, out m))
            {
                args = ActionEventArgs.None;
                return false;
            }

            for (int i = 0; i < m.Count; i++)
            {
                KeyMap map = m[i];

                if (isDown)
                {//on key down, we're looking for an action that matches the requirements
                    if (!map.caresAboutModifiers || ((map.requiresShift == key.shift) && (map.requiresCtrl == key.ctrl) && (map.requiresAlt == key.alt)))
                    {
                        map.isDown = true;
                        args = new ActionEventArgs(map.action, FiredBy.BUTTON, isDown, 0, 0);
                        return true;
                    }
                }
                else
                {//on key up, we're looking for an action that has already fired the ondown event
                    if (map.isDown)
                    {
                        map.isDown = false;
                        args = new ActionEventArgs(map.action, FiredBy.BUTTON, isDown, 0, 0);
                        return true;
                    }
                }
            }

            args = ActionEventArgs.None;
            return false;
        }

        /// <summary>
        /// Attempts to bind a key to an action.
        /// Returns true if the action succeeded, false if it didn't.
        /// 
        /// Ways this can fail:
        ///     1. Attempting to bind a key to an action that doesn't support keys.
        /// </summary>
        public bool addKeyAction(Scancode s, bool shift, bool ctrl, bool alt, ActionType action)
        {
            if (!Action.ActionSupportsButton(action))
                return false;

            bool caresAboutMods = Action.ActionCaresAboutModifiers(action);
            List<KeyMap> m;
            KeyMap map = new KeyMap(action, caresAboutMods, shift, ctrl, alt);

            if (!keyToAction.TryGetValue(s, out m))
            {
                m = new List<KeyMap>();
                m.Add(map);
                keyToAction.Add(s, m);
                input.onBindingChange(s, map, true);
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
                    unbindKey(s, m[0]);
                }

                m.Add(map);
                input.onBindingChange(s, map, true);
                return true;
            }

            if (m.Count == 1 && !m[0].caresAboutModifiers)
            {//the action already bound here doesn't care about modifiers, so we have to unbind it
                unbindKey(s, m[0]);
                m.Add(map);
                input.onBindingChange(s, map, true);
                return true;
            }

            //none of the actions ignore modifiers, so check for any exact conflicts
            for (int i = 0; i < m.Count; i++)
            {
                if ((shift == m[i].requiresShift) && (ctrl == m[i].requiresCtrl) && (alt == m[i].requiresAlt))
                {//we found an exact conflict, so remove this key
                    unbindKey(s, m[i]);

                    //there can't be two conflicts, so we're done here.
                    break;
                }
            }

            //either we didn't find any conflicts, or we did and removed it
            m.Add(map);
            input.onBindingChange(s, map, true);
            return true;
        }

        private void unbindKey(Scancode s, KeyMap m)
        {
            List<KeyMap> list;

            if (!keyToAction.TryGetValue(s, out list))
            {
                Logger.WriteLine(LogType.POSSIBLE_ERROR, "Unbind Key was called, but we can't find the key binding: " + s);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == m)
                {
                    list.RemoveAt(i);
                    input.onBindingChange(s, m, false);
                    return;
                }
            }

            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Unbind Key was called, but we can't find the exact key map: " + s);
        }
        #endregion

        #region File IO
        public void WriteToFile(string path)
        {
            using (StreamWriter wr = new StreamWriter(new FileStream(path, FileMode.Create), Encoding.Unicode))
            {
                foreach (var ent in keyToAction)
                {
                    foreach (var map in ent.Value)
                    {
                        wr.WriteLine("K," + ent.Key.ToString() + "," + map.requiresShift + "," + map.requiresCtrl + "," + map.requiresAlt + "," + map.action.ToString());
                    }
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
                    string[] parts = line.Split(',');

                    if (parts.Length == 0)
                    {
                        Logger.WriteLine(LogType.POSSIBLE_ERROR, "Bindings config has an empty line?");
                        continue;
                    }

                    for (int i = 0; i < parts.Length; i++)
                    {
                        parts[i] = parts[i].Trim();
                    }

                    if (parts[0] == "K")
                    {//key mapping
                        if (parts.Length != 6)
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Key binding config has incorrect number of parts: " + line);
                            continue;
                        }

                        Scancode s;
                        bool shift, ctrl, alt;
                        ActionType action;

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

                        if (!Enum.TryParse(parts[5], out action))
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Can't find action value in a key binding line: " + parts[5]);
                            continue;
                        }

                        addKeyAction(s, shift, ctrl, alt, action);
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
        public List<KeyMap> getActionBinds(ActionType action)
        {
            List<KeyMap> toReturn = new List<KeyMap>();

            //We're just looping through the entire key map, as we don't have a reverse acceleration structure.
            //This should be fast enough, given we only expect to call this ~once per run.
            foreach (var ent in keyToAction)
            {
                foreach (var map in ent.Value)
                {
                    if (map.action == action)
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
