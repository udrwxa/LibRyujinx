using ARMeilleure.Memory;
using Ryujinx.Memory;
using Ryujinx.Memory.Tracking;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Ryujinx.Cpu.AppleHv
{
    /// <summary>
    /// Represents a CPU memory manager which maps guest virtual memory directly onto the Hypervisor page table.
    /// </summary>
    [SupportedOSPlatform("macos")]
    public class HvMemoryManager : MemoryManagerSoftware, ICpuMemoryManager, IVirtualMemoryManagerTracked, IWritableBlock
    {
        private readonly InvalidAccessHandler _invalidAccessHandler;

        private readonly ulong _addressSpaceSize;

        private readonly HvAddressSpace _addressSpace;

        internal HvAddressSpace AddressSpace => _addressSpace;

        public bool Supports4KBPages => true;

        public IntPtr PageTablePointer => IntPtr.Zero;

        public MemoryManagerType Type => MemoryManagerType.SoftwarePageTable;

        public event Action<ulong, ulong> UnmapEvent;

        /// <summary>
        /// Creates a new instance of the Hypervisor memory manager.
        /// </summary>
        /// <param name="backingMemory">Physical backing memory where virtual memory will be mapped to</param>
        /// <param name="addressSpaceSize">Size of the address space</param>
        /// <param name="invalidAccessHandler">Optional function to handle invalid memory accesses</param>
        public HvMemoryManager(MemoryBlock backingMemory, ulong addressSpaceSize, InvalidAccessHandler invalidAccessHandler = null) : base(backingMemory, addressSpaceSize, invalidAccessHandler)
        {
            _invalidAccessHandler = invalidAccessHandler;
            _addressSpaceSize = addressSpaceSize;

            _addressSpace = new HvAddressSpace(backingMemory, addressSpaceSize);

            Tracking = new MemoryTracking(this, PageSize, invalidAccessHandler);
        }

        /// <inheritdoc/>
        public void Map(ulong va, ulong pa, ulong size, MemoryMapFlags flags)
        {
            AssertValidAddressAndSize(va, size);

            PtMap(va, pa, size);
            _addressSpace.MapUser(va, pa, size, MemoryPermission.ReadWriteExecute);
            AddMapping(va, size);

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
            _addressSpace.UnmapUser(va, size);
            PtUnmap(va, size);
        }

        /// <summary>
        /// Computes the number of pages in a virtual address range.
        /// </summary>
        /// <param name="va">Virtual address of the range</param>
        /// <param name="size">Size of the range</param>
        /// <param name="startVa">The virtual address of the beginning of the first page</param>
        /// <remarks>This function does not differentiate between allocated and unallocated pages.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetPagesCount(ulong va, ulong size, out ulong startVa)
        {
            // WARNING: Always check if ulong does not overflow during the operations.
            startVa = va & ~(ulong)PageMask;
            ulong vaSpan = (va - startVa + size + PageMask) & ~(ulong)PageMask;

            return (int)(vaSpan / PageSize);
        }

        /// <inheritdoc/>
        public void Reprotect(ulong va, ulong size, MemoryPermission protection)
        {
            if (protection.HasFlag(MemoryPermission.Execute))
            {
                // Some applications use unordered exclusive memory access instructions
                // where it is not valid to do so, leading to memory re-ordering that
                // makes the code behave incorrectly on some CPUs.
                // To work around this, we force all such accesses to be ordered.

                using WritableRegion writableRegion = GetWritableRegion(va, (int)size);

                HvCodePatcher.RewriteUnorderedExclusiveInstructions(writableRegion.Memory.Span);
            }

            // TODO
        }

        /// <inheritdoc/>
        public override void TrackingReprotect(ulong va, ulong size, MemoryPermission protection)
        {
            base.TrackingReprotect(va, size, protection);

            _addressSpace.ReprotectUser(va, size, protection);
        }

        /// <summary>
        /// Disposes of resources used by the memory manager.
        /// </summary>
        protected override void Destroy()
        {
            _addressSpace.Dispose();
        }

        private static void ThrowInvalidMemoryRegionException(string message) => throw new InvalidMemoryRegionException(message);
    }
}
