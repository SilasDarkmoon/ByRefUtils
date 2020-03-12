using System;
using System.Runtime.InteropServices;

namespace Capstones.ByRefUtils
{
    public interface IRef
    {
        IntPtr Address { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawRef : IRef
    {
        private IntPtr _Ref;
        /// <summary>
        /// You should be very careful in setting the address.
        /// </summary>
        public IntPtr Address
        {
            get { return _Ref; }
            set { _Ref = value; }
        }
        public static implicit operator IntPtr(RawRef r)
        {
            return r._Ref;
        }
        public static explicit operator RawRef(IntPtr p)
        {
            return new RawRef() { _Ref = p };
        }
        public override bool Equals(object obj)
        {
            if (obj is IRef r)
            {
                return _Ref == r.Address;
            }
            else if (obj is IntPtr p)
            {
                return _Ref == p;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return _Ref.GetHashCode();
        }
        public static bool operator ==(RawRef r1, RawRef r2)
        {
            return r1._Ref == r2._Ref;
        }
        public static bool operator !=(RawRef r1, RawRef r2)
        {
            return r1._Ref != r2._Ref;
        }
        public static bool operator ==(RawRef r1, IntPtr p2)
        {
            return r1._Ref == p2;
        }
        public static bool operator !=(RawRef r1, IntPtr p2)
        {
            return r1._Ref != p2;
        }
        public static bool operator ==(IntPtr p1, RawRef r2)
        {
            return p1 == r2._Ref;
        }
        public static bool operator !=(IntPtr p1, RawRef r2)
        {
            return p1 != r2._Ref;
        }
        public static bool operator ==(RawRef r1, IRef r2)
        {
            return r1._Ref == r2.Address;
        }
        public static bool operator !=(RawRef r1, IRef r2)
        {
            return r1._Ref != r2.Address;
        }
        public static bool operator ==(IRef r1, RawRef r2)
        {
            return r1.Address == r2._Ref;
        }
        public static bool operator !=(IRef r1, RawRef r2)
        {
            return r1.Address != r2._Ref;
        }
        public static RawRef operator +(RawRef r, int offset)
        {
            r.Address += offset;
            return r;
        }
        public static RawRef operator -(RawRef r, int offset)
        {
            r.Address -= offset;
            return r;
        }

        public ref T GetRef<T>()
        {
            throw new NotImplementedException();
        }
        public void SetRef<T>(ref T r)
        {
            throw new NotImplementedException();
        }
        public T GetValue<T>()
        {
            return GetRef<T>();
        }
        public void SetValue<T>(T value)
        {
            GetRef<T>() = value;
        }
        /// <summary>
        /// It is useful to get the address of obj. But be careful to get content from the address, as object has a header before its content.
        /// </summary>
        public void SetRefObj(object obj)
        {
            throw new NotImplementedException();
        }
        public object GetRefObj()
        {
            throw new NotImplementedException();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public sealed class Ref : IRef
    {
        private RawRef _Ref = new RawRef();
        /// <summary>
        /// You should be very careful in setting the address.
        /// </summary>
        public IntPtr Address
        {
            get { return _Ref.Address; }
            set { _Ref.Address = value; }
        }
        public static implicit operator IntPtr(Ref r)
        {
            return r._Ref;
        }
        public static explicit operator Ref(IntPtr p)
        {
            return new Ref() { Address = p };
        }
        public override bool Equals(object obj)
        {
            if (obj is IRef r)
            {
                return _Ref == r.Address;
            }
            else if (obj is IntPtr p)
            {
                return _Ref == p;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return _Ref.GetHashCode();
        }
        public static bool operator ==(Ref r1, Ref r2)
        {
            return r1._Ref == r2._Ref;
        }
        public static bool operator !=(Ref r1, Ref r2)
        {
            return r1._Ref != r2._Ref;
        }
        public static bool operator ==(Ref r1, IntPtr p2)
        {
            return r1._Ref == p2;
        }
        public static bool operator !=(Ref r1, IntPtr p2)
        {
            return r1._Ref != p2;
        }
        public static bool operator ==(IntPtr p1, Ref r2)
        {
            return p1 == r2._Ref;
        }
        public static bool operator !=(IntPtr p1, Ref r2)
        {
            return p1 != r2._Ref;
        }
        public static bool operator ==(Ref r1, IRef r2)
        {
            return r1._Ref == r2.Address;
        }
        public static bool operator !=(Ref r1, IRef r2)
        {
            return r1._Ref != r2.Address;
        }
        public static bool operator ==(IRef r1, Ref r2)
        {
            return r1.Address == r2._Ref;
        }
        public static bool operator !=(IRef r1, Ref r2)
        {
            return r1.Address != r2._Ref;
        }
        // because this will create new instance, use Ref.Address += offset instead.
        //public static Ref operator +(Ref r, int offset)
        //{
        //    return new Ref() { Address = r.Address + offset };
        //}
        //public static Ref operator -(Ref r, int offset)
        //{
        //    return new Ref() { Address = r.Address - offset };
        //}

        public ref T GetRef<T>()
        {
            return ref _Ref.GetRef<T>();
        }
        public void SetRef<T>(ref T r)
        {
            _Ref.SetRef<T>(ref r);
        }
        public T GetValue<T>()
        {
            return _Ref.GetValue<T>();
        }
        public void SetValue<T>(T value)
        {
            _Ref.SetValue<T>(value);
        }
        /// <summary>
        /// It is useful to get the address of obj. But be careful to get content from the address, as object has a header before its content.
        /// </summary>
        public void SetRefObj(object obj)
        {
            _Ref.SetRefObj(obj);
        }
        public object GetRefObj()
        {
            return _Ref.GetRefObj();
        }

        public static bool RefEquals<T>(ref T a, ref T b)
        {
            return false;
        }
        public static ref T GetEmptyRef<T>()
        {
            throw new NotImplementedException();
        }
        public static bool IsEmpty<T>(ref T r)
        {
            return RefEquals(ref r, ref GetEmptyRef<T>());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public sealed class Ref<T> : IRef
    {
        private RawRef _Ref = new RawRef();
        /// <summary>
        /// You should be very careful in setting the address.
        /// </summary>
        public IntPtr Address
        {
            get { return _Ref.Address; }
            set { _Ref.Address = value; }
        }
        public static implicit operator IntPtr(Ref<T> r)
        {
            return r._Ref;
        }
        public static explicit operator Ref<T>(IntPtr p)
        {
            return new Ref<T>() { Address = p };
        }
        public override bool Equals(object obj)
        {
            if (obj is IRef r)
            {
                return _Ref == r.Address;
            }
            else if (obj is IntPtr p)
            {
                return _Ref == p;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return _Ref.GetHashCode();
        }
        public static bool operator ==(Ref<T> r1, Ref<T> r2)
        {
            return r1._Ref == r2._Ref;
        }
        public static bool operator !=(Ref<T> r1, Ref<T> r2)
        {
            return r1._Ref != r2._Ref;
        }
        public static bool operator ==(Ref<T> r1, IntPtr p2)
        {
            return r1._Ref == p2;
        }
        public static bool operator !=(Ref<T> r1, IntPtr p2)
        {
            return r1._Ref != p2;
        }
        public static bool operator ==(IntPtr p1, Ref<T> r2)
        {
            return p1 == r2._Ref;
        }
        public static bool operator !=(IntPtr p1, Ref<T> r2)
        {
            return p1 != r2._Ref;
        }
        public static bool operator ==(Ref<T> r1, IRef r2)
        {
            return r1._Ref == r2.Address;
        }
        public static bool operator !=(Ref<T> r1, IRef r2)
        {
            return r1._Ref != r2.Address;
        }
        public static bool operator ==(IRef r1, Ref<T> r2)
        {
            return r1.Address == r2._Ref;
        }
        public static bool operator !=(IRef r1, Ref<T> r2)
        {
            return r1.Address != r2._Ref;
        }
        // because this will create new instance, use Ref<T>.Address += offset instead.
        //public static Ref<T> operator +(Ref<T> r, int offset)
        //{
        //    return new Ref<T>() { Address = r.Address + offset };
        //}
        //public static Ref<T> operator -(Ref<T> r, int offset)
        //{
        //    return new Ref<T>() { Address = r.Address - offset };
        //}

        public ref T GetRef()
        {
            return ref _Ref.GetRef<T>();
        }
        public void SetRef(ref T r)
        {
            _Ref.SetRef<T>(ref r);
        }

        public T Value
        {
            get { return _Ref.GetValue<T>(); }
            set { _Ref.SetValue<T>(value); }
        }
    }
}
