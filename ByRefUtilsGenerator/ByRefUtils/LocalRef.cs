using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Mod.LowLevel
{
    public interface IIndirectRef : IRef
    {
        RawRef Ref2Ref { get; }
    }
    public interface IIndirectRef<T> : IIndirectRef, IRef<T>
    {
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct LocalRef : IIndirectRef
    {
        private RawRef _Ref2Ref;
        public RawRef Ref2Ref => _Ref2Ref;

        public IntPtr Address
        {
            get
            {
                var r = _Ref2Ref.GetValue<IntPtr>();
                return r;
            }
        }

        public void SetSlotRef<T>(ref T r, int offset)
        {
            _Ref2Ref.SetRef<T>(ref r);
            _Ref2Ref.Address += offset;
        }
        public void SetSlotRef<T>(ref T r)
        {
            _Ref2Ref.SetRef<T>(ref r);
            _Ref2Ref.Address -= IntPtr.Size;
        }

        public LocalRef(int offset)
        {
            _Ref2Ref = new RawRef();
            _Ref2Ref.SetRef(ref this);
            if (offset == 0)
            {
                _Ref2Ref.Address -= IntPtr.Size;
            }
            else
            {
                _Ref2Ref.Address += offset;
            }
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

        public static bool operator ==(LocalRef r1, LocalRef r2)
        {
            return r1._Ref2Ref == r2._Ref2Ref;
        }
        public static bool operator !=(LocalRef r1, LocalRef r2)
        {
            return r1._Ref2Ref != r2._Ref2Ref;
        }
        public static bool operator ==(LocalRef r1, IIndirectRef r2)
        {
            return r1._Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(LocalRef r1, IIndirectRef r2)
        {
            return r1._Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(IIndirectRef r1, LocalRef r2)
        {
            return r1.Ref2Ref == r2._Ref2Ref;
        }
        public static bool operator !=(IIndirectRef r1, LocalRef r2)
        {
            return r1.Ref2Ref != r2._Ref2Ref;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LocalRef<T> : IIndirectRef<T>
    {
        private LocalRef _LocalRef;
        public RawRef Ref2Ref => _LocalRef.Ref2Ref;
        public IntPtr Address => _LocalRef.Address;
        public LocalRef(int offset)
        {
            _LocalRef = new LocalRef(offset);
        }
        public void SetSlotRef<S>(ref S r, int offset)
        {
            _LocalRef.SetSlotRef(ref r, offset);
        }
        public void SetSlotRef<S>(ref S r)
        {
            _LocalRef.SetSlotRef(ref r);
        }
        public void SetRef(ref T r)
        {
            _LocalRef.SetRef(ref r);
        }
        public ref T GetRef()
        {
            return ref _LocalRef.GetRef<T>();
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

        public static bool operator ==(LocalRef<T> r1, LocalRef<T> r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(LocalRef<T> r1, LocalRef<T> r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(LocalRef<T> r1, LocalRef r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(LocalRef<T> r1, LocalRef r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(LocalRef r1, LocalRef<T> r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(LocalRef r1, LocalRef<T> r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(LocalRef<T> r1, IIndirectRef r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(LocalRef<T> r1, IIndirectRef r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
        public static bool operator ==(IIndirectRef r1, LocalRef<T> r2)
        {
            return r1.Ref2Ref == r2.Ref2Ref;
        }
        public static bool operator !=(IIndirectRef r1, LocalRef<T> r2)
        {
            return r1.Ref2Ref != r2.Ref2Ref;
        }
    }
}
