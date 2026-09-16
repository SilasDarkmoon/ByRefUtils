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
            private set
            {
                if (_UnionType.Type != value)
                {
                    if (_UnionType.Type == RefType.Tracking)
                    {
                        Tracking.Dispose();
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
                    Tracking.Dispose();
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
                    Tracking.Dispose();
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
                    Tracking.Dispose();
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
                    return ref Local.GetRef<T>();
                case RefType.Raw:
                    return ref Raw.GetRef<T>();
                case RefType.Tracking:
                    return ref Tracking.GetRef<T>();
                default:
                    return ref Ref.GetEmptyRef<T>();
            }
        }
        public void SetRef<T>(ref T r)
        {
            switch (Type)
            {
                case RefType.Local:
                    Local.SetRef(ref r);
                    break;
                case RefType.Raw:
                    Raw.SetRef(ref r);
                    break;
                case RefType.Tracking:
                    Tracking.SetRef(ref r);
                    break;
            }
        }
        public T GetValue<T>()
        {
            switch (Type)
            {
                case RefType.Local:
                    return Local.GetValue<T>();
                case RefType.Raw:
                    return Raw.GetValue<T>();
                case RefType.Tracking:
                    return Tracking.GetValue<T>();
                default:
                    return default;
            }
        }
        public void SetValue<T>(T value)
        {
            switch (Type)
            {
                case RefType.Local:
                    Local.SetValue(value);
                    break;
                case RefType.Raw:
                    Raw.SetValue(value);
                    break;
                case RefType.Tracking:
                    Tracking.SetValue(value);
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
                        return Local.Address;
                    case RefType.Raw:
                        return Raw.Address;
                    case RefType.Tracking:
                        return Tracking.Address;
                    default:
                        return IntPtr.Zero;
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
}
