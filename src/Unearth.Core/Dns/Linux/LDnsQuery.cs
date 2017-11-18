using System;
using System.Text;
using System.Collections.Generic;
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
                //if (_timedOut) return DnsQueryStatus.Timeout;
                if (_typeRecords == null) return DnsQueryStatus.Unknown;
                return (_typeRecords.Length > 0) ? DnsQueryStatus.Found : DnsQueryStatus.NotFound;
            }
        }

        public Task<DnsEntry[]> TryResolve()
        {
            byte[] buffer = new byte[1024];
            byte[] name = new byte[256];
            ushort type, dlen, priority, weight, port, cls;
            uint ttl;
            int size;
            GCHandle handle;

            var records = new List<DnsEntry>();

            size = LinuxLib.res_query(Query, C_IN, (int)Type, buffer, buffer.Length);

            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            try {
                HEADER header = (HEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(HEADER));

                int qdcount = LinuxLib.ntohs(header.qdcount);
                int ancount = LinuxLib.ntohs(header.ancount);

                int headerSize = Marshal.SizeOf(header);

                unsafe {
                    fixed (byte* pBuffer = buffer) {

                        byte *pos = pBuffer + headerSize;
                        byte *end = pBuffer + size;

                        // Question
                        while (qdcount-- > 0 && pos < end) {
                            size = LinuxLib.dn_expand(pBuffer, end, pos, name, 256);
                            if (size < 0) return null;
                            pos += size + 4;
                        }

                        // Answers
                        while (ancount-- > 0 && pos < end) {
                            size = LinuxLib.dn_expand(pBuffer, end, pos, name, 256);
                            if (size < 0) return null;

                            pos += size;

                            type = GETINT16(ref pos);
                            cls = GETINT16(ref pos);
                            ttl = GETINT32(ref pos);

                            dlen = GETINT16(ref pos);

                            if (type == (int)DnsRecordType.SRV) {
                                priority = GETINT16(ref pos);
                                weight = GETINT16(ref pos);
                                port = GETINT16(ref pos);

                                size = LinuxLib.dn_expand(pBuffer, end, pos, name, 256);
                                if (size < 0) return null;

                                string nameStr = null;
                                fixed (byte* pName = name) {
                                    nameStr = new String((sbyte*)pName);
                                }

                                records.Add(
                                    new DnsServiceEntry(Query, nameStr, port, priority, weight, ttl)
                                );

                                pos += size;
                            } else {
                                pos += dlen;
                            }
                        }
                    }
                }

            } finally {
                handle.Free();
            }

            _allRecords = records.ToArray();
            _typeRecords = records.ToArray();

            return Task.FromResult( _typeRecords );
        }

        private unsafe static ushort GETINT16 (ref byte* buf)
        {
            byte *t_cp = (byte*)(buf);
            ushort s = (ushort) (((ushort)t_cp[0] << 8) | ((ushort)t_cp[1]));
            buf += sizeof(ushort);
            return s;
        }

        private unsafe static uint GETINT32 (ref byte* buf)
        {
            byte *t_cp = (byte*)(buf);
            uint s = (uint) (((uint)t_cp[0] << 24) | ((uint)t_cp[1] << 16) | ((uint)t_cp[2] << 8) | ((uint)t_cp[3]));
            buf += sizeof(uint);
            return s;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct HEADER
        {
            /* The first 4 bytes are a bunch of random crap that
            * nobody cares about */

            [FieldOffset(4)]
            public UInt16 qdcount; /* number of question entries */

            [FieldOffset(6)]
            public UInt16 ancount; /* number of header entries */

            [FieldOffset(8)]
            public UInt16 nscount; /* number of authority entries */

            [FieldOffset(10)]
            public UInt16 arcount; /* number of resource entries */
        }
    }

    internal static unsafe class LinuxLib 
    {
        private const string LIBRESOLV = "libresolv.so.2";
        private const string LIBC = "libc";

        [DllImport(LIBRESOLV, EntryPoint="__res_query")]
        private static extern int linux_res_query (string dname, int cls, int type, byte[] header, int headerlen);

        [DllImport(LIBRESOLV, EntryPoint="__dn_expand")]
        private unsafe static extern int linux_dn_expand (byte* msg, byte* endorig, byte* comp_dn, byte[] exp_dn, int length);

        [DllImport(LIBRESOLV, EntryPoint="res_query")]
        private static extern int bsd_res_query (string dname, int cls, int type, byte[] header, int headerlen);

        [DllImport(LIBRESOLV, EntryPoint="dn_expand")]
        private unsafe static extern int bsd_dn_expand (byte* msg, byte* endorig, byte* comp_dn, byte[] exp_dn, int length);

        internal unsafe static int res_query (string dname, int cls, int type, byte[] header, int headerlen)
        {
            try {
                return linux_res_query(dname, cls, type, header, headerlen);
            } catch (EntryPointNotFoundException) {
                return bsd_res_query(dname, cls, type, header, headerlen);
            }
        }

        internal unsafe static int dn_expand (byte* msg, byte* endorig, byte* comp_dn, byte[] exp_dn, int length)
        {
            try {
                return linux_dn_expand(msg, endorig, comp_dn, exp_dn, length);
            } catch (EntryPointNotFoundException) {
                return bsd_dn_expand(msg, endorig, comp_dn, exp_dn, length);
            }
        }

        [DllImport(LIBC)]
        internal static extern UInt16 ntohs(UInt16 netshort);
    }
}