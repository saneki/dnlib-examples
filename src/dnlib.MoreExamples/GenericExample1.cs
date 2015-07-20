using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

/*

This example shows how to create an assembly from scratch with a Main
method that constructs a List<String> and uses a few of its methods.


dnSpy output of created file:

using System;
using System.Collections.Generic;

namespace My.Namespace
{
	internal class Startup
	{
		private static int Main(string[] args)
		{
			Console.WriteLine("Count: {0}", new List<string>
			{
				"Item 1"
			}.Count);
			return 0;
		}
	}
}

peverify output:

	C:\> peverify list-test.exe /IL /MD

	Microsoft (R) .NET Framework PE Verifier.  Version  4.0.30319.33440
	Copyright (c) Microsoft Corporation.  All rights reserved.

	All Classes and Methods in list-test.exe Verified.


Output of program:

	C:\>list-test.exe
	Count: 1

*/

namespace dnlib.MoreExamples
{
	public class GenericExample1
	{
		public static void Run()
		{
			// This is the file that will be created
			string newFileName = @"list-test.exe";

			// Create the module
			var mod = new ModuleDefUser("list-test", Guid.NewGuid(),
				new AssemblyRefUser(new AssemblyNameInfo(typeof(int).Assembly.GetName().FullName)));
			// It's a console app
			mod.Kind = ModuleKind.Console;
			// Create the assembly and add the created module to it
			new AssemblyDefUser("list-test", new Version(1, 2, 3, 4)).Modules.Add(mod);

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

			//
			// Method 1: Create List<String> inst signature by importing (easy way)
			// --------------------------------------------------------------------
			//Importer importer = new Importer(mod);
			//var listGenericInstSig = importer.ImportAsTypeSig(typeof(System.Collections.Generic.List<String>));

			//
			// Method 2: Create List<String> inst signature manually (harder way)
			// ------------------------------------------------------------------
			var assemblyRef = mod.CorLibTypes.AssemblyRef;
			var listRef = new TypeRefUser(mod, @"System.Collections.Generic", "List`1", assemblyRef);
			// Create the GenericInstSig from a ClassSig with <String> generic arg
			var listGenericInstSig = new GenericInstSig(new ClassSig(listRef), mod.CorLibTypes.String);

			// Create TypeSpec from GenericInstSig
			var listTypeSpec = new TypeSpecUser(listGenericInstSig);
			// Create System.Collections.Generic.List<String>::.ctor method reference
			var listCtor = new MemberRefUser(mod, ".ctor", MethodSig.CreateInstance(mod.CorLibTypes.Void),
				listTypeSpec);

			// Create Add(!0) method reference, !0 signifying first generic argument of declaring type
			// In this case, would be Add(String item)
			// (GenericMVar would be used for method generic argument, such as Add<!!0>(!!0))
			var listAdd = new MemberRefUser(mod, "Add",
				MethodSig.CreateInstance(mod.CorLibTypes.Void, new GenericVar(0)),
				listTypeSpec);

			var listGetCount = new MemberRefUser(mod, "get_Count",
				MethodSig.CreateInstance(mod.CorLibTypes.Int32),
				listTypeSpec);

			IList<Local> locals = new List<Local>();
			locals.Add(new Local(listGenericInstSig)); // local[0]: class [mscorlib]System.Collections.Generic.List`1<string>

			var body = new CilBody(true, new List<Instruction>(), new List<ExceptionHandler>(), locals);
			// Call the list .ctor
			body.Instructions.Add(OpCodes.Newobj.ToInstruction(listCtor));
			body.Instructions.Add(OpCodes.Stloc_0.ToInstruction()); // Store list to local[0]

			// list.Add("Item 1")
			body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction());
			body.Instructions.Add(OpCodes.Ldstr.ToInstruction("Item 1"));
			body.Instructions.Add(OpCodes.Callvirt.ToInstruction(listAdd));

			// WriteLine("Array: {0}", list.ToArray());
			//body.Instructions.Add(OpCodes.Ldstr.ToInstruction("Array: {0}"));
			//body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction()); // Load list from local[0]
			//body.Instructions.Add(OpCodes.Callvirt.ToInstruction(listToArray));
			//body.Instructions.Add(OpCodes.Call.ToInstruction(writeLine2));

			// WriteLine("Count: {0}", list.Count)
			body.Instructions.Add(OpCodes.Ldstr.ToInstruction("Count: {0}"));
			body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction()); // Load list from local[0]
			body.Instructions.Add(OpCodes.Callvirt.ToInstruction(listGetCount));
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
