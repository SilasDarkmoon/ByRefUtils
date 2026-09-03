using System;
using System.Collections.Generic;
using System.Text;

namespace Mod.LowLevel
{
    public struct RefContainer : IUntypedRef
    {
        private object _InnerRef;

        public IntPtr Address
        {
            get => RawRef.Of(_InnerRef).Address;
            set => RawRef.Of(ref _InnerRef).GetRef<RawRef>().Address = value;
        }
        public ref T GetRef<T>()
        {
            return ref RawRef.Of(_InnerRef).GetRef<T>();
        }
        public void SetRef<T>(ref T r)
        {
            RawRef.Of(ref _InnerRef).GetRef<RawRef>().SetRef<T>(ref r);
        }
        public T GetValue<T>()
        {
            return GetRef<T>();
        }
        public void SetValue<T>(T value)
        {
            GetRef<T>() = value;
        }
        public void SetRefObj(object obj)
        {
            _InnerRef = obj;
        }
        public object GetRefObj()
        {
            return _InnerRef;
        }

        public static RefContainer Of(object obj)
        {
            RefContainer result = default(RefContainer);
            result._InnerRef = obj;
            return result;
        }

        public static RefContainer Of<T>(ref T r)
        {
            RefContainer result = default(RefContainer);
            result.SetRef(ref r);
            return result;
        }

        public static RefContainer Of(IntPtr address)
        {
            RefContainer result = default(RefContainer);
            result.Address = address;
            return result;
        }

        public static implicit operator IntPtr(RefContainer r)
        {
            return r.Address;
        }
        public static explicit operator RefContainer(IntPtr p)
        {
            return new RefContainer() { Address = p };
        }
        public static implicit operator RawRef(RefContainer r)
        {
            return new RawRef() { Address = r.Address };
        }
        public static implicit operator RefContainer(RawRef p)
        {
            return new RefContainer() { Address = p.Address };
        }
        public override bool Equals(object obj)
        {
            if (obj is IRef r)
            {
                return Address == r.Address;
            }
            else if (obj is IntPtr p)
            {
                return Address == p;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }
        public override string ToString()
        {
            return Address.ToString("X");
        }
        public static bool operator ==(RefContainer r1, RefContainer r2)
        {
            return ReferenceEquals(r1._InnerRef, r2._InnerRef);
        }
        public static bool operator !=(RefContainer r1, RefContainer r2)
        {
            return !ReferenceEquals(r1._InnerRef, r2._InnerRef);
        }
        public static bool operator ==(RefContainer r1, IntPtr p2)
        {
            return r1.Address == p2;
        }
        public static bool operator !=(RefContainer r1, IntPtr p2)
        {
            return r1.Address != p2;
        }
        public static bool operator ==(IntPtr p1, RefContainer r2)
        {
            return p1 == r2.Address;
        }
        public static bool operator !=(IntPtr p1, RefContainer r2)
        {
            return p1 != r2.Address;
        }
        public static bool operator ==(RawRef r1, RefContainer p2)
        {
            return r1.Address == p2.Address;
        }
        public static bool operator !=(RawRef r1, RefContainer p2)
        {
            return r1.Address != p2.Address;
        }
        public static bool operator ==(RefContainer p1, RawRef r2)
        {
            return p1.Address == r2.Address;
        }
        public static bool operator !=(RefContainer p1, RawRef r2)
        {
            return p1.Address != r2.Address;
        }
        public static bool operator ==(RefContainer r1, IRef r2)
        {
            return r1.Address == r2.Address;
        }
        public static bool operator !=(RefContainer r1, IRef r2)
        {
            return r1.Address != r2.Address;
        }
        public static bool operator ==(IRef r1, RefContainer r2)
        {
            return r1.Address == r2.Address;
        }
        public static bool operator !=(IRef r1, RefContainer r2)
        {
            return r1.Address != r2.Address;
        }
        public static RefContainer operator +(RefContainer r, int offset)
        {
            RefContainer result = default(RefContainer);
            for (; ; )
            {
                result.Address = r.Address + offset;
                if (result.Address == r.Address + offset)
                {
                    break;
                }
            }
            return result;
        }
        public static RefContainer operator -(RefContainer r, int offset)
        {
            RefContainer result = default(RefContainer);
            for (; ; )
            {
                result.Address = r.Address - offset;
                if (result.Address == r.Address - offset)
                {
                    break;
                }
            }
            return result;
        }
    }

    public struct RefContainer<T> : IRef<T>
    {
        private RefContainer _BaseRef;

        public RefContainer(ref T r)
        {
            _BaseRef = new RefContainer();
            _BaseRef.SetRef<T>(ref r);
        }
        public RefContainer(IntPtr address)
        {
            _BaseRef = new RefContainer() { Address = address };
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
        public ref T R
        {
            get => ref GetRef();
        }
        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }
        public IntPtr Address
        {
            get
            {
                return _BaseRef.Address;
            }
            set
            {
                _BaseRef.Address = value;
            }
        }

        public static implicit operator IntPtr(RefContainer<T> r)
        {
            return r.Address;
        }
        public static explicit operator RefContainer<T>(IntPtr p)
        {
            return new RefContainer<T>() { Address = p };
        }
        public static implicit operator RawRef<T>(RefContainer<T> r)
        {
            return new RawRef<T>() { Address = r.Address };
        }
        public static implicit operator RefContainer<T>(RawRef<T> p)
        {
            return new RefContainer<T>() { Address = p.Address };
        }
        public override bool Equals(object obj)
        {
            if (obj is IRef r)
            {
                return Address == r.Address;
            }
            else if (obj is IntPtr p)
            {
                return Address == p;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }
        public override string ToString()
        {
            return Address.ToString("X");
        }
        public static bool operator ==(RefContainer<T> r1, RefContainer<T> r2)
        {
            return r1._BaseRef == r2._BaseRef;
        }
        public static bool operator !=(RefContainer<T> r1, RefContainer<T> r2)
        {
            return r1._BaseRef != r2._BaseRef;
        }
        public static bool operator ==(RefContainer<T> r1, IntPtr p2)
        {
            return r1.Address == p2;
        }
        public static bool operator !=(RefContainer<T> r1, IntPtr p2)
        {
            return r1.Address != p2;
        }
        public static bool operator ==(IntPtr p1, RefContainer<T> r2)
        {
            return p1 == r2.Address;
        }
        public static bool operator !=(IntPtr p1, RefContainer<T> r2)
        {
            return p1 != r2.Address;
        }
        public static bool operator ==(RefContainer<T> r1, RawRef p2)
        {
            return r1.Address == p2.Address;
        }
        public static bool operator !=(RefContainer<T> r1, RawRef p2)
        {
            return r1.Address != p2.Address;    
        }
        public static bool operator ==(RawRef p1, RefContainer<T> r2)
        {
            return p1.Address == r2.Address;
        }
        public static bool operator !=(RawRef p1, RefContainer<T> r2)
        {
            return p1.Address != r2.Address;
        }
        public static bool operator ==(RefContainer<T> r1, RawRef<T> p2)
        {
            return r1.Address == p2.Address;
        }
        public static bool operator !=(RefContainer<T> r1, RawRef<T> p2)
        {
            return r1.Address != p2.Address;    
        }
        public static bool operator ==(RawRef<T> p1, RefContainer<T> r2)
        {
            return p1.Address == r2.Address;
        }
        public static bool operator !=(RawRef<T> p1, RefContainer<T> r2)
        {
            return p1.Address != r2.Address;
        }
        public static bool operator ==(RefContainer<T> r1, RefContainer r2)
        {
            return r1._BaseRef == r2;
        }
        public static bool operator !=(RefContainer<T> r1, RefContainer r2)
        {
            return r1._BaseRef != r2;
        }
        public static bool operator ==(RefContainer r1, RefContainer<T> r2)
        {
            return r1 == r2._BaseRef;
        }
        public static bool operator !=(RefContainer r1, RefContainer<T> r2)
        {
            return r1 != r2._BaseRef;
        }
        public static bool operator ==(RefContainer<T> r1, IRef r2)
        {
            return r1.Address == r2.Address;
        }
        public static bool operator !=(RefContainer<T> r1, IRef r2)
        {
            return r1.Address != r2.Address;
        }
        public static bool operator ==(IRef r1, RefContainer<T> r2)
        {
            return r1.Address == r2.Address;
        }
        public static bool operator !=(IRef r1, RefContainer<T> r2)
        {
            return r1.Address != r2.Address;
        }
    }
}
