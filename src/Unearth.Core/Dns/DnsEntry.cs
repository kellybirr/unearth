﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Primitives;
using Unearth.Dns.Windows;

namespace Unearth.Dns
{
    public interface IOrderedDnsEntry
    {
        int SortOrder { get; }
    }

    public abstract class DnsEntry
    {
        internal static DnsEntry Create(Win32.DNS_RECORD record, IntPtr ptr)
        {
            switch (record.Type)
            {
                case (ushort)DnsRecordType.A:
                case (ushort)DnsRecordType.AAAA:
                    return new DnsHostEntry(record, ptr);
                case (ushort)DnsRecordType.NS:
                case (ushort)DnsRecordType.PTR:
                case (ushort)DnsRecordType.CNAME:
                    return new DnsPointerEntry(record, ptr);
                case (ushort)DnsRecordType.MX:
                    return new DnsMailExchangeEntry(record, ptr);
                case (ushort)DnsRecordType.SRV:
                    return new DnsServiceEntry(record, ptr);
                case (ushort)DnsRecordType.TXT:
                    return new DnsTextEntry(record, ptr);
                default:
                    throw new NotSupportedException();
            }
        }

        internal DnsEntry(Win32.DNS_RECORD record)
        {
            Type = (DnsRecordType)record.Type;
            Name = Marshal.PtrToStringUni(record.Name);

            TimeToLive = TimeSpan.FromSeconds(record.TimeToLive);
            Expires = DateTime.UtcNow.Add(TimeToLive);
        }

        internal DnsEntry(DnsRecordType type, string name, uint ttl)
        {
            Type = type;
            Name = name;

            TimeToLive = TimeSpan.FromSeconds(ttl);
            Expires = DateTime.UtcNow.Add(TimeToLive);            
        }

        public DnsRecordType Type { get; }

        public string Name { get; }

        public TimeSpan TimeToLive { get; }

        public DateTime Expires { get; }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ToString()?.Equals(obj) ?? (obj == null);
        }
    }

    public class DnsHostEntry : DnsEntry
    {
        internal DnsHostEntry(Win32.DNS_RECORD record, IntPtr ptr) : base(record)
        {
            var data = record.GetData<Win32.DnsHostUnion>(ptr);

            Address = (Type == DnsRecordType.A)
                ? Win32.ConvertUintToIpAddress(data.A.IpAddress)
                : Win32.ConvertAAAAToIpAddress(data.AAAA);
        }

        public IPAddress Address { get; }
        public override string ToString()
        {
            return Address?.ToString();
        }
    }

    public class DnsMailExchangeEntry : DnsEntry, IOrderedDnsEntry, IComparable<DnsMailExchangeEntry>, IComparable
    {
        internal DnsMailExchangeEntry(Win32.DNS_RECORD record, IntPtr ptr) : base(record)
        {
            var data = record.GetData<Win32.DNS_MX_DATA>(ptr);

            Exchanger = Marshal.PtrToStringUni(data.NameExchange);
            Preference = data.Preference;
        }

        public string Exchanger { get; }

        public int Preference { get; }

        int IOrderedDnsEntry.SortOrder => Preference;

        int IComparable<DnsMailExchangeEntry>.CompareTo(DnsMailExchangeEntry other)
        {
            return Preference.CompareTo(other.Preference);
        }

        int IComparable.CompareTo(object obj)
        {
            return Preference.CompareTo(((DnsMailExchangeEntry)obj).Preference);
        }

        public override string ToString()
        {
            return $"[{Preference}]{Exchanger}";
        }
    }

    public class DnsPointerEntry : DnsEntry
    {
        internal DnsPointerEntry(Win32.DNS_RECORD record, IntPtr ptr) : base(record)
        {
            var data = record.GetData<Win32.DNS_PTR_DATA>(ptr);

            Target = Marshal.PtrToStringUni(data.NameHost);
        }

        public string Target { get; }

        public override string ToString()
        {
            return Target;
        }
    }

    public class DnsServiceEntry : DnsEntry, IOrderedDnsEntry, IComparable<DnsServiceEntry>, IComparable
    {
        internal DnsServiceEntry(Win32.DNS_RECORD record, IntPtr ptr) : base(record)
        {
            var data = record.GetData<Win32.DNS_SRV_DATA>(ptr);

            Host = Marshal.PtrToStringUni(data.NameTarget);
            Priority = data.Priority;
            Weight = data.Weight;
            Port = data.Port;
        }

        internal DnsServiceEntry(string name, string host, int port, int priority, int weight, uint ttl)
            : base(DnsRecordType.SRV, name, ttl)
        {
            Host = host;
            Port = port;
            Priority = priority;
            Weight = weight;
        }

        public string Host { get; }

        public int Priority { get; }

        public int Weight { get; }

        public int Port { get; }

        int IOrderedDnsEntry.SortOrder => Priority;

        int IComparable<DnsServiceEntry>.CompareTo(DnsServiceEntry other)
        {
            return Priority.CompareTo(other.Priority);
        }

        int IComparable.CompareTo(object obj)
        {
            return Priority.CompareTo(((DnsServiceEntry) obj).Priority);
        }

        public override string ToString()
        {
            return $"[{Priority}][{Weight}][{Port}]{Host}";
        }
    }

    public class DnsTextEntry : DnsEntry
    {
        internal DnsTextEntry(Win32.DNS_RECORD record, IntPtr ptr) : base(record)
        {
            // get TXT DATA block
            var txt = record.GetData<Win32.DNS_TXT_DATA>(ptr);
            // get pointers to strings
            var stringPointers = new IntPtr[txt.Count()];
            IntPtr pointer0 = ptr + Marshal.SizeOf(record) + Marshal.SizeOf(txt.StringCount);
            Marshal.Copy(pointer0, stringPointers, 0, stringPointers.Length);

            // get string values
            Text = new string[stringPointers.Length];
            for (int i = 0; i < stringPointers.Length; i++)
                Text[i] = Marshal.PtrToStringUni(stringPointers[i]);
        }

        public string[] Text { get; }

        public override string ToString() => String.Join(";",Text);

        public IDictionary<string, StringValues> ToDictionary()
        {
            var dict = new Dictionary<string, StringValues>();
            var list = new List<KeyValuePair<string,string>>();

            foreach (string s in Text)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                string[] parts = s.Split(new[] {'='}, 2);

                if (parts.Length >= 2)
                    list.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
            }

            var keys = list.Select(v => v.Key).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (string key in keys)
                dict.Add(key, (from v in list where v.Key.Equals(key, StringComparison.OrdinalIgnoreCase) select v.Value).ToArray() );

            return dict;
        }
    }
}

