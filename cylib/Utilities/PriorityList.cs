using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using cyUtility;

namespace cylib
{
    public struct Pair<T, V>
    {
        public readonly bool hasVal1;
        public readonly T val1;
        public readonly V val2;

        public Pair(T val1)
        {
            hasVal1 = true;
            this.val1 = val1;
            val2 = default(V);
        }

        public Pair(V val2)
        {
            hasVal1 = false;
            val1 = default(T);
            this.val2 = val2;
        }
    }

    class PriorityList<T> : IEnumerable<T>
    {
        //we really only need the int -> List<T> map, but the rest are for ~constant time cleanup
        //if we ever have enough input consumers that this matters in any way, I'll be surprised.
        readonly Dictionary<T, List<T>> reverseLookup = new Dictionary<T, List<T>>();
        readonly Dictionary<List<T>, int> reverseList = new Dictionary<List<T>, int>();
        readonly SortedDictionary<int, List<T>> listSet = new SortedDictionary<int, List<T>>();

        public PriorityList()
        {

        }

        public void Clear()
        {
            reverseLookup.Clear();
            reverseList.Clear();
            listSet.Clear();
        }

        public void AddAll(PriorityList<T> otherList)
        {
            foreach (var ent in otherList.listSet)
            {
                foreach (T t in ent.Value)
                {
                    addElement(ent.Key, t);
                }
            }
        }

        public void addElement(int priority, T val)
        {
            List<T> list;

            if (!listSet.TryGetValue(priority, out list))
            {
                list = new List<T>();
                listSet.Add(priority, list);
                reverseList.Add(list, priority);
            }

            if (reverseLookup.ContainsKey(val))
                throw new Exception("You're adding an element that already exists at a different priority. Use change priority instead.");

            list.Add(val);
            reverseLookup.Add(val, list);
        }

        public void changePriority(int priority, T val)
        {
            //this could just be two lines of remove, add
            //we go through the trouble of duplicating removal code to possible-error-check to see if we're changing priority to what it already was
            //could theoretically save runtime, too, I guess.
            List<T> list;

            if (reverseLookup.TryGetValue(val, out list))
            {
                int prevPri = reverseList[list];

                if (prevPri == priority)
                {
                    Logger.WriteLine(LogType.POSSIBLE_ERROR, "Attempting to change priority of an element to the same priority it already has: " + val.ToString());
                    return;
                }

                list.Remove(val);
                reverseLookup.Remove(val);

                if (list.Count == 0)
                {
                    reverseList.Remove(list);
                    listSet.Remove(prevPri);
                }

                //and now we just be lazy and call this
                addElement(priority, val);
            }
            else
            {
                Logger.WriteLine(LogType.POSSIBLE_ERROR, "Attempting to change priority of an element in a priority list that isn't in the list: " + val.ToString());
            }
        }

        public void removeElement(T val)
        {
            List<T> list;

            if (reverseLookup.TryGetValue(val, out list))
            {
                list.Remove(val);
                reverseLookup.Remove(val);

                if (list.Count == 0)
                {
                    int p = reverseList[list];
                    reverseList.Remove(list);
                    listSet.Remove(p);
                }
            }
            else
            {
                Logger.WriteLine(LogType.POSSIBLE_ERROR, "Attempting to remove an element from a priority list that isn't in the list: " + val.ToString());
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            //Hey!
            //If you get an access violation here, what probably happened was that something changed/added/removed to the priority list during an event
            //but then DIDN'T return true, stopping the event from continuing.
            //
            //Return true if you use the event! Dang!
            foreach (var vals in listSet)
            {
                foreach (T t in vals.Value)
                {
                    yield return t;
                }
            }
        }

        public IEnumerator<(int priority, T val)> GetEnumeratorWithPriority()
        {//useful if you want to check this with, like a single other action without inserting crap
            foreach (var b in listSet)
            {
                int p = b.Key;
                foreach (var t in b.Value)
                {
                    yield return (p, t);
                }
            }
        }

        public IEnumerable<Pair<T, V>> Union<V>(PriorityList<V> otherList)
        {
            if (otherList == null)
            {
                foreach (T t in this)
                {
                    yield return new Pair<T, V>(t);
                }
                yield break;
            }
            var keyEnum = listSet.GetEnumerator();
            var actionEnum = otherList.listSet.GetEnumerator();

            if (!keyEnum.MoveNext())
            {//we don't have any keys
                if (!actionEnum.MoveNext())
                    yield break; //we dont have anything, rip

                do
                {//drain the action list
                    var actionList = actionEnum.Current;
                    foreach (V v in actionList.Value)
                    {
                        yield return new Pair<T, V>(v);
                    }
                } while (actionEnum.MoveNext());

                yield break;
            }

            //we do have keys!
            if (!actionEnum.MoveNext())
            {//but we dont have any actions
                do
                {//just drain the keyList
                    var keyList = keyEnum.Current;
                    foreach (T t in keyList.Value)
                    {
                        yield return new Pair<T, V>(t);
                    }
                } while (keyEnum.MoveNext());

                yield break;
            }

            //okay, we have crap in both lists
            while (true)
            {
                var keyList = keyEnum.Current;
                var actionList = actionEnum.Current;

                if (keyList.Key <= actionList.Key)
                {//key pri is lower, so drain the current key set first
                    foreach (T t in keyList.Value)
                    {
                        yield return new Pair<T, V>(t);
                    }

                    if (!keyEnum.MoveNext())
                    {//we're completely out key of data
                        //so just empty all of the action list
                        do
                        {//drain the action list
                            actionList = actionEnum.Current;
                            foreach (V v in actionList.Value)
                            {
                                yield return new Pair<T, V>(v);
                            }
                        } while (actionEnum.MoveNext());

                        yield break;
                    }
                }
                else
                {//the other pri is lower, so go through their current first
                    foreach (V v in actionList.Value)
                    {
                        yield return new Pair<T, V>(v);
                    }

                    if (!actionEnum.MoveNext())
                    {//we're completely out of action data
                        //so just empty all of the key list
                        do
                        {
                            keyList = keyEnum.Current;
                            foreach (T t in keyList.Value)
                            {
                                yield return new Pair<T, V>(t);
                            }
                        } while (keyEnum.MoveNext());

                        yield break;//we're done
                    }
                }
            }//end of while(true)
            //should never get here
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
