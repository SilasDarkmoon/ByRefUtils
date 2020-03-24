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
        var r = new Capstones.ByRefUtils.RawRef();
        r.SetRef(ref i);
```
RawRef is a struct. You can also use ```Capstones.ByRefUtils.Ref``` (class) or ```Capstones.ByRefUtils.Ref<T>``` (class)

2) Get ref from RawRef / Ref / Ref<T>
```C#
        Capstones.ByRefUtils.RawRef r;
        //...
        ref int ri = ref r.GetRef<int>();
```

3) Get / Set Value
```C#
        int i = 0;
        var r = new Capstones.ByRefUtils.RawRef();
        r.SetRef(ref i);
        r.SetValue(2);
```

4) Get empty (null) ref
```C#
        Capstones.ByRefUtils.Ref.GetEmptyRef<T>()
        // or new RawRef / Ref / Ref<T> and GetRef from them
```

4) Check ref equals
```C#
        Capstones.ByRefUtils.Ref.RefEquals<T>(ref T a, ref T b)
```

5) Check a ref is empty (null)
```C#
        // check ref equals to GetEmptyRef
        Capstones.ByRefUtils.Ref.RefEquals<T>(ref T a, ref GetEmptyRef<T>())
        // or
        Capstones.ByRefUtils.Ref.IsEmpty<T>(ref T r)
```

6) We can do dangerous convert use it
```C#
        var r = new Capstones.ByRefUtils.RawRef();
        int i = 1;
        r.SetRef(ref i);
        var plat = r.GetRef<RuntimePlatform>(); // RuntimePlatform is an enum
```

## Remarks
1) CLR GC will move objects at managed heap, so holding a ref to an address at heap for a long time is dangerous. In this case, you should use TrackingRef instead.
2) Declaring a ref to a variable at stack is safe. But we should notice memory layout.
3) It is tested in .NET Core 3.0 and Unity (both Mono and IL2CPP)
4) We cannot implement this dll in C#, but IL can. Some of the code is injected with Mono.Cecil

## TrackingRef
Because objects on gc heap will be moved by garbage collector, so if you need a ref pointing to an object's field, you should use TrackingRef instead of Ref.

### How to use
1) Import and reference the ByRefUtils.TrackingRef.dll
2) Use ```Capstones.ByRefUtils.RawTrackingRef``` (struct) or ```Capstones.ByRefUtils.TrackingRef``` (class) or ```Capstones.ByRefUtils.TrackingRef<T>``` (class). You can use them just like RawRef/Ref.
3) Donot forget to Dispose TrackingRef.

### About the trick to implement TrackingRef
After gc moved the object, gc will auto change any ref on execution stack to the correct address. So we can make a new thread and make ref locals on this thread's execution stack. We use RawRef to get the address of locals on thread's stack and read real address from it.

## The Visual Studio Solution
1) Compile and Run Generator project (in Release mode) to generate ByRefUtils.dll and ByRefUtils.TrackingRef.dll
2) Compile and Run TestByRefUtils project to make a test.

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
3) 使用 ByRefUtils.dll 中的类型。（集中在 Capstones.ByRefUtils 命名空间中）

## 代码示例

1) 声明一个变量的引用
```C#
        int i = 0;
        var r = new Capstones.ByRefUtils.RawRef();
        r.SetRef(ref i);
```
RawRef 是值类型(struct). 也可以使用 ```Capstones.ByRefUtils.Ref``` (引用类型class) or ```Capstones.ByRefUtils.Ref<T>``` (泛型引用类型)

2) 从 RawRef / Ref / Ref<T> 里拿引用
```C#
        Capstones.ByRefUtils.RawRef r;
        //...
        ref int ri = ref r.GetRef<int>();
```

3) 取值/赋值
```C#
        int i = 0;
        var r = new Capstones.ByRefUtils.RawRef();
        r.SetRef(ref i);
        r.SetValue(2);
```

4) 拿一个空引用
```C#
        Capstones.ByRefUtils.Ref.GetEmptyRef<T>()
        // 也可以 new RawRef / Ref / Ref<T> 然后直接调用 GetRef
```

4) 检查引用的地址是否相等
```C#
        Capstones.ByRefUtils.Ref.RefEquals<T>(ref T a, ref T b)
```

5) 检查一个引用是否为空
```C#
        // 与 GetEmptyRef 进行引用比等
        Capstones.ByRefUtils.Ref.RefEquals<T>(ref T a, ref GetEmptyRef<T>())
        // 或者调用这个函数
        Capstones.ByRefUtils.Ref.IsEmpty<T>(ref T r)
```

6) 最后我们可以使用它来进行快速（也危险）的类型转换
```C#
        var r = new Capstones.ByRefUtils.RawRef();
        int i = 1;
        r.SetRef(ref i);
        var plat = r.GetRef<RuntimePlatform>(); // RuntimePlatform 是一个枚举
```

## 说明
1) 运行时的垃圾回收机制有可能移动托管堆上的对象，因此长时间持有一个指向托管堆的引用是危险的，这种情况下，请使用下面的TrackingRef。
2) 声明一个指向栈上的引用一般是安全的。但是要确定尽量不要访问栈顶之上的内存（如果是简单值类型引用似乎也没啥危险），以及进行类型转换时一定要确定值的内存大小和排列是兼容的。（比如对一个int的枚举使用ulong的引用去转的话，在小端系统上没啥问题，在大端系统上就不正确）
3) 在 .NET Core 3.0 和 Unity 中进行过测试 (Mono 和 IL2CPP 都进行过测试)。
4) 这个程序集纯用C#是无法实现的，但是IL(中间语言)是支持这些操作的，所以这个程序集才能做出来。部分代码是通过 Mono.Cecil 来编辑 IL 得到的。

## TrackingRef
因为运行时的垃圾回收机制有可能移动托管堆上的对象，如果想引用堆上对象的字段，应该使用TrackingRef来代替Ref。

### 怎样使用
1) 导入并引用 ByRefUtils.TrackingRef.dll
2) 使用 ```Capstones.ByRefUtils.RawTrackingRef``` (struct) 或 ```Capstones.ByRefUtils.TrackingRef``` (class) 或 ```Capstones.ByRefUtils.TrackingRef<T>``` (class) 代替 ```RawRef``` / ```Ref``` / ```Ref<T>```
3) 使用TrackingRef / RawTrackingRef后一定要记得 Dispose()！！

### 实现原理
当gc移动了堆上object之后，它会检查调用栈上是否有ref引用到这个object的字段，并自动将这些ref引用的地址更正为新的值。在TrackingRef中新建了一个线程来维护一个调用栈，这个线程的栈上基本全是ref局部变量。然后通过RawRef来获取线程栈上局部变量的地址（ref局部变量的地址，相当于指针的指针），然后从中读取一个IntPtr，就是真正的地址了。

## Visual Studio工程
1) 编译并运行 Generator 工程 (最好是 Release) 来生成 ByRefUtils.dll 与 ByRefUtils.TrackingRef.dll
2) 然后编译并执行 TestByRefUtils 工程来进行一下测试
