using Mod.LowLevel;
using System;

namespace TestByRefUtils
{
    class Program
    {
        static byte[] Buffer0;
        static byte[] Buffer1;
        static byte[] Buffer2;
        static byte[] Buffer3;
        static byte[] Buffer4;
        static byte[] Buffer5;

        static void Main(string[] args)
        {
            Buffer0 = new byte[1024];
            Buffer1 = new byte[1024];
            Buffer2 = new byte[1024];
            Buffer3 = new byte[1024];
            Buffer4 = new byte[1024];
            Buffer5 = new byte[1024];

            ref byte b = ref Buffer5[111];
            LocalRef lr = new LocalRef(0);
            TrackingRef<byte> r = new TrackingRef<byte>();
            r.SetRef(ref b);
            r.Value = 127;
            Console.WriteLine(r.Address.ToString("X"));
            Console.WriteLine(r.Value);
            Console.WriteLine(lr.Address.ToString("X"));
            Console.WriteLine(lr.GetRef<byte>());
            object fake = null;
            RawRef.Of(ref fake).GetRef<RawRef>().SetRef(ref b);
            Console.WriteLine(RawRef.Of(fake).Address.ToString("X"));
            Console.WriteLine(RawRef.Of(fake).GetRef<byte>());
            RefContainer container = default;
            RawRef.Of(ref container).GetRef<RawRef>().SetRef(ref b);
            Console.WriteLine(RawRef.Of(ref container).GetRef<RawRef>().Address.ToString("X"));
            Console.WriteLine(RawRef.Of(ref container).GetRef<RawRef>().GetRef<byte>());

            Console.WriteLine("Waiting for GC...");
            System.Threading.Thread.Sleep(2000);
            Buffer1 = null;
            Buffer2 = null;
            Buffer3 = null;
            Buffer4 = null;
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            System.GC.Collect(2, GCCollectionMode.Forced, true, true);
            System.GC.WaitForFullGCComplete();
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            System.GC.Collect(2, GCCollectionMode.Forced, true, true);
            System.GC.WaitForFullGCComplete();
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            System.GC.Collect(2, GCCollectionMode.Forced, true, true);
            System.GC.WaitForFullGCComplete();
            System.Threading.Thread.Sleep(2000);

            Console.WriteLine(r.Address.ToString("X"));
            Console.WriteLine(r.Value);
            Console.WriteLine(lr.Address.ToString("X"));
            Console.WriteLine(lr.GetRef<byte>());
            Console.WriteLine(RawRef.Of(fake).Address.ToString("X"));
            Console.WriteLine(RawRef.Of(fake).GetRef<byte>());
            Console.WriteLine(RawRef.Of(ref container).GetRef<RawRef>().Address.ToString("X"));
            Console.WriteLine(RawRef.Of(ref container).GetRef<RawRef>().GetRef<byte>());
            r.Dispose();

            PerfTest();

            //TrackingRef.Close();
        }

        static void PerfTest()
        {
            const int count = 1000000;
            byte[] buffer = new byte[1024];
            ref byte r = ref buffer[244];
            LocalRef lr = new LocalRef(0);
            RawRef rb = new RawRef();
            rb.SetRef(ref r);
            TrackingRef<byte> tr = new TrackingRef<byte>();
            tr.SetRef(ref r);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            sw.Stop();

            // Because of cache, the first run may be slow. Ignore first run.
            sw.Restart();
            //Console.WriteLine("Performance test - ref keyword:");
            for (int i = 0; i < count; i++)
            {
                int b = r;
                r = (byte)(b + 1);
            }
            sw.Stop();
            //Console.WriteLine($"ref keyword: {sw.ElapsedMilliseconds} ms");
            
            Console.WriteLine("Performance test - ref keyword:");
            sw.Restart();
            for (int i = 0; i < count; i++)
            {
                int b = r;
                r = (byte)(b + 1);
            }
            sw.Stop();
            Console.WriteLine($"ref keyword: {sw.ElapsedMilliseconds} ms");

            Console.WriteLine("Performance test - RawRef:");
            sw.Restart();
            for (int i = 0; i < count; i++)
            {
                int b = rb.GetValue<byte>();
                rb.SetValue((byte)(b + 1));
            }
            sw.Stop();
            Console.WriteLine($"RawRef: {sw.ElapsedMilliseconds} ms");

            Console.WriteLine("Performance test - LocalRef:");
            sw.Restart();
            for (int i = 0; i < count; i++)
            {
                int b = lr.GetRef<byte>();
                lr.GetRef<byte>() = ((byte)(b + 1));
            }
            sw.Stop();
            Console.WriteLine($"LocalRef: {sw.ElapsedMilliseconds} ms");

            Console.WriteLine("Performance test - TrackingRef:");
            sw.Restart();
            for (int i = 0; i < count; i++)
            {
                int b = tr.Value;
                tr.Value = ((byte)(b + 1));
            }
            sw.Stop();
            Console.WriteLine($"TrackingRef: {sw.ElapsedMilliseconds} ms");
            tr.Dispose();

            Console.WriteLine("Performance test - create ref keyword:");
            sw.Restart();
            for (int i = 0; i < count; i++)
            {
                r = ref buffer[i % buffer.Length];
                r = (byte)i;
            }
            sw.Stop();
            Console.WriteLine($"ref keyword: {sw.ElapsedMilliseconds} ms");

            Console.WriteLine("Performance test - create RawRef:");
            sw.Restart();
            for (int i = 0; i < count; i++)
            {
                rb = new RawRef();
                rb.SetRef(ref buffer[i % buffer.Length]);
                rb.SetValue((byte)i);
            }
            sw.Stop();
            Console.WriteLine($"RawRef: {sw.ElapsedMilliseconds} ms");

            Console.WriteLine("Performance test - create TrackingRef:");
            sw.Restart();
            for (int i = 0; i < count; i++)
            {
                tr = new TrackingRef<byte>();
                tr.SetRef(ref buffer[i % buffer.Length]);
                tr.Value = ((byte)i);
                tr.Dispose();
            }
            sw.Stop();
            Console.WriteLine($"TrackingRef: {sw.ElapsedMilliseconds} ms");
        }
    }
}