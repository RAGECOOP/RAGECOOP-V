using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SHVDN;
using RageCoop.Core;
using GTA.Native;
using System.Reflection;
using System.Windows.Forms;

namespace RageCoop.Client.Scripting
{
    public class ResourceDomain : MarshalByRefObject, IDisposable
    {
        public static ResourceDomain Instance;
        readonly ScriptDomain RootDomain;
        ScriptDomain CurrentDomain => ScriptDomain.CurrentDomain;
        internal ResourceDomain(ScriptDomain root)
        {
            RootDomain = root;

            // Bridge to current ScriptDomain
            root.Tick += Tick;
            root.KeyEvent += KeyEvent;
        }
        public static void Load()
        {
            if (Instance != null)
            {
                throw new Exception("Already loaded");
            }
            ScriptDomain domain = null;
            try
            {
                var dir = Path.GetFullPath(@"RageCoop\Scripts");

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
                    domain.AppDomain.SetData("Console",ScriptDomain.CurrentDomain.AppDomain.GetData("Console"));
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
        void Tick(object sender, EventArgs args)
        {
            CurrentDomain.DoTick();
        }
        void KeyEvent(Keys keys, bool status)
        {
            CurrentDomain.DoKeyEvent(keys, status);
        }

        public void Dispose()
        {
            RootDomain.Tick -= Tick;
            RootDomain.KeyEvent -= KeyEvent;
        }
    }
}
