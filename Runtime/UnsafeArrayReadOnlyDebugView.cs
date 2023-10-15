namespace UnsafeArray.Runtime
{
    internal sealed class UnsafeArrayReadOnlyDebugView<T> where T : struct
    {
        private readonly UnsafeArray<T>.ReadOnly m_Array;

        public T[] Items => !m_Array.IsCreated ? null : m_Array.ToArray();
        
        public UnsafeArrayReadOnlyDebugView(UnsafeArray<T>.ReadOnly array) => m_Array = array;
    }
}