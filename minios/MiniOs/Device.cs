using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Net.Sockets;
using NetMf.CommonExtensions;
using System.Text;

namespace MiniOS
{
    /// <summary>
    /// main class used for managing the loaded components
    /// </summary>
    internal class Device : IDisposable
    {
        private const string COMPONENTNAME = "System";
        
        // 64k of ram on netduino plus
        private const uint MAXMEMORY = 65536;

        private Thread[] Components;
        private IComponent[] ComponentTypes;
        private int compCount;

        private static string logfilename;
        private static FileStream logfile;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="numberOfComponents">number of components intended to be used</param>
        /// <param name="logfilename">full path and name of the logfile</param>
        public Device(int numberOfComponents, string logfilename)
        {
            // uncomment this to see memory usage -> only available in Visual Studio Debugging mode...
            //Debug.EnableGCMessages(true);
            Device.logfilename = logfilename;
            OpenLogfile();
            Components = new Thread[numberOfComponents];
            ComponentTypes = new IComponent[numberOfComponents];
            compCount = 0;
        }

        /// <summary>
        /// registers a component
        /// </summary>
        /// <param name="component">type of the component</param>
        /// <param name="constructorparams">the constructor parameters</param>
        internal void RegisterComponent(Type component, params object[] constructorparams)
        {
            // TODO: this has to be rewritten to support 
            // a) other assemblies - e.g. from the sd card
            // b) other constructor types (build the array of types by determining the types from the object array "constructorparams"
            ConstructorInfo ci = component.GetConstructor(new Type[] { typeof(OutputPort) });
            if (ci == null)
            {
                Device.LogMessage(COMPONENTNAME, "Could not register component of type '{0}'", component);
                return;
            }

            IComponent compInstance = (IComponent)ci.Invoke(constructorparams);
            
            // next 3 lines are pretty messed up... this can definitly be rewritten in a more elegant way...
            // maybe creating a new iterator base class
            this.Components[this.compCount] = new Thread(() => { compInstance.StartInstance(); });
            this.ComponentTypes[this.compCount] = compInstance;
            this.compCount = this.compCount + 1;
        }

        /// <summary>
        /// initializes the network component and starts all components depending on the network
        /// </summary>
        internal void CheckNetwork()
        {
            Device.LogMessage(COMPONENTNAME, "Checking network");
            if (NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress == "0.0.0.0")
            {
                Device.LogMessage(COMPONENTNAME, "Trying to get an ip address by dhcp");
                NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];
                ni.EnableDynamicDns();
                ni.EnableDhcp();
                //ni.ReleaseDhcpLease();
                ni.RenewDhcpLease();

                while (NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress == "0.0.0.0")
                {
                    Device.LogMessage(COMPONENTNAME, "Waiting for IP address...");
                    Thread.Sleep(1000);
                }
            }
            Device.LogMessage(COMPONENTNAME, "IPAddress: {0}", NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress);
            for (int i = 0; i < compCount; i++)
            {
                if (this.ComponentTypes[i].StartOnComponentReady == DeviceComponentType.Network)
                    this.Components[i].Start();
            }

        }

        /// <summary>
        /// logs a specific message to the "system.log" file and Debug.Print
        /// </summary>
        /// <param name="component">the component name</param>
        /// <param name="format">message format</param>
        /// <param name="parameters">parameters to be inserted into format</param>
        internal static void LogMessage(string component, string format, params object[] parameters)
        {
            string temp = StringUtility.Format(format, parameters).Replace("\r\n", "\t");
            format = StringUtility.Format("[{0}]\t{1}\t{2}\r\n", DateTime.Now.ToString(), component, temp);
            Debug.Print(format);
            byte[] bf = Encoding.UTF8.GetBytes(format);
            logfile.Write(bf, 0, bf.Length);
        }

        /// <summary>
        /// request a soft reboot
        /// </summary>
        /// <param name="componenentname">the name of the component that requests the reboot</param>
        /// <param name="reason">the reason for the reboot</param>
        internal static void RequestDeviceReboot(string componenentname, string reason)
        {
            Device.LogMessage(COMPONENTNAME, "'{0}' requested a reboot. Reason: {0}", componenentname, reason);
            Device.CloseLogfile();
            Microsoft.SPOT.Hardware.PowerState.RebootDevice(true, 1000);
        }

        /// <summary>
        /// shuts down the instance and all registered components
        /// </summary>
        public void Dispose()
        {
            if (logfile != null)
            {
                logfile.Flush();
                logfile.Close();
            }
        }

        /// <summary>
        /// starts the instance and check all system components (network/usb/sd card/ etc)
        /// </summary>
        internal void Start()
        {
            Device.LogMessage(COMPONENTNAME, "Starting up...");
            CheckNetwork();
        }

        /// <summary>
        /// returns the amount of free memory
        /// </summary>
        /// <returns>amount of free memory</returns>
        /// <remarks>do not use this, as this method is currently not really sophisticated</remarks>
        [Obsolete("do not use this method. there is currently no supported method in the net mf that returns the amount of free memory!")] 
        internal static uint AvailableMemory()
        {
            uint mem = Debug.GC(true);
            Device.LogMessage(COMPONENTNAME, "Available memory request. Current available memory is {0} byte", mem.ToString());
            mem = Debug.GC(true);
            return mem;
        }

        /// <summary>
        /// closes the logfile
        /// </summary>
        internal static void CloseLogfile()
        {
            if (logfile != null)
            {
                logfile.Flush();
                logfile.Close();
            }
        }

        /// <summary>
        /// opens the log file specific when instancing the class
        /// </summary>
        internal static void OpenLogfile()
        {
            if (logfile == null && !StringUtility.IsNullOrEmpty(logfilename))
                logfile = new FileStream(logfilename, FileMode.Append);
        }

        /// <summary>
        /// loads the specific file into the memory
        /// </summary>
        /// <param name="filename">full path and file name</param>
        /// <returns>byte array of the file</returns>
        private byte[] LoadFileContent(string filename)
        {
            if (!File.Exists(filename)) throw new ArgumentException(StringUtility.Format("File '{0}' not found!", filename));
            return File.ReadAllBytes(filename);
        }

        /// <summary>
        /// loads the specific assembly
        /// </summary>
        /// <param name="filename">full path and file name</param>
        /// <returns><see cref="System.Reflection.Assembly"/> object</returns>
        private Assembly LoadAssembly(string filename)
        {
            byte[] file = this.LoadFileContent(filename);
            System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(file);
            return assembly;
        }
    }
}
