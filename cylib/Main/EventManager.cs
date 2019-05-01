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

        private readonly PriorityList<OnFocusChange> e_focusChange = new PriorityList<OnFocusChange>();
        public IEnumerable<OnFocusChange> focusChangeList
        {
            get
            {
                return e_focusChange;
            }
        }

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

        private readonly PriorityList<OnAction> e_Action = new PriorityList<OnAction>();
        public IEnumerable<OnAction> actionList
        {
            get
            {
                return e_Action;
            }
        }
        public IEnumerable<Pair<OnKeyChange, OnAction>> keyActionList
        {
            get
            {
                return e_keyChange.Union(e_Action);
            }
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

        public EventManager()
        {

        }

        public void Clear()
        {
            drawMRTs.Clear();
            drawPostProcs.Clear();
            draw2Ds.Clear();
            pointLights.Clear();
            updaters.Clear();

            e_pointerChange.Clear();
            e_focusChange.Clear();
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

        public void addEventHandler(int priority, OnAction e)
        {
            e_Action.addElement(priority, e);
        }

        public void changePriority(int newPriority, OnAction e)
        {
            e_Action.changePriority(newPriority, e);
        }

        public void removeEventHandler(OnAction e)
        {
            e_Action.removeElement(e);
        }

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
