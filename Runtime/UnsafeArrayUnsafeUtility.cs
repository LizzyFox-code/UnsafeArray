namespace UnsafeArray.Runtime
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static class UnsafeArrayUnsafeUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe UnsafeArray<T> ConvertExistingDataToNativeArray<T>(void* dataPointer, int length, AllocatorManager.AllocatorHandle allocatorHandle) where T : struct
        {
            CheckConvertArguments<T>(length);
            return new UnsafeArray<T>
            {
                m_Buffer = dataPointer,
                m_Length = length,
                m_Handle = allocatorHandle,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* GetUnsafePtr<T>(this UnsafeArray<T> nativeArray) where T : struct
        {
            return nativeArray.m_Buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this UnsafeArray<T>.ReadOnly nativeArray) where T : struct
        {
            return nativeArray.Buffer;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T ReadRef<T>(this UnsafeArray<T> array, int index) where T : struct
        {
            CheckOutOfRange(index, array.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRef<T>(this UnsafeArray<T> array, ref T data, int index) where T : unmanaged
        {
            CheckOutOfRange(index, array.Length);
            ((T*)array.GetUnsafePtr())[index] = data;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckOutOfRange(int index, int length)
        {
            if (index < 0 || index >= length)
                throw new IndexOutOfRangeException($"Index {index} is out of range of '{length}' Length");
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckConvertArguments<T>(int length) where T : struct
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof (length), "Length must be >= 0");
            UnsafeArray<T>.IsUnmanagedAndThrow();
        }
    }
}