using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

using GTA;

namespace CoopClient
{
    /// <summary>
    /// Don't use this!
    /// </summary>
    public class JavascriptHook : Script
    {
        private static List<V8ScriptEngine> ScriptEngines;

        /// <summary>
        /// Don't use this!
        /// </summary>
        public JavascriptHook()
        {
            Tick += Ontick;
        }

        private void Ontick(object sender, EventArgs e)
        {
            if (!Main.MainNetworking.IsOnServer())
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
            ScriptEngines = new List<V8ScriptEngine>();

            string serverAddress = Main.MainSettings.LastServerAddress.Replace(":", ".");

            if (!Directory.Exists("scripts\\resources\\" + serverAddress))
            {
                try
                {
                    Directory.CreateDirectory("scripts\\resources\\" + serverAddress);
                }
                catch (Exception ex)
                {
                    // TODO
                }
            }

            lock (ScriptEngines)
            {
                foreach (string script in Directory.GetFiles("scripts\\resources\\" + serverAddress, "*.js"))
                {
                    V8ScriptEngine engine = new V8ScriptEngine();

                    engine.AddHostObject("SHVDN", new HostTypeCollection(Assembly.LoadFrom("ScriptHookVDotNet3.dll")));
                    engine.AddHostObject("LemonUI", new HostTypeCollection(Assembly.LoadFrom("scripts\\LemonUI.SHVDN3.dll")));
                    engine.AddHostObject("API", new ScriptContext());

                    try
                    {
                        engine.Execute(File.ReadAllText(script));
                    }
                    catch (Exception ex)
                    {
                        // TODO
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

    /// <summary>
    /// FOR JAVASCRIPT ONLY!
    /// </summary>
    public class ScriptContext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="nethandle"></param>
        /// <param name="reason"></param>
        public delegate void PlayerConnectEvent(string username, long nethandle, string reason);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="message"></param>
        public delegate void ChatMessageEvent(string from, string message);

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler OnStart, OnStop, OnTick;
        /// <summary>
        /// 
        /// </summary>
        public event PlayerConnectEvent OnPlayerConnect, OnPlayerDisconnect;
        /// <summary>
        /// 
        /// </summary>
        public event ChatMessageEvent OnChatMessage;

        internal void InvokeStart()
        {
            OnStart?.Invoke(this, EventArgs.Empty);
        }

        internal void InvokeStop()
        {
            OnStop?.Invoke(this, EventArgs.Empty);
        }

        internal void InvokeTick()
        {
            OnTick?.Invoke(this, EventArgs.Empty);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void SendLocalMessage(string message)
        {
            Main.MainChat.AddMessage("JAVASCRIPT", message);
        }
    }
}
