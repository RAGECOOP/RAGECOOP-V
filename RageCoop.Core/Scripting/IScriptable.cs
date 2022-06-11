using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;
namespace RageCoop.Core.Scripting
{
	public interface IScriptable
	{
		void OnStart();
		void OnStop();
	}
}
