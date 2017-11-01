using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Unearth.Dns.Windows
{
    internal class WDnsQuery : IDnsQuery
    {
        private readonly Win32.QueryCompletionRoutineFunctionPointer _callback;
        private TaskCompletionSource<DnsEntry[]> _taskCompletion;
        private IntPtr _requestPtr, _resultPtr;
        private DnsEntry[] _typeRecords, _allRecords;

        private readonly object _syncLock = new object();
        private readonly ManualResetEvent _runningWait;
        private volatile bool _completed;

        private Timer _timer;
        private bool _timedOut;
        private const int MAX_TIMEOUT = 8000;

        public WDnsQuery(string query, DnsRecordType type)
        {
            Query = query;
            Type = type;

            _callback = QueryComplete;
            _runningWait = new ManualResetEvent(true);
        }

        ~WDnsQuery()
        {
            // This is a last resort to prevent GC of running queries
            // GC on this object, while running, causes unmanaged exceptions
            // The 1 minute timeout is to prevent hanging GC forever
            _runningWait.WaitOne(TimeSpan.FromMinutes(1));
        }

        public string Query { get; }
        public DnsRecordType Type { get; }

        public DnsEntry[] AllRecords => _allRecords;

        public DnsQueryStatus QueryStatus
        {
            get
            {
                if (_timedOut) return DnsQueryStatus.Timeout;
                if (_typeRecords == null) return DnsQueryStatus.Unknown;
                return (_typeRecords.Length > 0) ? DnsQueryStatus.Found : DnsQueryStatus.NotFound;
            }
        }

        public Task<DnsEntry[]> TryResolve()
        {
            lock (_syncLock)
            {   // make thread safe
                if (_taskCompletion != null) return _taskCompletion.Task;
                _taskCompletion = new TaskCompletionSource<DnsEntry[]>(this);
            }

            try
            {
                // reset wait to 'running'
                _runningWait.Reset();

                // create query request
                var request = new Win32.DNS_QUERY_REQUEST
                {
                    Version = 1,
                    QueryName = Query,
                    QueryType = (ushort) Type,
                    QueryOptions = (ulong) DnsQueryOptions.DNS_QUERY_STANDARD,
                    QueryCompletionCallback = Marshal.GetFunctionPointerForDelegate(_callback)
                };

                // marshal request
                _requestPtr = Marshal.AllocHGlobal(Marshal.SizeOf(request));
                Marshal.StructureToPtr(request, _requestPtr, false);

                // prep query result
                var queryResult = new Win32.DNS_QUERY_RESULT {Version = request.Version};
                _resultPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Win32.DNS_QUERY_RESULT>());
                Marshal.StructureToPtr(queryResult, _resultPtr, false);

                // Start request via DNS API
                int resCode = Win32.DnsQueryEx(_requestPtr, _resultPtr, IntPtr.Zero);

                switch (resCode)
                {
                    case Win32.DnsRequestPending:
                        _timer = new Timer(OnTimeout, this, MAX_TIMEOUT, Timeout.Infinite);
                        break;
                    case Win32.DnsRequestComplete:
                    case Win32.DnsRecordsNoInfo:
                        QueryComplete(IntPtr.Zero, _resultPtr);
                        break;
                    default:
                        throw new Win32Exception(resCode);
                }
            }
            catch (Exception ex)
            {
                _completed = true;
                _taskCompletion.SetException(ex);

                Win32.FreeHGlobal(ref _requestPtr);
                Win32.FreeHGlobal(ref _resultPtr);

                _runningWait.Set();
                GC.SuppressFinalize(this);
            }

            return _taskCompletion.Task;
        }

        private void QueryComplete(IntPtr contextPtr, IntPtr resultPtr)
        {
            lock (_syncLock)
            {
                if (_completed) return;
                _completed = true;

                // stop timer
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }

            try
            {
                // check if timed out
                if (_timedOut)
                {
                    _taskCompletion.SetCanceled();
                    return;
                }

                // process results
                var records = new List<DnsEntry>();
                if (resultPtr != IntPtr.Zero)
                {
                    var queryResult = Marshal.PtrToStructure<Win32.DNS_QUERY_RESULT>(resultPtr);
                    if (queryResult.QueryStatus == 0)   // SUCCESS
                    {
                        IntPtr ptr = queryResult.QueryRecords;
                        while (ptr != IntPtr.Zero)
                        {
                            var record = Marshal.PtrToStructure<Win32.DNS_RECORD>(ptr);
                            records.Add(DnsEntry.Create(record, ptr));

                            ptr = record.Next;  // Next Record
                        }

                        if (queryResult.QueryRecords != IntPtr.Zero)
                            Win32.DnsRecordListFree(queryResult.QueryRecords, (int)Win32.DNS_FREE_TYPE.DnsFreeRecordList);
                    }
                }

                _allRecords = records.ToArray();
                _typeRecords = records.Where(r => r.Type == Type).ToArray();

                if (Type == DnsRecordType.SRV || Type == DnsRecordType.MX)    // sort
                    _typeRecords = _typeRecords.OrderBy(r => ((IOrderedDnsEntry)r).SortOrder).ToArray();

                _taskCompletion.SetResult(_typeRecords);
            }
            catch (Exception ex)
            {
                _taskCompletion.SetException(ex);
            }
            finally
            {
                if (resultPtr != _resultPtr)
                    Win32.FreeHGlobal(ref resultPtr);

                Win32.FreeHGlobal(ref _resultPtr);
                Win32.FreeHGlobal(ref _requestPtr);

                _runningWait.Set();
                GC.SuppressFinalize(this);
            }
        }

        private void OnTimeout(object state)
        {
            lock (_syncLock)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }

            _timedOut = true;
            QueryComplete(IntPtr.Zero, IntPtr.Zero);
        }
    }


    [Flags]
    internal enum DnsQueryOptions
    {
        DNS_QUERY_STANDARD = 0x0,
        DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE = 0x1,
        DNS_QUERY_USE_TCP_ONLY = 0x2,
        DNS_QUERY_NO_RECURSION = 0x4,
        DNS_QUERY_BYPASS_CACHE = 0x8,
        DNS_QUERY_NO_WIRE_QUERY = 0x10,
        DNS_QUERY_NO_LOCAL_NAME = 0x20,
        DNS_QUERY_NO_HOSTS_FILE = 0x40,
        DNS_QUERY_NO_NETBT = 0x80,
        DNS_QUERY_WIRE_ONLY = 0x100,
        DNS_QUERY_RETURN_MESSAGE = 0x200,
        DNS_QUERY_MULTICAST_ONLY = 0x400,
        DNS_QUERY_NO_MULTICAST = 0x800,
        DNS_QUERY_TREAT_AS_FQDN = 0x1000,
        DNS_QUERY_ADDRCONFIG = 0x2000,
        DNS_QUERY_DUAL_ADDR = 0x4000,
        DNS_QUERY_MULTICAST_WAIT = 0x20000,
        DNS_QUERY_MULTICAST_VERIFY = 0x40000,
        DNS_QUERY_DONT_RESET_TTL_VALUES = 0x100000,
        DNS_QUERY_DISABLE_IDN_ENCODING = 0x200000,
        DNS_QUERY_APPEND_MULTILABEL = 0x800000,
        DNS_QUERY_RESERVED = unchecked((int)0xF0000000)
    }
}

