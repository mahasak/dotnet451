//---------------------------------------------------------------------------
//
// <copyright file="SafeMILHandle.cs" company="Microsoft">
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
// 
//
// Description: 
//      A safe way to deal with unmanaged MIL interface pointers.
//---------------------------------------------------------------------------

using System;
using System.Security;
using Microsoft.Win32.SafeHandles;

using UnsafeNativeMethods = MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    internal class SafeMILHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Use this constructor if the handle isn't ready yet and later
        /// set the handle with SetHandle. SafeMILHandle owns the release
        /// of the handle.
        /// </summary>
        /// <SecurityNote>
        ///    Critical: This derives from a class that has a link demand and inheritance demand
        ///    TreatAsSafe: Ok to call constructor
        ///  </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        internal SafeMILHandle() : base(true) 
        { 
        }

        /// <summary>
        /// Use this constructor if the handle exists at construction time.
        /// SafeMILHandle owns the release of the parameter.
        /// </summary>
        /// <SecurityNote>
        ///    Critical: This derives from a class that has a link demand and inheritance demand
        ///  </SecurityNote>
        [SecurityCritical]
        internal SafeMILHandle(IntPtr handle) : base(true) 
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Change our size to the new size specified
        /// </summary>
        /// <SecurityNote>
        ///    Critical: This code calls into AddMemoryPressure and RemoveMemoryPressure
        ///                 both of which have link demands. It is used to keep memory around
        ///  </SecurityNote>
        [SecurityCritical]
        internal void UpdateEstimatedSize(long estimatedSize)
        {
            if (_gcPressure != null)
            {
                _gcPressure.Release();
            }

            //
            // estimatedSize may be 0 for small images with fewer than 8 bits per pixel,
            // attempting to call GC.AddMemoryPressure with a pressure of 0 will cause it to
            // throw, so don't add memory pressure if estimatedSize is 0.
            //
            if (estimatedSize > 0)
            {
                _gcPressure = new SafeMILHandleMemoryPressure(estimatedSize);
                _gcPressure.AddRef();
            }
        }

        internal void CopyMemoryPressure(SafeMILHandle original)
        {
            _gcPressure = original._gcPressure;
            if (_gcPressure != null)
            {
                _gcPressure.AddRef();
            }
        }

        /// <SecurityNote>
        /// Critical - calls unmanaged code, not treat as safe because you must
        ///            validate that handle is a valid COM object.
        /// </SecurityNote>
        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref handle);

            if (_gcPressure != null)
            {
                _gcPressure.Release();
                _gcPressure = null;
            }

            return true;
        }

        // Estimated size of the unmanaged memory
        private SafeMILHandleMemoryPressure _gcPressure; 
    }
}

