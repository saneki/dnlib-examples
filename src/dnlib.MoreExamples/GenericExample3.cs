using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

/*

This example shows how to create types with generic instance
parameters, and nested types with even more of them.


dnSpy output of created file:

using System;

namespace My.Namespace
{
	internal class Startup
	{
		private static int Main(string[] args)
		{
			return 0;
		}
	}

	public class GClass<A, B>
	{
		public class GSubClass<C>
		{
			public void SomeMethod(B b, C c)
			{
			}

			public void SomeOtherMethod<D>(D d, A a, C c)
			{
			}
		}
	}
}

peverify output:

	C:\> peverify GenericExample3.exe /IL /MD

	Microsoft (R) .NET Framework PE Verifier.  Version  4.0.30319.33440
	Copyright (c) Microsoft Corporation.  All rights reserved.

	All Classes and Methods in GenericExample3.exe Verified.

*/

namespace dnlib.MoreExamples
{
	public class GenericExample3
	{
		public static void Run()
		{
			// This is the file that will be created
			string newFileName = "GenericExample3.exe";

			// Create the module
			var mod = new ModuleDefUser("GenericExample3", Guid.NewGuid(),
				new AssemblyRefUser(new AssemblyNameInfo(typeof(int).Assembly.GetName().FullName)));
			// It's a console app
			mod.Kind = ModuleKind.Console;
			// Create the assembly and add the created module to it
			new AssemblyDefUser("GenericExample3", new Version(1, 2, 3, 4)).Modules.Add(mod);

			// Add the startup type. It derives from System.Object.
			TypeDef startUpType = new TypeDefUser("My.Namespace", "Startup", mod.CorLibTypes.Object.TypeDefOrRef);
			startUpType.Attributes = TypeAttributes.NotPublic | TypeAttributes.AutoLayout |
									TypeAttributes.Class | TypeAttributes.AnsiClass;
			// Add the type to the module
			mod.Types.Add(startUpType);

			// Create the entry point method
			MethodDef entryPoint = new MethodDefUser("Main",
				MethodSig.CreateStatic(mod.CorLibTypes.Int32, new SZArraySig(mod.CorLibTypes.String)));
			entryPoint.Attributes = MethodAttributes.Private | MethodAttributes.Static |
							MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
			entryPoint.ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed;
			// Name the 1st argument (argument 0 is the return type)
			entryPoint.ParamDefs.Add(new ParamDefUser("args", 1));
			// Add the method to the startup type
			startUpType.Methods.Add(entryPoint);
			// Set module entry point
			mod.EntryPoint = entryPoint;

			// Create a type with 2 generic parameters, A and B
			// Would look like: public class GClass<A, B>
			var genericType = new TypeDefUser("My.Namespace", "GClass", mod.CorLibTypes.Object.TypeDefOrRef);
			genericType.Attributes = TypeAttributes.Public | TypeAttributes.AutoLayout |
				TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit;
			genericType.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "A"));
			genericType.GenericParameters.Add(new GenericParamUser(1, GenericParamAttributes.NonVariant, "B"));
			// Add generic type to module
			mod.Types.Add(genericType);

			// Note: NestedPublic instead of Public, blank namespace
			// Would look like: public class GSubClass<A, B, C>
			var genericSubType = new TypeDefUser("", "GSubClass", mod.CorLibTypes.Object.TypeDefOrRef);
			genericSubType.Attributes = TypeAttributes.NestedPublic | TypeAttributes.AutoLayout |
				TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit;
			// Need to add the 2 generic parameters from the nested-parent class, A and B
			genericSubType.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "A"));
			genericSubType.GenericParameters.Add(new GenericParamUser(1, GenericParamAttributes.NonVariant, "B"));
			// Add a generic parameter specific to this nested class, C
			genericSubType.GenericParameters.Add(new GenericParamUser(2, GenericParamAttributes.NonVariant, "C"));

			// public void GSubClass<A, B, C>.SomeMethod(B arg1, C arg2) { ... }
			// or: public void GSubClass<!0, !1, !2>.SomeMethod(!1, !2) { ... }
			var someMethod = new MethodDefUser("SomeMethod",
				MethodSig.CreateInstance(mod.CorLibTypes.Void, new GenericVar(1), new GenericVar(2)));
			someMethod.Attributes = MethodAttributes.Public;
			someMethod.ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed;
			genericSubType.Methods.Add(someMethod);

			// Create method with a method generic parameter (GenericMVar)
			// public void GSubClass<A, B, C>.SomeOtherMethod<D>(D arg1, A arg2, C arg3) { ... }
			// or: public void GSubClass<!0, !1, !2>.SomeOtherMethod<!!0>(!!0, !0, !2) { ... }
			var someGenericMethod = new MethodDefUser("SomeOtherMethod",
				MethodSig.CreateInstanceGeneric(1, mod.CorLibTypes.Void, new GenericMVar(0), new GenericVar(0), new GenericVar(2)));
			someGenericMethod.Attributes = MethodAttributes.Public;
			someGenericMethod.ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed;
			// Create GenericParam for !!0
			someGenericMethod.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "D"));
			genericSubType.Methods.Add(someGenericMethod);

			// Add as nested type
			genericType.NestedTypes.Add(genericSubType);

			someMethod.Body = new CilBody();
			someMethod.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

			someGenericMethod.Body = new CilBody();
			someGenericMethod.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

			entryPoint.Body = new CilBody();
			entryPoint.Body.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
			entryPoint.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

			mod.Write(newFileName);
		}
	}
}
