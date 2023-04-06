//
// tuner.cs: WebAssembly build time helpers
//
//
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Json;
using System.Collections.Generic;
using Mono.Cecil;
using System.Diagnostics;

public class WasmTuner
{
	public static int Main (String[] args) {
		return new WasmTuner ().Run (args);
	}

	void Usage () {
		Console.WriteLine ("Usage: tuner.exe <arguments>");
		Console.WriteLine ("Arguments:");
		Console.WriteLine ("--gen-icall-table icall-table.json <assemblies>.");
		Console.WriteLine ("--gen-pinvoke-table <list of native library names separated by commas> <assemblies>.");
		Console.WriteLine ("--gen-interp-to-native <output file name> <assemblies>.");
		Console.WriteLine ("--gen-empty-assemblies <filenames>.");
	}

	int Run (String[] args)
	{
		if (args.Length < 1) {
			Usage ();
			return 1;
		}
		string cmd = args [0];
		if (cmd == "--gen-icall-table") {
			if (args.Length < 3) {
				Usage ();
				return 1;
			}
			return GenIcallTable (args);
		} else if (cmd == "--gen-pinvoke-table") {
			return GenPinvokeTable (args);
		} else if (cmd == "--gen-empty-assemblies") {
			return GenEmptyAssemblies2 (args);
		} else {
			Usage ();
			return 1;
		}
	}

	public static string MapType (TypeReference t) {
		if (t.Name == "Void")
			return "void";
		else if (t.Name == "Double")
			return "double";
		else if (t.Name == "Single")
			return "float";
		else if (t.Name == "Int64")
			return "int64_t";
		else if (t.Name == "UInt64")
			return "uint64_t";
		else
			return "int";
	}

	int GenPinvokeTable (string[] args)
	{
		if (args[1].StartsWith("@"))
		{
			var rawContent = File.ReadAllText(args[1].Substring(1));

			var content = rawContent.Split(" ");

			args = new[] { args[0] }
				.Concat(content)
				.ToArray();
		}

		var outputFile = args[1];
		var icallTable = args[3];

		var modules = new Dictionary<string, string> ();
		foreach (var module in args [2].Split (','))
		{
			modules [module] = module;
		}

		var files = args.Skip(4).ToArray();

		var generator = new PInvokeTableGenerator();

		Console.WriteLine($"Generating to {outputFile}");
		var PInvokeOutputFile = outputFile;
		var pInvokeCookiesList = generator.Generate(modules.Keys.ToArray(), files.ToArray(), PInvokeOutputFile);

		var icallGenerator = new IcallTableGenerator();
		var iCallCookiesList = icallGenerator.Generate(icallTable, files, Path.GetTempFileName());

		var m2nInvoke = Path.Combine(Path.GetDirectoryName(PInvokeOutputFile), "wasm_m2n_invoke.g.h");
		Console.WriteLine($"Generating interp to native to {m2nInvoke}");
		var interpNativeGenerator = new InterpToNativeGenerator();
		interpNativeGenerator.Generate(pInvokeCookiesList.Concat(iCallCookiesList), m2nInvoke);

		return 0;
	}

	void Error (string msg) {
		Console.Error.WriteLine (msg);
		Environment.Exit (1);
	}


	//
	// Given the runtime generated icall table, and a set of assemblies, generate
	// a smaller linked icall table mapping tokens to C function names
	//
	int GenIcallTable(string[] args) {
		var icall_table_filename = args [2];
		var fileNames = args.Skip (3).ToArray ();

#if NETFRAMEWORK
		throw new NotSupportedException($"icall table generation is not supported for netstandard2.0");
#else
		Console.WriteLine($"Generating to {args[1]}");

		var generator = new IcallTableGenerator();
		generator.Generate(icall_table_filename, fileNames, args[2]);
#endif

		return 0;
	}

	// Generate empty assemblies for the filenames in ARGS if they don't exist
	int GenEmptyAssemblies2 (IEnumerable<string> args)
	{
		args = args.SelectMany(arg =>
		{
			// Expand a response file

			if (arg.StartsWith("@"))
			{
				var rawContent = File.ReadAllText(arg.Substring(1));
				var content = rawContent.Split(" ");

#if DEBUG
				Console.WriteLine($"Tuner Response content: {rawContent}");
#endif

				return content;
			}
			return new[] { arg };
		});

		foreach (var fname in args.Skip(1))
		{
			if (File.Exists (fname) || !Path.GetExtension(fname).Equals(".dll", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var basename = Path.GetFileName (fname).Replace (".exe", "").Replace (".dll", "");
			var assembly = AssemblyDefinition.CreateAssembly (new AssemblyNameDefinition (basename, new Version (0, 0, 0, 0)), basename, ModuleKind.Dll);
			assembly.Write (fname);

			File.WriteAllText(Path.ChangeExtension(fname, ".aot-only"), "");
			File.WriteAllText(Path.ChangeExtension(fname, ".pdb"), "");
		}
		return 0;
	}
}
