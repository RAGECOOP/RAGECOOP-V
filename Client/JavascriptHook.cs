using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

using GTA;
using GTA.Native;

namespace CoopClient
{
    /// <summary>
    /// Don't use this!
    /// </summary>
    public class JavascriptHook : Script
    {
        private static readonly List<V8ScriptEngine> ScriptEngines = new List<V8ScriptEngine>();

        /// <summary>
        /// Don't use this!
        /// </summary>
        public JavascriptHook()
        {
            Tick += Ontick;
        }

        private void Ontick(object sender, EventArgs e)
        {
            if (!Main.MainNetworking.IsOnServer() || ScriptEngines.Count == 0)
            {
                return;
            }

            lock (ScriptEngines)
            {
                ScriptEngines.ForEach(engine => engine.Script.API.InvokeTick());
            }
        }

        internal static void LoadAll()
        {
            string serverAddress = Main.MainSettings.LastServerAddress.Replace(":", ".");

            if (!Directory.Exists("scripts\\resources\\" + serverAddress))
            {
                try
                {
                    Directory.CreateDirectory("scripts\\resources\\" + serverAddress);
                }
                catch (Exception ex)
                {
                    GTA.UI.Notification.Show("~r~~h~Javascript Error");
                    Logger.Write(ex.Message, Logger.LogLevel.Server);

                    // Without the directory we can't do the other stuff
                    return;
                }
            }

            lock (ScriptEngines)
            {
                foreach (string script in Directory.GetFiles("scripts\\resources\\" + serverAddress, "*.js"))
                {
                    V8ScriptEngine engine = new V8ScriptEngine();

                    engine.AddHostObject("SHVDN", new HostTypeCollection(Assembly.LoadFrom("ScriptHookVDotNet3.dll")));
                    engine.AddHostObject("LemonUI", new HostTypeCollection(Assembly.LoadFrom("scripts\\LemonUI.SHVDN3.dll")));
                    engine.AccessContext = typeof(ScriptContext);
                    engine.AddHostObject("API", new ScriptContext());

                    try
                    {
                        engine.Execute(File.ReadAllText(script));
                    }
                    catch (Exception ex)
                    {
                        GTA.UI.Notification.Show("~r~~h~Javascript Error");
                        Logger.Write(ex.Message, Logger.LogLevel.Server);
                    }
                    finally
                    {
                        engine.Script.API.InvokeStart();
                        ScriptEngines.Add(engine);
                    }
                }
            }
        }

        internal static void StopAll()
        {
            lock (ScriptEngines)
            {
                ScriptEngines.ForEach(engine => engine.Script.API.InvokeStop());
                ScriptEngines.Clear();
            }
        }

        internal static void InvokePlayerConnect(string username, long nethandle)
        {
            lock (ScriptEngines)
            {
                ScriptEngines.ForEach(engine => engine.Script.API.InvokePlayerConnect(username, nethandle));
            }
        }

        internal static void InvokePlayerDisonnect(string username, long nethandle, string reason = null)
        {
            lock (ScriptEngines)
            {
                ScriptEngines.ForEach(engine => engine.Script.API.InvokePlayerDisonnect(username, nethandle, reason));
            }
        }

        internal static void InvokeChatMessage(string from, string message)
        {
            lock (ScriptEngines)
            {
                ScriptEngines.ForEach(engine => engine.Script.API.InvokeChatMessage(from, message));
            }
        }
    }

    internal class ScriptContext
    {
        #region DELEGATES
        public delegate void EmptyEvent();
        public delegate void PlayerConnectEvent(string username, long nethandle, string reason);
        public delegate void ChatMessageEvent(string from, string message);
        #endregion

        #region EVENTS
        public event EmptyEvent OnStart, OnStop, OnTick;
        public event PlayerConnectEvent OnPlayerConnect, OnPlayerDisconnect;
        public event ChatMessageEvent OnChatMessage;

        internal void InvokeStart()
        {
            OnStart?.Invoke();
        }

        internal void InvokeStop()
        {
            OnStop?.Invoke();
        }

        internal void InvokeTick()
        {
            OnTick?.Invoke();
        }

        internal void InvokePlayerConnect(string username, long nethandle)
        {
            OnPlayerConnect?.Invoke(username, nethandle, null);
        }

        internal void InvokePlayerDisonnect(string username, long nethandle, string reason)
        {
            OnPlayerDisconnect?.Invoke(username, nethandle, reason);
        }

        internal void InvokeChatMessage(string from, string message)
        {
            OnChatMessage?.Invoke(from, message);
        }
        #endregion

        /* ===== PLAYER STUFF ===== */
        public void SendLocalMessage(string message)
        {
            Main.MainChat.AddMessage("JAVASCRIPT", message);
        }

        public string GetLocalUsername()
        {
            return Main.MainSettings.Username;
        }

        public long GetLocalNetHandle()
        {
            return Main.LocalNetHandle;
        }

        // This only applies to server-side created objects
        public void CleanUpWorld()
        {
            Main.CleanUpWorld();
        }

        // This create an object to delete it with CleanUpWorld() or on disconnect
        public void CreateObject(string hash, params object[] args)
        {
            if (!Hash.TryParse(hash, out Hash ourHash) || !Main.CheckNativeHash.ContainsKey((ulong)ourHash))
            {
                GTA.UI.Notification.Show("~r~~h~Javascript Error");
                Logger.Write($"Hash \"{ourHash}\" has not been found!", Logger.LogLevel.Server);
                return;
            }

            int result = Function.Call<int>(ourHash, args.Select(o => new InputArgument(o)).ToArray());

            foreach (KeyValuePair<ulong, byte> checkHash in Main.CheckNativeHash)
            {
                if (checkHash.Key == (ulong)ourHash)
                {
                    lock (Main.ServerItems)
                    {
                        Main.ServerItems.Add(result, checkHash.Value);
                    }
                    break;
                }
            }
        }
        /* ===== PLAYER STUFF ===== */
    }
}
