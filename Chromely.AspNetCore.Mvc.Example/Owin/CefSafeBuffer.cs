using System;
using System.Runtime.InteropServices;

namespace Chromely.AspNetCore.Mvc.Example.Owin
{
    public class CefSafeBuffer : SafeBuffer
    {
        public CefSafeBuffer(IntPtr data, ulong noOfBytes) : base(false)
        {
            SetHandle(data);
            Initialize(noOfBytes);
        }

        protected override bool ReleaseHandle()
        {
            return true;
        }
    }
}
