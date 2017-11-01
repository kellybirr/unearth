using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Unearth.Dns.Windows
{
    internal class Win32
    {
        internal const int DnsRequestComplete = 0;
        internal const int DnsRecordsNoInfo = 9501;
        internal const int DnsRequestPending = 9506;
        internal const int DNSQueryCancelSize = 32;

        internal delegate void QueryCompletionRoutineFunctionPointer(IntPtr queryContext, IntPtr queryResults);

        [DllImport("dnsapi", EntryPoint = "DnsQueryEx", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        internal static extern int DnsQueryEx(IntPtr queryRequest, IntPtr queryResults, IntPtr cancelHandle);

        [DllImport("dnsapi", EntryPoint = "DnsRecordListFree", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern void DnsRecordListFree(IntPtr pRecordList, int FreeType);

        [DllImport("dnsapi", SetLastError = true)]
        internal static extern int DnsCancelQuery(IntPtr cancelHandle);

        internal enum DNS_FREE_TYPE
        {
            DnsFreeFlat = 0,
            DnsFreeRecordList = 1,
            DnsFreeParsedMessageFields = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_QUERY_REQUEST
        {
            public uint Version;
            [MarshalAs(UnmanagedType.LPWStr)] public string QueryName;
            public ushort QueryType;
            public ulong QueryOptions;
            public IntPtr DnsServerList;
            public uint InterfaceIndex;
            public IntPtr QueryCompletionCallback;
            public IntPtr QueryContext;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_ADDR_ARRAY
        {
            public uint MaxCount;
            public uint AddrCount;
            public uint Tag;
            public ushort Family;
            public ushort WordReserved;
            public uint Flags;
            public uint MatchFlag;
            public uint Reserved1;

            public uint Reserved2;
            //// the array of DNS_ADDR follows this
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_QUERY_RESULT
        {
            public uint Version;
            public int QueryStatus;
            public ulong QueryOptions;
            public IntPtr QueryRecords;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_RECORD
        {
            public IntPtr Next;
            public IntPtr Name;
            public ushort Type;
            public ushort DataLength;
            public FlagsUnion Flags;
            public uint TimeToLive;
            public uint Reserved;

            public Tdata GetData<Tdata>(IntPtr ptr) where Tdata : struct
            {
                IntPtr dataPtr = ptr + Marshal.SizeOf(this);
                return Marshal.PtrToStructure<Tdata>(dataPtr);
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct FlagsUnion
        {
            [FieldOffset(0)] public uint DW;
            [FieldOffset(0)] public DNS_RECORD_FLAGS S;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_RECORD_FLAGS
        {
            internal uint Data;

            public uint Section
            {
                get => Data & 0x3u;
                set => Data = (Data & ~0x3u) | (value & 0x3u);
            }

            public uint Delete
            {
                get => (Data >> 2) & 0x1u;
                set => Data = (Data & ~(0x1u << 2)) | (value & 0x1u) << 2;
            }

            public uint CharSet
            {
                get => (Data >> 3) & 0x3u;
                set => Data = (Data & ~(0x3u << 3)) | (value & 0x3u) << 3;
            }

            public uint Unused
            {
                get => (Data >> 5) & 0x7u;
                set => Data = (Data & ~(0x7u << 5)) | (value & 0x7u) << 5;
            }

            public uint Reserved
            {
                get => (Data >> 8) & 0xFFFFFFu;
                set => Data = (Data & ~(0xFFFFFFu << 8)) | (value & 0xFFFFFFu) << 8;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct DnsHostUnion
        {
            [FieldOffset(0)] public DNS_A_DATA A;
            [FieldOffset(0)] public DNS_AAAA_DATA AAAA;
        }

        //[StructLayout(LayoutKind.Explicit)]
        //internal struct DataUnion
        //{
        //    [FieldOffset(0)] public DNS_A_DATA A;
        //    [FieldOffset(0)] public DNS_PTR_DATA PTR, NS, CNAME;
        //    [FieldOffset(0)] public DNS_MX_DATA MX;
        //    [FieldOffset(0)] public DNS_TXT_DATA TXT;
        //    [FieldOffset(0)] public DNS_AAAA_DATA AAAA;
        //    [FieldOffset(0)] public DNS_SRV_DATA SRV;
        //}

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_A_DATA
        {
            public uint IpAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_PTR_DATA
        {
            public IntPtr NameHost;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_MX_DATA
        {
            public IntPtr NameExchange;
            public ushort Preference;
            public ushort Pad;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_TXT_DATA
        {
            // MSDN docs list StringCount as a DWORD, except it changes to 8-bytes on 64-bit runtime.
            // Using an IntPtr makes it shift and seems to work properly on both 32-bit and 64-bit.
            // I don't know the root cause of this issue. I can only assume a MS runtime or API defect.
            public IntPtr StringCount;

            public IntPtr StringArray;

            public int Count() => (int) StringCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_AAAA_DATA
        {
            public UInt32 Ip6Address0;
            public UInt32 Ip6Address1;
            public UInt32 Ip6Address2;
            public UInt32 Ip6Address3;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_SRV_DATA
        {
            public IntPtr NameTarget;
            public ushort Priority;
            public ushort Weight;
            public ushort Port;
            public ushort Pad;
        }

        internal static void FreeHGlobal(ref IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
                ptr = IntPtr.Zero;
            }
        }

        internal static IPAddress ConvertUintToIpAddress(UInt32 ipAddress)
        {
            var addressBytes = new byte[4];
            addressBytes[0] = (byte)(ipAddress & 0x000000FFu);
            addressBytes[1] = (byte)((ipAddress & 0x0000FF00u) >> 8);
            addressBytes[2] = (byte)((ipAddress & 0x00FF0000u) >> 16);
            addressBytes[3] = (byte)((ipAddress & 0xFF000000u) >> 24);

            return new IPAddress(addressBytes);
        }

        internal static IPAddress ConvertAAAAToIpAddress(DNS_AAAA_DATA data)
        {
            var addressBytes = new byte[16];
            addressBytes[0] = (byte)(data.Ip6Address0 & 0x000000FF);
            addressBytes[1] = (byte)((data.Ip6Address0 & 0x0000FF00) >> 8);
            addressBytes[2] = (byte)((data.Ip6Address0 & 0x00FF0000) >> 16);
            addressBytes[3] = (byte)((data.Ip6Address0 & 0xFF000000) >> 24);
            addressBytes[4] = (byte)(data.Ip6Address1 & 0x000000FF);
            addressBytes[5] = (byte)((data.Ip6Address1 & 0x0000FF00) >> 8);
            addressBytes[6] = (byte)((data.Ip6Address1 & 0x00FF0000) >> 16);
            addressBytes[7] = (byte)((data.Ip6Address1 & 0xFF000000) >> 24);
            addressBytes[8] = (byte)(data.Ip6Address2 & 0x000000FF);
            addressBytes[9] = (byte)((data.Ip6Address2 & 0x0000FF00) >> 8);
            addressBytes[10] = (byte)((data.Ip6Address2 & 0x00FF0000) >> 16);
            addressBytes[11] = (byte)((data.Ip6Address2 & 0xFF000000) >> 24);
            addressBytes[12] = (byte)(data.Ip6Address3 & 0x000000FF);
            addressBytes[13] = (byte)((data.Ip6Address3 & 0x0000FF00) >> 8);
            addressBytes[14] = (byte)((data.Ip6Address3 & 0x00FF0000) >> 16);
            addressBytes[15] = (byte)((data.Ip6Address3 & 0xFF000000) >> 24);

            return new IPAddress(addressBytes);
        }
    }
}
