using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Capstones.ByRefUtils
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RawTrackingRef : IDisposable, IRef
    {
        private int _Data;
        public bool IsValid
        {
            get
            {
                return (_Data & 0x100000) != 0;
            }
            private set
            {
                if (value)
                {
                    _Data |= 0x100000;
                }
                else
                {
                    _Data &= ~0x100000;
                }
            }
        }
        public int Level
        {
            get
            {
                return (_Data & 0xFFC00) >> 10;
            }
            private set
            {
                _Data &= ~0xFFC00;
                _Data |= (value << 10) & 0xFFC00;
            }
        }
        public int Slot
        {
            get
            {
                return _Data & 0x3FF;
            }
            private set
            {
                _Data &= ~0x3FF;
                _Data |= value & 0x3FF;
            }
        }

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
            if (IsValid)
            {
                TrackingRefManager.GlobalManager.SetRef(Level, Slot, ref r);
            }
        }
        public ref T GetRef<T>()
        {
            if (IsValid)
            {
                return ref TrackingRefManager.GlobalManager.GetRef<T>(Level, Slot);
            }
            return ref Ref.GetEmptyRef<T>();
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
                ref int r = ref GetRef<int>();
                RawRef rr = new RawRef();
                rr.SetRef(ref r);
                return rr.Address;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class TrackingRef : IDisposable, IRef
    {
        private RawTrackingRef _Ref;
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
    }


    [StructLayout(LayoutKind.Sequential)]
    public class TrackingRef<T> : IDisposable, IRef
    {
        private RawTrackingRef _Ref;
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

        public T Value
        {
            get { return _Ref.GetValue<T>(); }
            set { _Ref.SetValue<T>(value); }
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
