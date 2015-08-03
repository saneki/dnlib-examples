using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

/*

This example shows how to create an assembly from scratch with a Main
method that converts an array of ints to a ReadOnlyCollection<Int32>
using the static generic method Array.AsReadOnly<T>.


dnSpy output of created file:

using System;
using System.Collections.ObjectModel;

namespace My.Namespace
{
	internal class Startup
	{
		private static int Main(string[] args)
		{
			ReadOnlyCollection<int> readOnlyCollection = Array.AsReadOnly<int>(new int[]
			{
				5,
				111
			});
			Console.WriteLine("Count: {0}", readOnlyCollection.Count);
			return 0;
		}
	}
}

peverify output:

	C:\> peverify GenericExample2.exe /IL /MD

	Microsoft (R) .NET Framework PE Verifier.  Version  4.0.30319.33440
	Copyright (c) Microsoft Corporation.  All rights reserved.

	All Classes and Methods in generic-smethod-test.exe Verified.


Output of program:

	C:\> GenericExample2.exe
	Count: 2

*/

namespace dnlib.MoreExamples
{
	public class GenericExample2
	{
		public static void Run()
		{
			// This is the file that will be created
			string newFileName = @"GenericExample2.exe";

			// Create the module
			var mod = new ModuleDefUser("GenericExample2", Guid.NewGuid(),
				new AssemblyRefUser(new AssemblyNameInfo(typeof(int).Assembly.GetName().FullName)));
			// It's a console app
			mod.Kind = ModuleKind.Console;
			// Create the assembly and add the created module to it
			new AssemblyDefUser("GenericExample2", new Version(1, 2, 3, 4)).Modules.Add(mod);

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

			// Create System.Console type reference
			var systemConsole = mod.CorLibTypes.GetTypeRef("System", "Console");
			// Create 'void System.Console.WriteLine(string,object)' method reference
			var writeLine2 = new MemberRefUser(mod, "WriteLine",
							MethodSig.CreateStatic(mod.CorLibTypes.Void, mod.CorLibTypes.String,
								mod.CorLibTypes.Object),
							systemConsole);

			var assemblyRef = mod.CorLibTypes.AssemblyRef;
			// Create 'System.Collections.ObjectModel.ReadOnlyCollection`1' type ref
			var roCollectionRef = new TypeRefUser(mod, "System.Collections.ObjectModel", "ReadOnlyCollection`1", assemblyRef);
			// Create 'ReadOnlyCollection<!!0>' signature for return type
			var roCollectionSig = new GenericInstSig(new ClassSig(roCollectionRef), new GenericMVar(0)); // Return type

			// Create 'ReadOnlyCollection<Int32>' type spec
			var roCollectionTypeSpec = new TypeSpecUser(new GenericInstSig(new ClassSig(roCollectionRef), mod.CorLibTypes.Int32));
			// Create 'ReadOnlyCollection<Int32>.get_Count()' method reference
			var roCollectionGetCount = new MemberRefUser(mod, "get_Count",
				MethodSig.CreateInstance(mod.CorLibTypes.Int32),
				roCollectionTypeSpec);

			// Create 'System.Array' type ref
			var arrayRef = new TypeRefUser(mod, "System", "Array", assemblyRef);
			// Create 'ReadOnlyCollection<T> Array.AsReadOnly<T>(T[] array)' method reference
			// Apparently CreateStaticGeneric should be used only if at least one GenericMVar is used? Not 100% certain.
			var asReadOnly = new MemberRefUser(mod, "AsReadOnly",
							MethodSig.CreateStaticGeneric(1, roCollectionSig, new SZArraySig(new GenericMVar(0))),
							arrayRef);
			// Create 'Array.AsReadOnly<Int32>' method spec
			var asReadOnlySpec = new MethodSpecUser(asReadOnly,
				new GenericInstMethodSig(mod.CorLibTypes.Int32));

			// Create 'ReadOnlyCollection<Int32>' signature for local
			var roCollectionInt32 = roCollectionTypeSpec.TryGetGenericInstSig();

			// Method body locals
			IList<Local> locals = new List<Local>();
			locals.Add(new Local(new SZArraySig(mod.CorLibTypes.Int32))); // local[0]: Int32[]
			locals.Add(new Local(roCollectionInt32)); // local[1]: class [mscorlib]System.Collections.ObjectModel.ReadOnlyCollection`1<Int32>

			var body = new CilBody(true, new List<Instruction>(), new List<ExceptionHandler>(), locals);

			// array = new Int32[2];
			body.Instructions.Add(OpCodes.Ldc_I4_2.ToInstruction());
			body.Instructions.Add(OpCodes.Newarr.ToInstruction(mod.CorLibTypes.Int32));
			body.Instructions.Add(OpCodes.Stloc_0.ToInstruction()); // Store array to local[0]

			// array[0] = 5;
			body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction());
			body.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
			body.Instructions.Add(OpCodes.Ldc_I4_5.ToInstruction());
			body.Instructions.Add(OpCodes.Stelem_I4.ToInstruction());

			// array[1] = 111;
			body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction());
			body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
			body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(111));
			body.Instructions.Add(OpCodes.Stelem_I4.ToInstruction());

			// collection = Array.AsReadOnly<Int32>(array)
			body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction());
			body.Instructions.Add(OpCodes.Call.ToInstruction(asReadOnlySpec));
			body.Instructions.Add(OpCodes.Stloc_1.ToInstruction());

			// Console.WriteLine("Count: {0}", collection.Count)
			body.Instructions.Add(OpCodes.Ldstr.ToInstruction("Count: {0}"));
			body.Instructions.Add(OpCodes.Ldloc_1.ToInstruction());
			body.Instructions.Add(OpCodes.Callvirt.ToInstruction(roCollectionGetCount));
			body.Instructions.Add(OpCodes.Box.ToInstruction(mod.CorLibTypes.Int32));
			body.Instructions.Add(OpCodes.Call.ToInstruction(writeLine2));

			// return 0;
			body.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
			body.Instructions.Add(OpCodes.Ret.ToInstruction());

			entryPoint.Body = body;

			// Save the assembly
			mod.Write(newFileName);
		}
	}
}
