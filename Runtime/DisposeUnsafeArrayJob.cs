namespace UnsafeArray.Runtime
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    internal struct DisposeUnsafeArrayJob<T> : IJob where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        public unsafe void* Buffer;
        public AllocatorManager.AllocatorHandle Handle;
        
        public int Length;
        
        public unsafe void Execute()
        {
            AllocatorManager.Free(Handle, Buffer, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), Length);
        }
    }
}