using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Mod.LowLevel
{
    [StructLayout(LayoutKind.Explicit)]
    public struct UnionRef : IUntypedRef, IDisposable
    {
        public enum RefType
        {
            Local = 0,
            Raw = 0x01000000,
            Tracking = 0x02000000,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UnionTypeIndicator
        {
            private IntPtr _Address;
            private int _Type;
            public RefType Type
            {
                get => (RefType)(_Type & 0x7F000000);
                set
                {
                    _Type = (_Type & unchecked((int)0x80FFFFFF)) | (int)value;
                }
            }
        }

        [FieldOffset(0)]
        private UnionTypeIndicator _UnionType;
        [FieldOffset(0)]
        public RawRef _Raw;
        [FieldOffset(0)]
        public LocalRef _Local;
        [FieldOffset(0)]
        public RawTrackingRef _Tracking;

        public RefType Type
        {
            get => _UnionType.Type;
            set
            {
                if (_UnionType.Type != value)
                {
                    ref byte addr = ref GetRef<byte>();
                    if (_UnionType.Type == RefType.Tracking)
                    {
                        _Tracking.Dispose();
                    }
                    switch (value)
                    {
                        case RefType.Local:
                            var addrthis = RawRef.Of(ref this).Address;
                            Local = new LocalRef(addrthis - LocalRef.StackDir);
                            break;
                        case RefType.Raw:
                            Raw = new RawRef();
                            break;
                        case RefType.Tracking:
                            Tracking = RawTrackingRef.Create();
                            break;
                    }
                    SetRef(ref addr);
                }
            }
        }
        public RawRef Raw
        {
            get => _Raw;
            set
            {
                if (_UnionType.Type == RefType.Tracking)
                {
                    _Tracking.Dispose();
                }
                _Raw = value;
                _UnionType.Type = RefType.Raw;
            }
        }
        public LocalRef Local
        {
            get => _Local;
            set
            {
                if (_UnionType.Type == RefType.Tracking)
                {
                    _Tracking.Dispose();
                }
                _Local = value;
                _UnionType.Type = RefType.Local;
            }
        }
        public RawTrackingRef Tracking
        {
            get => _Tracking;
            set
            {
                if (_UnionType.Type == RefType.Tracking)
                {
                    if (_Tracking == value)
                    {
                        return;
                    }
                    _Tracking.Dispose();
                }
                _Tracking = value;
                _UnionType.Type = RefType.Tracking;
            }
        }

        public ref T GetRef<T>()
        {
            switch (Type)
            {
                case RefType.Local:
                    return ref _Local.GetRef<T>();
                case RefType.Raw:
                    return ref _Raw.GetRef<T>();
                case RefType.Tracking:
                    return ref _Tracking.GetRef<T>();
                default:
                    return ref Ref.GetEmptyRef<T>();
            }
        }
        public void SetRef<T>(ref T r)
        {
            switch (Type)
            {
                case RefType.Local:
                    _Local.SetRef(ref r);
                    break;
                case RefType.Raw:
                    _Raw.SetRef(ref r);
                    break;
                case RefType.Tracking:
                    _Tracking.SetRef(ref r);
                    break;
            }
        }
        public T GetValue<T>()
        {
            switch (Type)
            {
                case RefType.Local:
                    return _Local.GetValue<T>();
                case RefType.Raw:
                    return _Raw.GetValue<T>();
                case RefType.Tracking:
                    return _Tracking.GetValue<T>();
                default:
                    return default;
            }
        }
        public void SetValue<T>(T value)
        {
            switch (Type)
            {
                case RefType.Local:
                    _Local.SetValue(value);
                    break;
                case RefType.Raw:
                    _Raw.SetValue(value);
                    break;
                case RefType.Tracking:
                    _Tracking.SetValue(value);
                    break;
            }
        }
        public IntPtr Address
        {
            get
            {
                switch (Type)
                {
                    case RefType.Local:
                        return _Local.Address;
                    case RefType.Raw:
                        return _Raw.Address;
                    case RefType.Tracking:
                        return _Tracking.Address;
                    default:
                        return IntPtr.Zero;
                }
            }
            set
            {
                switch (Type)
                {
                    case RefType.Local:
                        _Local.Address = value;
                        break;
                    case RefType.Raw:
                        _Raw.Address = value;
                        break;
                    case RefType.Tracking:
                        _Tracking.Address = value;
                        break;
                }
            }
        }

        public UnionRef(RawRef r)
        {
            _UnionType = default;
            _Local = default;
            _Tracking = default;
            _Raw = default;
            Raw = r;
        }
        public UnionRef(LocalRef r)
        {
            _UnionType = default;
            _Local = default;
            _Tracking = default;
            _Raw = default;
            Local = r;
        }
        public UnionRef(RawTrackingRef r)
        {
            _UnionType = default;
            _Local = default;
            _Tracking = default;
            _Raw = default;
            Tracking = r;
        }
        public UnionRef(RefType type)
        {
            _UnionType = default;
            _Local = default;
            _Tracking = default;
            _Raw = default;
            Type = type;
        }
        public void Dispose()
        {
            if (_UnionType.Type == RefType.Tracking)
            {
                _Tracking.Dispose();
            }
            this = default;
        }
        public static UnionRef Of<T>(ref T r, bool tracking = true)
        {
            if (tracking)
            {
                var tr = RawTrackingRef.Create();
                tr.SetRef(ref r);
                return new UnionRef(tr);
            }
            else
            {
                return new UnionRef(RawRef.Of(ref r));
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UnionRef<T> : IRef<T>, IDisposable
    {
        private UnionRef _Inner;

        public ref RawRef<T> _Raw => ref RawRef.Of(ref _Inner).GetRef<RawRef<T>>();
        public ref LocalRef<T> _Local => ref RawRef.Of(ref _Inner).GetRef<LocalRef<T>>();
        public ref RawTrackingRef<T> _Tracking => ref RawRef.Of(ref _Inner).GetRef<RawTrackingRef<T>>();

        public UnionRef.RefType Type
        {
            get => _Inner.Type;
            set => _Inner.Type = value;
        }
        public RawRef<T> Raw
        {
            get => _Raw;
            set => _Inner.Raw = RawRef.Of(ref value).GetValue<RawRef>();
        }
        public LocalRef<T> Local
        {
            get => _Local;
            set => _Inner.Local = RawRef.Of(ref value).GetValue<LocalRef>();
        }
        public RawTrackingRef<T> Tracking
        {
            get => _Tracking;
            set => _Inner._Tracking = RawRef.Of(ref value).GetValue<RawTrackingRef>();
        }

        public ref T GetRef()
        {
            return ref _Inner.GetRef<T>();
        }
        public void SetRef(ref T r)
        {
            _Inner.SetRef(ref r);
        }
        public T GetValue()
        {
            return _Inner.GetValue<T>();
        }
        public void SetValue(T value)
        {
            _Inner.SetValue(value);
        }
        public ref T R => ref GetRef();
        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }
        public IntPtr Address
        {
            get => _Inner.Address;
            set => _Inner.Address = value;
        }

        public UnionRef(RawRef<T> r)
        {
            _Inner = new UnionRef(RawRef.Of(ref r).GetValue<RawRef>());
        }
        public UnionRef(LocalRef<T> r)
        {
            _Inner = new UnionRef(RawRef.Of(ref r).GetValue<LocalRef>());
        }
        public UnionRef(RawTrackingRef<T> r)
        {
            _Inner = new UnionRef(RawRef.Of(ref r).GetValue<RawTrackingRef>());
        }
        public UnionRef(UnionRef.RefType type)
        {
            _Inner = default;
            _Inner.Type = type;
        }
        public UnionRef(ref T r) : this(UnionRef.RefType.Raw)
        {
            SetRef(ref r);
        }
        public UnionRef(UnionRef.RefType type, ref T r) : this(type)
        {
            SetRef(ref r);
        }
        public void Dispose()
        {
            _Inner.Dispose();
        }
    }
}
