namespace UnsafeArray.Runtime
{
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    internal sealed class UnsafeArrayDebugView<T> where T : struct
    {
        private UnsafeArray<T> m_Array;

        public unsafe T[] Items
        {
            get
            {
                if (!m_Array.IsCreated)
                    return null;
                var length = m_Array.m_Length;
                var objArray = new T[length];
                var gcHandle = GCHandle.Alloc(objArray, GCHandleType.Pinned);
                UnsafeUtility.MemCpy((void*) gcHandle.AddrOfPinnedObject(), m_Array.m_Buffer, length * UnsafeUtility.SizeOf<T>());
                gcHandle.Free();
                return objArray;
            }
        }
    
        public UnsafeArrayDebugView(UnsafeArray<T> array) => m_Array = array;
    }
}