using ConsolePasswordMasker;
using System;
using System.IO;
using System.Threading;

namespace Unlocker
{
    internal class Program
    {
        static PasswordMasker masker = new PasswordMasker();
        static string SLOT_DIR = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\ProgramData\\Resolver Watcher";
        static string SLOT = $"{SLOT_DIR}\\slot";

        static void RunCheckLock()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (!File.Exists(".lock"))
                {
                    Console.WriteLine("Unlocked...");
                    break;
                }
            }
        }
        static void Main(string[] args)
        {

            if (!Directory.Exists(SLOT_DIR))
                Directory.CreateDirectory(SLOT_DIR);
            while (true)
            {
                string password = masker.Mask();
                File.WriteAllText(SLOT, password);

                var thread = new Thread(new ThreadStart(RunCheckLock)) { IsBackground = true };
                thread.Start();

                Console.WriteLine();
                Console.ReadKey();
            }
        }
    }
 }

