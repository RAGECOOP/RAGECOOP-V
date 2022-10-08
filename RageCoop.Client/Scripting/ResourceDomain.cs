using SHVDN;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace RageCoop.Client.Scripting
{
    internal class ResourceDomain : MarshalByRefObject, IDisposable
    {
        public static ResourceDomain Instance;
        public static ScriptDomain PrimaryDomain;
        public static string blah = "blah";
        private ScriptDomain CurrentDomain => ScriptDomain.CurrentDomain;
        private ResourceDomain(ScriptDomain primary)
        {
            PrimaryDomain = primary;

            // Bridge to current ScriptDomain
            primary.Tick += Tick;
            primary.KeyEvent += KeyEvent;
            AppDomain.CurrentDomain.SetData("Primary",false);
            Main.Console.PrintInfo("Loaded scondary domain: " + AppDomain.CurrentDomain.Id + " " + Main.IsPrimaryDomain);
        }
        public static void Load(string dir = @"RageCoop\Scripts")
        {
            if (Instance != null)
            {
                throw new Exception("Already loaded");
            }
            else if (!Main.IsPrimaryDomain)
            {
                throw new InvalidOperationException("Cannot load in another domain");
            }
            ScriptDomain domain = null;
            try
            {
                dir = Path.GetFullPath(dir);

                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
                Directory.CreateDirectory(dir);

                // Copy API assemblies
                var api = typeof(ResourceDomain).Assembly;
                File.Copy(api.Location, Path.Combine(dir, Path.GetFileName(api.Location)), true);
                foreach (var a in api.GetReferencedAssemblies())
                {
                    var asm = Assembly.Load(a.FullName);
                    if (string.IsNullOrEmpty(asm.Location))
                    {
                        continue;
                    }
                    File.Copy(asm.Location, Path.Combine(dir, Path.GetFileName(asm.Location)), true);
                }

                // Copy test script
                // File.Copy(@"M:\SandBox-Shared\repos\RAGECOOP\RAGECOOP-V\bin\Debug\TestScript.dll", Path.Combine(dir, Path.GetFileName("TestScript.dll")), true);

                // Load domain in main thread
                Main.QueueToMainThread(() =>
                {
                    domain = ScriptDomain.Load(Directory.GetParent(typeof(ScriptDomain).Assembly.Location).FullName, dir);
                    domain.AppDomain.SetData("Console", ScriptDomain.CurrentDomain.AppDomain.GetData("Console"));
                    domain.AppDomain.SetData("RageCoop.Client.API", API.GetInstance());
                    Instance = (ResourceDomain)domain.AppDomain.CreateInstanceFromAndUnwrap(typeof(ResourceDomain).Assembly.Location, typeof(ResourceDomain).FullName, false, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { ScriptDomain.CurrentDomain }, null, null);
                    domain.Start();
                });

                // Wait till next tick
                GTA.Script.Yield();
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.Show(ex.ToString());
                Main.Logger.Error(ex);
                if (domain != null)
                {
                    ScriptDomain.Unload(domain);
                }
            }
        }

        public static void Unload()
        {
            if (Instance == null)
            {
                return;
            }
            Instance.Dispose();
            ScriptDomain.Unload(Instance.CurrentDomain);
            Instance = null;
        }

        private void Tick(object sender, EventArgs args)
        {
            CurrentDomain.DoTick();
        }

        private void KeyEvent(Keys keys, bool status)
        {
            CurrentDomain.DoKeyEvent(keys, status);
        }

        public void Dispose()
        {
            PrimaryDomain.Tick -= Tick;
            PrimaryDomain.KeyEvent -= KeyEvent;
        }
    }
}
