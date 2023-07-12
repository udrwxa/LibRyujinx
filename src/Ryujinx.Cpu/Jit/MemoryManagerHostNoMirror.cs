using ARMeilleure.Memory;
using Ryujinx.Memory;
using Ryujinx.Memory.Tracking;
using System;

namespace Ryujinx.Cpu.Jit
{
    /// <summary>
    /// Represents a CPU memory manager which maps guest virtual memory directly onto a host virtual region.
    /// </summary>
    public sealed class MemoryManagerHostNoMirror : MemoryManagerSoftware, ICpuMemoryManager, IVirtualMemoryManagerTracked, IWritableBlock
    {
        private readonly InvalidAccessHandler _invalidAccessHandler;
        private readonly bool _unsafeMode;

        private readonly MemoryBlock _addressSpace;
        private readonly MemoryBlock _backingMemory;

        public ulong AddressSpaceSize { get; }

        private readonly MemoryEhMeilleure _memoryEh;

        private readonly ulong[] _pageBitmap;

        /// <inheritdoc/>
        public bool Supports4KBPages => MemoryBlock.GetPageSize() == PageSize;

        public IntPtr PageTablePointer => _addressSpace.Pointer;

        public MemoryManagerType Type => _unsafeMode ? MemoryManagerType.HostMappedUnsafe : MemoryManagerType.HostMapped;

        public event Action<ulong, ulong> UnmapEvent;

        /// <summary>
        /// Creates a new instance of the host mapped memory manager.
        /// </summary>
        /// <param name="addressSpace">Address space instance to use</param>
        /// <param name="unsafeMode">True if unmanaged access should not be masked (unsafe), false otherwise.</param>
        /// <param name="invalidAccessHandler">Optional function to handle invalid memory accesses</param>
        public MemoryManagerHostNoMirror(
            MemoryBlock addressSpace,
            MemoryBlock backingMemory,
            bool unsafeMode,
            InvalidAccessHandler invalidAccessHandler) : base(backingMemory, addressSpace.Size, invalidAccessHandler)
        {
            _addressSpace = addressSpace;
            _backingMemory = backingMemory;
            _invalidAccessHandler = invalidAccessHandler;
            _unsafeMode = unsafeMode;
            AddressSpaceSize = addressSpace.Size;

            _pageBitmap = new ulong[1 << (AddressSpaceBits - (PageBits + PageToPteShift))];

            Tracking = new MemoryTracking(this, (int)MemoryBlock.GetPageSize(), invalidAccessHandler);
            _memoryEh = new MemoryEhMeilleure(addressSpace, null, Tracking);
        }

        /// <inheritdoc/>
        public void Map(ulong va, ulong pa, ulong size, MemoryMapFlags flags)
        {
            AssertValidAddressAndSize(va, size);

            _addressSpace.MapView(_backingMemory, pa, va, size);
            AddMapping(va, size);
            PtMap(va, pa, size);

            Tracking.Map(va, size);
        }

        /// <inheritdoc/>
        public void MapForeign(ulong va, nuint hostPointer, ulong size)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public void Unmap(ulong va, ulong size)
        {
            AssertValidAddressAndSize(va, size);

            UnmapEvent?.Invoke(va, size);
            Tracking.Unmap(va, size);

            RemoveMapping(va, size);
            PtUnmap(va, size);
            _addressSpace.UnmapView(_backingMemory, va, size);
        }

        /// <inheritdoc/>
        public void Reprotect(ulong va, ulong size, MemoryPermission permission)
        {
        }

        /// <inheritdoc/>
        public override void TrackingReprotect(ulong va, ulong size, MemoryPermission protection)
        {
            base.TrackingReprotect(va, size, protection);

            _addressSpace.Reprotect(va, size, protection, false);
        }

        /// <summary>
        /// Disposes of resources used by the memory manager.
        /// </summary>
        protected override void Destroy()
        {
            _addressSpace.Dispose();
            _memoryEh.Dispose();
        }
    }
}
