using log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cylib
{
    public class EventManager
    {
        #region List Declaration
        private readonly PriorityList<OnPointerChange> e_pointerChange = new PriorityList<OnPointerChange>();
        public IEnumerable<OnPointerChange> pointerChangeList
        {
            get
            {
                return e_pointerChange;
            }
        }

        /* I'm not actually sure why this specifically existed -- input events stop on
        private readonly PriorityList<OnFocusChange> e_focusChange = new PriorityList<OnFocusChange>();
        public IEnumerable<OnFocusChange> focusChangeList
        {
            get
            {
                return e_focusChange;
            }
        }
        */

        private readonly PriorityList<OnAxisMove> e_axisMove = new PriorityList<OnAxisMove>();
        public IEnumerable<OnAxisMove> axisMoveList
        {
            get
            {
                return e_axisMove;
            }
        }

        private readonly PriorityList<OnTriggerMove> e_triggerMove = new PriorityList<OnTriggerMove>();
        public IEnumerable<OnTriggerMove> triggerMoveList
        {
            get
            {
                return e_triggerMove;
            }
        }

        private readonly PriorityList<OnKeyChange> e_keyChange = new PriorityList<OnKeyChange>();
        public IEnumerable<OnKeyChange> keyChangeList
        {
            get
            {
                return e_keyChange;
            }
        }

        private readonly Dictionary<string, PriorityList<OnAction>> e_Action = new Dictionary<string, PriorityList<OnAction>>();
        public IEnumerable<OnAction> ActionList(string name)
        {
            if (e_Action.TryGetValue(name, out var actionList))
            {
                foreach (var a in actionList)
                {
                    yield return a;
                }
            }
            else
                yield break;
        }
        public IEnumerable<Pair<OnKeyChange, OnAction>> KeyActionList(string name)
        {
            if (e_Action.TryGetValue(name, out var actionList))
            {
                return e_keyChange.Union(actionList);
            }
            return e_keyChange.Union<OnAction>(null);
        }
        public IEnumerable<Pair<OnPointerChange, OnAction>> PointerActionList(string name)
        {
            if (e_Action.TryGetValue(name, out var actionList))
            {
                return e_pointerChange.Union(actionList);
            }
            return e_pointerChange.Union<OnAction>(null);
        }


        private readonly List<PointLight> pointLights = new List<PointLight>();
        public IEnumerable<PointLight> pointLightList
        {
            get
            {
                return pointLights;
            }
        }

        private readonly List<DirectionalLight> directionalLights = new List<DirectionalLight>();
        public IEnumerable<DirectionalLight> directionalLightList
        {
            get
            {
                return directionalLights;
            }
        }

        private readonly PriorityList<DrawDelegate> drawMRTs = new PriorityList<DrawDelegate>();
        public IEnumerable<DrawDelegate> drawMRTList
        {
            get
            {
                return drawMRTs;
            }
        }

        private readonly PriorityList<DrawDelegate> drawPostProcs = new PriorityList<DrawDelegate>();
        public IEnumerable<DrawDelegate> drawPostProcList
        {
            get
            {
                return drawPostProcs;
            }
        }

        private readonly PriorityList<DrawDelegate> draw2Ds = new PriorityList<DrawDelegate>();
        public IEnumerable<DrawDelegate> draw2DList
        {
            get
            {
                return draw2Ds;
            }
        }

        private readonly PriorityList<UpdateDelegate> updaters = new PriorityList<UpdateDelegate>();
        public IEnumerable<UpdateDelegate> updateList
        {
            get
            {
                return updaters;
            }
        }
        #endregion

        private InputHandler input;

        public EventManager(InputHandler input)
        {
            this.input = input;
        }

        public void StartTyping(OnTextInput callback)
        {
            input.StartTyping(callback);
        }

        public void StopTyping()
        {
            input.StopTyping();
        }

        public void Clear()
        {
            drawMRTs.Clear();
            drawPostProcs.Clear();
            draw2Ds.Clear();
            pointLights.Clear();
            updaters.Clear();

            e_pointerChange.Clear();
            //e_focusChange.Clear();
            e_axisMove.Clear();
            e_triggerMove.Clear();
            e_keyChange.Clear();
            e_Action.Clear();
        }

        public void addDrawMRT(int priority, DrawDelegate d)
        {
            drawMRTs.addElement(priority, d);
        }

        public void changeMRT(int priority, DrawDelegate d)
        {
            drawMRTs.changePriority(priority, d);
        }

        public void removeMRT(DrawDelegate d)
        {
            drawMRTs.removeElement(d);
        }

        public void addDrawPostProc(int priority, DrawDelegate d)
        {
            drawPostProcs.addElement(priority, d);
        }

        public void changePostProc(int priority, DrawDelegate d)
        {
            drawPostProcs.changePriority(priority, d);
        }

        public void removePostProc(DrawDelegate d)
        {
            drawPostProcs.removeElement(d);
        }

        public void addDraw2D(int priority, DrawDelegate d)
        {
            draw2Ds.addElement(priority, d);
        }

        public void change2D(int priority, DrawDelegate d)
        {
            draw2Ds.changePriority(priority, d);
        }

        public void remove2D(DrawDelegate d)
        {
            draw2Ds.removeElement(d);
        }

        public void addLight(PointLight light)
        {
            pointLights.Add(light);
        }

        public void removeLight(PointLight light)
        {
            pointLights.Remove(light);
        }

        public void addLight(DirectionalLight light)
        {
            directionalLights.Add(light);
        }

        public void removeLight(DirectionalLight light)
        {
            directionalLights.Remove(light);
        }

        public void addUpdateListener(int priority, UpdateDelegate d)
        {
            updaters.addElement(priority, d);
        }

        public void changeUpdateListener(int priority, UpdateDelegate d)
        {
            updaters.changePriority(priority, d);
        }

        public void removeUpdateListener(UpdateDelegate d)
        {
            updaters.removeElement(d);
        }

        public void addEventHandler(int priority, OnPointerChange e)
        {
            e_pointerChange.addElement(priority, e);
        }

        public void changePriority(int newPriority, OnPointerChange e)
        {
            e_pointerChange.changePriority(newPriority, e);
        }

        public void removeEventHandler(OnPointerChange e)
        {
            e_pointerChange.removeElement(e);
        }

        public void addEventHandler(int priority, string action, OnAction e)
        {
            if (!e_Action.TryGetValue(action, out var pList))
            {
                pList = new PriorityList<OnAction>();
            }

            pList.addElement(priority, e);
            e_Action.Add(action, pList);
        }

        public void changePriority(int newPriority, string action, OnAction e)
        {
            if (!e_Action.TryGetValue(action, out var pList))
            {
                Logger.WriteLine(LogType.POSSIBLE_ERROR, "Attempting to change priority of an action not in the action list: " + action);
            }

            pList.changePriority(newPriority, e);
        }

        public void removeEventHandler(string action, OnAction e)
        {
            if (!e_Action.TryGetValue(action, out var pList))
            {
                Logger.WriteLine(LogType.POSSIBLE_ERROR, "Attempting to remove handler of an action not in the action list: " + action);
            }

            pList.removeElement(e);
        }
        
        /*
        public void addEventHandler(int priority, OnFocusChange e)
        {
            e_focusChange.addElement(priority, e);
        }

        public void changePriority(int newPriority, OnFocusChange e)
        {
            e_focusChange.changePriority(newPriority, e);
        }

        public void removeEventHandler(OnFocusChange e)
        {
            e_focusChange.removeElement(e);
        }
        */

        public void addEventHandler(int priority, OnAxisMove e)
        {
            e_axisMove.addElement(priority, e);
        }

        public void changePriority(int newPriority, OnAxisMove e)
        {
            e_axisMove.changePriority(newPriority, e);
        }

        public void removeEventHandler(OnAxisMove e)
        {
            e_axisMove.removeElement(e);
        }

        public void addEventHandler(int priority, OnTriggerMove e)
        {
            e_triggerMove.addElement(priority, e);
        }

        public void changePriority(int newPriority, OnTriggerMove e)
        {
            e_triggerMove.changePriority(newPriority, e);
        }

        public void removeEventHandler(OnTriggerMove e)
        {
            e_triggerMove.removeElement(e);
        }

        public void addEventHandler(int priority, OnKeyChange e)
        {
            e_keyChange.addElement(priority, e);
        }

        public void changePriority(int newPriority, OnKeyChange e)
        {
            e_keyChange.changePriority(newPriority, e);
        }

        public void removeEventHandler(OnKeyChange e)
        {
            e_keyChange.removeElement(e);
        }
    }
}
