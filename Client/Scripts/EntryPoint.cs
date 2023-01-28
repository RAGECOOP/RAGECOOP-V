using System;
using System.Runtime.InteropServices;
using GTA;
using RageCoop.Client;
using SHVDN;

/// <summary>
/// Template for generating AOT entrypoints
/// </summary>
public static class EntryPoint
{
    private static void ModuleSetup()
    {
        Script script = new Main();
        Core.RegisterScript(script);
        script = new WorldThread();
        Core.RegisterScript(script);
        script = new DevTool();
        Core.RegisterScript(script);
    }

    [UnmanagedCallersOnly(EntryPoint = "OnInit")]
    public unsafe static void OnInit(IntPtr module)
    {
        try
        {
            Core.OnInit(module);
            ModuleSetup();
        }
        catch (Exception ex)
        {
            PInvoke.MessageBoxA((IntPtr)0, ex.ToString(), "Module initialization error", 0u);
            throw;
        }
    }

    /// <summary>
    /// Called prior to module unload
    /// </summary>
    /// <param name="module"></param>
    [UnmanagedCallersOnly(EntryPoint = "OnUnload")]
    public static void OnUnload(IntPtr module)
    {
        try
        {
            Core.OnUnload(module);
        }
        catch (Exception ex)
        {
            Logger.Error((ReadOnlySpan<char>)("Module unload error: " + ex.ToString()));
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "OnKeyboard")]
    public unsafe static void OnKeyboard(int key, ushort repeats, bool scanCode, bool isExtended, bool isWithAlt, bool wasDownBefore, bool isUpNow)
    {
        try
        {
            Core.DoKeyEvent(key, !isUpNow, (PInvoke.GetAsyncKeyState(17) & 0x8000) != 0, (PInvoke.GetAsyncKeyState(16) & 0x8000) != 0, isWithAlt);
        }
        catch (Exception ex)
        {
            Logger.Error((ReadOnlySpan<char>)("Keyboard event error: " + ex.ToString()));
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "OnTick")]
    public static void OnTick(IntPtr currentFiber)
    {
        try
        {
            Core.DoTick(currentFiber);
        }
        catch (Exception ex)
        {
            Logger.Error((ReadOnlySpan<char>)("Tick error: " + ex.ToString()));
        }
    }
}