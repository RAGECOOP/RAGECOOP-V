using RageCoop.Client.Scripting;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeGen
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(@"..\..\..\");
            var props = new StringBuilder();
            var config = new StringBuilder();
            var funcs = new StringBuilder();
            foreach (var prop in typeof(API.Config).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                config.AppendLine($"public static {prop.PropertyType.ToTypeName()} {prop.Name} => GetConfig<{prop.PropertyType.ToTypeName()}>(\"{prop.Name}\");");
            }
            foreach (var prop in typeof(API).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                props.AppendLine($"public static {prop.PropertyType.ToTypeName()} {prop.Name} => GetProperty<{prop.PropertyType.ToTypeName()}>(\"{prop.Name}\");");
            }
            foreach (var f in
                typeof(API).GetMethods(BindingFlags.Public | BindingFlags.Static).
                Where(x =>
                {
                    var attri = x.GetCustomAttribute<RemotingAttribute>();
                    if (attri ==null) return false;
                    return attri.GenBridge;
                }))
            {
                var ret = f.ReturnType.ToTypeName();
                var gReturn = $"<{ret}>";
                if (ret == "System.Void") { ret = "void"; gReturn = string.Empty; }
                var ps = f.GetParameters();
                var paras = string.Join(',', ps.Select(x => $"{x.ParameterType.ToTypeName()} {x.Name}"));
                var parasNoType = string.Join(',', ps.Select(x => x.Name));
                if (ps.Length > 0)
                {
                    parasNoType = "," + parasNoType;
                }
                funcs.AppendLine($"public static {ret} {f.Name}({paras}) => InvokeCommand{gReturn}(\"{f.Name}\"{parasNoType});");
            }
            var code = $@"namespace RageCoop.Client.Scripting
{{
	public static unsafe partial class APIBridge
	{{

        public static class Config
        {{
            {config}
        }}

        #region PROPERTIES

        {props}

        #endregion
        
        #region FUNCTIONS

        {funcs}

        #endregion
	}}
}}
";
            File.WriteAllText(@"Client\Scripting\APIBridge.Generated.cs", code);
        }
        static string ToTypeName(this Type type)
        {
            var name = type.ToString();
            if (type.GenericTypeArguments.Length > 0)
            {
                name = $"{name.Substring(0, name.IndexOf('`'))}<{string.Join(',', type.GenericTypeArguments.Select(ToTypeName))}>";
            }
            name = name.Replace("RageCoop.Client.Player", "RageCoop.Client.Scripting.PlayerInfo");
            return name;
        }
    }
}