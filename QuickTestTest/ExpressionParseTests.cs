using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QuickTest.Tests
{
	[TestClass]
	public class ExpressionParseTests
	{
		[TestMethod]
		public void ObjectLiteralJavaScript ()
		{
			var expr = Expression.Parse ("{foo:42,bar:23}");
			Assert.IsInstanceOfType (expr, typeof (ObjectLiteralExpression));
			var e = (ObjectLiteralExpression)expr;

			Assert.AreEqual (2, e.Assignments.Count);
			Assert.AreEqual ("foo", e.Assignments[0].Name);
			Assert.AreEqual ("42", e.Assignments[0].Value.ToString ());
			Assert.AreEqual ("bar", e.Assignments[1].Name);
			Assert.AreEqual ("23", e.Assignments[1].Value.ToString ());
		}

		[TestMethod]
		public void ObjectLiteralJson ()
		{
			var expr = Expression.Parse ("{\"foo\":42,\"bar\":23}");
			Assert.IsInstanceOfType (expr, typeof (ObjectLiteralExpression));
			var e = (ObjectLiteralExpression)expr;

			Assert.AreEqual (2, e.Assignments.Count);
			Assert.AreEqual ("foo", e.Assignments[0].Name);
			Assert.AreEqual ("42", e.Assignments[0].Value.ToString ());
			Assert.AreEqual ("bar", e.Assignments[1].Name);
			Assert.AreEqual ("23", e.Assignments[1].Value.ToString ());
		}

		[TestMethod]
		public void ObjectLiteralCSharp ()
		{
			var expr = Expression.Parse ("{foo=42,bar=23}");
			Assert.IsInstanceOfType (expr, typeof (ObjectLiteralExpression));
			var e = (ObjectLiteralExpression)expr;

			Assert.AreEqual (2, e.Assignments.Count);
			Assert.AreEqual ("foo", e.Assignments[0].Name);
			Assert.AreEqual ("42", e.Assignments[0].Value.ToString ());
			Assert.AreEqual ("bar", e.Assignments[1].Name);
			Assert.AreEqual ("23", e.Assignments[1].Value.ToString ());
		}

		[TestMethod]
		public void ObjectLiteralCSharpStrings ()
		{
			var expr = Expression.Parse ("{foo=\"foov\",bar=\"barv\"}");
			Assert.IsInstanceOfType (expr, typeof (ObjectLiteralExpression));
			var e = (ObjectLiteralExpression)expr;

			Assert.AreEqual (2, e.Assignments.Count);
			Assert.AreEqual ("foo", e.Assignments[0].Name);
			Assert.AreEqual ("foov", e.Assignments[0].Value.ToString ());
			Assert.AreEqual ("bar", e.Assignments[1].Name);
			Assert.AreEqual ("barv", e.Assignments[1].Value.ToString ());
		}

		[TestMethod]
		public void Or ()
		{
			var expr = Expression.Parse ("1 || 2 || 3");
			Assert.IsInstanceOfType (expr, typeof (BinOpExpression));
			var e = (BinOpExpression)expr;
			Assert.AreEqual (TokenType.LogicalOr, e.Operator);
			Assert.IsInstanceOfType (e.Left, typeof (BinOpExpression));
			Assert.AreEqual (TokenType.LogicalOr, ((BinOpExpression)e.Left).Operator);
			Assert.IsInstanceOfType (e.Right, typeof (ConstantExpression));
		}

		[TestMethod]
		public void And ()
		{
			var expr = Expression.Parse ("1 && 2 && 3");
			Assert.IsInstanceOfType (expr, typeof (BinOpExpression));
			var e = (BinOpExpression)expr;
			Assert.AreEqual (TokenType.LogicalAnd, e.Operator);
			Assert.IsInstanceOfType (e.Left, typeof (BinOpExpression));
			Assert.AreEqual (TokenType.LogicalAnd, ((BinOpExpression)e.Left).Operator);
			Assert.IsInstanceOfType (e.Right, typeof (ConstantExpression));
		}

		[TestMethod]
		public void OrAnd ()
		{
			var expr = Expression.Parse ("1 || 2 && 3");
			Assert.IsInstanceOfType (expr, typeof (BinOpExpression));
			var e = (BinOpExpression)expr;
			Assert.AreEqual (TokenType.LogicalOr, e.Operator);
			Assert.IsInstanceOfType (e.Left, typeof (ConstantExpression));
			Assert.IsInstanceOfType (e.Right, typeof (BinOpExpression));
			Assert.AreEqual (TokenType.LogicalAnd, ((BinOpExpression)e.Right).Operator);
		}

		[TestMethod]
		public void Parens ()
		{
			var expr = Expression.Parse ("(1 || 2) && 3");
			Assert.IsInstanceOfType (expr, typeof (BinOpExpression));
			var e = (BinOpExpression)expr;
			Assert.AreEqual (TokenType.LogicalAnd, e.Operator);
			Assert.IsInstanceOfType (e.Left, typeof (BinOpExpression));
			Assert.AreEqual (TokenType.LogicalOr, ((BinOpExpression)e.Left).Operator);
			Assert.IsInstanceOfType (e.Right, typeof (ConstantExpression));
		}

		[TestMethod]
		public void Variable ()
		{
			var expr = Expression.Parse ("foo");
			Assert.IsInstanceOfType (expr, typeof (VariableExpression));
			var e = (VariableExpression)expr;
			Assert.AreEqual ("foo", e.Name);
		}

		[TestMethod]
		public void True ()
		{
			var expr = Expression.Parse ("true");
			Assert.IsInstanceOfType (expr, typeof (ConstantExpression));
			var e = (ConstantExpression)expr;
			Assert.IsTrue ((bool)e.Value);
		}

		[TestMethod]
		public void False ()
		{
			var expr = Expression.Parse ("false");
			Assert.IsInstanceOfType (expr, typeof (ConstantExpression));
			var e = (ConstantExpression)expr;
			Assert.IsFalse ((bool)e.Value);
		}

		[TestMethod]
		public void StringConstant ()
		{
			var expr = Expression.Parse ("\"foo\"");
			Assert.IsInstanceOfType (expr, typeof (ConstantExpression));
			var e = (ConstantExpression)expr;
			Assert.AreEqual ("foo", e.Value);
		}

		[TestMethod]
		public void LessThan ()
		{
			var expr = Expression.Parse ("foo < bar");
			Assert.IsInstanceOfType (expr, typeof (BinOpExpression));
			var e = (BinOpExpression)expr;
			Assert.AreEqual (TokenType.LessThan, e.Operator);
			Assert.IsInstanceOfType (e.Left, typeof (VariableExpression));
			Assert.IsInstanceOfType (e.Right, typeof (VariableExpression));
		}

		[TestMethod]
		public void Equal ()
		{
			var expr = Expression.Parse ("foo == bar");
			Assert.IsInstanceOfType (expr, typeof (BinOpExpression));
			var e = (BinOpExpression)expr;
			Assert.AreEqual (TokenType.Equal, e.Operator);
			Assert.IsInstanceOfType (e.Left, typeof (VariableExpression));
			Assert.IsInstanceOfType (e.Right, typeof (VariableExpression));
		}

		[TestMethod]
		public void NotEqual ()
		{
			var expr = Expression.Parse ("foo != bar");
			Assert.IsInstanceOfType (expr, typeof (BinOpExpression));
			var e = (BinOpExpression)expr;
			Assert.AreEqual (TokenType.NotEqual, e.Operator);
			Assert.IsInstanceOfType (e.Left, typeof (VariableExpression));
			Assert.IsInstanceOfType (e.Right, typeof (VariableExpression));
		}

		[TestMethod]
		public void MemberAccess ()
		{
			var expr = Expression.Parse ("(2||3).ToString.Invoke");
			Assert.IsInstanceOfType (expr, typeof (MemberExpression));
			var e = (MemberExpression)expr;
			Assert.AreEqual ("Invoke", e.Name);
			Assert.IsInstanceOfType (e.Object, typeof (MemberExpression));
			var e1 = (MemberExpression)e.Object;
			Assert.AreEqual ("ToString", e1.Name);
			Assert.IsInstanceOfType (e1.Object, typeof (BinOpExpression));
		}

		[TestMethod]
		public void SubtractAdd ()
		{
			var expr = Expression.Parse ("1 - 2 + 3");
			Assert.IsInstanceOfType (expr, typeof (BinOpExpression));
			var e = (BinOpExpression)expr;
			Assert.AreEqual (TokenType.Add, e.Operator);			
			Assert.IsInstanceOfType (e.Left, typeof (BinOpExpression));
			Assert.AreEqual (TokenType.Subtract, ((BinOpExpression)e.Left).Operator);
			Assert.IsInstanceOfType (e.Right, typeof (ConstantExpression));
		}

		[TestMethod]
		public void AddMultiply ()
		{
			var expr = Expression.Parse ("1 + 2 * 3");
			Assert.IsInstanceOfType (expr, typeof (BinOpExpression));
			var e = (BinOpExpression)expr;
			Assert.AreEqual (TokenType.Add, e.Operator);			
			Assert.IsInstanceOfType (e.Left, typeof (ConstantExpression));
			Assert.IsInstanceOfType (e.Right, typeof (BinOpExpression));
			Assert.AreEqual (TokenType.Multiply, ((BinOpExpression)e.Right).Operator);
		}
	}
}
