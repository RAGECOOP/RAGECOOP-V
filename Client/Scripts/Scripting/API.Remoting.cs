using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Client.Scripting
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	internal class RemotingAttribute : Attribute
	{
		public bool GenBridge = true;
	}

    /// <summary>
    /// Some local remoting implementation with json-based serialization, somewhar slow, but convenient
    /// </summary>
    internal static unsafe partial class API
	{
		static readonly MethodInfo[] _apiEntries =
			typeof(API).GetMethods(BindingFlags.Static | BindingFlags.Public);

		static readonly Dictionary<string, MethodInfo> _commands =
			new(typeof(API).GetMethods().
				Where(md => md.CustomAttributes.
				Any(attri => attri.AttributeType == typeof(RemotingAttribute))).
				Select(x => new KeyValuePair<string, MethodInfo>(x.Name, x)));

		static string _invokeCommand(string name, string[] argsJson)
		{
			if (_commands.TryGetValue(name, out var method))
			{
				var ps = method.GetParameters();

				if (argsJson.Length != ps.Length)
					throw new ArgumentException($"Parameter count mismatch, expecting {ps.Length} parameters, got {argsJson.Length}", nameof(argsJson));

				object[] args = new object[ps.Length];
				for (int i = 0; i < ps.Length; i++)
				{
					args[i] = JsonDeserialize(argsJson[i], ps[i].ParameterType);
				}
				var result = method.Invoke(null, args);
				if (method.ReturnType == typeof(void))
				{
					return "void";
				}
				return JsonSerialize(result);
			}
			throw new KeyNotFoundException($"Command {name} was not found");
		}
	}
}
