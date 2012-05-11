using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QuickTest.Tests
{
	[TestClass]
	public class RunTests
	{
		struct Location
		{
			public double Lat, Lon;
		}

		class Person
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public Location Location { get; private set; }

			public string FullName
			{
				get
				{
					return FirstName + " " + LastName;
				}
				set
				{
					var parts = value.Split (new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					FirstName = parts[0];
					LastName = parts.Length > 1 ? string.Join (" ", parts.Skip (1)) : "";
				}
			}

			public void LowerCase ()
			{
				FirstName = FirstName.ToLowerInvariant ();
				LastName = LastName.ToLowerInvariant ();
			}

			public void SetLocation (Location loc)
			{
				Location = loc;
			}

			public double Personality { get; set; }

			public double GetPersonality () { return Personality; }
		}


		[TestMethod]
		public void PropertyGetterExpected ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.FullName",
				ThisString = "{ FirstName: \"Frank\", LastName:\"Krueger\" }",
				TestType = TestType.PropertyGetter,
				ExpectedValueString = "Frank Krueger",
			};
			t.Run ();
			Assert.AreEqual (TestResult.Pass, t.Result, t.FailInfo);
		}

		[TestMethod]
		public void PropertyGetterBadExpected ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.FullName",
				ThisString = "{ FirstName: \"Frank\", LastName:\"Krueger\" }",
				TestType = TestType.PropertyGetter,
				ExpectedValueString = "Frank A. Krueger",
			};
			t.Run ();
			Assert.AreEqual (TestResult.Fail, t.Result);
			Assert.AreEqual ("Expected Value Fail", t.FailInfo);
		}

		[TestMethod]
		public void PropertyGetterAssert ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.FullName",
				ThisString = "{ FirstName: \"Frank\", LastName:\"Krueger\" }",
				AssertString = "FullName == \"Frank Krueger\"",
				TestType = TestType.PropertyGetter,
			};
			t.Run ();
			Assert.AreEqual (TestResult.Pass, t.Result, t.FailInfo);
		}

		[TestMethod]
		public void PropertyGetterBadAssert ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.FullName",
				ThisString = "{ FirstName: \"Frank\", LastName:\"Krueger\" }",
				AssertString = "FullName == \"Frank Krueger\" && FirstName == \"Foo\"",
				TestType = TestType.PropertyGetter,
			};
			t.Run ();
			Assert.AreEqual (TestResult.Fail, t.Result, t.FailInfo);
			Assert.AreEqual ("Assert Fail", t.FailInfo);
		}

		[TestMethod]
		public void PropertyGetterUnknown ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.FullName",
				ThisString = "{ FirstName: \"Frank\", LastName:\"Krueger\" }",
				TestType = TestType.PropertyGetter,
			};
			t.Run ();
			Assert.AreEqual (TestResult.Unknown, t.Result, t.FailInfo);
		}

		[TestMethod]
		public void PropertySetterAssert ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.FirstName",
				ThisString = "{ FirstName: \"Frank\", LastName:\"Krueger\" }",
				AssertString = "FullName == \"Alva Krueger\"",
				TestType = TestType.PropertySetter,
			};
			t.Arguments.Add (new TestArgument { Name = "value", ValueString = "\"Alva\"" });
			t.Run ();
			Assert.AreEqual (TestResult.Pass, t.Result, t.FailInfo);
		}

		[TestMethod]
		public void ProcedureAssert ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.LowerCase",
				ThisString = "{ FirstName: \"Frank\", LastName:\"Krueger\" }",
				AssertString = "FullName == \"frank krueger\"",
				TestType = TestType.Procedure,
			};
			t.Run ();
			Assert.AreEqual (TestResult.Pass, t.Result, t.FailInfo);
		}

		[TestMethod]
		public void ObjectLiteralArgs ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.SetLocation",
				ThisString = "{ FirstName: \"Frank\", LastName:\"Krueger\" }",
				AssertString = "Location.Lat == 1.0",
				TestType = TestType.Procedure,
			};
			t.Arguments.Add (new TestArgument { Name = "loc", ValueString = "{Lat=1,Lon=2}", ValueType = "QuickTest.Tests.RunTests+Location", });
			t.Run ();
			Assert.AreEqual (TestResult.Pass, t.Result, t.FailInfo);
		}

		[TestMethod]
		public void DollarSignInAssertForPropertyGetters ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.Personality",
				ThisString = "{ Personality:20 }",
				AssertString = "$ > 15 && $ < 30",
				TestType = TestType.PropertyGetter,
			};
			t.Run ();
			Assert.AreEqual (TestResult.Pass, t.Result, t.FailInfo);
		}

		[TestMethod]
		public void DollarSignInAssertForFunction ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.GetPersonality",
				ThisString = "{ Personality:20 }",
				AssertString = "$ > 15 && $ < 30",
				TestType = TestType.Function,
			};
			t.Run ();
			Assert.AreEqual (TestResult.Pass, t.Result, t.FailInfo);
		}

		[TestMethod]
		public void ThisInAssert ()
		{
			var t = new Test {
				Member = "QuickTest.Tests.RunTests+Person.Personality",
				ThisString = "{ Personality:20 }",
				AssertString = "this.Personality > 15 && this.Personality < 30",
				TestType = TestType.PropertyGetter,
			};
			t.Run ();
			Assert.AreEqual (TestResult.Pass, t.Result, t.FailInfo);
		}

		[TestMethod]
		public void FailInfoCodeRef ()
		{
			var t = new Test {
				FailInfo = @"System.NullReferenceException: Object reference not set to an instance of an object.
   at Circuit.CircuitPoint.SetLocation(CircuitPoint other) in C:\Projects\Circuit\CircuitLib\Point.cs:line 59",
			};

			var f = t.GetFailCodeReference ();

			Assert.AreEqual (@"C:\Projects\Circuit\CircuitLib\Point.cs", f.Path);
			Assert.AreEqual (59, f.Line);
		}
	}
}
