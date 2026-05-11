using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mod.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RawTrackingRef : IDisposable, IIndirectRef
    {
        private RawRef _Ref2Ref;
        private int _SlotData;
        public bool IsValid
        {
            get
            {
                return (_SlotData & 0x100000) != 0;
            }
            private set
            {
                if (value)
                {
                    _SlotData |= 0x100000;
                }
                else
                {
                    _SlotData &= ~0x100000;
                }
            }
        }
        public int Level
        {
            get
            {
                return (_SlotData & 0xFFC00) >> 10;
            }
            private set
            {
                _SlotData &= ~0xFFC00;
                _SlotData |= (value << 10) & 0xFFC00;
            }
        }
        public int Slot
        {
            get
            {
                return _SlotData & 0x3FF;
            }
            private set
            {
                _SlotData &= ~0x3FF;
                _SlotData |= value & 0x3FF;
            }
        }
        public RawRef Ref2Ref => _Ref2Ref;

        public void Dispose()
        {
            if (IsValid)
            {
                var list = LevelList;
                for (int i = 0; i < list.Count; ++i)
                {
                    var lman = list[i];
                    if (lman.Index == Level)
                    {
                        lman.ReturnEmptySlot(Slot);
                        if (lman.OccupiedCount <= 0)
                        {
                            list.RemoveAt(i);
                            lman.Dispose();
                        }
                        break;
                    }
                }
                IsValid = false;
            }
        }
        public static RawTrackingRef Create()
        {
            RawTrackingRef raw = new RawTrackingRef();
            var list = LevelList;
            for (int i = 0; i < list.Count; ++i)
            {
                var lman = list[i];
                int slot;
                if (lman.OccupiedCount < 1024 && (slot = lman.OccupyEmptySlot()) >= 0)
                {
                    raw.Level = lman.Index;
                    raw.Slot = slot;
                    raw.IsValid = true;
                    raw._Ref2Ref = TrackingRefManager.GlobalManager.GetSlotRef(raw.Level, raw.Slot);
                    return raw;
                }
            }
            var level = TrackingRefManager.GlobalManager.OccupyEmptyLevel();
            if (level >= 0)
            {
                LeveledTrackingRefManager lman;
                list.Add(lman = new LeveledTrackingRefManager() { Index = level });
                int slot = lman.OccupyEmptySlot();
                raw.Level = level;
                raw.Slot = slot;
                raw.IsValid = true;
                raw._Ref2Ref = TrackingRefManager.GlobalManager.GetSlotRef(raw.Level, raw.Slot);
                return raw;
            }

            return raw;
        }

        [ThreadStatic]
        private static List<LeveledTrackingRefManager> _LevelList;
        private static List<LeveledTrackingRefManager> LevelList
        {
            get
            {
                if (_LevelList == null)
                {
                    _LevelList = new List<LeveledTrackingRefManager>();
                }
                return _LevelList;
            }
        }

        public void SetRef<T>(ref T r)
        {
            //if (IsValid)
            //{
            //    TrackingRefManager.GlobalManager.SetRef(Level, Slot, ref r);
            //}
            ref RawRef rd = ref _Ref2Ref.GetRef<RawRef>();
            rd.SetRef<T>(ref r);
        }
        public ref T GetRef<T>()
        {
            //if (IsValid)
            //{
            //    return ref TrackingRefManager.GlobalManager.GetRef<T>(Level, Slot);
            //}
            //return ref Ref.GetEmptyRef<T>();
            ref RawRef r = ref _Ref2Ref.GetRef<RawRef>();
            for (; ; )
            {
                var oldAddr = r.Address;
                ref T rv = ref r.GetRef<T>();
                var newAddr = r.Address;
                if (oldAddr == newAddr)
                {
                    return ref rv;
                }
            }
        }
        public void SetValue<T>(T val)
        {
            GetRef<T>() = val;
        }
        public T GetValue<T>()
        {
            return GetRef<T>();
        }

        public IntPtr Address
        {
            get
            {
                //ref int r = ref GetRef<int>();
                //RawRef rr = new RawRef();
                //rr.SetRef(ref r);
                //return rr.Address;
                var r = _Ref2Ref.GetValue<IntPtr>();
                return r;
            }
        }

        // I decide not to implement convert operator to the indirect ref.

        public override bool Equals(object obj)
        {
            if (obj is IIndirectRef r)
            {
                return _Ref2Ref == r.Ref2Ref;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return _Ref2Ref.GetHashCode();
        }
        public override string ToString()
        {
            return _Ref2Ref.Address.ToString("X") + " -> " + Address.ToString("X");
        }

        public static bool operator ==(RawTrackingRef r1, RawTrackingRef r2)
        {
            return r1._Ref2Ref == r2._Ref2Ref;
        }
        public static bool operator !=(RawTrackingRef r1, RawTrackingRef r2)
        {
            return r1._Ref2Ref != r2._Ref2Ref;
        }
        public static bool operator ==(RawTrackingRef r1, IIndirectRef r2)
        {
            return r1._Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(RawTrackingRef r1, IIndirectRef r2)
        {
            return r1._Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(IIndirectRef r1, RawTrackingRef r2)
        {
            return r1.Ref2Ref == r2._Ref2Ref;
        }
        public static bool operator !=(IIndirectRef r1, RawTrackingRef r2)
        {
            return r1.Ref2Ref != r2._Ref2Ref;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawTrackingRef<T> : IDisposable, IIndirectRef<T>
    {
        private RawTrackingRef _BaseRef;
        public RawRef Ref2Ref => _BaseRef.Ref2Ref;

        public void Dispose()
        {
            _BaseRef.Dispose();
        }
        public static RawTrackingRef<T> Create()
        {
            RawTrackingRef<T> raw = new RawTrackingRef<T>();
            raw._BaseRef = RawTrackingRef.Create();
            return raw;
        }
        public void SetRef(ref T r)
        {
            _BaseRef.SetRef<T>(ref r);
        }
        public ref T GetRef()
        {
            return ref _BaseRef.GetRef<T>();
        }
        public void SetValue(T val)
        {
            _BaseRef.SetValue<T>(val);
        }
        public T GetValue()
        {
            return _BaseRef.GetValue<T>();
        }
        public IntPtr Address
        {
            get
            {
                var r = _BaseRef.Address;
                return r;
            }
        }

        public ref T R
        {
            get => ref GetRef();
        }
        public T Value
        {
            get { return GetValue(); }
            set { SetValue(value); }
        }

        // I decide not to implement convert operator to the indirect ref.

        public override bool Equals(object obj)
        {
            if (obj is IIndirectRef r)
            {
                return Ref2Ref == r.Ref2Ref;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Ref2Ref.GetHashCode();
        }
        public override string ToString()
        {
            return Ref2Ref.Address.ToString("X") + " -> " + Address.ToString("X");
        }

        public static bool operator ==(RawTrackingRef<T> r1, RawTrackingRef<T> r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(RawTrackingRef<T> r1, RawTrackingRef<T> r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(RawTrackingRef<T> r1, RawTrackingRef r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(RawTrackingRef<T> r1, RawTrackingRef r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(RawTrackingRef r1, RawTrackingRef<T> r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(RawTrackingRef r1, RawTrackingRef<T> r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(RawTrackingRef<T> r1, IIndirectRef r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(RawTrackingRef<T> r1, IIndirectRef r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(IIndirectRef r1, RawTrackingRef<T> r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(IIndirectRef r1, RawTrackingRef<T> r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class TrackingRef : IDisposable, IIndirectRef
    {
        private RawTrackingRef _Ref;
        public RawRef Ref2Ref => _Ref.Ref2Ref;

        public TrackingRef()
        {
            _Ref = RawTrackingRef.Create();
        }

        #region IDisposable Support
        private bool _Disposed = false; // 要检测冗余调用
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                _Disposed = true;
                _Ref.Dispose();
            }
        }
        ~TrackingRef()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }
        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        public IntPtr Address { get { return _Ref.Address; } }

        public void SetRef<T>(ref T r)
        {
            _Ref.SetRef(ref r);
        }
        public ref T GetRef<T>()
        {
            return ref _Ref.GetRef<T>();
        }
        public void SetValue<T>(T val)
        {
            _Ref.SetValue<T>(val);
        }
        public T GetValue<T>()
        {
            return _Ref.GetValue<T>();
        }

        public static void Close()
        {
            TrackingRefManager.GlobalManager.Dispose();
        }

        // I decide not to implement convert operator to the indirect ref.

        public override bool Equals(object obj)
        {
            if (obj is IIndirectRef r)
            {
                return Ref2Ref == r.Ref2Ref;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Ref2Ref.GetHashCode();
        }
        public override string ToString()
        {
            return Ref2Ref.Address.ToString("X") + " -> " + Address.ToString("X");
        }

        public static bool operator ==(TrackingRef r1, TrackingRef r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(TrackingRef r1, TrackingRef r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(TrackingRef r1, IIndirectRef r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(TrackingRef r1, IIndirectRef r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(IIndirectRef r1, TrackingRef r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(IIndirectRef r1, TrackingRef r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public class TrackingRef<T> : IDisposable, IIndirectRef<T>
    {
        private RawTrackingRef _Ref;
        public RawRef Ref2Ref => _Ref.Ref2Ref;
        public TrackingRef()
        {
            _Ref = RawTrackingRef.Create();
        }

        #region IDisposable Support
        private bool _Disposed = false; // 要检测冗余调用
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                _Disposed = true;
                _Ref.Dispose();
            }
        }
        ~TrackingRef()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }
        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        public IntPtr Address { get { return _Ref.Address; } }
        
        public void SetRef(ref T r)
        {
            _Ref.SetRef(ref r);
        }
        public ref T GetRef()
        {
            return ref _Ref.GetRef<T>();
        }
        public void SetValue(T val)
        {
            GetRef() = val;
        }
        public T GetValue()
        {
            return GetRef();
        }

        public ref T R
        {
            get => ref GetRef();
        }
        public T Value
        {
            get { return _Ref.GetValue<T>(); }
            set { _Ref.SetValue<T>(value); }
        }

        // I decide not to implement convert operator to the indirect ref.

        public override bool Equals(object obj)
        {
            if (obj is IIndirectRef r)
            {
                return Ref2Ref == r.Ref2Ref;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Ref2Ref.GetHashCode();
        }
        public override string ToString()
        {
            return Ref2Ref.Address.ToString("X") + " -> " + Address.ToString("X");
        }

        public static bool operator ==(TrackingRef<T> r1, TrackingRef<T> r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(TrackingRef<T> r1, TrackingRef<T> r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(TrackingRef<T> r1, TrackingRef r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(TrackingRef<T> r1, TrackingRef r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(TrackingRef r1, TrackingRef<T> r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(TrackingRef r1, TrackingRef<T> r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(TrackingRef<T> r1, IIndirectRef r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(TrackingRef<T> r1, IIndirectRef r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(IIndirectRef r1, TrackingRef<T> r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(IIndirectRef r1, TrackingRef<T> r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
    }

    internal partial class TrackingRefManager : IDisposable
    {
        protected struct TrackingRefManagerOp
        {
            public Action<IntPtr> OnComplete;
            public bool IsPop;
            public bool IsExit;
        }
        protected class TrackingRefManagerThreadWorkInput
        {
            public ConcurrentQueue<TrackingRefManagerOp> Ops;
            public AutoResetEvent WaitHandle;
        }
        protected ConcurrentQueue<TrackingRefManagerOp> _StackOps = new ConcurrentQueue<TrackingRefManagerOp>();
        protected AutoResetEvent _WaitForStackOp = new AutoResetEvent(false);
        protected Thread _Thread;

        public TrackingRefManager()
        {
            _Thread = new Thread(ThreadWork);
            _Thread.IsBackground = true;
            _Thread.Start(new TrackingRefManagerThreadWorkInput() { Ops = _StackOps, WaitHandle = _WaitForStackOp });
        }

        protected static void ThreadWork(object state)
        {
            TrackingRefManagerThreadWorkInput input = state as TrackingRefManagerThreadWorkInput;
            try
            {
                while (true)
                {
                    input.WaitHandle.WaitOne();
                    TrackingRefManagerOp op;
                    while (input.Ops.TryDequeue(out op))
                    {
                        if (op.IsPop || op.IsExit)
                        {
                            return;
                        }
                        else
                        {
                            MakeMoreSlot(input, op.OnComplete);
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            finally
            {
                TrackingRefManagerOp op;
                while (input.Ops.TryDequeue(out op))
                {
                    op.OnComplete?.Invoke(IntPtr.Zero);
                }
                input.WaitHandle.Dispose();
            }
        }

        public void EnqueueGrowWork(Action<IntPtr> onComplete)
        {
            _StackOps.Enqueue(new TrackingRefManagerOp()
            {
                OnComplete = onComplete
            });
            _WaitForStackOp.Set();
        }

        public static int GetStackDir()
        {
            RawRef r1 = new RawRef();
            r1.SetRef(ref r1);
            RawRef r2 = new RawRef();
            r2.SetRef(ref r2);
            return (int)(((long)r2.Address) - ((long)r1.Address));
        }

        public static readonly TrackingRefManager GlobalManager = new TrackingRefManager();
        
        internal struct LevelInfo
        {
            public volatile IntPtr BaseAddress;
            public volatile int IsInUsing;
        }
        internal readonly LevelInfo[] Levels = new LevelInfo[1024];
        internal volatile int FilledLevelCount = 0;

        internal int OccupyEmptyLevel()
        {
            for (int i = 0; i < FilledLevelCount && i < Levels.Length; ++i)
            {
                if (Interlocked.CompareExchange(ref Levels[i].IsInUsing, 1, 0) == 0)
                {
                    if (Levels[i].BaseAddress != IntPtr.Zero)
                    {
                        return i;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            while (true)
            {
                var fcnt = FilledLevelCount;
                if (fcnt >= Levels.Length)
                {
                    break;
                }
                if (Interlocked.CompareExchange(ref Levels[fcnt].IsInUsing, 1, 0) == 0)
                {
                    if (Interlocked.CompareExchange(ref FilledLevelCount, fcnt + 1, fcnt) == fcnt)
                    {
                        ManualResetEvent WaitComplete = new ManualResetEvent(false);
                        GlobalManager.EnqueueGrowWork(address =>
                        {
                            Levels[fcnt].BaseAddress = address;
                            WaitComplete.Set();
                        });
                        WaitComplete.WaitOne();
                        WaitComplete.Dispose();
                    }
                    if (Levels[fcnt].BaseAddress != IntPtr.Zero)
                    {
                        return fcnt;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            return -1;
        }
        internal void ReturnEmptyLevel(int level)
        {
            Interlocked.Exchange(ref Levels[level].IsInUsing, 0);
        }

        public void SetRef<T>(int level, int slot, ref T r)
        {
            var baseAddress = Levels[level].BaseAddress;
            if (baseAddress != IntPtr.Zero)
            {
                var slotAddress = baseAddress;
                if (GetStackDir() > 0)
                {
                    slotAddress -= (slot) * IntPtr.Size;
                }
                else
                {
                    slotAddress += (slot) * IntPtr.Size;
                }
                while (true)
                {
                    RawRef source = new RawRef();
                    source.SetRef(ref r);
                    var sourceAddress = source.Address;

                    RawRef dest = new RawRef();
                    dest.Address = slotAddress;
                    dest.SetValue(sourceAddress);

                    source.SetRef(ref r);
                    if (sourceAddress == source.Address)
                    {
                        return;
                    }
                }
            }
        }
        public ref T GetRef<T>(int level, int slot)
        {
            var baseAddress = Levels[level].BaseAddress;
            if (baseAddress != IntPtr.Zero)
            {
                var slotAddress = baseAddress;
                if (GetStackDir() > 0)
                {
                    slotAddress -= (slot) * IntPtr.Size;
                }
                else
                {
                    slotAddress += (slot) * IntPtr.Size;
                }
                while (true)
                {
                    RawRef dest = new RawRef();
                    dest.Address = slotAddress;
                    var destAddress = dest.GetValue<IntPtr>();

                    RawRef real = new RawRef();
                    real.Address = destAddress;
                    ref T r = ref real.GetRef<T>();

                    if (destAddress == dest.GetValue<IntPtr>())
                    {
                        return ref r;
                    }
                }
            }
            return ref Ref.GetEmptyRef<T>();
        }

        public RawRef GetSlotRef(int level, int slot)
        {
            var baseAddress = Levels[level].BaseAddress;
            if (baseAddress != IntPtr.Zero)
            {
                var slotAddress = baseAddress;
                if (GetStackDir() > 0)
                {
                    slotAddress -= (slot) * IntPtr.Size;
                }
                else
                {
                    slotAddress += (slot) * IntPtr.Size;
                }
                RawRef dest = new RawRef();
                dest.Address = slotAddress;
                return dest;
            }
            return new RawRef();
        }

        #region IDisposable Support
        private bool _Disposed = false; // 要检测冗余调用
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                _Disposed = true;
                if (_Thread != null)
                {
                    _StackOps.Enqueue(new TrackingRefManagerOp() { IsExit = true });
                    _WaitForStackOp.Set();
                    _Thread = null;
                }
            }
        }
        ~TrackingRefManager()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }
        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    internal class LeveledTrackingRefManager : IDisposable
    { // One LeveledTrackingRefManager should be used on One Thread.
        public int Index;

        protected struct SlotInfo
        {
            public bool IsInUsing;
        }
        protected readonly SlotInfo[] Slots = new SlotInfo[1024];
        protected int NextIndex = 0;
        protected int _OccupiedCount = 0;
        public int OccupiedCount { get { return _OccupiedCount; } }

        public int OccupyEmptySlot()
        {
            for (int i = 0; i < 1024; ++i)
            {
                var index = (NextIndex + i) & 0x3FF;
                if (!Slots[index].IsInUsing)
                {
                    Slots[index].IsInUsing = true;
                    NextIndex = index + 1;
                    ++_OccupiedCount;
                    return index;
                }
            }
            return -1;
        }
        public void ReturnEmptySlot(int index)
        {
            if (Slots[index].IsInUsing)
            {
                --_OccupiedCount;
                Slots[index].IsInUsing = false;
            }
        }

        #region IDisposable Support
        private bool _Disposed = false; // 要检测冗余调用
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                _Disposed = true;
                if (Index >= 0)
                {
                    TrackingRefManager.GlobalManager.ReturnEmptyLevel(Index);
                    Index = -1;
                }
            }
        }
        ~LeveledTrackingRefManager()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }
        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
