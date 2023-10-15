namespace UnsafeArray.Runtime
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Diagnostics.CodeAnalysis;
  using System.Runtime.CompilerServices;
  using System.Runtime.InteropServices;
  using Unity.Burst;
  using Unity.Collections;
  using Unity.Collections.LowLevel.Unsafe;
  using Unity.Jobs;

  [DebuggerTypeProxy(typeof (UnsafeArrayDebugView<>))]
  [DebuggerDisplay("Length = {m_Length}, IsCreated = {IsCreated}")]
  public struct UnsafeArray<T> : INativeDisposable, IEnumerable<T>, IEquatable<UnsafeArray<T>>
    where T : struct
  {
    [NativeDisableUnsafePtrRestriction] 
    internal unsafe void* m_Buffer;

    internal int m_Length;
    internal AllocatorManager.AllocatorHandle m_Handle;

    public readonly int Length
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => m_Length;
    }

    public unsafe bool IsCreated
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => (IntPtr) m_Buffer != IntPtr.Zero;
    }

    public unsafe T this[int index]
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      readonly get
      {
        FailOutOfRangeError(index);
        return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
      }
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set
      {
        FailOutOfRangeError(index);
        UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
      }
    }

    public unsafe UnsafeArray(int length, AllocatorManager.AllocatorHandle handle,
      NativeArrayOptions options = NativeArrayOptions.ClearMemory)
    {
      m_Handle = handle;
      m_Length = length;

      m_Buffer = AllocatorManager.Allocate(handle, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), length);

      if (options != NativeArrayOptions.ClearMemory)
        return;

      UnsafeUtility.MemClear(m_Buffer, UnsafeUtility.SizeOf<T>() * length);
    }

    public unsafe void Dispose()
    {
      AllocatorManager.Free(m_Handle, m_Buffer, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), m_Length);
      m_Handle = Allocator.Invalid;
      m_Buffer = null;
    }

    public unsafe JobHandle Dispose(JobHandle inputDeps)
    {
      var disposeJob = new DisposeUnsafeArrayJob<T>
      {
        Buffer = m_Buffer,
        Handle = m_Handle,
        Length = m_Length
      };
      var jobHandle = disposeJob.Schedule(inputDeps);

      m_Handle = Allocator.Invalid;
      m_Buffer = null;

      return jobHandle;
    }

    public void CopyFrom([NotNull] T[] array) => Copy(array, this);

    public void CopyFrom(UnsafeArray<T> array) => Copy(array, this);

    public readonly void CopyTo([NotNull] T[] array) => Copy(this, array);

    public readonly void CopyTo(UnsafeArray<T> array) => Copy(this, array);

    public readonly T[] ToArray()
    {
      var dst = new T[m_Length];
      Copy(this, dst, m_Length);
      return dst;
    }

    public Enumerator GetEnumerator() => new Enumerator(ref this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(ref this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public readonly unsafe bool Equals(UnsafeArray<T> other) =>
      m_Buffer == other.m_Buffer && m_Length == other.m_Length;

    public override readonly bool Equals(object obj) => obj != null && obj is UnsafeArray<T> other && Equals(other);

    public override readonly unsafe int GetHashCode() => (int) m_Buffer * 397 ^ m_Length;

    public static bool operator ==(UnsafeArray<T> left, UnsafeArray<T> right) => left.Equals(right);

    public static bool operator !=(UnsafeArray<T> left, UnsafeArray<T> right) => !left.Equals(right);

    public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst)
    {
      CheckCopyLengths(src.Length, dst.Length);
      CopySafe(src, 0, dst, 0, src.Length);
    }

    public static void Copy(ReadOnly src, UnsafeArray<T> dst)
    {
      CheckCopyLengths(src.Length, dst.Length);
      CopySafe(src, 0, dst, 0, src.Length);
    }

    public static void Copy(T[] src, UnsafeArray<T> dst)
    {
      CheckCopyLengths(src.Length, dst.Length);
      CopySafe(src, 0, dst, 0, src.Length);
    }

    public static void Copy(UnsafeArray<T> src, T[] dst)
    {
      CheckCopyLengths(src.Length, dst.Length);
      CopySafe(src, 0, dst, 0, src.Length);
    }

    public static void Copy(ReadOnly src, T[] dst)
    {
      CheckCopyLengths(src.Length, dst.Length);
      CopySafe(src, 0, dst, 0, src.Length);
    }

    public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst, int length) =>
      CopySafe(src, 0, dst, 0, length);

    public static void Copy(ReadOnly src, UnsafeArray<T> dst, int length) =>
      CopySafe(src, 0, dst, 0, length);

    public static void Copy(T[] src, UnsafeArray<T> dst, int length) =>
      CopySafe(src, 0, dst, 0, length);

    public static void Copy(UnsafeArray<T> src, T[] dst, int length) =>
      CopySafe(src, 0, dst, 0, length);

    public static void Copy(ReadOnly src, T[] dst, int length) =>
      CopySafe(src, 0, dst, 0, length);

    public static void Copy(UnsafeArray<T> src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
    {
      CopySafe(src, srcIndex, dst, dstIndex, length);
    }

    public static void Copy(ReadOnly src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
    {
      CopySafe(src, srcIndex, dst, dstIndex, length);
    }

    public static void Copy(T[] src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length) =>
      CopySafe(src, srcIndex, dst, dstIndex, length);

    public static void Copy(UnsafeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length) =>
      CopySafe(src, srcIndex, dst, dstIndex, length);

    public static void Copy(ReadOnly src, int srcIndex, T[] dst, int dstIndex, int length)
    {
      CopySafe(src, srcIndex, dst, dstIndex, length);
    }

    private static unsafe void CopySafe(UnsafeArray<T> src, int srcIndex, UnsafeArray<T> dst, int dstIndex,
      int length)
    {
      CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
      UnsafeUtility.MemCpy((void*) ((IntPtr) dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>()),
        (void*) ((IntPtr) src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>()),
        length * UnsafeUtility.SizeOf<T>());
    }

    private static unsafe void CopySafe(ReadOnly src, int srcIndex, UnsafeArray<T> dst, int dstIndex,
      int length)
    {
      CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
      UnsafeUtility.MemCpy((void*) ((IntPtr) dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>()),
        (void*) ((IntPtr) src.Buffer + srcIndex * UnsafeUtility.SizeOf<T>()),
        length * UnsafeUtility.SizeOf<T>());
    }

    private static unsafe void CopySafe(T[] src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
    {
      CheckCopyPtr(src);
      CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
      var gcHandle = GCHandle.Alloc(src, GCHandleType.Pinned);
      var num = gcHandle.AddrOfPinnedObject();
      UnsafeUtility.MemCpy((void*) ((IntPtr) dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>()),
        (void*) ((IntPtr) (void*) num + srcIndex * UnsafeUtility.SizeOf<T>()),
        length * UnsafeUtility.SizeOf<T>());
      gcHandle.Free();
    }

    private static unsafe void CopySafe(UnsafeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
    {
      CheckCopyPtr(dst);
      CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
      var gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
      UnsafeUtility.MemCpy(
        (void*) ((IntPtr) (void*) gcHandle.AddrOfPinnedObject() + dstIndex * UnsafeUtility.SizeOf<T>()),
        (void*) ((IntPtr) src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>()),
        length * UnsafeUtility.SizeOf<T>());
      gcHandle.Free();
    }

    private static unsafe void CopySafe(ReadOnly src, int srcIndex, T[] dst, int dstIndex, int length)
    {
      CheckCopyPtr(dst);
      CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
      var gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
      UnsafeUtility.MemCpy(
        (void*) ((IntPtr) (void*) gcHandle.AddrOfPinnedObject() + dstIndex * UnsafeUtility.SizeOf<T>()),
        (void*) ((IntPtr) src.Buffer + srcIndex * UnsafeUtility.SizeOf<T>()),
        length * UnsafeUtility.SizeOf<T>());
      gcHandle.Free();
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private static void CheckCopyPtr(T[] ptr)
    {
      if (ptr == null)
        throw new ArgumentNullException(nameof(ptr));
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private static void CheckCopyLengths(int srcLength, int dstLength)
    {
      if (srcLength != dstLength)
        throw new ArgumentException("source and destination length must be the same");
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private static void CheckCopyArguments(int srcLength, int srcIndex, int dstLength, int dstIndex, int length)
    {
      if (length < 0)
        throw new ArgumentOutOfRangeException(nameof(length), "length must be equal or greater than zero.");
      if (srcIndex < 0 || srcIndex > srcLength || srcIndex == srcLength && srcLength > 0)
        throw new ArgumentOutOfRangeException(nameof(srcIndex),
          "srcIndex is outside the range of valid indexes for the source UnsafeArray.");
      if (dstIndex < 0 || dstIndex > dstLength || dstIndex == dstLength && dstLength > 0)
        throw new ArgumentOutOfRangeException(nameof(dstIndex),
          "dstIndex is outside the range of valid indexes for the destination UnsafeArray.");
      if (srcIndex + length > srcLength)
        throw new ArgumentException(
          "length is greater than the number of elements from srcIndex to the end of the source UnsafeArray.",
          nameof(length));
      if (srcIndex + length < 0)
        throw new ArgumentException("srcIndex + length causes an integer overflow");
      if (dstIndex + length > dstLength)
        throw new ArgumentException(
          "length is greater than the number of elements from dstIndex to the end of the destination UnsafeArray.",
          nameof(length));
      if (dstIndex + length < 0)
        throw new ArgumentException("dstIndex + length causes an integer overflow");
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private readonly void FailOutOfRangeError(int index)
    {
      if (index < 0 || index >= Length)
        throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private readonly void CheckReinterpretLoadRange<U>(int sourceIndex) where U : struct
    {
      long num1 = UnsafeUtility.SizeOf<T>();
      long num2 = UnsafeUtility.SizeOf<U>();
      var num3 = Length * num1;
      var num4 = sourceIndex * num1;
      var num5 = num4 + num2;
      if (num4 < 0L || num5 > num3)
        throw new ArgumentOutOfRangeException(nameof(sourceIndex),
          "loaded byte range must fall inside container bounds");
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private readonly void CheckReinterpretStoreRange<U>(int destIndex) where U : struct
    {
      long num1 = UnsafeUtility.SizeOf<T>();
      long num2 = UnsafeUtility.SizeOf<U>();
      var num3 = Length * num1;
      var num4 = destIndex * num1;
      var num5 = num4 + num2;
      if (num4 < 0L || num5 > num3)
        throw new ArgumentOutOfRangeException(nameof(destIndex), "stored byte range must fall inside container bounds");
    }

    public readonly unsafe U ReinterpretLoad<U>(int sourceIndex) where U : struct
    {
      CheckReinterpretLoadRange<U>(sourceIndex);
      return UnsafeUtility.ReadArrayElement<U>((void*) ((IntPtr) m_Buffer + UnsafeUtility.SizeOf<T>() * sourceIndex),
        0);
    }

    public readonly unsafe void ReinterpretStore<U>(int destIndex, U data) where U : struct
    {
      CheckReinterpretStoreRange<U>(destIndex);
      UnsafeUtility.WriteArrayElement((void*) ((IntPtr) m_Buffer + UnsafeUtility.SizeOf<T>() *
        destIndex), 0, data);
    }

    private readonly unsafe UnsafeArray<U> InternalReinterpret<U>(int length) where U : struct
    {
      var nativeArray = UnsafeArrayUnsafeUtility.ConvertExistingDataToNativeArray<U>(m_Buffer, length, m_Handle);
      return nativeArray;
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private static void CheckReinterpretSize<U>() where U : struct
    {
      if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<U>())
        throw new InvalidOperationException(
          $"Types {typeof(T)} and {typeof(U)} are different sizes - direct reinterpretation is not possible. If this is what you intended, use Reinterpret(<type size>)");
    }

    public UnsafeArray<U> Reinterpret<U>() where U : struct
    {
      CheckReinterpretSize<U>();
      return InternalReinterpret<U>(Length);
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private readonly void CheckReinterpretSize<U>(long tSize, long uSize, int expectedTypeSize, long byteLen, long uLen)
    {
      if (tSize != expectedTypeSize)
        throw new InvalidOperationException(
          $"Type {typeof(T)} was expected to be {expectedTypeSize} but is {tSize} bytes");
      if (uLen * uSize != byteLen)
        throw new InvalidOperationException(
          $"Types {typeof(T)} (array length {Length}) and {typeof(U)} cannot be aliased due to size constraints. The size of the types and lengths involved must line up.");
    }

    public readonly UnsafeArray<U> Reinterpret<U>(int expectedTypeSize) where U : struct
    {
      long tSize = UnsafeUtility.SizeOf<T>();
      long uSize = UnsafeUtility.SizeOf<U>();
      var byteLen = Length * tSize;
      var uLen = byteLen / uSize;
      CheckReinterpretSize<U>(tSize, uSize, expectedTypeSize, byteLen, uLen);
      return InternalReinterpret<U>((int) uLen);
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private readonly void CheckGetSubArrayArguments(int start, int length)
    {
      if (start < 0)
        throw new ArgumentOutOfRangeException(nameof(start), "start must be >= 0");
      if (start + length > Length)
        throw new ArgumentOutOfRangeException(nameof(length),
          $"sub array range {start}-{start + length - 1} is outside the range of the native array 0-{Length - 1}");
      if (start + length < 0)
        throw new ArgumentException(
          $"sub array range {start}-{start + length - 1} caused an integer overflow and is outside the range of the native array 0-{Length - 1}");
    }

    public readonly unsafe UnsafeArray<T> GetSubArray(int start, int length)
    {
      CheckGetSubArrayArguments(start, length);
      var nativeArray = UnsafeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
        (void*) ((IntPtr) m_Buffer + UnsafeUtility.SizeOf<T>() * start), length, Allocator.None);
      return nativeArray;
    }

    public readonly unsafe ReadOnly AsReadOnly() => new ReadOnly(m_Buffer, m_Length);

    [BurstDiscard]
    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    internal static void IsUnmanagedAndThrow()
    {
      if (!UnsafeUtility.IsUnmanaged<T>())
        throw new InvalidOperationException(
          $"{(object) typeof(T)} used in UnsafeArray<{(object) typeof(T)}> must be unmanaged (contain no managed types).");
    }

    [WriteAccessRequired]
    public readonly unsafe Span<T> AsSpan()
    {
      return new Span<T>(m_Buffer, m_Length);
    }

    public readonly unsafe ReadOnlySpan<T> AsReadOnlySpan()
    {
      return new ReadOnlySpan<T>(m_Buffer, m_Length);
    }

    public static implicit operator Span<T>(in UnsafeArray<T> source) => source.AsSpan();

    public static implicit operator ReadOnlySpan<T>(in UnsafeArray<T> source) => source.AsReadOnlySpan();

    public struct Enumerator : IEnumerator<T>
    {
      private readonly UnsafeArray<T> m_Array;
      private int m_Index;
      private T m_Value;

      public Enumerator(ref UnsafeArray<T> array)
      {
        m_Array = array;
        m_Index = -1;
        m_Value = default;
      }

      public void Dispose()
      {
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public unsafe bool MoveNext()
      {
        ++m_Index;
        if (m_Index < m_Array.m_Length)
        {
          m_Value = UnsafeUtility.ReadArrayElement<T>(m_Array.m_Buffer, m_Index);
          return true;
        }

        m_Value = default;
        return false;
      }

      public void Reset() => m_Index = -1;

      public T Current
      {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Value;
      }

      object IEnumerator.Current
      {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Current;
      }
    }

    /// <summary>
    ///   <para>UnsafeArray interface constrained to read-only operation.</para>
    /// </summary>
    [NativeContainerIsReadOnly]
    [NativeContainer]
    [DebuggerTypeProxy(typeof(UnsafeArrayReadOnlyDebugView<>))]
    [DebuggerDisplay("Length = {Length}")]
    public readonly struct ReadOnly : IEnumerable<T>
    {
      [NativeDisableUnsafePtrRestriction] 
      internal readonly unsafe void* Buffer;
      private readonly int m_Length;

      internal unsafe ReadOnly(void* buffer, int length)
      {
        Buffer = buffer;
        m_Length = length;
      }

      public int Length
      {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Length;
      }

      public void CopyTo(T[] array) => Copy(this, array);

      public void CopyTo(UnsafeArray<T> array) => Copy(this, array);

      public T[] ToArray()
      {
        var dst = new T[m_Length];
        Copy(this, dst, m_Length);
        return dst;
      }

      public unsafe UnsafeArray<U>.ReadOnly Reinterpret<U>() where U : struct
      {
        CheckReinterpretSize<U>();
        return new UnsafeArray<U>.ReadOnly(Buffer, m_Length);
      }

      public unsafe T this[int index]
      {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
          CheckElementReadAccess(index);
          return UnsafeUtility.ReadArrayElement<T>(Buffer, index);
        }
      }

      public unsafe ref readonly T UnsafeElementAt(int index)
      {
        CheckElementReadAccess(index);
        return ref UnsafeUtility.ArrayElementAsRef<T>(Buffer, index);
      }

      [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private void CheckElementReadAccess(int index)
      {
        if ((uint) index >= (uint) m_Length)
          throw new IndexOutOfRangeException(
            $"Index {(object) index} is out of range (must be between 0 and {(object) (m_Length - 1)}).");
      }

      public unsafe bool IsCreated
      {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (IntPtr) Buffer != IntPtr.Zero;
      }

      public Enumerator GetEnumerator() => new Enumerator(in this);

      IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

      public readonly unsafe ReadOnlySpan<T> AsReadOnlySpan()
      {
        return new ReadOnlySpan<T>(Buffer, m_Length);
      }

      public static implicit operator ReadOnlySpan<T>(in ReadOnly source) => source.AsReadOnlySpan();

      public struct Enumerator : IEnumerator<T>
      {
        private readonly ReadOnly m_Array;
        private int m_Index;
        private T m_Value;

        public Enumerator(in ReadOnly array)
        {
          m_Array = array;
          m_Index = -1;
          m_Value = default;
        }

        public void Dispose()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool MoveNext()
        {
          ++m_Index;
          if (m_Index < m_Array.m_Length)
          {
            m_Value = UnsafeUtility.ReadArrayElement<T>(m_Array.Buffer, m_Index);
            return true;
          }

          m_Value = default;
          return false;
        }

        public void Reset() => m_Index = -1;

        public T Current
        {
          [MethodImpl(MethodImplOptions.AggressiveInlining)]
          get => m_Value;
        }

        object IEnumerator.Current => Current;
      }
    }
  }
}