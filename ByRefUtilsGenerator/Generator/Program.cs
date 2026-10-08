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
                var type = asm.MainModule.GetType("Mod.LowLevel.Ref");
                TypeDefinition reftype = asm.MainModule.GetType("Mod.LowLevel.RawRef");
                TypeReference objtype = asm.MainModule.TypeSystem.Object;
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
                    emitter.Emit(OpCodes.Ldflda, field);
                    emitter.Emit(OpCodes.Ldind_Ref);
                    emitter.Emit(OpCodes.Castclass, objtype);
                    emitter.Emit(OpCodes.Ret);
                }
                {
                    var method = reftype.GetMethod("SetRefObj");
                    method.Body.Instructions.Clear();

                    var emitter = method.Body.GetILProcessor();
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ldarg_1);
                    emitter.Emit(OpCodes.Conv_I);
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
                {
                    var method = type.GetMethod("Unprotect");
                    method.Body.Instructions.Clear();

                    var emitter = method.Body.GetILProcessor();
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ret);
                }
                {
                    var method = type.GetMethod("IgnoreOut");
                    method.Body.Instructions.Clear();

                    var emitter = method.Body.GetILProcessor();
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ret);
                }

                EmitRefSafetyRulesAttribute(asm);
                EmitScopedRefOnInParameters(asm);

                asm.Write(tar);
                asm.Dispose();
            }

            {
                var asm = AssemblyDefinition.ReadAssembly("ByRefUtils.TrackingRef.dll");
                var type = asm.MainModule.GetType("Mod.LowLevel.TrackingRefManager");

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

        /// <summary>
        /// Injects [module: RefSafetyRules(version)] so that C# 11+ consumers apply the
        /// updated ref-safety escape rules to this module's APIs (out-parameter passthrough
        /// bridges like Ref.IgnoreOut become usable in `return ref`). The attribute type is
        /// compiler-reserved (CS8335) and cannot be attached from source, so it has to be
        /// emitted here at the metadata level. Idempotent: skips if already present.
        /// </summary>
        static void EmitRefSafetyRulesAttribute(AssemblyDefinition asm, int version = 11)
        {
            const string attrNamespace = "System.Runtime.CompilerServices";
            const string attrFullName = attrNamespace + ".RefSafetyRulesAttribute";
            var module = asm.MainModule;

            // already attached to the module? nothing to do
            foreach (var ca in module.CustomAttributes)
            {
                if (ca.AttributeType.FullName == attrFullName)
                {
                    return;
                }
            }

            // find or create the shim attribute type (old assemblies don't contain it)
            var attrType = module.GetType(attrFullName);
            if (attrType == null)
            {
                attrType = new TypeDefinition(
                    attrNamespace,
                    "RefSafetyRulesAttribute",
                    TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                    module.ImportReference(typeof(System.Attribute)));

                // [AttributeUsage(Assembly | Module, AllowMultiple = false, Inherited = false)]
                var usageCtor = module.ImportReference(
                    typeof(AttributeUsageAttribute).GetConstructor(new[] { typeof(AttributeTargets) }));
                var usage = new CustomAttribute(usageCtor);
                usage.ConstructorArguments.Add(new CustomAttributeArgument(
                    module.ImportReference(typeof(AttributeTargets)),
                    (int)(AttributeTargets.Assembly | AttributeTargets.Module)));
                usage.Fields.Add(new CustomAttributeNamedArgument("AllowMultiple",
                    new CustomAttributeArgument(module.TypeSystem.Boolean, false)));
                usage.Fields.Add(new CustomAttributeNamedArgument("Inherited",
                    new CustomAttributeArgument(module.TypeSystem.Boolean, false)));
                attrType.CustomAttributes.Add(usage);

                // public readonly int Version;
                var versionField = new FieldDefinition("Version",
                    FieldAttributes.Public | FieldAttributes.InitOnly,
                    module.TypeSystem.Int32);

                // public RefSafetyRulesAttribute(int version) { Version = version; }
                var ctor = new MethodDefinition(".ctor",
                    MethodAttributes.Public | MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    module.TypeSystem.Void);
                ctor.Parameters.Add(new ParameterDefinition("version", ParameterAttributes.None, module.TypeSystem.Int32));

                // System.Attribute's own constructor is protected - reflect with NonPublic
                var baseCtor = module.ImportReference(typeof(System.Attribute).GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    binder: null, Type.EmptyTypes, modifiers: null));

                var emitter = ctor.Body.GetILProcessor();
                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Call, baseCtor);
                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldarg_1);
                emitter.Emit(OpCodes.Stfld, versionField);
                emitter.Emit(OpCodes.Ret);

                attrType.Fields.Add(versionField);
                attrType.Methods.Add(ctor);
                module.Types.Add(attrType);
            }

            // [module: RefSafetyRules(version)]
            var attrCtor = attrType.GetMethod(".ctor");
            var attribute = new CustomAttribute(attrCtor);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.Int32, version));
            module.CustomAttributes.Add(attribute);
        }

        /// <summary>
        /// Adds [ScopedRef] to every `in` parameter of ref-returning methods (currently
        /// Ref.Unprotect&lt;T&gt;(in T rreadonly) - the readonly-laundering bridge whose IL body
        /// above returns the in-parameter's ref directly). The scoped promise removes the
        /// parameter from the consumer's escape-min entirely, so ANY argument shape can be
        /// passed while returning the result by ref: `return ref Ref.Unprotect(in r)` or even
        /// `return ref Ref.Unprotect(someLocal)` compile. Without it the in parameter
        /// participates in the min and demands a ref-returnable argument (locals fail).
        /// The metadata shape mirrors what C# 11 emits for `scoped in`: the parameter
        /// carries [In, ScopedRef, IsReadOnly] - In/IsReadOnly are already present on
        /// plain `in`, so only ScopedRef is added here. Idempotent per parameter.
        /// NOTE: Cecil's IsByReference returns false for `T& modreq(InAttribute)`
        /// (the exact shape C# emits for `in`), so parameters are matched by the
        /// '&' suffix in their type name instead. `!p.IsOut` keeps out parameters
        /// (Ref.IgnoreOut) out - they are covered by the module-level RefSafetyRules.
        /// </summary>
        static void EmitScopedRefOnInParameters(AssemblyDefinition asm)
        {
            const string ns = "System.Runtime.CompilerServices";
            var module = asm.MainModule;

            // ScopedRefAttribute shim (ns2.0 lacks it)
            var shim = module.GetType($"{ns}.ScopedRefAttribute");
            if (shim == null)
            {
                shim = new TypeDefinition(ns, "ScopedRefAttribute",
                    TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                    module.ImportReference(typeof(System.Attribute)));

                // [AttributeUsage(Parameter | Field, AllowMultiple = false, Inherited = false)]
                var usageCtor = module.ImportReference(
                    typeof(AttributeUsageAttribute).GetConstructor(new[] { typeof(AttributeTargets) }));
                var usage = new CustomAttribute(usageCtor);
                usage.ConstructorArguments.Add(new CustomAttributeArgument(
                    module.ImportReference(typeof(AttributeTargets)),
                    (int)(AttributeTargets.Parameter | AttributeTargets.Field)));
                usage.Fields.Add(new CustomAttributeNamedArgument("AllowMultiple",
                    new CustomAttributeArgument(module.TypeSystem.Boolean, false)));
                usage.Fields.Add(new CustomAttributeNamedArgument("Inherited",
                    new CustomAttributeArgument(module.TypeSystem.Boolean, false)));
                shim.CustomAttributes.Add(usage);

                var ctor = new MethodDefinition(".ctor",
                    MethodAttributes.Public | MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    module.TypeSystem.Void);
                var baseCtor = module.ImportReference(typeof(System.Attribute).GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    binder: null, Type.EmptyTypes, modifiers: null));
                var il = ctor.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, baseCtor);
                il.Emit(OpCodes.Ret);
                shim.Methods.Add(ctor);
                module.Types.Add(shim);
            }
            var shimCtor = shim.GetMethod(".ctor");

            // annotate every `in` parameter of every ref-returning method
            // (currently just Ref.Unprotect; future in/ref-parameter additions are
            // covered automatically since !p.IsOut matches ref as well)
            int annotated = 0;
            foreach (var type in module.Types)
            {
                if (type.Namespace != "Mod.LowLevel") continue;
                foreach (var method in type.Methods)
                {
                    if (!method.ReturnType.IsByReference) continue;      // ref-returning methods
                    foreach (var p in method.Parameters)
                    {
                        // Cecil quirk: IsByReference is false for `T& modreq(InAttribute)`
                        bool isByRefLike = p.ParameterType.Name.Contains("&");
                        if (isByRefLike && !p.IsOut)
                        {
                            if (!p.CustomAttributes.Any(a => a.AttributeType.FullName == $"{ns}.ScopedRefAttribute"))
                            {
                                p.CustomAttributes.Add(new CustomAttribute(shimCtor));
                                annotated++;
                            }
                        }
                    }
                }
            }
            Console.WriteLine($"[ScopedRef] annotated {annotated} in-parameters");
        }
    }
}
