using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RTCLib.Sys
{
    class Interop
    {
        public static byte[] StructureToBytes<T>(T target)
        {
            int size = Marshal.SizeOf(target);
            byte[] bytes = new byte[size];
            GCHandle gch = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(target, gch.AddrOfPinnedObject(), false);

            gch.Free();
            return bytes;
        }

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
