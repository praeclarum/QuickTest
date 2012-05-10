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
		[TestMethod]
		public void Add ()
		{
			var e = Expression.Parse ("3 + 2");
			var env = new EvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (5, v);
		}

		[TestMethod]
		public void DividePromote ()
		{
			var e = Expression.Parse ("3.0 / 2");
			var env = new EvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (1.5, v);
		}

		[TestMethod]
		public void AddStrings ()
		{
			var e = Expression.Parse ("\"3\" + \"2\"");
			var env = new EvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual ("32", v);
		}

		[TestMethod]
		public void Subtract ()
		{
			var e = Expression.Parse ("3 - 2");
			var env = new EvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (1, v);
		}

		[TestMethod]
		public void Multiply ()
		{
			var e = Expression.Parse ("3 * 2");
			var env = new EvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (6, v);
		}

		[TestMethod]
		public void Divide ()
		{
			var e = Expression.Parse ("3 / 2");
			var env = new EvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (1, v);
		}

		[TestMethod]
		public void UnaryMinus ()
		{
			var e = Expression.Parse ("-3");
			var env = new EvalEnv ();
			var v = e.Eval (env);
			Assert.AreEqual (-3, v);
		}

		[TestMethod]
		public void ObjectLiteralMath ()
		{
			var e = (ObjectLiteralExpression)Expression.Parse ("{a:3-2}");
			var env = new EvalEnv ();
			var v = e.Assignments[0].Value.Eval (env);
			Assert.AreEqual (1, v);
		}
	}
}
