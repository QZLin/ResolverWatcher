using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ResolverWatcher
{
    internal class Program
    {
        // const string SET_PRIMARY_DNS = "interface ip set dns name=\"{0}\" source=static validate=no address={1}";
        // const string SET_DHCP = "interface ip set dns name=\"{0}\" source=dhcp";
        // const string ADD_DNS = "add dns name=\"{0}\" validate=no address={1}";
        const string FAKEDNS = "0.0.0.0";
        static string SetPrimaryDns(string name, string addr) => $"interface ip set dns name=\"{name}\" source=static validate=no address={addr}";
        static string SetDHCP(string name) => $"interface ip set dns name=\"{name}\" source=dhcp";
        static string AddDns(string name, string addr) => $"add dns name=\"{name}\" validate=no address={addr}";


        static string SLOT = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\ProgramData\\Resolver Watcher\\slot";
        static byte[] key = Convert.FromBase64String("HaB7u3f6gcL6lSWb4Eow9uzEfPE="); // SHA-1 of password encoded in Base64
        public enum LockAction
        {
            UNLOCK, LOCK, EXIT, RESTART
        }

        public static Queue<LockAction> commands;
        static int interval = 10000;
        static int relockInterval = 1800 * 1000;
        static class Status
        {
            public static bool locked = false;
        }

        static bool CompairBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        static void HandleSlot(string path)
        {
            Console.WriteLine("Getting Input...");
            var content = File.ReadAllText(path);
            var tmpSource = Encoding.ASCII.GetBytes(content);
            var tmpHash = new SHA1CryptoServiceProvider().ComputeHash(tmpSource);
            if (CompairBytes(key, tmpHash))
                commands.Enqueue(LockAction.UNLOCK);
            else
                commands.Enqueue(LockAction.LOCK);
            File.WriteAllText(path, "");
            try { File.Delete(path); }
            catch (IOException) { }
        }

        static void RunReadInput()
        {
            while (true)
            {
                if (File.Exists(SLOT))
                    HandleSlot(SLOT);
                Thread.Sleep(interval);
            }
        }
        static System.Timers.Timer timer;
        static int Main(string[] args)
        {
            commands = new Queue<LockAction>();
            commands.Enqueue(LockAction.LOCK);


            ThreadStart childref = new ThreadStart(RunReadInput);
            Thread childThread = new Thread(childref) { IsBackground = true };
            childThread.Start();
            while (true)
            {
                if (Status.locked)
                    Lock();
                Thread.Sleep(interval / 5);
                if (commands.Count == 0)
                    continue;
                switch (commands.Dequeue())
                {
                    case LockAction.UNLOCK:
                        new Thread(new ThreadStart(Unlock)) { IsBackground = true }.Start();
                        Status.locked = false;

                        timer = new System.Timers.Timer(relockInterval);
                        timer.Elapsed += (o, e) => { commands.Enqueue(LockAction.LOCK); Console.WriteLine("Time is out..."); };
                        timer.AutoReset = false;
                        timer.Start();

                        break;
                    case LockAction.LOCK:
                        new Thread(new ThreadStart(Lock)) { IsBackground = true }.Start();
                        Status.locked = true;
                        break;
                    case LockAction.EXIT:
                        return 0;
                }
            }
        }
        static void NetSh(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "netsh",
                    Arguments = command,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
        }
        static bool NeedUpdateDns(NetworkInterface interface_, string target)
        {
            var info = interface_.GetIPProperties();
            if (info.DnsAddresses.Count != 1)
                return true;
            if (info.DnsAddresses[0].ToString() != target)
                return true;
            return false;
        }
        static void Lock()
        {
            foreach (NetworkInterface interface_ in NetworkInterface.GetAllNetworkInterfaces())
            {
                Console.WriteLine($"DIABLED:{interface_.Name}");
                if (NeedUpdateDns(interface_, FAKEDNS))
                    NetSh(SetPrimaryDns(interface_.Name, FAKEDNS));
            }
            if (!File.Exists(".lock"))
                File.Create(".lock").Close();
        }
        static void Unlock()
        {

            foreach (NetworkInterface interface_ in NetworkInterface.GetAllNetworkInterfaces())
            {
                Console.WriteLine($"ENABLED:{interface_.Name}");
                NetSh(SetDHCP(interface_.Name));
            }
            if (File.Exists(".lock"))
                File.Delete(".lock");
        }

    }
}
