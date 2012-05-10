using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Globalization;

namespace QuickTest.Runner
{
	public class Program
	{
		TestPlan _plan;
		Assembly _asm;

		public static void Main (string[] args)
		{
			TestPlan plan = TestPlan.Open (args[0]);
			new Program {
				_plan = plan,
			}.Run ();
		}

		void Run ()
		{
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
			_asm = Assembly.LoadFrom (_plan.AssemblyPath);

			foreach (var t in _plan.Tests) {
				t.Run ();
			}

			_plan.Save (Console.Out);
		}

		List<IGrouping<string,string>> _referenceAssemblies;

		/// <summary>
		/// http://blogs.msdn.com/b/msbuild/archive/2007/04/12/new-reference-assemblies-location.aspx
		/// </summary>
		void AddReferenceAssemblies ()
		{
			var dir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles), "Reference Assemblies");
			var dlls = Directory.EnumerateFiles (dir, "*.dll", SearchOption.AllDirectories);
			_referenceAssemblies = dlls.GroupBy (x => Path.GetFileNameWithoutExtension (x)).ToList ();
		}

		System.Reflection.Assembly CurrentDomain_AssemblyResolve (object sender, ResolveEventArgs args)
		{
			if (_referenceAssemblies == null) {
				AddReferenceAssemblies ();
			}

			var name = args.Name;

			foreach (var ra in _referenceAssemblies) {
				if (args.Name.StartsWith (ra.Key)) {
					foreach (var raa in ra) {
						return Assembly.LoadFrom (raa);
					}
				}
			}

			return null;
		}
	}
}
