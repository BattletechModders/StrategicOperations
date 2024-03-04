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
    public class Settings
    {
        public bool debugLog { get; set; } = true;
    }
    internal static class Injector
    {
        public static Settings settings { get; set; } = new Settings();
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
            Log.BaseDirectory = AssemblyDirectory;
            Log.InitLog();
            Log.Err?.TWL(0, $"StrategicOperationsInjector initing {Assembly.GetExecutingAssembly().GetName().Version}", true);
            try
            {
                game = resolver.Resolve(new AssemblyNameReference("Assembly-CSharp", null));
                if (game == null)
                {
                    Log.Err?.WL(1, "can't resolve main game assembly", true);
                    return;
                }

                TypeDefinition LanceDef = game.MainModule.GetType("BattleTech.LanceDef");
                if (LanceDef == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.LanceDef type", true);
                    return;
                }
                TypeDefinition LanceDef_Unit = LanceDef.NestedTypes.First((x) => { return x.Name == "Unit"; });
                if (LanceDef_Unit == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.LanceDef.Unit type", true);
                    return;
                }
                Log.M?.WL(1, "fields before:");
                foreach (var field in LanceDef_Unit.Fields)
                {
                    Log.M?.WL(2, $"{field.Name}");
                }
                FieldDefinition LanceDef_Unit_Mounts = new FieldDefinition("Mounts", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(LanceDef_Unit.MakeArrayType()));
                LanceDef_Unit.Fields.Add(LanceDef_Unit_Mounts);
                //      List<CustomAttribute> statName_attrs = prefabNameFieldDef.HasCustomAttributes ? prefabNameFieldDef.CustomAttributes.ToList() : new List<CustomAttribute>();

                //      FieldDefinition LocalGUIDFieldDef = new FieldDefinition("LocalGUID", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(typeof(string)));
                //      FieldDefinition TargetComponentGUIDFieldDef = new FieldDefinition("TargetComponentGUID", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(typeof(string)));
                //      //FieldDefinition ApplyFirstOnlyFieldDef = new FieldDefinition("ApplyFirstOnly", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(typeof(bool)));
                //      Log.M?.WL(1, $"BattleTech.BaseComponentRef.prefabName custom attributes {statName_attrs.Count}:");
                //      foreach (var attr in statName_attrs)
                //      {
                //          LocalGUIDFieldDef.CustomAttributes.Add(attr);
                //          TargetComponentGUIDFieldDef.CustomAttributes.Add(attr);
                //          Log.M?.WL(2, $"{attr.AttributeType.Name}");
                //      }
                //      BaseComponentRef.Fields.Add(LocalGUIDFieldDef);
                //      BaseComponentRef.Fields.Add(TargetComponentGUIDFieldDef);
                Log.M?.WL(1, "fields after:");
                foreach (var field in LanceDef_Unit.Fields)
                {
                    Log.M?.WL(2, $"{field.Name}");
                }
                //      Log.M?.WL(1, "field added successfully", true);

                //      InjectSize(BaseComponentRef, LocalGUIDFieldDef, TargetComponentGUIDFieldDef);
                //      InjectSave(BaseComponentRef, LocalGUIDFieldDef, TargetComponentGUIDFieldDef);
                //      InjectLoad(BaseComponentRef, LocalGUIDFieldDef, TargetComponentGUIDFieldDef);
                //      InjectConstructor(BaseComponentRef, LocalGUIDFieldDef, TargetComponentGUIDFieldDef);

                //      FieldDefinition componentRef = new FieldDefinition("componentRef", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.MechComponentRef")));
                //      FieldDefinition mechDef = new FieldDefinition("mechDef", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.MechDef")));
                //      var LanceMechEquipmentListItem = game.MainModule.GetType("BattleTech.UI.LanceMechEquipmentListItem");
                //      LanceMechEquipmentListItem.Fields.Add(componentRef);
                //      LanceMechEquipmentListItem.Fields.Add(mechDef);
                //      MethodDefinition SetComponentRef = new MethodDefinition("SetComponentRef", Mono.Cecil.MethodAttributes.Public, game.MainModule.TypeSystem.Void);
                //      SetComponentRef.Parameters.Add(new ParameterDefinition("componentRef", Mono.Cecil.ParameterAttributes.None, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.MechComponentRef"))));
                //      SetComponentRef.Parameters.Add(new ParameterDefinition("mechDef", Mono.Cecil.ParameterAttributes.None, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.MechDef"))));
                //      LanceMechEquipmentListItem.Methods.Add(SetComponentRef);
                //      var body = SetComponentRef.Body.GetILProcessor();
                //      body.Emit(OpCodes.Ldarg_0);
                //      body.Emit(OpCodes.Ldarg_1);
                //      body.Emit(OpCodes.Stfld, componentRef);
                //      body.Emit(OpCodes.Ldarg_0);
                //      body.Emit(OpCodes.Ldarg_2);
                //      body.Emit(OpCodes.Stfld, mechDef);
                //      body.Emit(OpCodes.Ret);
                //      var SetLoadout = game.MainModule.GetType("BattleTech.UI.LanceMechEquipmentList").Methods.First(x => { return (x.Name == "SetLoadout") && (x.Parameters.Count == 4); });
                //      int ti = -1;
                //      var SetData = game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.UI.LanceMechEquipmentListItem").Methods.First(x => { return (x.Name == "SetData") && (x.Parameters.Count == 4); }));
                //      for (int i = 0; i < SetLoadout.Body.Instructions.Count; ++i)
                //      {
                //          var instruction = SetLoadout.Body.Instructions[i];
                //          if (instruction.OpCode == OpCodes.Callvirt && instruction.Operand == SetData) { ti = i; break; }
                //      }
                //      if (ti != -1)
                //      {
                //          body = SetLoadout.Body.GetILProcessor();
                //          List<Instruction> instructions = new List<Instruction>() {
                //  body.Create(OpCodes.Dup),
                //  body.Create(OpCodes.Ldloc_S,SetLoadout.Body.Variables[6]),
                //  body.Create(OpCodes.Ldarg_0),
                //  body.Create(OpCodes.Ldfld, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.UI.LanceMechEquipmentList").Fields.First(x=>x.Name=="activeMech"))),
                //  body.Create(OpCodes.Call, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.UI.LanceMechEquipmentListItem").Methods.First(x=>x.Name=="SetComponentRef"))),
                //};
                //          instructions.Reverse();
                //          foreach (var instruction in instructions)
                //              body.InsertAfter(SetLoadout.Body.Instructions[ti], instruction);
                //      }
            }
            catch (Exception e)
            {
                Log.Err?.TWL(0, e.ToString(), true);
            }
        }
    }
}
