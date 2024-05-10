using System;
using System.Collections.Generic;

namespace Opus
{
    public class LoopingCoroutine<T>
    {
        private Func<IEnumerable<T>> m_func;
        private IEnumerator<T> m_enumerator;

        public LoopingCoroutine(Func<IEnumerable<T>> func)
        {
            m_func = func;
        }

        public T Next()
        {
            if (m_enumerator == null || !m_enumerator.MoveNext())
            {
                m_enumerator = m_func().GetEnumerator();
                m_enumerator.MoveNext();
            }

            return m_enumerator.Current;
        }

        public void Reset()
        {
            m_enumerator = null;
        }
    }
}
