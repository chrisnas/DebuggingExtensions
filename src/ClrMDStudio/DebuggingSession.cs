using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ClrMDStudio
{
    public class DebuggingSession
    {
        public string LocalCacheFolder{ get; set; }
        public DataTarget Target { get; set; }
        public ClrRuntime Clr { get; set; }

        private ClrHeap _managedHeap;

        public ClrHeap ManagedHeap
        {
            get
            {
                if (_managedHeap == null)
                {
                    if (Clr == null)
                        throw new InvalidOperationException("First open a Clr/Target to get a ManagedHeap");

                    _managedHeap = Clr.Heap;
                }
                return _managedHeap;
            }
            set { _managedHeap = value; }
        }


        string _sympath;
        string _dumpFilename;


        public DebuggingSession(string localSymbolCacheFolder)
        {
            // set default Microsoft symbol path
            if (string.IsNullOrEmpty(localSymbolCacheFolder))
            {
                LocalCacheFolder = @"c:\symbols";
                _sympath = string.Format("SRV*{0}*http://msdl.microsoft.com/download/symbols", LocalCacheFolder);
            }
            else
            {
                LocalCacheFolder = localSymbolCacheFolder;
                _sympath = string.Format("SRV*{0}*http://msdl.microsoft.com/download/symbols", localSymbolCacheFolder);
            }

            // create the folder if it does not exist
            if (!Directory.Exists(LocalCacheFolder))
            {
                Directory.CreateDirectory(LocalCacheFolder);
            }
        }

        public bool Open(string dumpFilename)
        {
            Target = null;
            Clr = null;

            Target = DataTarget.LoadCrashDump(dumpFilename);
            Target.SymbolLocator.SymbolPath = _sympath;

            // try to load the runtime
            // --------------------------------------------------------------------------
            // 1. from an explicit mscordacwks file in the same folder as the dump file
            // 2. via default symbol cache otherwise
            try
            {
                Clr = Target.ClrVersions[0].CreateRuntime(Path.Combine(Path.GetDirectoryName(dumpFilename), "mscordacwks.dll"));
            }
            catch (Exception x)
            {
                Debug.WriteLine("Error loading SOS...");
                Debug.WriteLine(x.Message);
            }

            if (Clr == null)
            {
                try
                {
                    Clr = Target.ClrVersions[0].CreateRuntime();
                }
                catch (Exception x)
                {
                    Debug.WriteLine("Error loading SOS...");
                    Debug.WriteLine(x.Message);
                    Target = null;
                    Clr = null;
                    throw;
                }
            }

            if (Clr == null)
            {
                Target = null;
                return false;
            }
            else
            {
                // special case for mini dumps
                if (Clr.ThreadPool == null)
                {
                    throw new InvalidOperationException("Impossible to get CLR information: might be a mini-dump...");
                }
            }

            _dumpFilename = dumpFilename;
            return true;
        }

        public object GetFieldValue(ulong address, string fieldName)
        {
            var type = ManagedHeap.GetObjectType(address);
            ClrInstanceField field = type.GetFieldByName(fieldName);
            if (field == null)
                return null;

            return field.GetValue(address);
        }

        public List<ulong> GetInstancesOf(string typeName)
        {
            var clrType = ManagedHeap.GetTypeByName(typeName);
            if (clrType == null)
                return null;
            if (clrType.IsValueClass)
                return null;

            List<ulong> addresses = new List<ulong>(1024);
            foreach (var address in ManagedHeap.EnumerateObjectAddresses())
            {
                var type = ManagedHeap.GetObjectType(address);
                if (type != clrType)
                    continue;

                addresses.Add(address);
            }

            return addresses;
        }

        public void ForEach(ulong arrayAddress, Action<ulong> action)
        {
            ClrType type = ManagedHeap.GetObjectType(arrayAddress);
            int length = type.GetArrayLength(arrayAddress);
            for (int element = 0; element < length; element++)
            {
                var val = type.GetArrayElementValue(arrayAddress, element);
                ulong elementAddress = (ulong)val;
                if (elementAddress == 0)
                    continue;

                action(elementAddress);
            }
        }

        public ulong ForEachInstancesOf(string typeName, Action<ulong> action)
        {
            ulong count = 0;
            foreach (var address in ManagedHeap.EnumerateObjectAddresses())
            {
                var type = ManagedHeap.GetObjectType(address);

                // BUG? - type might be null...
                if (type == null)
                    continue;
                if (type.Name != typeName)
                    continue;

                action(address);
                count++;
            }

            return count;
        }

    }
}
