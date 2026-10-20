using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// Allocation-free replacement for <c>list.ToArray()</c> when iterating units while the callee may reuse the same
    /// scratch list (Deal → reaction → AoE …). Copies into a pooled list; foreach disposes the enumerator, which
    /// returns the list to the World's pool.
    /// </summary>
    public readonly struct UnitSnapshot
    {
        private readonly List<Unit> _list;
        private readonly World _w;

        public UnitSnapshot(List<Unit> src, World w)
        {
            _w = w;
            _list = w.RentList();
            _list.AddRange(src);
        }

        public int Count => _list.Count;
        public Enumerator GetEnumerator() => new Enumerator(_list, _w);

        public struct Enumerator : IDisposable
        {
            private readonly List<Unit> _l;
            private readonly World _w;
            private int _i;

            public Enumerator(List<Unit> l, World w) { _l = l; _w = w; _i = -1; }
            public Unit Current => _l[_i];
            public bool MoveNext() => ++_i < _l.Count;
            public void Dispose() => _w.ReturnList(_l);
        }
    }

    public static class UnitSnapshotExt
    {
        public static UnitSnapshot Snapshot(this List<Unit> src, World w) => new UnitSnapshot(src, w);
    }
}
