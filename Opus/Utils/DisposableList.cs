using System;
using System.Collections.Generic;

namespace Opus
{
    public class DisposableList<T> : List<T>, IDisposable where T : IDisposable
    {
        public DisposableList()
            : base()
        {
        }

        public DisposableList(int capacity)
            : base(capacity)
        {
        }

        public DisposableList(IEnumerable<T> items)
            : base(items)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var item in this)
                {
                    item.Dispose();
                }

                Clear();
            }
        }

        public new T Add(T item)
        {
            base.Add(item);

            return item;
        }
    }
}
