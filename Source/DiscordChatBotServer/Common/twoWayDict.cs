using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordChatBotServer.Common
{
    public class TwoWayDict<K, V>
    {
        private Dictionary<K, V> _dictKV;
        private Dictionary<V, K> _dictVK;
        private ReverseDict _reverseDict;

        public TwoWayDict()
        {
            _dictKV = new Dictionary<K, V>();
            _dictVK = new Dictionary<V, K>();
            _reverseDict = new ReverseDict(this);
        }

        public ReverseDict Reverse
        {
            get { return _reverseDict; }
        }

        // TwoWayDict[key] -> value
        public V this[K key]
        {
            get { return _dictKV[key]; }
            set
            {
                // Remove any existing key/value pair
                Remove(key);

                _dictKV[key] = value;
                _dictVK[value] = key;
            }
        }

        public void Remove(K key)
        {
            if (_dictKV.ContainsKey(key))
            {
                _dictVK.Remove(_dictKV[key]);
                _dictKV.Remove(key);
            }
        }

        // Wrapper that allows TwoWayDict to expose a convenient
        // 'Reverse' property.
        public class ReverseDict
        {
            private TwoWayDict<K, V> _parent;
            public ReverseDict(TwoWayDict<K, V> parent)
            {
                _parent = parent;
            }

            public K this[V reverseKey]
            {
                get { return _parent._dictVK[reverseKey]; }
                set { _parent[value] = reverseKey; }
            }

            public void Remove(V value)
            {
                if (_parent._dictVK.ContainsKey(value))
                {
                    _parent.Remove(_parent._dictVK[value]);
                }
            }
        }
    }
}
