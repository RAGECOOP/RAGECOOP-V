using System;

using Microsoft.ClearScript.V8;

using GTA;

namespace CoopClient
{
    /// <summary>
    /// Don't use this!
    /// </summary>
    public class JavascriptHook : Script
    {
        private bool LoadedEngine = false;

        private static V8ScriptEngine ScriptEngine;

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

            if (!LoadedEngine)
            {
                ScriptEngine = new V8ScriptEngine();
                LoadedEngine = true;

                ScriptEngine.AddHostObject("script", new ScriptContext());
                ScriptEngine.Execute(System.IO.File.ReadAllText("scripts\\test.js"));
            }
        }
    }

    /// <summary>
    /// FOR JAVASCRIPT ONLY!
    /// </summary>
    public class ScriptContext
    {
        /// <summary>
        /// Don't use this!
        /// </summary>
        public void SendMessage(string message)
        {
            Main.MainChat.AddMessage("JAVASCRIPT", message);
        }
    }
}
