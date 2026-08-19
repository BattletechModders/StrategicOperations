using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace StrategicOperationsInjector
{

    internal static class Injector
    {
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static AssemblyDefinition game;

        public static void Inject(IAssemblyResolver resolver)
        {
            Console.WriteLine($"StrategicOperationsInjector initing {Assembly.GetExecutingAssembly().GetName().Version}");
            try
            {
                game = resolver.Resolve(new AssemblyNameReference("Assembly-CSharp", null));
                if (game == null)
                {
                    Console.Error.WriteLine("can't resolve main game assembly");
                    return;
                }

                TypeDefinition LanceDef = game.MainModule.GetType("BattleTech.LanceDef");
                if (LanceDef == null)
                {
                    Console.Error.WriteLine("can't resolve BattleTech.LanceDef type");
                    return;
                }
                TypeDefinition LanceDef_Unit = LanceDef.NestedTypes.First((x) => { return x.Name == "Unit"; });
                if (LanceDef_Unit == null)
                {
                    Console.Error.WriteLine("can't resolve BattleTech.LanceDef.Unit type");
                    return;
                }

                Console.WriteLine("  fields before:");
                foreach (var field in LanceDef_Unit.Fields)
                {
                    Console.WriteLine($"    {field.Name}");
                }

                FieldDefinition LanceDef_Unit_Mounts = new FieldDefinition("Mounts", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(LanceDef_Unit.MakeArrayType()));
                LanceDef_Unit.Fields.Add(LanceDef_Unit_Mounts);

                Console.WriteLine("  fields after:");
                foreach (var field in LanceDef_Unit.Fields)
                {
                    Console.WriteLine($"    {field.Name}");
                }
             
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
    }
}
