using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mod.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RawTrackingRef : IDisposable, IUntypedIndirectRef
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
                TrackingRefManager.GlobalManager.ReturnSlot(Level, Slot);
                IsValid = false;
            }
        }
        public static RawTrackingRef Create()
        {
            RawTrackingRef raw = new RawTrackingRef();
            var (level, slot) = TrackingRefManager.GlobalManager.TakeSlot();
            if (level >= 0 && slot >= 0)
            {
                raw.Level = level;
                raw.Slot = slot;
                raw.IsValid = true;
                raw._Ref2Ref = TrackingRefManager.GlobalManager.GetSlotRef(raw.Level, raw.Slot);
            }
            return raw;
        }
        public static void Preserve(int count)
        {
            TrackingRefManager.GlobalManager.Preserve(count);
        }

        public void SetRef<T>(ref T r)
        {
            ref RawRef rd = ref _Ref2Ref.GetRef<RawRef>();
            rd.SetRef<T>(ref r);
        }
        public ref T GetRef<T>()
        {
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
    public class TrackingRef : IDisposable, IUntypedIndirectRef
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

}
