using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RTCLib.Sys
{
    /// <summary>
    /// Marshaling utilities
    /// </summary>
    public static class Interop
    {
        /// <summary>
        /// Convert structure to binary data
        /// </summary>
        /// <param name="target">target instance</param>
        /// <typeparam name="T">target type</typeparam>
        /// <returns></returns>
        public static byte[] StructureToBytes<T>(T target)
        {
            int size = Marshal.SizeOf(target);
            byte[] bytes = new byte[size];
            GCHandle gch = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(target, gch.AddrOfPinnedObject(), false);

            gch.Free();
            return bytes;
        }

        /// <summary>
        /// Convert binary data to structure
        /// </summary>
        /// <param name="target">target memory data</param>
        /// <typeparam name="T">target type</typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static T BytesToStructure<T>(byte[] target)
        {
            int size = Marshal.SizeOf(typeof(T));
            if (target == null) throw new ArgumentNullException(nameof(target));

            GCHandle gch = GCHandle.Alloc(target, GCHandleType.Pinned);
            T ret = (T)(Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(T)));
            gch.Free();

            return ret;
        }
    }
}
