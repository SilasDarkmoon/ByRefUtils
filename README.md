# ByRefUtils
C# (.NET Core / Standard 2.0) utilities for variable references ( ref T val )
## What's this project for

In C# 7 we got new feature: ref local and ref return.

But still, many things with "ref" cannot be done yet.

For example, we cannot declare a field in a class with type "ref int".
```C#
class WeWantThisClass
{
    public ref int Reference;
}
```

And we want to check whether 2 reference are equal.
```C#
public static bool ReferenceEquals(ref int a, ref int b)
```

And sometimes, we want to ref return null.
```C#
public static ref int Find(IList<int> list, int val)
{
    //...
    // if we cannot find?
    ref return null;
}
```

So I created the ByRefUtils.dll to help us to do these.

## How to use

1) Download and Copy the ByRefUtils.dll to your project.
2) Add Reference to ByRefUtils.dll
3) Use it.

## What can it do

1) Declare a ref to a variable
```C#
        int i = 0;
        var r = new Capstones.UnityEngineEx.ByRefUtils.RawRef();
        r.SetRef(ref i);
```
RawRef is a struct. You can also use Capstones.UnityEngineEx.ByRefUtils.Ref (class) or Capstones.UnityEngineEx.ByRefUtils.Ref<T>

2) Get ref from RawRef / Ref / Ref<T>
```C#
        Capstones.UnityEngineEx.ByRefUtils.RawRef r;
        //...
        ref int ri = ref r.GetRef<int>();
```

3) Get / Set Value
```C#
        int i = 0;
        var r = new Capstones.UnityEngineEx.ByRefUtils.RawRef();
        r.SetRef(ref i);
        r.SetValue(2);
```

4) Get empty (null) ref
```C#
        Capstones.UnityEngineEx.ByRefUtils.GetEmptyRef<T>()
        // or new RawRef / Ref / Ref<T> and GetRef from them
```

4) Check ref equals
```C#
        Capstones.UnityEngineEx.ByRefUtils.RefEquals<T>(ref T a, ref T b)
```

5) Check a ref is empty (null)
```C#
        // check ref equals to GetEmptyRef
        Capstones.UnityEngineEx.ByRefUtils.RefEquals<T>(ref T a, ref GetEmptyRef<T>())
        // or
        Capstones.UnityEngineEx.ByRefUtils.IsEmpty<T>(ref T r)
```

6) We can do dangerous convert use it
```C#
        var r = new Capstones.UnityEngineEx.ByRefUtils.RawRef();
        int i = 1;
        r.SetRef(ref i);
        var plat = r.GetRef<RuntimePlatform>(); // RuntimePlatform is an enum
```

## Remarks
1) CLR GC will move objects at managed heap, so holding a ref to an address at heap for a long time is dangerous, and is not tested.
2) Declaring a ref to a variable at stack is safe. But we should notice memory layout.
3) It is tested in .NET Core 3.0 and Unity (both Mono and IL2CPP)
4) We cannot implement this dll in C#, but IL can. Some of the code is injected with Mono.Cecil

# 中文说明
## 这个工程是做什么的？

在 C# 7.0 中，我们获得了新特性：ref 局部变量和返回值。

但是仍有许多限制阻碍着我们更自然地使用 "ref"。

比如，C#不允许我们声明 ref 的字段。比如下面这个类型就是非法的。

```C#
class WeWantThisClass
{
    public ref int Reference;
}
```
有时，我们想比较两个引用的内存地址是否相等，就像 object.ReferenceEquals 一样。
```C#
public static bool ReferenceEquals(ref int a, ref int b)
```

有时，我们想返回一个为空的地址。比如在集合中进行查找，没有找到时。
```C#
public static ref int Find(IList<int> list, int val)
{
    //...
    // 如果找不到怎么办?
    ref return null;
}
```

因此我创建了一个程序集(.NET standard 2.0)来解决上述的需求。

## 怎样使用呢？

1) 下载并拷贝 ByRefUtils.dll 到目标工程。
2) 在目标工程中添加 ByRefUtils.dll 的引用。
3) 使用 ByRefUtils.dll 中的类型。（集中在 Capstones.UnityEngineEx.ByRefUtils 静态类中）

## 代码示例

1) 声明一个变量的引用
```C#
        int i = 0;
        var r = new Capstones.UnityEngineEx.ByRefUtils.RawRef();
        r.SetRef(ref i);
```
RawRef 是值类型(struct). 也可以使用 Capstones.UnityEngineEx.ByRefUtils.Ref (引用类型class) or Capstones.UnityEngineEx.ByRefUtils.Ref<T> (泛型引用类型)

2) 从 RawRef / Ref / Ref<T> 里拿引用
```C#
        Capstones.UnityEngineEx.ByRefUtils.RawRef r;
        //...
        ref int ri = ref r.GetRef<int>();
```

3) 取值/赋值
```C#
        int i = 0;
        var r = new Capstones.UnityEngineEx.ByRefUtils.RawRef();
        r.SetRef(ref i);
        r.SetValue(2);
```

4) 拿一个空引用
```C#
        Capstones.UnityEngineEx.ByRefUtils.GetEmptyRef<T>()
        // 也可以 new RawRef / Ref / Ref<T> 然后直接调用 GetRef
```

4) 检查引用的地址是否相等
```C#
        Capstones.UnityEngineEx.ByRefUtils.RefEquals<T>(ref T a, ref T b)
```

5) 检查一个引用是否为空
```C#
        // 与 GetEmptyRef 进行引用比等
        Capstones.UnityEngineEx.ByRefUtils.RefEquals<T>(ref T a, ref GetEmptyRef<T>())
        // 或者调用这个函数
        Capstones.UnityEngineEx.ByRefUtils.IsEmpty<T>(ref T r)
```

6) 最后我们可以使用它来进行快速（也危险）的类型转换
```C#
        var r = new Capstones.UnityEngineEx.ByRefUtils.RawRef();
        int i = 1;
        r.SetRef(ref i);
        var plat = r.GetRef<RuntimePlatform>(); // RuntimePlatform 是一个枚举
```

## 说明
1) 运行时的垃圾回收机制有可能移动托管堆上的对象，因此长时间持有一个指向托管堆的引用是危险的，也没有对此情况进行过详尽的测试。
2) 声明一个指向栈上的引用一般是安全的。但是要确定尽量不要访问栈顶之上的内存（如果是简单值类型引用似乎也没啥危险），以及进行类型转换时一定要确定值的内存大小和排列是兼容的。（比如对一个int的枚举使用ulong的引用去转的话，在小端系统上没啥问题，在大端系统上就不正确）
3) 在 .NET Core 3.0 和 Unity 中进行过测试 (Mono 和 IL2CPP 都进行过测试)。
4) 这个程序集纯用C#是无法实现的，但是IL(中间语言)是支持这些操作的，所以这个程序集才能做出来。部分代码是通过 Mono.Cecil 来编辑 IL 得到的。