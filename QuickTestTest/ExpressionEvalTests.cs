using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QuickTest.Tests
{
	[TestClass]
	public class ExpressionEvalTests
	{
		class TestObject
		{
			public string Name { get; set; }
			public int Age { get; set; }

			public TestObject (string name, int age)
			{
			}

			public TestObject (string name)
				: this (name, 0)
			{
			}

			public TestObject ()
				: this ("", 0)
			{
			}
		}

		[TestMethod]
		public void NotEqual ()
		{
			var e = Expression.Parse ("3 != 2");
			var env = new ObjectEvalEnv ();
			var v = e.Eval (env);
			Assert.IsTrue ((bool)v);
		}

		[TestMethod]
		public void Add ()
		{
			var e = Expression.Parse ("3 + 2");
			var env = new ObjectEvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (5, v);
		}

		[TestMethod]
		public void DividePromote ()
		{
			var e = Expression.Parse ("3.0 / 2");
			var env = new ObjectEvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (1.5, v);
		}

		[TestMethod]
		public void AddStrings ()
		{
			var e = Expression.Parse ("\"3\" + \"2\"");
			var env = new ObjectEvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual ("32", v);
		}

		[TestMethod]
		public void Subtract ()
		{
			var e = Expression.Parse ("3 - 2");
			var env = new ObjectEvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (1, v);
		}

		[TestMethod]
		public void Multiply ()
		{
			var e = Expression.Parse ("3 * 2");
			var env = new ObjectEvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (6, v);
		}

		[TestMethod]
		public void Divide ()
		{
			var e = Expression.Parse ("3 / 2");
			var env = new ObjectEvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (1, v);
		}

		[TestMethod]
		public void UnaryMinus ()
		{
			var e = Expression.Parse ("-3");
			var env = new ObjectEvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (-3, v);
		}

		[TestMethod]
		public void ObjectLiteralMath ()
		{
			var e = (ObjectLiteralExpression)Expression.Parse ("{a:3-2}");
			var env = new ObjectEvalEnv ();
			var v = e.Assignments[0].Value.Eval (env);
			Assert.AreEqual (1, v);
		}

		[TestMethod]
		public void ConstructTypeFullName ()
		{
			var e = Expression.Parse ("new System.DateTime (2001, 9, 11)");
			var env = new LocalsEvalEnv ();
			var v = e.Eval (env);

			Assert.IsInstanceOfType (v, typeof(DateTime));
			var o = (DateTime)v;

			Assert.AreEqual (9, o.Month);
			Assert.AreEqual (11, o.Day);
			Assert.AreEqual (2001, o.Year);
		}

		[TestMethod]
		public void ConstructTypeName ()
		{
			var e = Expression.Parse ("new DateTime (2001, 9, 11)");
			var env = new LocalsEvalEnv ();
			var v = e.Eval (env);

			Assert.IsInstanceOfType (v, typeof (DateTime));
			var o = (DateTime)v;

			Assert.AreEqual (9, o.Month);
			Assert.AreEqual (11, o.Day);
			Assert.AreEqual (2001, o.Year);
		}

		[TestMethod]
		public void ConstructTypeNoName ()
		{
			var e = Expression.Parse ("new (2001, 9, 11)");
			var env = new ObjectEvalEnv (null, typeof (DateTime));
			var v = e.Eval (env);

			Assert.IsInstanceOfType (v, typeof (DateTime));
			var o = (DateTime)v;

			Assert.AreEqual (9, o.Month);
			Assert.AreEqual (11, o.Day);
			Assert.AreEqual (2001, o.Year);
		}
	}
}
