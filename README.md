## About
A version of NativeArray from Unity.Collections with support of AllocatorManager.AllocatorHandle and without safety checks.
No need to use AllocatorManager (or CollectionHelper) API directly to allocate/deallocate array.

## Dependencies
- Unity Burst: 1.8.8
- Unity Collections: 2.1.4

## Usage
#### Allocate UnsafeArray
```c#
var unsafeArray = new UnsafeArray<int>(100, Allocator.TempJob);
```

#### Allocate UnsafeArray with custom allocator
```c#
// customAllocator is a implementation of AllocatorManager.IAllocator
var allocatorHandle = customAllocator.Handle;
var unsafeArray = new UnsafeArray<int>(100, allocatorHandle);
```

#### Deallocate UnsafeArray
```c#
unsafeArray.Dispose();
```

#### Deallocate UnsafeArray with Job
```c#
var disposeHandle = unsafeArray.Dispose(dependencyHandle);
```
