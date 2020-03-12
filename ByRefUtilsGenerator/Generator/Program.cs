using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;

namespace Generator
{
    public static class MonoCecilExtensions
    {
        #region Mono.Cecil Extensions
        internal static MethodReference GetReference(this MethodDefinition method, GenericInstanceType type)
        {
            MethodReference mref = new MethodReference(method.Name, method.ReturnType, type);
            foreach (var par in method.Parameters)
            {
                mref.Parameters.Add(par);
            }
            if (!method.IsStatic)
            {
                mref.HasThis = true;
            }
            return mref;
        }
        internal static MethodReference GetReference(this MethodDefinition method)
        {
            MethodReference mref = new MethodReference(method.Name, method.ReturnType, method.DeclaringType);
            foreach (var par in method.Parameters)
            {
                mref.Parameters.Add(par);
            }
            if (!method.IsStatic)
            {
                mref.HasThis = true;
            }
            return mref;
        }
        internal static MethodReference GetReference(this MethodDefinition method, GenericInstanceType type, ModuleDefinition inModule)
        {
            MethodReference mref = inModule.ImportReference(method);
            mref.DeclaringType = inModule.ImportReference(type);
            if (!method.IsStatic)
            {
                mref.HasThis = true;
            }
            return mref;
        }
        internal static MethodReference GetReference(this MethodDefinition method, ModuleDefinition inModule)
        {
            MethodReference mref = inModule.ImportReference(method);
            if (!method.IsStatic)
            {
                mref.HasThis = true;
            }
            return mref;
        }
        internal static List<MethodDefinition> GetMethods(this TypeDefinition type, string name)
        {
            List<MethodDefinition> list = new List<MethodDefinition>();
            foreach (var method in type.Methods)
            {
                if (method.Name == name)
                {
                    list.Add(method);
                }
            }
            return list;
        }
        internal static MethodDefinition GetMethod(this TypeDefinition type, string name)
        {
            var methods = GetMethods(type, name);
            if (methods.Count > 0)
            {
                return methods[0];
            }
            return null;
        }
        internal static MethodDefinition GetMethod(this TypeDefinition type, string name, int paramCnt)
        {
            foreach (var method in type.Methods)
            {
                if (method.Name == name && method.Parameters.Count == paramCnt)
                {
                    return method;
                }
            }
            return null;
        }
        internal static MethodDefinition GetMethod(this TypeDefinition type, string name, params TypeReference[] pars)
        {
            pars = pars ?? new TypeReference[0];
            foreach (var method in type.Methods)
            {
                if (method.Name == name)
                {
                    if (method.Parameters.Count == pars.Length)
                    {
                        bool match = true;
                        for (int i = 0; i < pars.Length; ++i)
                        {
                            if (pars[i] != method.Parameters[i].ParameterType)
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            return method;
                        }
                    }
                }
            }
            return null;
        }
        internal static FieldDefinition GetField(this TypeDefinition type, string name)
        {
            foreach (var field in type.Fields)
            {
                if (field.Name == name)
                {
                    return field;
                }
            }
            return null;
        }
        internal static PropertyDefinition GetProperty(this TypeDefinition type, string name)
        {
            foreach (var prop in type.Properties)
            {
                if (prop.Name == name)
                {
                    return prop;
                }
            }
            return null;
        }
        internal static TypeDefinition GetNestedType(this TypeDefinition type, string name)
        {
            foreach (var ntype in type.NestedTypes)
            {
                if (ntype.Name == name)
                {
                    return ntype;
                }
            }
            return null;
        }
        internal static void AddRange<T>(this Mono.Collections.Generic.Collection<T> collection, IEnumerable<T> values)
        {
            foreach (var val in values)
            {
                collection.Add(val);
            }
        }
        internal static void AddRange<T>(this Mono.Collections.Generic.Collection<T> collection, params T[] values)
        {
            AddRange(collection, (IEnumerable<T>)values);
        }
        internal static void InsertRange<T>(this Mono.Collections.Generic.Collection<T> collection, int index, IEnumerable<T> values)
        {
            foreach (var val in values)
            {
                collection.Insert(index++, val);
            }
        }
        internal static void InsertRange<T>(this Mono.Collections.Generic.Collection<T> collection, int index, params T[] values)
        {
            InsertRange(collection, index, (IEnumerable<T>)values);
        }
        //internal static void Clear<T>(this Mono.Collections.Generic.Collection<T> collection)
        //{
        //    for (int i = collection.Count - 1; i >= 0; --i)
        //    {
        //        collection.RemoveAt(i);
        //    }
        //}
        #endregion
    }
    class Program
    {
        static void Main(string[] args)
        {
            var root = System.IO.Path.GetFullPath("../../../../../");
            var src = root + "/ByRefUtils.cs";
            var tar = root + "/ByRefUtils.dll";
            //var syntaxTree = CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(src));
            //MetadataReference[] references = new MetadataReference[]
            //{
            //    //MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            //};
            //CSharpCompilation compilation = CSharpCompilation.Create(
            //    "ByRefUtils",
            //    syntaxTrees: new[] { syntaxTree },
            //    references: references,
            //    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            //var compileresult = compilation.Emit(tar);

            //if (!compileresult.Success)
            //{
            //    foreach (var error in compileresult.Diagnostics)
            //    {
            //        Console.Error.WriteLine(error.GetMessage());
            //    }
            //    return;
            //}

            {
                var asm = AssemblyDefinition.ReadAssembly("ByRefUtils.dll");
                var type = asm.MainModule.GetType("Capstones.ByRefUtils.Ref");
                TypeDefinition reftype = asm.MainModule.GetType("Capstones.ByRefUtils.RawRef");
                var field = reftype.GetField("_Ref");

                {
                    var method = reftype.GetMethod("GetRef");
                    method.Body.Instructions.Clear();

                    var emitter = method.Body.GetILProcessor();
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ldfld, field);
                    emitter.Emit(OpCodes.Ret);
                }
                {
                    var method = reftype.GetMethod("SetRef");
                    method.Body.Instructions.Clear();

                    var emitter = method.Body.GetILProcessor();
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ldarg_1);
                    emitter.Emit(OpCodes.Stfld, field);
                    emitter.Emit(OpCodes.Ret);
                }
                {
                    var method = reftype.GetMethod("GetRefObj");
                    method.Body.Instructions.Clear();

                    var emitter = method.Body.GetILProcessor();
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ldfld, field);
                    emitter.Emit(OpCodes.Ret);
                }
                {
                    var method = reftype.GetMethod("SetRefObj");
                    method.Body.Instructions.Clear();

                    var emitter = method.Body.GetILProcessor();
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ldarg_1);
                    emitter.Emit(OpCodes.Stfld, field);
                    emitter.Emit(OpCodes.Ret);
                }
                {
                    var method = type.GetMethod("RefEquals");
                    method.Body.Instructions.Clear();

                    var emitter = method.Body.GetILProcessor();
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ldarg_1);
                    emitter.Emit(OpCodes.Ceq);
                    emitter.Emit(OpCodes.Ret);
                }
                {
                    var method = type.GetMethod("GetEmptyRef");
                    method.Body.Instructions.Clear();

                    var emitter = method.Body.GetILProcessor();
                    emitter.Emit(OpCodes.Ldnull);
                    emitter.Emit(OpCodes.Ret);
                }
                {
                    var method = type.GetMethod("IsEmpty");
                    method.Body.Instructions.Clear();

                    var emitter = method.Body.GetILProcessor();
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ldnull);
                    emitter.Emit(OpCodes.Ceq);
                    emitter.Emit(OpCodes.Ret);
                }

                asm.Write(tar);
                asm.Dispose();
            }

            {
                var asm = AssemblyDefinition.ReadAssembly("ByRefUtils.TrackingRef.dll");
                var type = asm.MainModule.GetType("Capstones.ByRefUtils.TrackingRefManager");

                {
                    var method = type.GetMethod("MakeMoreSlot");
                    for (int i = 0; i < 1024; ++i)
                    {
                        var v = new VariableDefinition(new ByReferenceType(asm.MainModule.TypeSystem.Int32));
                        method.Body.Variables.Add(v);
                    }
                    for (int i = 0; i < method.Body.Instructions.Count; ++i)
                    {
                        var ins = method.Body.Instructions[i];
                        if (ins.OpCode.Code == Code.Call && ins.Operand is MethodReference && ((MethodReference)ins.Operand).Name == "SetRef")
                        {
                            var previns = method.Body.Instructions[i - 1];
                            previns.OpCode = OpCodes.Ldloca;
                            previns.Operand = method.Body.Variables[method.Body.Variables.Count - 1];
                            break;
                        }
                    }
                }

                asm.Write(root + "/ByRefUtils.TrackingRef.dll");
                asm.Dispose();
            }
        }
    }
}
