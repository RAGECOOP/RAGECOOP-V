using System.IO;
using RageCoop.Core.Scripting;
using Ionic.Zip;

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
						(s as ClientScript).CurrentResource=d;
						Main.QueueAction(() => s.OnStart());
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
						Main.QueueAction(() => s.OnStop());
					}
				}
			}
		}
		/// <summary>
		/// Load all resources from the server
		/// </summary>
		/// <param name="path">The path to the directory containing the resources.</param>
		public void Load(string path)
		{
			Unload();
			foreach (var d in Directory.GetDirectories(path))
			{
				Directory.Delete(d, true);
			}
			using (var zip = ZipFile.Read(Path.Combine(path, "Resources.zip")))
			{
				zip.ExtractAll(path, ExtractExistingFileAction.OverwriteSilently);
			}
			Directory.CreateDirectory(path);
			foreach (var resource in Directory.GetDirectories(path))
			{
				Logger?.Info($"Loading resource: {Path.GetFileName(resource)}");
				LoadResource(resource);
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
