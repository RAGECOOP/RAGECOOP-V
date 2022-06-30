using System.IO;
using RageCoop.Core.Scripting;
using ICSharpCode.SharpZipLib.Zip;
using System;

namespace RageCoop.Client.Scripting
{
	internal class Resources:ResourceLoader
	{
		public Resources() : base("RageCoop.Client.Scripting.ClientScript", Main.Logger) { }
		private void StartAll()
		{
			lock (LoadedResources)
			{
				foreach (var d in LoadedResources)
				{
					foreach (var s in d.Scripts)
					{
                        try
                        {
							s.OnStart();
						}
						catch(Exception ex)
                        {
							Logger.Error("Error occurred when starting script:"+s.GetType().FullName);
							Logger?.Error(ex);
                        }
					}
				}
			}
		}
		private void StopAll()
		{
			lock (LoadedResources)
			{
				foreach (var d in LoadedResources)
				{
					foreach (var s in d.Scripts)
					{
						try
						{
							s.OnStop();
						}
						catch (Exception ex)
						{
							Logger.Error("Error occurred when stopping script:"+s.GetType().FullName);
							Logger?.Error(ex);
						}
					}
				}
			}
		}
		/// <summary>
		/// Load all resources from the server
		/// </summary>
		/// <param name="path">The path to the directory containing all resources to load.</param>
		public void Load(string path)
		{
			Unload();
			foreach (var d in Directory.GetDirectories(path))
			{
				if(Path.GetFileName(d).ToLower() != "data")
				{
					Directory.Delete(d, true);
				}
			}
			Directory.CreateDirectory(path);
			foreach (var resource in Directory.GetDirectories(path))
			{
                if (Path.GetFileName(resource).ToLower()!="data") { continue; }
				Logger?.Info($"Loading resource: {Path.GetFileName(resource)}");
				LoadResource(resource,Path.Combine(path,"data"));
			}
			StartAll();
		}
		public void Unload()
        {
			StopAll();
			LoadedResources.Clear();
        }
	}

}
