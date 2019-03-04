using Microsoft.Win32;
using NLog;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace DeploymentToolkit.Registry.Modals
{
    public class WinRegistryKey : IDisposable
    {
        [DllImport("advapi32.dll")]
        static extern int RegQueryValueEx(
            UIntPtr hKey,
            string lpValueName,
            int lpReserved,
            ref RegistryValueKind lpType,
            IntPtr lpData,
            ref int lpcbData
        );

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegSetValueEx(
            UIntPtr hKey,
            [MarshalAs(UnmanagedType.LPStr)] string lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            IntPtr lpData,
            int cbData
        );

        [DllImport("advapi32.dll")]
        static extern int RegDeleteValue(
            UIntPtr hKey,
            [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpValueName
        );

        public string Key { get; }

        internal UIntPtr RegPointer { get; }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private WinRegistryBase _winRegistryBase { get; }
        private RegistryHive _hive { get; }

        public WinRegistryKey(WinRegistryBase winRegistryBase, UIntPtr regPointer, string Key, RegistryHive hive)
        {
            this._winRegistryBase = winRegistryBase;
            this.Key = Key;
            this.RegPointer = regPointer;
            this._hive = hive;
        }

        public bool DeleteValue(string key)
        {
            _logger.Trace($"DeleteValue({key})");
            var error = RegDeleteValue(RegPointer, ref key);
            _logger.Trace($"Errorlevel: {error}");

            if (error == 0)
                return true;

            throw new Win32Exception(error);
        }

        public bool SetValue(string key, object value, RegistryValueKind type)
        {
            _logger.Trace($"SetValue({key}, {value}, {type})");
            var size = 0;
            var data = IntPtr.Zero;
            try
            {
                switch (type)
                {
                    case RegistryValueKind.String:
                        {
                            _logger.Trace("Allocating string ...");
                            size = ((string)value).Length + 1;
                            data = Marshal.StringToHGlobalAnsi((string)value);
                        }
                        break;

                    case RegistryValueKind.DWord:
                        {
                            _logger.Trace("Allocating int ...");
                            size = Marshal.SizeOf(typeof(Int32));
                            data = Marshal.AllocHGlobal(size);
                            Marshal.WriteInt32(data, (int)value);
                        }
                        break;

                    case RegistryValueKind.QWord:
                        {
                            _logger.Trace("Allocating long ...");
                            size = Marshal.SizeOf(typeof(Int64));
                            data = Marshal.AllocHGlobal(size);
                            Marshal.WriteInt64(data, (int)value);
                        }
                        break;
                }

                var error = RegSetValueEx(RegPointer, key, 0, type, data, size);
                _logger.Trace($"Errorlevel: {error}");

                if (error == 0)
                        return true;

                throw new Win32Exception(error);
            }
            finally
            {
                if (data != IntPtr.Zero)
                    Marshal.FreeHGlobal(data);
            }
        }

        public T GetValue<T>(string key, RegistryValueKind type)
        {
            _logger.Trace($"GetValue({key}, {type})");
            int size = 0;
            var result = IntPtr.Zero;
            try
            {
                var error = RegQueryValueEx(RegPointer, key, 0, ref type, IntPtr.Zero, ref size);
                _logger.Trace($"Errorlevel: {error}");
                if (error != 0)
                    throw new Win32Exception(error);

                result = Marshal.AllocHGlobal(size);
                error = RegQueryValueEx(RegPointer, key, 0, ref type, result, ref size);
                _logger.Trace($"Errorlevel: {error}");
                if (error == 0)
                {
                    var resultObject = default(T);
                    switch (type)
                    {
                        case RegistryValueKind.String:
                            _logger.Trace("Fetching string ...");
                            resultObject = (T)Convert.ChangeType(Marshal.PtrToStringAnsi(result), typeof(T));
                            break;
                        case RegistryValueKind.DWord:
                            _logger.Trace("Fetching int ...");
                            resultObject = (T)Convert.ChangeType(Marshal.ReadInt32(result), typeof(T));
                            break;
                        case RegistryValueKind.QWord:
                            _logger.Trace("Fetching long ...");
                            resultObject = (T)Convert.ChangeType(Marshal.ReadInt64(result), typeof(T));
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(type), "Unsupported Reg type");
                    }

                    return resultObject;
                }
                throw new Win32Exception(error);
            }
            finally
            {
                if (result != IntPtr.Zero)
                    Marshal.FreeHGlobal(result);
            }
        }

        public WinRegistryKey CreateSubKey(string name)
        {
            return _winRegistryBase.InternalCreateOrOpenKey(
                Path.Combine(
                    Key,
                    name
                ),
                _hive
            );
        }

        public bool DeleteSubKey(string name)
        {
            return _winRegistryBase.DeleteKey(this, name);
        }

        public void Dispose()
        {
            _winRegistryBase.CloseKey(this);
        }
    }
}
