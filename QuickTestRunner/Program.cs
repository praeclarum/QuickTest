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

		public static void Main (string[] args)
		{
			TestPlan plan = TestPlan.Open (args[0]);
			new Program {
				_plan = plan,
			}.Run ();
		}

		void Run ()
		{
			_plan.Run ();
			_plan.Save (Console.Out);
		}
	}
}
