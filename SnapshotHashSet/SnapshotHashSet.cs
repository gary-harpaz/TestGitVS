using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SnapshotHashSet
{
    public class SnapshotHashSet<T>
    {
        private readonly object  _lock = new object();
        private readonly List<SnapshotData<T>> _snapshots;
        private readonly HashSet<T> _master = new HashSet<T>();
        private int _numLooping = 0;
        private int _snapshotNumber;
        public SnapshotHashSet(int snapshot_number)
        {
            _snapshotNumber = snapshot_number;
            for (int i = 0; i < _snapshotNumber; i++)
            {
                _snapshots.Add(new SnapshotData<T>());
            }
        }

        private class SnapshotData<T>
        {
            public readonly HashSet<T> HashSet = new HashSet<T>();
            public bool IsItrerating;
            public readonly Queue<SetAction> Queue = new Queue<SetAction>();

        }
        private struct SetAction
        {
            public bool Remove ;
            public T Item;
        }

        public bool AddIfNotExists(T item)
        {           

            if (!_master.Contains(item))
            {
                //most of the time the master will contain the item
                lock (_lock)
                {
                    bool result = false;
                    if (!_master.Contains(item))
                    {
                        _master.Add(item);
                        for (int i = 0; i < _snapshotNumber; i++)
                        {
                            SnapshotData<T> sd = _snapshots[i];
                            if (sd.IsItrerating)
                            {
                                sd.Queue.Enqueue(new SetAction() { Item = item, Remove = false });
                            }
                            else
                            {
                                sd.HashSet.Add(item);
                            }
                        }
                        result = true;
                    }
                    return result;
                }
            }
            return false;
        }

        public void Iterate(Action<T> action)
        {
            SnapshotData<T> set = null;
            lock (_lock)
            {
                while (_numLooping >= _snapshotNumber)
                    Monitor.Wait(_lock);
                _numLooping++;
                for (int i = 0; i < _snapshotNumber; i++)
                {
                    set = _snapshots[i];
                    if (!set.IsItrerating)
                    {
                        set.IsItrerating=true;
                        break;
                    }
                }
            }
            foreach (T k in set.HashSet)
            {
                action(k);
            }


            lock (_lock)
            {
                _numLooping--;
                set.IsItrerating = false;
                while (set.Queue.Count > 0)
                {
                    SetAction sa = set.Queue.Dequeue();
                    if (!sa.Remove)
                        set.HashSet.Add(sa.Item);
                    else
                        set.HashSet.Remove(sa.Item);
                }
                Monitor.Pulse(_lock);
            }
        }
       

        public bool RemoveIfNotExists(T item)
        {
            bool remove = true;
            return false;
            //return AddOrRemoveIfNotExists(item,remove);
        }

      

        
    }

}
