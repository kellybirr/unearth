using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Unearth.Dns.Linux
{
    internal class LDnsQuery : IDnsQuery
    {
        private const int C_IN = 1;

        private DnsEntry[] _typeRecords, _allRecords;

        public LDnsQuery(string query, DnsRecordType type)
        {
            Query = query;
            Type = type;
        }

        public string Query { get; }
        public DnsRecordType Type { get; }

        public DnsEntry[] AllRecords => _allRecords;

        public DnsQueryStatus QueryStatus
        {
            get
            {
                if (_typeRecords == null) return DnsQueryStatus.Unknown;
                return (_typeRecords.Length > 0) ? DnsQueryStatus.Found : DnsQueryStatus.NotFound;
            }
        }

        [SuppressMessage("ReSharper", "UnusedVariable")]
        public unsafe Task<DnsEntry[]> TryResolve()
        {
            var records = new List<DnsEntry>();

            byte[] dataBuffer = new byte[1024];
            int dataLen = LinuxLib.res_query(Query, C_IN, (int)Type, dataBuffer, dataBuffer.Length);

            if (dataLen > 0) 
            {
                GCHandle handle = GCHandle.Alloc(dataBuffer, GCHandleType.Pinned);

                try 
                {
                    fixed (byte* pBuffer = dataBuffer)
                    {
                        var reader = new LDnsReader
                        {
                            Buffer = pBuffer,
                            Current = pBuffer,
                            End = pBuffer + dataLen
                        };

                        // Response Header
                        int queryId = reader.UInt16();  // Query Identifier (Read & Ignore)
                        byte[] hBits = reader.Bytes(2); // Header Bits & Flags (Read & Ignore)

                        int qdCount = reader.UInt16();  // Question Count (Use Below)
                        int anCount = reader.UInt16();  // Answer Count (Use Below)

                        int nsCount = reader.UInt16();  // NameServer Count (Read & Ignore)
                        int arCount = reader.UInt16();  // Resource Count (Read & Ignore)

                        // Question Section (read and ignore)
                        for (int q = 0; q < qdCount && reader.OK(); q++)
                        {
                            string qName = reader.Name();
                            ushort qType = reader.UInt16();
                            ushort qClass = reader.UInt16();
                        }

                        // Answers (the good stuff)
                        for (int a = 0; a < anCount && reader.OK(); a++)
                        {
                            var ansHead = new LDnsHeader(reader);
                            DnsEntry dnsEntry = DnsEntry.Create(ansHead, reader);

                            records.Add(dnsEntry);
                        }
                    }
                } 
                finally 
                {
                    handle.Free();
                }
            }

            _allRecords = records.ToArray();
            _typeRecords = records.Where(r => r.Type == Type).ToArray();

            if (Type == DnsRecordType.SRV || Type == DnsRecordType.MX)    // sort
                _typeRecords = _typeRecords.OrderBy(r => ((IOrderedDnsEntry)r).SortOrder).ToArray();

            return Task.FromResult( _typeRecords );
        }
    }

#pragma warning disable IDE1006 // Naming Styles
    internal static unsafe class LinuxLib 
    {
        private const string BSD_LIBRESOLV = "libresolv.so.2";
        private const string LINUX_LIBRESOLV = "libresolv.so.2";
        private const string OSX_LIBRESOLV = "libresolv.9.dylib";

        private const string LIBC = "libc";

        [DllImport(LINUX_LIBRESOLV, EntryPoint="__res_query")]
        private static extern int linux_res_query (string dname, int cls, int type, byte[] header, int headerlen);

        [DllImport(LINUX_LIBRESOLV, EntryPoint="__dn_expand")]
        private static extern int linux_dn_expand (byte* msg, byte* endorig, byte* comp_dn, byte[] exp_dn, int length);

        [DllImport(BSD_LIBRESOLV, EntryPoint="res_query")]
        private static extern int bsd_res_query (string dname, int cls, int type, byte[] header, int headerlen);

        [DllImport(BSD_LIBRESOLV, EntryPoint="dn_expand")]
        private static extern int bsd_dn_expand (byte* msg, byte* endorig, byte* comp_dn, byte[] exp_dn, int length);

        [DllImport(OSX_LIBRESOLV, EntryPoint = "res_query")]
        private static extern int osx_res_query(string dname, int cls, int type, byte[] header, int headerlen);

        [DllImport(OSX_LIBRESOLV, EntryPoint = "dn_expand")]
        private static extern int osx_dn_expand(byte* msg, byte* endorig, byte* comp_dn, byte[] exp_dn, int length);

        [DllImport(LIBC)]
        internal static extern UInt16 ntohs(UInt16 netValue);

        [DllImport(LIBC)]
        internal static extern UInt32 ntohl(UInt32 netValue);

        internal static int res_query (string dname, int cls, int type, byte[] header, int headerlen)
        {
            try
            {  
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))    // OSX
                    return osx_res_query(dname, cls, type, header, headerlen);
                else                                                    // LINUX
                    return linux_res_query(dname, cls, type, header, headerlen);
            }
            catch (EntryPointNotFoundException)
            {   // BSD or ALT OSX ?
                return bsd_res_query(dname, cls, type, header, headerlen);
            }
        }

        internal static int dn_expand (byte* msg, byte* endorig, byte* comp_dn, byte[] exp_dn, int length)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))    // OSX
                    return osx_dn_expand(msg, endorig, comp_dn, exp_dn, length);
                else                                                    // LINUX
                    return linux_dn_expand(msg, endorig, comp_dn, exp_dn, length);
            }
            catch (EntryPointNotFoundException)
            {   // BSD or ALT OSX ?
                return bsd_dn_expand(msg, endorig, comp_dn, exp_dn, length);
            }
        }

    }
#pragma warning restore IDE1006 // Naming Styles

    internal unsafe class LDnsReader
    {
        public byte* Buffer, End, Current;

        public bool OK() => Current < End;

        public void Skip(int bytes) => Current += bytes;

        public UInt16 UInt16()
        {
            ushort netValue = ((ushort*)Current)[0];
            ushort hostValue = LinuxLib.ntohs(netValue);

            //byte* t_cp = (byte*)(Current);
            //ushort s = (ushort)(((ushort)t_cp[0] << 8) | ((ushort)t_cp[1]));

            Current += sizeof(ushort);

            return hostValue;
        }

        public UInt32 UInt32()
        {
            uint netValue = ((uint*)Current)[0];
            uint hostValue = LinuxLib.ntohl(netValue);

            //byte* t_cp = (byte*)(Current);
            //uint s = (uint)(((uint)t_cp[0] << 24) | ((uint)t_cp[1] << 16) | ((uint)t_cp[2] << 8) | ((uint)t_cp[3]));

            Current += sizeof(uint);

            return hostValue;
        }

        public string Name()
        {
            byte[] stringBuffer = new byte[256];
            int len = LinuxLib.dn_expand(Buffer, End, Current, stringBuffer, stringBuffer.Length);
            if (len < 0) return null;

            Current += len;

            fixed (byte* pStrData = stringBuffer)
                return new String((sbyte*)pStrData);
        }

        public string Text(int len)
        {
            var txt = new String((sbyte*)Current, 0, len);

            Current += len;

            return txt;                
        }


        public byte[] Bytes(int len)
        {
            var result = new byte[len];
            for (int i = 0; i < len; i++)
                result[i] = Current[i];

            Current += len;

            return result;
        }
    }

    internal class LDnsHeader
    {
        public LDnsHeader(LDnsReader reader)
        {
            Name = reader.Name();
            Type = reader.UInt16();
            Class = reader.UInt16();
            TTL = reader.UInt32();
            DataLen = reader.UInt16();
        }

        public string Name { get;  }
        public UInt16 Type { get; }
        public UInt32 TTL { get; }
        public UInt16 Class { get; }
        public UInt16 DataLen { get; }
    }
}
