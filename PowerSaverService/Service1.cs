using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace PowerSaverService
{
    public partial class Service1 : ServiceBase
    {
        System.Threading.Semaphore sem;
        string[] services = { "wuauserv", "SysMain", "AeXNSClient", "WSearch", "Browser", "iphlpsvc", "ShellHWDetection", "NlaSvc", "Altiris Deployment Agent", "AeXAgentSrvHost" };
        ManagementEventWatcher watcher;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            sem = new System.Threading.Semaphore(0, 1, "BatteryStatusChanging");
            sem.Release();
            Thread ServiceThread = new Thread(new ThreadStart(eventWait));
            ServiceThread.Start();
            
        }

        protected override void OnStop()
        {
            // watcher.Stop();
            sem.Dispose();
        }

        private void eventWait()
        {
            WqlEventQuery query =
                new WqlEventQuery("__instancemodificationevent", new TimeSpan(0, 0, 3), "TargetInstance isa \"win32_battery\" and TargetInstance.batterystatus <> PreviousInstance.batterystatus");

            watcher = new ManagementEventWatcher();
            watcher.Query = query;

            while (true)
            {
                ManagementBaseObject e = watcher.WaitForNextEvent();                
                OnBatteryStatusChange((UInt16)((ManagementBaseObject)e.GetPropertyValue("TargetInstance")).GetPropertyValue("BatteryStatus"));                
            }
            
        }

        private void OnBatteryStatusChange(UInt16 newStatus)
        {
            sem.WaitOne();
            if ( newStatus == 1 ) 
            {
                // "The computer was unplugged.";           

                foreach (string serviceName in services)
                {
                    StopService(serviceName, 15000);
                }

            }
            else if (newStatus == 2)
            {
                // "The computer was plugged in.";           

                foreach (string serviceName in services)
                {
                    StartService(serviceName, 15000);
                }
            }

            sem.Release();

        }

        public static void StartService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch
            {
             
            }
        }

        public static void StopService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            catch
            {
            
            }
        }
    }
}
