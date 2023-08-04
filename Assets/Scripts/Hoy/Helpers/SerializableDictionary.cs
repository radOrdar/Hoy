using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hoy.Helpers
{
    [Serializable]
    public class SerializableDictionary<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<K> m_Keys = new List<K>();

        [SerializeField]
        private List<V> m_Values = new List<V>();

        public void OnBeforeSerialize()
        {
            if (m_Values.Count != m_Keys.Count)
                return;
            m_Keys.Clear();
            m_Values.Clear();

            foreach (var kvp in this)
            {
                m_Keys.Add(kvp.Key);
                m_Values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            if (m_Values.Count != m_Keys.Count)
                return;
            Clear();
            for (int i = 0; i < m_Keys.Count; i++)
                Add(m_Keys[i], m_Values[i]);

            m_Keys.Clear();
            m_Values.Clear();
        }
    }
}
