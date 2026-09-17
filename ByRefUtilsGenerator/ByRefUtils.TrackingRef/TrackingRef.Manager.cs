using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Mod.LowLevel
{
    /// <summary>
    /// Thread-safe integer pool (lock-free; synchronized entirely via Interlocked/Volatile).
    ///
    /// Composition:
    ///  _NextPacked —— fresh-value allocator + trim mutex (packed into one long:
    ///                low 32 bits = next value to hand out, high 32 bits = trim flag).
    ///                The allocated range is [0, value).
    ///  _Bag        —— free-list storage, a 4-level array _Bag[b1][b2][b3][b4];
    ///                 the index is split into 4 bytes, b4 being the lowest one.
    ///  _Head       —— head of the free list.
    ///  _Allocated  —— allocation bitmap, same 4-level shape as _Bag but with
    ///                 ulong[4] leaves (256 values = 4 words x 64 bits per leaf).
    ///                 bit=1 ⟺ the value is currently handed out. Take sets the
    ///                 bit (Interlocked.Or; a bit already set = DOUBLE ALLOCATION),
    ///                 Return clears it (Interlocked.And; a bit already clear =
    ///                 DOUBLE RETURN). Both checks are O(1) and atomic (the
    ///                 Interlocked op returns the old value), turning the old
    ///                 "silent pool poisoning that explodes in a random later
    ///                 test" into an immediate, located throw at the offending
    ///                 call site.
    ///                 Bitmap levels are allocated on demand via CAS (idempotent:
    ///                 the loser of the CAS simply uses the winner's array) and
    ///                 leaves are reclaimed inside the trim window together with
    ///                 the _Bag leaves (the window blocks fresh issuing and the
    ///                 trim precondition guarantees no live holder in the range,
    ///                 so no concurrent bit write can race the reclaim).
    ///                 Note: the bitmap detects same-generation double return;
    ///                 a stale copy returning a value that has already been
    ///                 re-issued is epoch territory (a future extension).
    ///
    /// List encoding:
    ///   _Head low 32 bits = head index+1; 0 means the list is empty
    ///                        (this is the _BagHead from the original spec);
    ///   _Bag[i]            = next element in the list (next index+1);
    ///                        -1 marks the list tail, 0 means "not in the list".
    ///   _Head high 32 bits = version number, incremented on every head change
    ///                        (eliminates ABA on the head CAS).
    ///
    /// Concurrency-safe automatic leaf trimming (leaf arrays are reclaimed when
    /// _Next falls back onto a leaf boundary):
    ///   The ONLY source of dangerous writes is "a fresh value being issued inside
    ///   the target leaf" — existing holders cannot exist (_Next can only fall back
    ///   to boundary B ⟹ every value in [B, B+256) has been returned; if the list
    ///   contained an entry of this leaf, _Next would be stuck above it).
    ///   Therefore, upon falling back to a boundary we CAS-set the trim flag (fresh
    ///   issuers spin-wait), during which the target leaf cannot receive any new
    ///   writes, making the all-zero check plus the reclaim absolutely safe;
    ///   finally the flag is cleared with another CAS.
    ///   A top-of-stack return that encounters the trim flag does NOT wait — it
    ///   falls through to the free list instead (semantically lossless).
    ///
    /// Contract: every taken value is returned exactly once (single-return semantics).
    /// Value range: [0, int.MaxValue).
    /// </summary>
    public class IntBag
    {
        private const int LeafSize = 256;

        /// <summary>Disables automatic leaf trimming (keeps leaf arrays as a cache
        /// so a later re-expansion of the value range avoids rebuilding them). Enabled by default.</summary>
        public bool AllowAutomaticLeafTrim = true;

        // Low 32 bits: head encoding (head index+1, 0 = empty); high 32 bits: ABA version.
        private long _Head = 0;

        // Low 32 bits = next value to hand out; high 32 bits = trim flag (0 = none, 1 = trim in progress).
        private long _NextPacked = 0;

        private readonly int[][][][] _Bag = new int[256][][][];

        // Allocation bitmap: _Allocated[b1][b2][b3] is a long[4] leaf covering the same
        // 256-value range as the _Bag leaf (4 words x 64 bits). bit=1 ⟺ value is taken.
        // (long rather than ulong: netstandard2.0 has no Interlocked overloads for
        // unsigned types — the bit patterns are identical either way.)
        private readonly long[][][][] _Allocated = new long[256][][][];

        private static int NextValue(long packed) => (int)packed;
        private static long Pack(int value, int trim) => ((long)trim << 32) | (uint)value;

        // ================ allocation bitmap ================

        /// <summary>
        /// Ensures the bitmap leaf covering <paramref name="index"/> exists, allocating
        /// missing intermediate levels on demand via CAS (idempotent: a CAS loser
        /// simply picks up the winner's array — no lock, no leak, no torn state).
        /// </summary>
        private long[] EnsureAllocatedLeaf(int index)
        {
            int b1 = (byte)(index >> 24);
            int b2 = (byte)(index >> 16);
            int b3 = (byte)(index >> 8);

            var l3 = _Allocated[b1];
            if (l3 == null)
            {
                Interlocked.CompareExchange(ref _Allocated[b1], new long[256][][], null);
                l3 = _Allocated[b1];
            }

            var l2 = l3[b2];
            if (l2 == null)
            {
                Interlocked.CompareExchange(ref l3[b2], new long[256][], null);
                l2 = l3[b2];
            }

            var leaf = l2[b3];
            if (leaf == null)
            {
                Interlocked.CompareExchange(ref l2[b3], new long[4], null);
                leaf = l2[b3];
            }
            return leaf;
        }

        /// <summary>Read-only leaf lookup; null when any level is not yet allocated
        /// (a value below _Next always has an allocated leaf unless a trim reclaimed
        /// it — both mean "bit is clear").</summary>
        private long[] GetAllocatedLeaf(int index)
        {
            var l3 = _Allocated[(byte)(index >> 24)];
            if (l3 == null) return null;
            var l2 = l3[(byte)(index >> 16)];
            if (l2 == null) return null;
            return l2[(byte)(index >> 8)];
        }

        /// <summary>
        /// Marks <paramref name="index"/> as allocated. Returns false when the bit was
        /// already set — the value was handed out twice (double allocation: the free
        /// list or the fresh counter is corrupted).
        /// </summary>
        private bool TryMarkAllocated(int index)
        {
            var leaf = EnsureAllocatedLeaf(index);
            long bit = 1L << (index & 63);
            long old = InterlockedOr(ref leaf[(index >> 6) & 3], bit);
            return (old & bit) == 0;
        }

        /// <summary>
        /// Marks <paramref name="index"/> as returned. Returns false when the bit was
        /// already clear — the value was returned twice (or was never allocated:
        /// the range check in Return has already rejected never-allocated values,
        /// so reaching here with a clear bit means a duplicate return).
        /// </summary>
        private bool TryMarkReturned(int index)
        {
            var leaf = GetAllocatedLeaf(index);
            if (leaf == null) return false;
            long bit = 1L << (index & 63);
            long old = InterlockedAnd(ref leaf[(index >> 6) & 3], ~bit);
            return (old & bit) != 0;
        }

        /// <summary>Atomic OR returning the old value (Interlocked.Or is .NET 9+;
        /// this build targets netstandard2.0, so a CAS loop it is).</summary>
        private static long InterlockedOr(ref long location, long value)
        {
            long comparand = Volatile.Read(ref location);
            while (true)
            {
                long original = Interlocked.CompareExchange(ref location, comparand | value, comparand);
                if (original == comparand) return original;
                comparand = original;
            }
        }

        /// <summary>Atomic AND returning the old value (Interlocked.And is .NET 9+;
        /// this build targets netstandard2.0, so a CAS loop it is).</summary>
        private static long InterlockedAnd(ref long location, long value)
        {
            long comparand = Volatile.Read(ref location);
            while (true)
            {
                long original = Interlocked.CompareExchange(ref location, comparand & value, comparand);
                if (original == comparand) return original;
                comparand = original;
            }
        }

        /// <summary>
        /// Take a value: preferably from the free list (returns _BagHead-1 when _BagHead != 0);
        /// if the list is empty, increments _Next and returns the pre-increment value.
        /// </summary>
        public int Take()
        {
            // ---- 1. Try the free list first ----
            while (true)
            {
                long head = Volatile.Read(ref _Head);
                int headEnc = (int)head;                 // head encoding (low 32 bits)
                if (headEnc == 0)
                    break;                               // list empty → fresh allocation

                int index = headEnc - 1;                 // the value to return this time
                int link = GetBagValue(index);           // next element in the list
                int nextEnc = link == -1 ? 0 : link;     // tail(-1) → empty list(0)

                long newHead = ((head >> 32) + 1) << 32 | (uint)nextEnc;
                if (Interlocked.CompareExchange(ref _Head, newHead, head) == head)
                {
                    // CAS won: the value is definitively ours — mark it allocated.
                    // A bit already set means the free list handed out a value twice.
                    if (!TryMarkAllocated(index))
                        throw new InvalidOperationException($"IntBag: double allocation of value {index} (free list corrupted).");
                    // Clear the slot (0 = not in list) — prerequisite for
                    // the reliability of Return's duplicate-return check.
                    SetBagValue(index, 0);
                    return index;
                }
                // CAS lost (version differs) → retry.
            }

            // ---- 2. Free list empty: allocate a fresh value (CAS loop; spin while a trim is in progress) ----
            while (true)
            {
                long packed = Volatile.Read(ref _NextPacked);
                if ((int)(packed >> 32) != 0)
                {
                    Thread.SpinWait(16);                 // trim in progress (very short) — spin
                    continue;
                }
                int next = NextValue(packed);
                if (Interlocked.CompareExchange(ref _NextPacked, Pack(next + 1, 0), packed) == packed)
                {
                    // CAS won: the value is definitively ours — mark it allocated.
                    // A bit already set means the fresh counter re-issued a live value.
                    if (!TryMarkAllocated(next))
                        throw new InvalidOperationException($"IntBag: double allocation of value {next} (fresh counter corrupted).");
                    return next;
                }
                // CAS lost (concurrent issue / return) → retry.
            }
        }

        /// <summary>
        /// Return a value.
        /// If index == _Next-1 (the top), _Next is simply rolled back; when the rollback
        /// lands exactly on a leaf boundary the leaf array is reclaimed automatically;
        /// otherwise the value is pushed onto the free list.
        /// </summary>
        public void Return(int index)
        {
            if ((uint)index >= (uint)NextValue(Volatile.Read(ref _NextPacked)))
                throw new ArgumentOutOfRangeException(nameof(index), "Returned a value that was never taken.");
            // Duplicate-return check (atomic, O(1)): a clear bit means this value has
            // already been returned — previously this case was silently ignored (or, in
            // the top-rollback timing, corrupted _Next), letting the damage travel
            // through the pool and explode in a random later Take/Return. Now it
            // throws at the offending call site.
            if (!TryMarkReturned(index))
                throw new InvalidOperationException($"IntBag: value {index} was returned twice (duplicate return detected).");

            // ---- 1. Top value: roll _Next back directly ----
            while (true)
            {
                long packed = Volatile.Read(ref _NextPacked);
                int next = NextValue(packed);
                if (index != next - 1)
                    break;                               // not the top → free list
                if ((int)(packed >> 32) != 0)
                    break;                               // trim in progress → free list instead (non-blocking, lossless)

                // CAS rollback (a plain write would race with Take's issuing CAS and double-issue values).
                if (Interlocked.CompareExchange(ref _NextPacked, Pack(next - 1, 0), packed) == packed)
                {
                    // Rollback landed exactly on a leaf boundary → automatic multi-level trim
                    // (best effort: if we lose the race, leave it for the next time).
                    if (AllowAutomaticLeafTrim && next > 0 && (next - 1) % LeafSize == 0)
                        TryTrimLevels(next - 1);
                    return;
                }
                // CAS lost → retry (index may no longer be the top and fall through to the free list).
            }

            // ---- 2. Not the top: push onto the free list ----
            bool firstAttempt = true;
            while (true)
            {
                long head = Volatile.Read(ref _Head);
                int headEnc = (int)head;

                if (firstAttempt)
                {
                    // Duplicate-return check (exact): done on the first attempt only —
                    // on a CAS-retry, the link value written by our own previous round
                    // is still sitting in the slot and would be misjudged as "already in list".
                    if (GetBagValue(index) != 0 || headEnc == index + 1)
                        return;
                    firstAttempt = false;
                }

                // Write the link: points at the current head; write -1 when the list is empty (tail marker).
                SetBagValue(index, headEnc == 0 ? -1 : headEnc);

                long newHead = ((head >> 32) + 1) << 32 | (uint)(index + 1);
                if (Interlocked.CompareExchange(ref _Head, newHead, head) == head)
                    return;                              // became the new head — return complete
                // CAS lost → retry (the link value gets overwritten with the new head).
            }
        }

        /// <summary>
        /// Automatic multi-level trim (precondition: this thread has just rolled _Next back
        /// to baseIndex, and baseIndex is a leaf boundary — a multiple of 256).
        /// Protocol: CAS-set the trim flag (blocks fresh issuing) → trim from the lowest
        /// level upward (leaf, then l2 at 65536 boundaries, then l3 at 16777216 boundaries)
        /// → CAS-clear the flag.
        ///
        /// Level eligibility and safety (all levels share the same line of reasoning):
        ///   leaf level: [B, B+256) all returned → the leaf int[256] can be reclaimed;
        ///   l2  level:  B % 65536 == 0 ⟹ [B, B+65536) all returned ⟹ the l2 array
        ///               (l3[b2], covering exactly that range) can be reclaimed;
        ///   l3  level:  B % 16777216 == 0 ⟹ [B, B+16777216) all returned ⟹ the l3 array
        ///               (_Bag[b1]) can be reclaimed.
        ///   Holders cannot exist above _Next; list entries of these ranges would have
        ///   blocked the rollback; push-onto-list writes only touch indices < _Next and
        ///   therefore can never reach into the target range; the only write source —
        ///   fresh issuing — is blocked by the trim flag.
        ///
        /// Order matters (smallest level first): when _Next lands on a 65536 boundary the
        /// leaf [B, B+256) itself has not been reclaimed yet, so the l2 all-null check
        /// would always fail if attempted first. Clearing the leaf first lets the l2
        /// check succeed, which in turn lets the l3 check succeed — the whole chain can
        /// be reclaimed within a single trim window (e.g. _Next returning to 0 clears
        /// leaf, l2 and l3 in one go).
        ///
        /// Best effort: on any contention (_Next already changed / trim already set)
        /// it simply gives up and leaves the work to the next opportunity.
        /// A non-zero leaf (broken contract) aborts all higher levels too — safer to
        /// skip the reclaim than to tear the list.
        /// </summary>
        private void TryTrimLevels(int baseIndex)
        {
            // ---- 1. Acquire the trim right: only if _Next is still at baseIndex and no other trim is active ----
            long packed = Volatile.Read(ref _NextPacked);
            if (NextValue(packed) != baseIndex || (int)(packed >> 32) != 0)
                return;
            if (Interlocked.CompareExchange(ref _NextPacked, Pack(baseIndex, 1), packed) != packed)
                return;                                  // CAS lost → give up (try again later)

            try
            {
                int[][][] l3 = _Bag[(byte)(baseIndex >> 24)];
                int[][] l2 = null;
                bool leafDone = false;

                // ---- 2. Leaf level: check all-zero and reclaim [baseIndex, baseIndex+256) ----
                if (l3 != null)
                {
                    l2 = l3[(byte)(baseIndex >> 16)];
                    if (l2 == null)
                    {
                        leafDone = true;                 // leaf already reclaimed → continue with l2/l3
                    }
                    else
                    {
                        int[] leaf = l2[(byte)(baseIndex >> 8)];
                        if (leaf == null)
                        {
                            leafDone = true;             // same
                        }
                        else
                        {
                            // In theory this must be all-zero (see class doc). The check is
                            // a defense against a broken contract — abort everything, stay safe.
                            bool allZero = true;
                            for (int i = 0; i < LeafSize; i++)
                            {
                                if (Volatile.Read(ref leaf[i]) != 0) { allZero = false; break; }
                            }
                            if (!allZero)
                                return;                 // contract broken → skip all levels

                            Interlocked.Exchange(ref l2[(byte)(baseIndex >> 8)], null);
                            leafDone = true;
                        }
                    }
                }

                // ---- 3. l2 level: only at a 65536 boundary; reclaim l3[b2            }

            // ---- 2b. Bitmap leaf for the same range: defensive all-zero check, then reclaim ----
            // Inside the trim window fresh issuing is blocked and the trim precondition
            // guarantees no live holder in [baseIndex, baseIndex+256) — so no set bit
            // can appear concurrently. A set bit here means a broken contract — skip
            // everything, stay safe (same philosophy as the _Bag all-zero check).
            {
                var aL3 = _Allocated[(byte)(baseIndex >> 24)];
                if (aL3 != null)
                {
                    var aL2 = aL3[(byte)(baseIndex >> 16)];
                    if (aL2 != null)
                    {
                        var aLeaf = aL2[(byte)(baseIndex >> 8)];
                        if (aLeaf != null)
                        {
                            bool bitsAllZero = true;
                            for (int i = 0; i < 4; i++)
                            {
                                if (Volatile.Read(ref aLeaf[i]) != 0L) { bitsAllZero = false; break; }
                            }
                            if (!bitsAllZero)
                                return;                 // contract broken → skip all levels
                            Interlocked.Exchange(ref aL2[(byte)(baseIndex >> 8)], null);
                        }
                    }
                }
            }

            // ---- 3. l2 level: only at a 65536 boundary; reclaim l3[b2] if all 256 leaves are gone ----
                if (leafDone && baseIndex % (LeafSize * LeafSize) == 0 && l3 != null && l2 != null)
                {
                    bool allNull = true;
                    for (int i = 0; i < 256; i++)
                    {
                        if (l2[i] != null) { allNull = false; break; }
                    }
                    if (allNull)
                        Interlocked.Exchange(ref l3[(byte)(baseIndex >> 16)], null);
                }

                // ---- 4. l3 level: only at a 16777216 boundary; reclaim _Bag[b1] if all 256 l2s are gone ----
                if (leafDone && baseIndex % (LeafSize * LeafSize * LeafSize) == 0 && l3 != null)
                {
                    bool allNull = true;
                    for (int i = 0; i < 256; i++)
                    {
                        if (l3[i] != null) { allNull = false; break; }
                    }
                    if (allNull)
                        Interlocked.Exchange(ref _Bag[(byte)(baseIndex >> 24)], null);
                }
            }
            finally
            {
                // ---- 5. Release: clear the trim flag (value unchanged) ----
                while (true)
                {
                    long p = Volatile.Read(ref _NextPacked);
                    if (Interlocked.CompareExchange(ref _NextPacked, (uint)NextValue(p), p) == p)
                        break;
                }
            }
        }

        // ================ _Bag access ================

        /// <summary>Read _Bag[index]; returns 0 (equivalent to "not in list") if any level is not yet allocated.</summary>
        private int GetBagValue(int index)
        {
            int[][][] l3 = _Bag[(byte)(index >> 24)];
            if (l3 == null) return 0;
            int[][] l2 = l3[(byte)(index >> 16)];
            if (l2 == null) return 0;
            int[] leaf = l2[(byte)(index >> 8)];
            if (leaf == null) return 0;
            return Volatile.Read(ref leaf[(byte)index]);
        }

        /// <summary>Write _Bag[index] = value; allocates missing intermediate levels on demand (via CAS).</summary>
        private void SetBagValue(int index, int value)
        {
            int b1 = (byte)(index >> 24);
            int b2 = (byte)(index >> 16);
            int b3 = (byte)(index >> 8);
            int b4 = (byte)index;

            int[][][] l3 = _Bag[b1];
            if (l3 == null)
            {
                Interlocked.CompareExchange(ref _Bag[b1], new int[256][][], null);
                l3 = _Bag[b1];
            }

            int[][] l2 = l3[b2];
            if (l2 == null)
            {
                Interlocked.CompareExchange(ref l3[b2], new int[256][], null);
                l2 = l3[b2];
            }

            int[] leaf = l2[b3];
            if (leaf == null)
            {
                Interlocked.CompareExchange(ref l2[b3], new int[LeafSize], null);
                leaf = l2[b3];
            }

            Volatile.Write(ref leaf[b4], value);
        }

        /// <summary>
        /// Manual multi-level reclaim: walks the entire _Bag tree and reclaims every
        /// fully-idle leaf (all-zero, list head not inside), then reclaims l2 arrays
        /// whose 256 leaves are all gone, then l3 arrays whose 256 l2s are all gone.
        /// The cascade happens within a single pass: a leaf reclaimed by this very
        /// call counts as gone for its parent l2, and a just-reclaimed l2 counts as
        /// gone for its parent l3 (mirroring the single-window cascade of the
        /// automatic trim).
        /// Precondition: no concurrent Take/Return when called (a quiescent point).
        /// Complements the automatic trim: the automatic variant only reclaims ranges
        /// that _Next just rolled back past, whereas this method can reclaim any
        /// historically idle structure (e.g. non-boundary leaves left by out-of-order returns).
        /// Returns the number of leaf arrays reclaimed (higher levels are not counted).
        /// </summary>
        public int TrimLeaves()
        {
            long head = Volatile.Read(ref _Head);
            int headEnc = (int)head;
            int headLeaf = headEnc == 0 ? -1 : (headEnc - 1) / LeafSize;

            int trimmed = 0;
            for (int b1 = 0; b1 < 256; b1++)
            {
                int[][][] l3 = _Bag[b1];
                if (l3 == null) continue;
                bool l3AllGone = true;
                for (int b2 = 0; b2 < 256; b2++)
                {
                    int[][] l2 = l3[b2];
                    if (l2 == null) continue;
                    bool l2Survives = false;
                    for (int b3 = 0; b3 < 256; b3++)
                    {
                        int[] leaf = l2[b3];
                        if (leaf == null) continue;

                        if (((b1 << 16) | (b2 << 8) | b3) == headLeaf)
                        {
                            l2Survives = true;           // the head leaf must stay
                            continue;
                        }

                        bool allZero = true;
                        for (int i = 0; i < LeafSize; i++)
                        {
                            if (Volatile.Read(ref leaf[i]) != 0) { allZero = false; break; }
                        }
                        if (allZero)
                        {
                            Interlocked.Exchange(ref l2[b3], null);
                            trimmed++;
                            // reclaimed by this very pass → does NOT keep its l2 alive
                        }
                        else
                        {
                            l2Survives = true;           // a live leaf survives
                        }
                    }
                    if (l2Survives)
                    {
                        l3AllGone = false;               // this l2 stays → its l3 stays
                    }
                    else
                    {
                        // all 256 leaves gone (null before, or just reclaimed above)
                        Interlocked.Exchange(ref l3[b2], null);
                    }
                }
                if (l3AllGone)
                {
                    // all 256 l2s gone (null before, or just reclaimed above)
                    Interlocked.Exchange(ref _Bag[b1], null);
                }
            }
            return trimmed;
        }
    }

    internal partial class TrackingRefManager : IDisposable
    {
        protected struct TrackingRefManagerOp
        {
            public Action<IntPtr> OnComplete;
            public bool IsPop;
            public bool IsExit;
        }
        protected class TrackingRefManagerThreadWorkInput
        {
            public ConcurrentQueue<TrackingRefManagerOp> Ops;
            public AutoResetEvent WaitHandle;
        }
        protected ConcurrentQueue<TrackingRefManagerOp> _StackOps = new ConcurrentQueue<TrackingRefManagerOp>();
        protected AutoResetEvent _WaitForStackOp = new AutoResetEvent(false);
        protected Thread _Thread;

        public TrackingRefManager()
        {
            _Thread = new Thread(ThreadWork);
            _Thread.IsBackground = true;
            _Thread.Start(new TrackingRefManagerThreadWorkInput() { Ops = _StackOps, WaitHandle = _WaitForStackOp });
        }

        protected static void ThreadWork(object state)
        {
            TrackingRefManagerThreadWorkInput input = state as TrackingRefManagerThreadWorkInput;
            try
            {
                while (true)
                {
                    input.WaitHandle.WaitOne();
                    TrackingRefManagerOp op;
                    while (input.Ops.TryDequeue(out op))
                    {
                        if (op.IsPop || op.IsExit)
                        {
                            return;
                        }
                        else
                        {
                            MakeMoreSlot(input, op.OnComplete);
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            finally
            {
                TrackingRefManagerOp op;
                while (input.Ops.TryDequeue(out op))
                {
                    op.OnComplete?.Invoke(IntPtr.Zero);
                }
                input.WaitHandle.Dispose();
            }
        }

        public void EnqueueGrowWork(Action<IntPtr> onComplete)
        {
            _StackOps.Enqueue(new TrackingRefManagerOp()
            {
                OnComplete = onComplete
            });
            _WaitForStackOp.Set();
        }

        public static readonly TrackingRefManager GlobalManager = new TrackingRefManager();

        internal struct LevelInfo
        {
            public volatile IntPtr BaseAddress;
            public volatile int IsReady;
        }
        internal readonly LevelInfo[] Levels = new LevelInfo[1024];
        internal volatile int FilledLevelCount = 0;

        internal int TakeNewLevel()
        {
            while (true)
            {
                var fcnt = FilledLevelCount;
                if (fcnt >= Levels.Length)
                {
                    break;
                }
                if (Interlocked.CompareExchange(ref FilledLevelCount, fcnt + 1, fcnt) == fcnt)
                {
                    GlobalManager.EnqueueGrowWork(address =>
                    {
                        Levels[fcnt].BaseAddress = address;
                        Levels[fcnt].IsReady = 1;
                    });
                    while (Levels[fcnt].IsReady == 0)
                    {
                        Thread.Sleep(0);
                    }
                }
                if (Levels[fcnt].BaseAddress != IntPtr.Zero)
                {
                    return fcnt;
                }
                else
                {
                    return -1;
                }
            }
            return -1;
        }

        public void SetRef<T>(int level, int slot, ref T r)
        {
            var baseAddress = Levels[level].BaseAddress;
            if (baseAddress != IntPtr.Zero)
            {
                var slotAddress = baseAddress;
                if (LocalRef.StackDir > 0)
                {
                    slotAddress -= (slot) * IntPtr.Size;
                }
                else
                {
                    slotAddress += (slot) * IntPtr.Size;
                }
                while (true)
                {
                    RawRef source = new RawRef();
                    source.SetRef(ref r);
                    var sourceAddress = source.Address;

                    RawRef dest = new RawRef();
                    dest.Address = slotAddress;
                    dest.SetValue(sourceAddress);

                    source.SetRef(ref r);
                    if (sourceAddress == source.Address)
                    {
                        return;
                    }
                }
            }
        }
        public ref T GetRef<T>(int level, int slot)
        {
            var baseAddress = Levels[level].BaseAddress;
            if (baseAddress != IntPtr.Zero)
            {
                var slotAddress = baseAddress;
                if (LocalRef.StackDir > 0)
                {
                    slotAddress -= (slot) * IntPtr.Size;
                }
                else
                {
                    slotAddress += (slot) * IntPtr.Size;
                }
                while (true)
                {
                    RawRef dest = new RawRef();
                    dest.Address = slotAddress;
                    var destAddress = dest.GetValue<IntPtr>();

                    RawRef real = new RawRef();
                    real.Address = destAddress;
                    ref T r = ref real.GetRef<T>();

                    if (destAddress == dest.GetValue<IntPtr>())
                    {
                        return ref r;
                    }
                }
            }
            return ref Ref.GetEmptyRef<T>();
        }

        public RawRef GetSlotRef(int level, int slot)
        {
            var baseAddress = Levels[level].BaseAddress;
            if (baseAddress != IntPtr.Zero)
            {
                var slotAddress = baseAddress;
                if (LocalRef.StackDir > 0)
                {
                    slotAddress -= (slot) * IntPtr.Size;
                }
                else
                {
                    slotAddress += (slot) * IntPtr.Size;
                }
                RawRef dest = new RawRef();
                dest.Address = slotAddress;
                return dest;
            }
            return new RawRef();
        }

        internal readonly IntBag _SlotBag = new IntBag();
        public (int level, int slot) TakeSlot()
        {
            var index = _SlotBag.Take();
            if (index >= 1024 * 1024)
            {
                _SlotBag.Return(index);
                return (-1, -1);
            }
            var level = index / 1024;
            var slot = index % 1024;
            while (FilledLevelCount <= level)
            {
                TakeNewLevel();
            }
            while (Levels[level].IsReady == 0)
            {
                Thread.Sleep(0);
            }
            return (level, slot);
        }
        public void ReturnSlot(int level, int slot)
        {
            var index = level * 1024 + slot;
            GetSlotRef(level, slot).SetValue(IntPtr.Zero);
            _SlotBag.Return(index);
        }
        public void Preserve(int count)
        {
            var level = count / 1024;
            while (FilledLevelCount <= level)
            {
                TakeNewLevel();
            }
            while (Levels[level].IsReady == 0)
            {
                Thread.Sleep(0);
            }
        }

        #region IDisposable Support
        private bool _Disposed = false; // 要检测冗余调用
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                _Disposed = true;
                if (_Thread != null)
                {
                    _StackOps.Enqueue(new TrackingRefManagerOp() { IsExit = true });
                    _WaitForStackOp.Set();
                    _Thread = null;
                }
            }
        }
        ~TrackingRefManager()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }
        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
