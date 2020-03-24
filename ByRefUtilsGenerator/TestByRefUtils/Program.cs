using Capstones.ByRefUtils;
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

            ref byte b = ref Buffer5[0];
            TrackingRef<byte> r = new TrackingRef<byte>();
            r.SetRef(ref b);
            r.Value = 127;
            Console.WriteLine(r.Address.ToString("X"));
            Console.WriteLine(r.Value);

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
            r.Dispose();
            //TrackingRef.Close();
        }
    }
}
