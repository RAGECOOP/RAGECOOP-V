using RageCoop.Core;
using Console = GTA.Console;
using SHVDN;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using static System.Net.WebRequestMethods;

namespace RageCoop.Client.Scripting
{
    internal class ResourceDomain : MarshalByRefObject, IDisposable
    {
        public static ConcurrentDictionary<string, ResourceDomain> LoadedDomains => new ConcurrentDictionary<string, ResourceDomain>(_loadedDomains);
        static readonly ConcurrentDictionary<string, ResourceDomain> _loadedDomains = new ConcurrentDictionary<string, ResourceDomain>();
        public static ScriptDomain PrimaryDomain;
        public string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private ScriptDomain CurrentDomain => ScriptDomain.CurrentDomain;
        private static ConcurrentDictionary<string, Action<object>> _callBacks = new ConcurrentDictionary<string, Action<object>>();
        API API => API.GetInstance();
        private ResourceDomain(ScriptDomain primary, string[] apiPaths)
        {

            AppDomain.CurrentDomain.SetData("Primary", primary);
            foreach (var apiPath in apiPaths)
            {
                try
                {
                    Assembly.LoadFrom(apiPath);
                }
                catch
                {

                }
            }
            PrimaryDomain = primary;
            primary.Tick += Tick;
            primary.KeyEvent += KeyEvent; 
            CurrentDomain.Start();
            SetupScripts();
            Console.WriteLine($"Loaded domain: {AppDomain.CurrentDomain.FriendlyName}, {AppDomain.CurrentDomain.BaseDirectory}");
        }
        public static bool IsLoaded(string dir)
        {
            return _loadedDomains.ContainsKey(Path.GetFullPath(dir).ToLower());
        }
        public void SetupScripts()
        {
            foreach (var s in GetClientScripts())
            {

                try
                {
                    API.Logger.Debug("Starting script: " + s.GetType().FullName);
                    var script = (ClientScript)s;
                    var res = API.GetResource(Path.GetFileName(Directory.GetParent(script.Filename).FullName));
                    if (res == null) { API.Logger.Warning("Failed to locate resource for script: " + script.Filename); continue; }
                    script.CurrentResource = res;
                    script.CurrentFile = res.Files.Values.Where(x => x.Name.ToLower() == script.Filename.Substring(res.ScriptsDirectory.Length + 1).Replace('\\', '/')).FirstOrDefault();
                    res.Scripts.Add(script);
                    s.GetType().Assembly.GetReferencedAssemblies().ForEach(x => Assembly.Load(x.FullName));
                    script.OnStart();
                }
                catch (Exception ex)
                {
                    API.Logger.Error($"Failed to start {s.GetType().FullName}", ex);
                }
            }
        }
        public object[] GetClientScripts()
        {
            Console.WriteLine("Running scripts: " + ScriptDomain.CurrentDomain.RunningScripts.Select(x => x.ScriptInstance.GetType().FullName).Dump());
            return ScriptDomain.CurrentDomain.RunningScripts.Where(x =>
            x.ScriptInstance.GetType().IsSubclassOf(typeof(ClientScript)) &&
            !x.ScriptInstance.GetType().IsAbstract).Select(x => x.ScriptInstance).ToArray();
        }
        public static void RegisterCallBackForCurrentDomain(string name, Action<object> callback)
        {
            if (!_callBacks.TryAdd(name, callback))
            {
                throw new Exception("Failed to add callback");
            }
        }
        public void DoCallback(string name,object data)
        {
            if(_callBacks.TryGetValue(name, out var callBack))
            {
                callBack(data);
            }
        }
        public static void DoCallBack(string name,object data)
        {
            foreach(var d in _loadedDomains)
            {
                d.Value.DoCallback(name, data);
            }
        }
        public static ResourceDomain Load(string dir = @"RageCoop\Scripts\Debug")
        {
            lock (_loadedDomains)
            {
                dir = Path.GetFullPath(dir).ToLower();
                if (!Util.IsPrimaryDomain)
                {
                    throw new InvalidOperationException("Cannot load in another domain");
                }
                if (IsLoaded(dir))
                {
                    throw new Exception("Already loaded");
                }
                ScriptDomain sDomain = null;
                try
                {
                    dir = Path.GetFullPath(dir);
                    Directory.CreateDirectory(dir);

                    // Copy test script
                    // File.Copy(@"M:\SandBox-Shared\repos\RAGECOOP\RAGECOOP-V\bin\Debug\TestScript.dll", Path.Combine(dir, Path.GetFileName("TestScript.dll")), true);

                    // Load domain in main thread
                    Main.QueueToMainThread(() =>
                    {

                        var api = new List<string>();
                        api.Add(typeof(ResourceDomain).Assembly.Location);
                        api.AddRange(typeof(ResourceDomain).Assembly.GetReferencedAssemblies()
                            .Select(x => Assembly.Load(x.FullName).Location)
                            .Where(x => !string.IsNullOrEmpty(x)));

                        sDomain = ScriptDomain.Load(Directory.GetParent(typeof(ScriptDomain).Assembly.Location).FullName, dir);
                        sDomain.AppDomain.SetData("Console", ScriptDomain.CurrentDomain.AppDomain.GetData("Console"));
                        sDomain.AppDomain.SetData("RageCoop.Client.API", API.GetInstance());
                        _loadedDomains.TryAdd(dir, (ResourceDomain)sDomain.AppDomain.CreateInstanceFromAndUnwrap(typeof(ResourceDomain).Assembly.Location, typeof(ResourceDomain).FullName, false, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { ScriptDomain.CurrentDomain, api.ToArray() }, null, null));
                    });

                    // Wait till next tick
                    GTA.Script.Yield();
                    return _loadedDomains[dir];
                }
                catch (Exception ex)
                {
                    GTA.UI.Notification.Show(ex.ToString());
                    Main.Logger.Error(ex);
                    if (sDomain != null)
                    {
                        ScriptDomain.Unload(sDomain);
                    }
                    throw;
                }
            }
        }

        public static void Unload(ResourceDomain domain)
        {
            lock (_loadedDomains)
            {
                Exception ex = null;
                Main.QueueToMainThread(() =>
                {
                    try
                    {
                        domain.Dispose();
                        ScriptDomain.Unload(domain.CurrentDomain);
                        _loadedDomains.TryRemove(domain.BaseDirectory, out _);
                    }
                    catch (Exception e) { ex = e; }
                });
                GTA.Script.Yield();
                if (ex != null) { throw ex; }
            }
        }
        public static void Unload(string dir)
        {
            Unload(_loadedDomains[Path.GetFullPath(dir).ToLower()]);
        }
        public static void UnloadAll()
        {
            lock (_loadedDomains)
            {
                foreach (var d in _loadedDomains.Values.ToArray())
                {
                    Unload(d);
                }
            }
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
            foreach (var s in GetClientScripts())
            {
                try
                {
                    API.Logger.Debug("Stopping script: " + s.GetType().FullName);
                    ((ClientScript)s).OnStop();
                }
                catch (Exception ex)
                {
                    API.Logger.Error($"Failed to stop {s.GetType().FullName}", ex);
                }
            }
        }
    }
}
