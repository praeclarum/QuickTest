using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace QuickTest.Tests
{
	[TestClass]
	public class TokenizeTests
	{
		[TestMethod]
		public void TokenizeDoubleQuoteString ()
		{
			var toks = Token.Tokenize ("\"Hello \\\"world\\\"\"").ToList();
			Assert.AreEqual (1, toks.Count);
			Assert.AreEqual ("Hello \"world\"", toks[0].ToString ());
		}

		[TestMethod]
		public void IgnoreWhiteSpace ()
		{
			var toks = Token.Tokenize ("\t  \r\n :\t\r , ").ToList ();
			Assert.AreEqual (2, toks.Count);
			Assert.AreEqual (":", toks[0].ToString ());
			Assert.AreEqual (",", toks[1].ToString ());
		}

		[TestMethod]
		public void DotNumbers ()
		{
			var toks = Token.Tokenize (".1").ToList ();
			Assert.AreEqual (1, toks.Count);
			Assert.AreEqual (TokenType.Number, toks[0].Type);
			Assert.AreEqual (".1", toks[0].ToString ());
		}

		[TestMethod]
		public void Identifier ()
		{
			var toks = Token.Tokenize ("__HelloWorld069").ToList ();
			Assert.AreEqual (1, toks.Count);
			Assert.AreEqual (TokenType.Identifier, toks[0].Type);
			Assert.AreEqual ("__HelloWorld069", toks[0].ToString ());
		}

		[TestMethod]
		public void IdentifierDollar ()
		{
			var toks = Token.Tokenize ("$").ToList ();
			Assert.AreEqual (1, toks.Count);
			Assert.AreEqual (TokenType.Identifier, toks[0].Type);
			Assert.AreEqual ("$", toks[0].ToString ());
		}

		[TestMethod]
		public void ObjectLiteral ()
		{
			var toks = Token.Tokenize ("{foo:42,bar:23}").ToList ();
			Assert.AreEqual (9, toks.Count);
		}

		[TestMethod]
		public void New ()
		{
			var toks = Token.Tokenize ("new").ToList ();
			Assert.AreEqual (1, toks.Count);
			Assert.AreEqual (TokenType.New, toks[0].Type);
		}
	}
}
