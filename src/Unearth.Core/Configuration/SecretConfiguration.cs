using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Unearth.Configuration
{
    static class SecretConfiguration
    {
        private const string LINUX_CONF_FILE = "/run/secrets/unearth.conf";
        private const string WIN_CONF_FILE = "c:\\programdata\\docker\\secrets\\unearth.conf";

        private static string _pepper;

        static SecretConfiguration()
        {
            string confPath = WIN_CONF_FILE;
#if (NETSTANDARD2_0)
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                confPath = LINUX_CONF_FILE;
#endif

            if (File.Exists(confPath))
            {
                try
                {
                    string[] confData = File.ReadAllLines(confPath);
                    foreach (string line in confData)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] parts = line.Split(new[] { '=' }, 2);

                        if (parts.Length >= 2)
                        {
                            switch (parts[0].ToUpperInvariant())
                            {
                                case "PEPPER":
                                    _pepper = parts[1].Trim();
                                    break;
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    /* ignore - continue */
                }
            }
        }

        public static string Pepper
        {
            get
            {
                string p = Environment.GetEnvironmentVariable("PEPPER");                
                return string.IsNullOrEmpty(p) ? _pepper : p;
            }
            set
            {
                _pepper = value;
                Environment.SetEnvironmentVariable("PEPPER", _pepper);
            }
        }
    }
}
