using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QuickTest.Tests
{
	[TestClass]
	public class StringMatrixTests
	{
		[TestMethod]
		public void CrazyRead ()
		{
			var m = StringMatrix.FromTsv ("\"123\"\t100\r\n1,2,3\t45,45'\r\n\"\"\"69,\t69\"\"\"\t\"a\t\r\nb\"\r\n");

			Assert.AreEqual (3, m.Rows.Count);

			Assert.AreEqual (2, m.Rows[0].Count);
			Assert.AreEqual ("\"123\"", m.Rows[0][0]);
			Assert.AreEqual ("100", m.Rows[0][1]);

			Assert.AreEqual (2, m.Rows[1].Count);
			Assert.AreEqual ("1,2,3", m.Rows[1][0]);
			Assert.AreEqual ("45,45'", m.Rows[1][1]);

			Assert.AreEqual (2, m.Rows[2].Count);
			Assert.AreEqual ("\"69,\t69\"", m.Rows[2][0]);
			Assert.AreEqual ("a\t\r\nb", m.Rows[2][1]);
		}
		
		[TestMethod]
		public void CrazyWrite ()
		{
			var s = "\"123\"\t100\r\n1,2,3\t45,45'\r\n\"\"\"69,\t69\"\"\"\t\"a\t\r\nb\"\r\n";
			var m = StringMatrix.FromTsv (s);
			var t = m.Tsv;
			Assert.AreEqual (s, t);
		}
	}
}
