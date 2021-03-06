// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Security.Cryptography.System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    ///     SafeHandle representing a BCRYPT_ALG_HANDLE
    /// </summary>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    internal sealed class SafeBCryptAlgorithmHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeBCryptAlgorithmHandle() : base(true)
        {
        }

        [DllImport("bcrypt")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        private static extern BCryptNative.ErrorCode BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, int flags);

        protected override bool ReleaseHandle()
        {
            return BCryptCloseAlgorithmProvider(handle, 0) == BCryptNative.ErrorCode.Success;
        }
    }

    /// <summary>
    ///     Safe handle representing a BCRYPT_HASH_HANDLE and the associated buffer holding the hash object
    /// </summary>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    internal sealed class SafeBCryptHashHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private IntPtr m_hashObject;

        private SafeBCryptHashHandle() : base(true)
        {
        }

        /// <summary>
        ///     Buffer holding the hash object. This buffer should be allocated with Marshal.AllocCoTaskMem.
        /// </summary>
        internal IntPtr HashObject
        {
            get { return m_hashObject; }

            set
            {
                Contract.Requires(value != IntPtr.Zero);
                m_hashObject = value;
            }
        }


        [DllImport("bcrypt")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        private static extern BCryptNative.ErrorCode BCryptDestroyHash(IntPtr hHash);

        protected override bool ReleaseHandle()
        {
            bool success = BCryptDestroyHash(handle) == BCryptNative.ErrorCode.Success;

            // The hash object buffer must be released only after destroying the hash handle
            if (m_hashObject != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(m_hashObject);
            }

            return success;
        }
    }

    /// <summary>
    ///     SafeHandle for a native BCRYPT_KEY_HANDLE.
    /// </summary>
    [SecuritySafeCritical]
    internal sealed class SafeBCryptKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeBCryptKeyHandle() : base(true) { }

        [DllImport("bcrypt.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern BCryptNative.ErrorCode BCryptDestroyKey(IntPtr hKey);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected override bool ReleaseHandle()
        {
            return BCryptDestroyKey(handle) == BCryptNative.ErrorCode.Success;
        }
    }
        /// <summary>
        ///     SafeHandle for buffers returned by the Axl APIs
        /// </summary>
#if !FEATURE_CORESYSTEM
#pragma warning disable 618    // Have not migrated to v4 transparency yet
        [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
#endif
        internal sealed class SafeAxlBufferHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeAxlBufferHandle() : base(true)
            {
                return;
            }

            [DllImport("kernel32")]
#if !FEATURE_CORESYSTEM
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
            [SuppressUnmanagedCodeSecurity]
            private static extern IntPtr GetProcessHeap();

            [DllImport("kernel32")]
#if !FEATURE_CORESYSTEM
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool HeapFree(IntPtr hHeap, int dwFlags, IntPtr lpMem);

            protected override bool ReleaseHandle()
            {
                // _AxlFree is a wrapper around HeapFree on the process heap. Since it is not exported from mscorwks
                // we just call HeapFree directly. This needs to be updated if _AxlFree is ever changed.
                HeapFree(GetProcessHeap(), 0, handle);
                return true;
            }
        }

        /// <summary>
        ///     SafeHandle base class for CAPI handles (such as HCRYPTKEY and HCRYPTHASH) which must keep their
        ///     CSP alive as long as they stay alive as well. CAPI requires that all child handles belonging to a
        ///     HCRYPTPROV must be destroyed up before the reference count to the HCRYPTPROV drops to zero.
        ///     Since we cannot control the order of finalization between the two safe handles, SafeCapiHandleBase
        ///     maintains a native refcount on its parent HCRYPTPROV to ensure that if the corresponding
        ///     SafeCspKeyHandle is finalized first CAPI still keeps the provider alive.
        /// </summary>
#if FEATURE_CORESYSTEM
    [System.Security.SecurityCritical]
#else
#pragma warning disable 618    // Have not migrated to v4 transparency yet
        [SecurityCritical(SecurityCriticalScope.Everything)]
#pragma warning restore 618
#endif
        internal abstract class SafeCapiHandleBase : SafeHandleZeroOrMinusOneIsInvalid
        {
            private IntPtr m_csp;

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
            internal SafeCapiHandleBase() : base(true)
            {
            }

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            [DllImport("advapi32", SetLastError = true)]
#if !FEATURE_CORESYSTEM
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CryptContextAddRef(IntPtr hProv,
                                                          IntPtr pdwReserved,
                                                          int dwFlags);

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            [DllImport("advapi32")]
#if !FEATURE_CORESYSTEM
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CryptReleaseContext(IntPtr hProv, int dwFlags);


            protected IntPtr ParentCsp
            {
                get { return m_csp; }

#if !FEATURE_CORESYSTEM
                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
                set
                {
                    // We should not be resetting the parent CSP if it's already been set once - that will
                    // lead to leaking the original handle.
                    Debug.Assert(m_csp == IntPtr.Zero);

                    int error = (int)CapiNative.ErrorCode.Success;

                    // A successful call to CryptContextAddRef and an assignment of the handle value to our field
                    // SafeHandle need to happen atomically, so we contain them within a CER. 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { }
                    finally
                    {
                        if (CryptContextAddRef(value, IntPtr.Zero, 0))
                        {
                            m_csp = value;
                        }
                        else
                        {
                            error = Marshal.GetLastWin32Error();
                        }
                    }

                    if (error != (int)CapiNative.ErrorCode.Success)
                    {
                        throw new CryptographicException(error);
                    }
                }
            }

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
#if !FEATURE_CORESYSTEM
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
            internal void SetParentCsp(SafeCspHandle parentCsp)
            {
                bool addedRef = false;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    parentCsp.DangerousAddRef(ref addedRef);
                    IntPtr rawParentHandle = parentCsp.DangerousGetHandle();
                    ParentCsp = rawParentHandle;
                }
                finally
                {
                    if (addedRef)
                    {
                        parentCsp.DangerousRelease();
                    }
                }
            }

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            protected abstract bool ReleaseCapiChildHandle();

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            protected override sealed bool ReleaseHandle()
            {
                // Order is important here - we must destroy the child handle before the parent CSP
                bool destroyedChild = ReleaseCapiChildHandle();
                bool releasedCsp = true;

                if (m_csp != IntPtr.Zero)
                {
                    releasedCsp = CryptReleaseContext(m_csp, 0);
                }

                return destroyedChild && releasedCsp;
            }
        }

        /// <summary>
        ///     SafeHandle for CAPI hash algorithms (HCRYPTHASH)
        /// </summary>
#if FEATURE_CORESYSTEM
    [System.Security.SecurityCritical]
#else
#pragma warning disable 618    // Have not migrated to v4 transparency yet
        [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
#endif
        internal sealed class SafeCapiHashHandle : SafeCapiHandleBase
        {
            private static volatile SafeCapiHashHandle s_invalidHandle;

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            private SafeCapiHashHandle()
            {
            }

            /// <summary>
            ///     NULL hash handle
            /// </summary>
            public static SafeCapiHashHandle InvalidHandle
            {
                get
                {
                    if (s_invalidHandle == null)
                    {
                        // More than one of these might get created in parallel, but that's okay.
                        // Saving one to the field saves on GC tracking, but by SuppressingFinalize on
                        // any instance returned there's already less finalization pressure.
                        SafeCapiHashHandle handle = new SafeCapiHashHandle();
                        handle.SetHandle(IntPtr.Zero);
                        GC.SuppressFinalize(handle);
                        s_invalidHandle = handle;
                    }

                    return s_invalidHandle;
                }
            }

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            [DllImport("advapi32")]
#if !FEATURE_CORESYSTEM
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CryptDestroyHash(IntPtr hHash);

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            protected override bool ReleaseCapiChildHandle()
            {
                return CryptDestroyHash(handle);
            }
        }

        /// <summary>
        ///     SafeHandle for CAPI keys (HCRYPTKEY)
        /// </summary>
#if FEATURE_CORESYSTEM
    [System.Security.SecurityCritical]
#else
#pragma warning disable 618    // Have not migrated to v4 transparency yet
        [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
#endif
        internal sealed class SafeCapiKeyHandle : SafeCapiHandleBase
        {
            private static volatile SafeCapiKeyHandle s_invalidHandle;

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            private SafeCapiKeyHandle()
            {
            }

            /// <summary>
            ///     NULL key handle
            /// </summary>
            internal static SafeCapiKeyHandle InvalidHandle
            {
                [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
                get
                {
                    if (s_invalidHandle == null)
                    {
                        // More than one of these might get created in parallel, but that's okay.
                        // Saving one to the field saves on GC tracking, but by SuppressingFinalize on
                        // any instance returned there's already less finalization pressure.
                        SafeCapiKeyHandle handle = new SafeCapiKeyHandle();
                        handle.SetHandle(IntPtr.Zero);
                        GC.SuppressFinalize(handle);
                        s_invalidHandle = handle;
                    }

                    return s_invalidHandle;
                }
            }

            [DllImport("advapi32")]
#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#else
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CryptDestroyKey(IntPtr hKey);

            /// <summary>
            ///     Make a copy of this key handle
            /// </summary>
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            internal SafeCapiKeyHandle Duplicate()
            {
                Contract.Requires(!IsInvalid && !IsClosed);
                Contract.Ensures(Contract.Result<SafeCapiKeyHandle>() != null && !Contract.Result<SafeCapiKeyHandle>().IsInvalid && !Contract.Result<SafeCapiKeyHandle>().IsClosed);

                SafeCapiKeyHandle duplicate = null;

                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    if (!CapiNative.UnsafeNativeMethods.CryptDuplicateKey(this, IntPtr.Zero, 0, out duplicate))
                    {
                        throw new CryptographicException(Marshal.GetLastWin32Error());
                    }
                }
                finally
                {
                    if (duplicate != null && !duplicate.IsInvalid && ParentCsp != IntPtr.Zero)
                    {
                        duplicate.ParentCsp = ParentCsp;
                    }
                }

                return duplicate;
            }

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
            protected override bool ReleaseCapiChildHandle()
            {
                return CryptDestroyKey(handle);
            }
        }

    /// <summary>
    ///     SafeHandle for crypto service providers (HCRYPTPROV)
    /// </summary>
#if FEATURE_CORESYSTEM
    [System.Security.SecurityCritical]
#else
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
#endif
    internal sealed class SafeCspHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
        private SafeCspHandle() : base(true)
        {
            return;
        }

        [DllImport("advapi32", SetLastError = true)]
#if !FEATURE_CORESYSTEM
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        [SuppressUnmanagedCodeSecurity]
#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptContextAddRef(SafeCspHandle hProv,
                                                     IntPtr pdwReserved,
                                                     int dwFlags);

        [DllImport("advapi32")]
#if !FEATURE_CORESYSTEM
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        [SuppressUnmanagedCodeSecurity]
#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptReleaseContext(IntPtr hProv, int dwFlags);

        /// <summary>
        ///     Create a second SafeCspHandle which refers to the same HCRYPTPROV
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
        public SafeCspHandle Duplicate()
        {
            Contract.Requires(!IsInvalid && !IsClosed);

            // In the window between the call to CryptContextAddRef and when the raw handle value is assigned
            // into this safe handle, there's a second reference to the original safe handle that the CLR does
            // not know about, so we need to bump the reference count around this entire operation to ensure
            // that we don't have the original handle closed underneath us.
            bool acquired = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref acquired);
                IntPtr originalHandle = DangerousGetHandle();

                int error = (int)CapiNative.ErrorCode.Success;

                SafeCspHandle duplicate = new SafeCspHandle();

                // A successful call to CryptContextAddRef and an assignment of the handle value to the duplicate
                // SafeHandle need to happen atomically, so we contain them within a CER. 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    if (!CryptContextAddRef(this, IntPtr.Zero, 0))
                    {
                        error = Marshal.GetLastWin32Error();
                    }
                    else
                    {
                        duplicate.SetHandle(originalHandle);
                    }
                }

                // If we could not call CryptContextAddRef succesfully, then throw the error here otherwise
                // we should be in a valid state at this point.
                if (error != (int)CapiNative.ErrorCode.Success)
                {
                    duplicate.Dispose();
                    throw new CryptographicException(error);
                }
                else
                {
                    Debug.Assert(!duplicate.IsInvalid, "Failed to duplicate handle successfully");
                }

                return duplicate;
            }
            finally
            {
                if (acquired)
                {
                    DangerousRelease();
                }
            }
        }

#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#endif
        protected override bool ReleaseHandle()
        {
            return CryptReleaseContext(handle, 0);
        }
    }
}