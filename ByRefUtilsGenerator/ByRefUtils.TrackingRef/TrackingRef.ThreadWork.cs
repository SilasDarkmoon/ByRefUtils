using System;

namespace Capstones.ByRefUtils
{
    internal partial class TrackingRefManager
    {
        protected static void MakeMoreSlot(TrackingRefManagerThreadWorkInput input, Action<IntPtr> onComplete)
        {
            RawRef r = new RawRef();
            r.SetRef(ref r);
            onComplete?.Invoke(r.Address);

            while (true)
            {
                TrackingRefManagerOp op;
                while (input.Ops.TryDequeue(out op))
                {
                    if (op.IsExit)
                    {
                        throw new ObjectDisposedException("this");
                    }
                    else if (op.IsPop)
                    {
                        return;
                    }
                    else
                    {
                        try
                        {
                            MakeMoreSlot(input, op.OnComplete);
                        }
                        catch (StackOverflowException)
                        {
                            op.OnComplete?.Invoke(IntPtr.Zero);
                        }
                    }
                }
                input.WaitHandle.WaitOne();
            }
        }
    }
}
