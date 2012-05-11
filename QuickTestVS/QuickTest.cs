using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;
using System.Reflection;

namespace QuickTest
{
	[Serializable]
	public class TestRepo
	{
		public const string FileExtension = ".quicktest";

		public List<MemberTests> MemberTests { get; set; }

		public MemberTests GetMemberTests (string member)
		{
			if (string.IsNullOrEmpty (member)) throw new ArgumentNullException ("member");

			var tests = MemberTests.FirstOrDefault (x => x.Member == member);

			if (tests == null) {
				tests = new MemberTests {
					Member = member,
				};
				MemberTests.Add (tests);
			}

			return tests;
		}

		public TestRepo ()
		{
			MemberTests = new List<MemberTests> ();
		}

		public void Save (string path)
		{
			var x = new XmlSerializer (GetType ());
			using (var f = File.Create (path)) {
				x.Serialize (f, this);
			}
		}

		public static TestRepo Open (string path)
		{
			var x = new XmlSerializer (typeof (TestRepo));
			using (var f = File.OpenRead (path)) {
				return (TestRepo)x.Deserialize (f);
			}
		}
	}

	[Serializable]
	public class MemberTests
	{
		public string Member { get; set; }
		public List<Test> Tests { get; set; }

		public MemberTests ()
		{
			Tests = new List<Test> ();
		}

		public Test GetTest (Guid id)
		{
			return Tests.First (x => x.Id == id);
		}
	}

	[Serializable]
	public class TestPlan
	{
		public string AssemblyPath { get; set; }
		public List<Test> Tests { get; set; }

		public TestPlan ()
		{
			AssemblyPath = "";
			Tests = new List<Test> ();
		}

		public void Save (string path)
		{
			var x = new XmlSerializer (GetType());
			using (var f = File.Create (path)) {
				x.Serialize (f, this);
			}
		}

		public void Save (TextWriter writer)
		{
			var x = new XmlSerializer (GetType ());
			x.Serialize (writer, this);
		}

		public static TestPlan Open (string path)
		{
			var x = new XmlSerializer (typeof (TestPlan));
			using (var f = File.OpenRead (path)) {
				return (TestPlan)x.Deserialize (f);
			}
		}

		public static TestPlan Open (TextReader reader)
		{
			var x = new XmlSerializer (typeof (TestPlan));
			return (TestPlan)x.Deserialize (reader);
		}

		public void Run ()
		{
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler (CurrentDomain_AssemblyResolve);
			var asm = Assembly.LoadFrom (AssemblyPath);

			foreach (var a in asm.GetReferencedAssemblies ()) {
				try {
					Assembly.Load (a);
				}
				catch (Exception) {
				}			
			}

			foreach (var t in Tests) {
				t.Run ();
			}
		}

		List<IGrouping<string, string>> _referenceAssemblies;

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

	[Serializable]
	public class TestArgument
	{
		public string Name { get; set; }
		public string ValueString { get; set; }
		public string ValueType { get; set; }		

		public TestArgument ()
		{
			Name = "";
		}

		[XmlIgnore]
		public object Value
		{
			get
			{
				return Test.ReadValue (ValueString, ValueType);
			}
			set
			{
				Test.WriteValue (value, s => { ValueString = s; }, t => { ValueType = t; });
			}
		}

		public override string ToString ()
		{
			return ValueType + " " + Name + " = " + ValueString;
		}
	}

	public enum TestType
	{
		Function,
		Procedure,
		PropertyGetter,
		PropertySetter,
	}

	[Serializable]
	public class Test
	{
		public Guid Id { get; set; }
		public string Member { get; set; }
		public TestType TestType { get; set; }


		public string ThisString { get; set; }
		public List<TestArgument> Arguments { get; set; }
		public string ExpectedValueString { get; set; }
		public string AssertString { get; set; }


		public TestResult Result { get; set; }
		public DateTime ResultTimeUtc { get; set; }
		public string ValueString { get; set; }
		public string ValueType { get; set; }
		public string FailInfo { get; set; }


		public TestArgument GetArgument (string name)
		{
			var a = Arguments.FirstOrDefault (x => x.Name == name);
			if (a == null) {
				a = new TestArgument {
					Name = name,
				};
				Arguments.Add (a);
			}
			return a;
		}
		

		public static object ReadValue (string valueString, string type)
		{
			if (string.IsNullOrEmpty (type) || string.IsNullOrEmpty (type)) return null;
				foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
					var t = asm.GetType (type);
					if (t != null) {
						return Convert.ChangeType (valueString, t, CultureInfo.InvariantCulture);
					}
				}
				return null;
		}

		public static void WriteValue (object value, Action<string> writeValueString, Action<string> writeValueType)
		{
			if (value != null) {
				writeValueType (value.GetType ().FullName);
				writeValueString (Convert.ToString (value, CultureInfo.InvariantCulture));
			}
			else {
				writeValueType ("");
				writeValueString ("");
			}
		}

		[XmlIgnore]
		public object Value
		{
			get
			{
				return ReadValue (ValueString, ValueType);
			}
			set
			{
				WriteValue (value, s => { ValueString = s; }, t => { ValueType = t; });				
			}
		}

		public Test()
		{
			Member = "";
			Arguments = new List<TestArgument> ();
		}

		public void Run ()
		{
			var name = Member;
			var pi = name.IndexOf ('(');
			if (pi >= 0) {
				name = name.Substring (0, pi);
			}
			var parts = name.Trim ().Split ('.');
			var methodName = parts[parts.Length - 1];
			var typeName = string.Join (".", parts.Take (parts.Length - 1));

			Result = TestResult.Unknown;
			FailInfo = "";
			Value = null;

			try {
				//
				// Get the reflection info
				//
				var type = FindType (typeName);

				var members = type.GetMember (methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				if (members == null || members.Length == 0) {
					throw new Exception ("Member '" + methodName + "' not found in '" + type.FullName + "'");
				}

				//
				// Get the this object
				//
				object obj = null;
				if (!IsStatic (members [0])) {
					obj = CreateObject (type, ThisString, null);
				}

				//
				// Run the appropriate test
				//
				switch (TestType) {
				case QuickTest.TestType.Function:
					RunFunctionTest (obj, type, members);
					break;  
				case QuickTest.TestType.Procedure:
					RunProcedureTest (obj, type, members);
					break;
				case QuickTest.TestType.PropertyGetter:
					RunPropertyGetterTest (obj, type, members);
					break;
				case QuickTest.TestType.PropertySetter:
					RunPropertySetterTest (obj, type, members);
					break;
				default:
					throw new NotImplementedException (TestType.ToString ());					
				}
				
			}
			catch (TargetInvocationException tiex) {
				Result = TestResult.Fail;
				var ex = tiex.InnerException;
				FailInfo = ex.ToString ();
			}
			catch (Exception ex) {
				Result = TestResult.Fail;
				FailInfo = ex.GetType () + ": " + ex.Message;
			}

			ResultTimeUtc = DateTime.UtcNow;
		}

		static bool IsStatic (MemberInfo member)
		{
			if (member is PropertyInfo) {
				var propInfo = (PropertyInfo)member;
				return (propInfo.CanRead && propInfo.GetGetMethod ().IsStatic) ||
						(propInfo.CanWrite && propInfo.GetSetMethod ().IsStatic);
			}
			else if (member is MethodInfo) {
				var methInfo = (MethodInfo)member;
				return methInfo.IsStatic;
			}
			else {
				return false;
			}
		}

		static void SetValue (object obj, MemberInfo member, object value)
		{
			if (member is PropertyInfo) {
				var propInfo = (PropertyInfo)member;
				propInfo.SetValue (obj, value, null);
			}
			else if (member is FieldInfo) {
				var fieldInfo = (FieldInfo)member;
				fieldInfo.SetValue (obj, value);
			}
			else {
				throw new Exception ("Cannot assign values to '" + member.Name + "'");
			}
		}

		/*object Eval (object obj, Type objType, Expression expr)
		{
			var env = new EvalEnv (obj, objType);
			return expr.Eval (env);
		}*/

		object AssignObject (object obj, Type objType, ObjectLiteralExpression literal, EvalEnv env)
		{
			if (env == null) {
				env = new ObjectEvalEnv (obj, objType);
			}

			foreach (var a in literal.Assignments) {
				var members = objType.GetMember (a.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				if (members == null || members.Length == 0) {
					throw new Exception ("'" + a.Name + "' not found in '" + objType.FullName + "'");
				}
				var value = a.Value.Eval (env);
				SetValue (obj, members[0], value);
			}
			return obj;
		}

		static Type FindType (string typeFullName)
		{
			if (string.IsNullOrEmpty (typeFullName)) throw new ArgumentNullException ("typeFullName");

			var asms = AppDomain.CurrentDomain.GetAssemblies ();

			foreach (var a in asms) {
				var type = a.GetType (typeFullName);
				if (type != null) return type;
			}
			throw new Exception ("Type '" + typeFullName + "' not found");
		}

		object CreateObject (string objTypeName, ObjectLiteralExpression literal, EvalEnv env)
		{
			var type = FindType (objTypeName);
			var obj = Activator.CreateInstance (type);
			AssignObject (obj, type, literal, env);
			return obj;
		}

		object CreateObject (string objTypeName, string expressionText, EvalEnv env)
		{
			var objType = FindType (objTypeName);
			return CreateObject (objType, expressionText, env);
		}

		object CreateObject (Type objType, string expressionText, EvalEnv env)
		{
			if (string.IsNullOrEmpty (expressionText)) {
				return Activator.CreateInstance (objType);
			}
			else {
				var expr = Expression.Parse (expressionText);

				var literal = expr as ObjectLiteralExpression;
				if (literal != null) {
					var obj = Activator.CreateInstance (objType);
					AssignObject (obj, objType, literal, env);
					return obj;
				}
				else {
					return expr.Eval (new ObjectEvalEnv ());
				}
			}
		}

		void CheckExpectedValue ()
		{
			if (Result == TestResult.Fail) return;
			if (!string.IsNullOrEmpty (ExpectedValueString)) {
				if (ValueString != ExpectedValueString) {
					Result = TestResult.Fail;
					FailInfo = "Expected Value Fail";
				}
				else {
					Result = TestResult.Pass;
				}
			}
		}

		void CheckAsserts (object obj, Type objType)
		{
			if (Result == TestResult.Fail) return;

			if (string.IsNullOrWhiteSpace (AssertString)) return;

			var expr = Expression.Parse (AssertString);

			var oenv = new ObjectEvalEnv (obj, objType);
			var env = new LocalsEvalEnv (oenv);
			env["$"] = Value;

			var val = expr.Eval (env);

			if (val != null && val is bool && (bool)val) {
				Result = TestResult.Pass;
			}
			else {
				Result = TestResult.Fail;
				FailInfo = "Assert Fail";
			}
		}

		void InvokeMethod (object obj, Type objType, MemberInfo[] members)
		{
			var method = (MethodInfo)members[0];
			if (members.Length > 0) {
				foreach (var m in members.Cast<MethodInfo> ()) {
					var ps = m.GetParameters ();
					if (ps.Length != Arguments.Count) continue;
					var match = true;
					for (var i = 0; i < ps.Length && match; i++) {
						match = ps[i].ParameterType.FullName == Arguments[i].ValueType;
					}
					if (match) {
						method = m;
						break;
					}
				}
			}
			var args = EvalArguments (obj, objType);
			Value = method.Invoke (obj, args);
		}

		object[] EvalArguments (object obj, Type objType)
		{
			var env = new ObjectEvalEnv (obj, objType);
			var vals = new object[Arguments.Count];
			for (var i = 0; i < vals.Length; i++) {
				var a = Arguments[i];
				if (string.IsNullOrWhiteSpace (a.ValueString)) {
					vals[i] = CreateObject (a.ValueType, a.ValueString, env);
				}
				else {
					var e = Expression.Parse (a.ValueString);
					if (e is ObjectLiteralExpression) {
						vals[i] = CreateObject (a.ValueType, (ObjectLiteralExpression)e, env);
					}
					else {
						vals[i] = e.Eval (env);
					}
				}
			}
			return vals;
		}

		void RunFunctionTest (object obj, Type objType, MemberInfo[] members)
		{
			InvokeMethod (obj, objType, members);
			CheckExpectedValue ();
			CheckAsserts (obj, objType);
		}

		void RunProcedureTest (object obj, Type objType, MemberInfo[] members)
		{
			InvokeMethod (obj, objType, members);
			CheckAsserts (obj, objType);
		}

		void RunPropertyGetterTest (object obj, Type objType, MemberInfo[] member)
		{
			var property = (PropertyInfo)member[0];
			var getter = property.GetGetMethod (true);
			Value = getter.Invoke (obj, null);

			CheckExpectedValue ();
			CheckAsserts (obj, objType);
		}

		void RunPropertySetterTest (object obj, Type objType, MemberInfo[] member)
		{
			var property = (PropertyInfo)member[0];
			var setter = property.GetSetMethod (true);
			Value = setter.Invoke (obj, EvalArguments (obj, objType));
			CheckAsserts (obj, objType);
		}

		public void RecordResults (Test r)
		{
			Result = r.Result;
			ResultTimeUtc = r.ResultTimeUtc;
			ValueString = r.ValueString;
			ValueType = r.ValueType;
			FailInfo = r.FailInfo;
		}

		public CodeReference GetFailCodeReference ()
		{
			var lineI = FailInfo.IndexOf (":line ");
			
			if (lineI > 0) {
				var inI = FailInfo.Substring (0, lineI).LastIndexOf ("in ");
				if (inI >= 0) {
					var cr = new CodeReference ();
					cr.Path = FailInfo.Substring (inI + 3, lineI - inI - 3);
					var lineSI = lineI + 6;
					var p = lineSI;
					var end = FailInfo.Length;
					while (p < end && char.IsDigit (FailInfo[p])) p++;
					if (p > lineSI) {
						cr.Line = int.Parse (FailInfo.Substring (lineSI, p - lineSI));
					}
					return cr;
				}
			}

			return null;
		}
	}

	public class CodeReference
	{
		public string Path;
		public int Line;
	}

	public enum TestResult
	{
		Fail = 0,
		Pass = 1,
		Unknown = 2,
	}

	abstract class EvalEnv
	{
		EvalEnv _parent;
		public EvalEnv (EvalEnv parent = null)
		{
			_parent = parent;
		}
		public object Lookup (string name)
		{
			object value = null;
			if (TryGetValue (name, out value)) {
				return value;
			}
			else if (_parent != null) {
				return _parent.Lookup (name);
			}
			else {
				throw new Exception ("'" + name + "' not found");
			}
		}
		protected abstract bool TryGetValue (string name, out object value);
	}

	class LocalsEvalEnv : EvalEnv
	{
		public LocalsEvalEnv (EvalEnv parent)
			: base (parent)
		{
		}
		readonly Dictionary<string, object> _values = new Dictionary<string, object> ();
		public object this[string key]
		{
			get { return _values[key]; }
			set { _values[key] = value; }
		}
		protected override bool TryGetValue (string name, out object value)
		{
			return _values.TryGetValue (name, out value);
		}
	}

	class ObjectEvalEnv : EvalEnv
	{
		object _obj;
		Type _objType;

		public ObjectEvalEnv (EvalEnv parent = null)
			: base (parent)
		{
			_obj = null;
			_objType = typeof (object);
		}

		public ObjectEvalEnv (object obj, Type objType, EvalEnv parent = null)
			: base (parent)
		{
			_obj = obj;
			_objType = objType;
		}

		protected override bool TryGetValue (string name, out object value)
		{
			if (name == "this") {
				value = _obj;
				return true;
			}
			else {
				var members = _objType.GetMember (name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

				if (members == null || members.Length == 0) {
					value = null;
					return false;
				}

				var member = members[0];

				var prop = member as PropertyInfo;
				if (prop != null) {
					value = prop.GetValue (_obj, null);
					return true;
				}
				else {
					var field = member as FieldInfo;
					if (field != null) {
						value = field.GetValue (_obj);
						return true;
					}
					else {
						value = null;
						return false;
					}
				}
			}
		}
	}

	abstract class Expression
	{
		public abstract object Eval (EvalEnv env);

		public static Expression Parse (string src)
		{
			var toks = Token.Tokenize (src).ToArray ();
			var p = 0;
			return Parse (toks, ref p);
		}

		static Expression Parse (Token[] toks, ref int p)
		{
			return ParseConditionalOr (toks, ref p);
		}

		static Expression ParseConditionalOr (Token[] toks, ref int p)
		{
			var end = toks.Length;
			var e = ParseConditionalAnd (toks, ref p);
			while (p < end && toks[p].Type == TokenType.LogicalOr) {
				p++;
				var o = ParseConditionalAnd (toks, ref p);
				e = new BinOpExpression (TokenType.LogicalOr, e, o);
			}
			return e;
		}

		static Expression ParseConditionalAnd (Token[] toks, ref int p)
		{
			var end = toks.Length;
			var e = ParseEquality (toks, ref p);
			while (p < end && toks[p].Type == TokenType.LogicalAnd) {
				p++;
				var o = ParseEquality (toks, ref p);
				e = new BinOpExpression (TokenType.LogicalAnd, e, o);
			}
			return e;
		}

		static Expression ParseEquality (Token[] toks, ref int p)
		{
			var end = toks.Length;
			var e = ParseRelational (toks, ref p);
			if (p < end && (toks[p].Type == TokenType.Equal || toks[p].Type == TokenType.NotEqual)) {
				var op = toks[p].Type;
				p++;
				var o = ParseRelational (toks, ref p);
				e = new BinOpExpression (op, e, o);
			}
			return e;
		}

		static Expression ParseRelational (Token[] toks, ref int p)
		{
			var end = toks.Length;
			var e = ParseAdditive (toks, ref p);
			if (p < end && (toks[p].Type == TokenType.LessThan || toks[p].Type == TokenType.LessThanOrEqual ||
				toks[p].Type == TokenType.GreaterThan || toks[p].Type == TokenType.GreaterThanOrEqual)) {
				var op = toks[p].Type;
				p++;
				var o = ParseAdditive (toks, ref p);
				e = new BinOpExpression (op, e, o);
			}
			return e;
		}

		static Expression ParseAdditive (Token[] toks, ref int p)
		{
			var end = toks.Length;
			var e = ParseMultiplicative (toks, ref p);
			while (p < end && (toks[p].Type == TokenType.Add || toks[p].Type == TokenType.Subtract)) {
				var op = toks[p].Type;
				p++;
				var o = ParseMultiplicative (toks, ref p);
				e = new BinOpExpression (op, e, o);
			}
			return e;
		}

		static Expression ParseMultiplicative (Token[] toks, ref int p)
		{
			var end = toks.Length;
			var e = ParseUnary (toks, ref p);
			while (p < end && (toks[p].Type == TokenType.Multiply || toks[p].Type == TokenType.Divide)) {
				var op = toks[p].Type;
				p++;
				var o = ParseUnary (toks, ref p);
				e = new BinOpExpression (op, e, o);
			}
			return e;
		}

		static Expression ParseUnary (Token[] toks, ref int p)
		{
			var end = toks.Length;

			var t = toks[p];
			if (t.Type == TokenType.Subtract) {
				var op = t.Type;
				p++;
				var e = ParsePrimary (toks, ref p);
				return new UnOpExpression (op, e);
			}
			else {
				return ParsePrimary (toks, ref p);
			}
		}

		static Expression ParsePrimary (Token[] toks, ref int p)
		{
			var end = toks.Length;

			Expression e = null;

			while (p < end) {

				var t = toks[p];

				if (t.Type == TokenType.Identifier) {
					p++;
					var ident = t.ToString ();
					if (ident == "true") {
						e = new ConstantExpression (true);
					}
					else if (ident == "false") {
						e = new ConstantExpression (false);
					}
					else if (ident == "null") {
						e = new ConstantExpression (null);
					}
					else {
						e = new VariableExpression (t.ToString ());
					}
				}
				else if (t.Type == TokenType.String) {
					p++;
					e = new ConstantExpression (t.ToString ());
				}
				else if (t.Type == TokenType.Number) {
					p++;
					var intVal = 0;
					if (int.TryParse (t.ToString (), out intVal)) {
						e = new ConstantExpression (intVal);
					}
					else {
						var doubleVal = 0.0;
						if (double.TryParse (t.ToString (), out doubleVal)) {
							e = new ConstantExpression (doubleVal);
						}
						else {
							throw new ParseException ("Cannot interpret number '" + t.ToString () + "'");
						}
					}
				}
				else if (t.Type == TokenType.LeftParen) {
					p++;
					e = Parse (toks, ref p);
					if (p < end && toks[p].Type == TokenType.RightParen) {
						p++;
					}
					else {
						throw new ParseException ("Expected closing ')'");
					}
				}
				else if (t.Type == TokenType.LeftCurly) {
					e = ParseObjectLiteral (toks, ref p);
				}

				if (p < end && toks[p].Type == TokenType.Dot) {
					if (p + 1 < end && toks[p + 1].Type == TokenType.Identifier) {
						e = new MemberExpression (e, toks[p + 1].ToString ());
						p += 2;
					}
					else {
						break;
					}
				}
				else {
					break;
				}
			}

			return e;
		}

		static ObjectLiteralExpression ParseObjectLiteral (Token[] toks, ref int p)
		{
			var e = new ObjectLiteralExpression ();

			var end = toks.Length;

			p++; // Consume '{'

			while (p < end) {
				var t = toks[p];
				if (t.Type == TokenType.Identifier || t.Type == TokenType.String) {
					var ident = t.ToString ();
					p++;
					if (p < end && (toks[p].Type == TokenType.Colon || toks[p].Type == TokenType.Assign)) {
						p++;
						if (p < end) {
							var val = Parse (toks, ref p);
							e.Add (ident, val);
						}
					}
				}
				else if (t.Type == TokenType.RightCurly) {
					p++;
					break;
				}
				else {
					// Unexpected. Just keep reading until we get to a right curly
					p++;
				}
			}

			return e;
		}
	}

	class ParseException : Exception
	{
		public ParseException (string message)
			: base (message)
		{
		}
	}

	class UnOpExpression : Expression
	{
		public readonly TokenType Operator;
		public readonly Expression Value;

		public UnOpExpression (TokenType op, Expression value)
		{
			Operator = op;
			Value = value;
		}

		public override object Eval (EvalEnv env)
		{
			var a = Value.Eval (env);

			if (a is double) {
				if (Operator == TokenType.Subtract) return -(double)a;
			}
			else if (a is float) {
				if (Operator == TokenType.Subtract) return -(float)a;
			}
			else if (a is decimal) {
				if (Operator == TokenType.Subtract) return -(decimal)a;
			}
			else if (a is ulong) {
				if (Operator == TokenType.Subtract) throw new NotSupportedException ("Cannot negate UInt64");
			}
			else if (a is long) {
				if (Operator == TokenType.Subtract) return -(long)a;
			}
			else if (a is uint) {
				if (Operator == TokenType.Subtract) return -(uint)a;
			}
			else {
				if (Operator == TokenType.Subtract) return -Convert.ToInt32 (a);
			}

			throw new NotImplementedException (Operator.ToString ());
		}
	}

	class BinOpExpression : Expression
	{
		public readonly TokenType Operator;
		public readonly Expression Left;
		public readonly Expression Right;

		public BinOpExpression (TokenType op, Expression left, Expression right)
		{
			Operator = op;
			Left = left;
			Right = right;
		}

		public override object Eval (EvalEnv env)
		{
			var left = Left.Eval (env);

			switch (Operator) {
			case TokenType.LogicalAnd:
				return (bool)left && (bool)Right.Eval (env);
			case TokenType.LogicalOr:
				return (bool)left || (bool)Right.Eval (env);
			case TokenType.Equal:
				return left.Equals (Right.Eval (env));
			case TokenType.Add:
				return Add (left, Right.Eval (env));
			case TokenType.Subtract:
				return Subtract (left, Right.Eval (env));
			case TokenType.Multiply:
				return Multiply (left, Right.Eval (env));
			case TokenType.Divide:
				return Divide (left, Right.Eval (env));
			case TokenType.GreaterThan:
				return GreaterThan (left, Right.Eval (env));
			case TokenType.LessThan:
				return LessThan (left, Right.Eval (env));
			default:
				throw new NotImplementedException (Operator.ToString ());
			}
		}

		static object GreaterThan (object a, object b)
		{
			object pa, pb;
			Promote (a, b, out pa, out pb);
			if (a is double) return (double)pa > (double)pb;
			else if (a is float) return (float)pa > (float)pb;
			else if (a is decimal) return (decimal)pa > (decimal)pb;
			else if (a is ulong) return (ulong)pa > (ulong)pb;
			else if (a is long) return (long)pa > (long)pb;
			else if (a is uint) return (uint)pa > (uint)pb;
			else return (int)pa > (int)pb;
		}

		static object LessThan (object a, object b)
		{
			object pa, pb;
			Promote (a, b, out pa, out pb);
			if (a is double) return (double)pa < (double)pb;
			else if (a is float) return (float)pa < (float)pb;
			else if (a is decimal) return (decimal)pa < (decimal)pb;
			else if (a is ulong) return (ulong)pa < (ulong)pb;
			else if (a is long) return (long)pa < (long)pb;
			else if (a is uint) return (uint)pa < (uint)pb;
			else return (int)pa < (int)pb;
		}

		static object Add (object a, object b)
		{
			if (a is string || b is string) {
				return a + "" + b;
			}
			object pa, pb;
			Promote (a, b, out pa, out pb);
			if (a is double) return (double)pa + (double)pb;
			else if (a is float) return (float)pa + (float)pb;
			else if (a is decimal) return (decimal)pa + (decimal)pb;
			else if (a is ulong) return (ulong)pa + (ulong)pb;
			else if (a is long) return (long)pa + (long)pb;
			else if (a is uint) return (uint)pa + (uint)pb;
			else return (int)pa + (int)pb;
		}

		static object Subtract (object a, object b)
		{
			object pa, pb;
			Promote (a, b, out pa, out pb);
			if (a is double) return (double)pa - (double)pb;
			else if (a is float) return (float)pa - (float)pb;
			else if (a is decimal) return (decimal)pa - (decimal)pb;
			else if (a is ulong) return (ulong)pa - (ulong)pb;
			else if (a is long) return (long)pa - (long)pb;
			else if (a is uint) return (uint)pa - (uint)pb;
			else return (int)pa - (int)pb;
		}

		static object Multiply (object a, object b)
		{
			object pa, pb;
			Promote (a, b, out pa, out pb);
			if (a is double) return (double)pa * (double)pb;
			else if (a is float) return (float)pa * (float)pb;
			else if (a is decimal) return (decimal)pa * (decimal)pb;
			else if (a is ulong) return (ulong)pa * (ulong)pb;
			else if (a is long) return (long)pa * (long)pb;
			else if (a is uint) return (uint)pa * (uint)pb;
			else return (int)pa * (int)pb;
		}

		static object Divide (object a, object b)
		{
			object pa, pb;
			Promote (a, b, out pa, out pb);
			if (a is double) return (double)pa / (double)pb;
			else if (a is float) return (float)pa / (float)pb;
			else if (a is decimal) return (decimal)pa / (decimal)pb;
			else if (a is ulong) return (ulong)pa / (ulong)pb;
			else if (a is long) return (long)pa / (long)pb;
			else if (a is uint) return (uint)pa / (uint)pb;
			else return (int)pa / (int)pb;
		}

		static void Promote (object a, object b, out object pa, out object pb)
		{
			if (a is double || b is double) {
				pa = Convert.ToDouble (a);
				pb = Convert.ToDouble (b);
			}
			else if (a is float || b is float) {
				pa = Convert.ToSingle (a);
				pb = Convert.ToSingle (b);
			}
			else if (a is decimal || b is decimal) {
				pa = Convert.ToDecimal (a);
				pb = Convert.ToDecimal (b);
			}
			else if (a is ulong || b is ulong) {
				pa = Convert.ToUInt64 (a);
				pb = Convert.ToUInt64 (b);
			}
			else if (a is long || b is long) {
				pa = Convert.ToInt64 (a);
				pb = Convert.ToInt64 (b);
			}
			else if (a is uint || b is uint) {
				pa = Convert.ToUInt32 (a);
				pb = Convert.ToUInt32 (b);
			}
			else {
				pa = Convert.ToInt32 (a);
				pb = Convert.ToInt32 (b);
			}
		}
	}

	class ConstantExpression : Expression
	{
		public readonly object Value;
		public ConstantExpression (object val)
		{
			Value = val;
		}
		public override object Eval (EvalEnv env)
		{
			return Value;
		}
		public override string ToString ()
		{
			return Value.ToString ();
		}
	}

	class ObjectLiteralExpression : Expression
	{
		public readonly List<Assignment> Assignments = new List<Assignment> ();
		public class Assignment
		{
			public string Name;
			public Expression Value;
		}
		public override object Eval (EvalEnv env)
		{
			throw new NotImplementedException ();
		}
		public void Add (string ident, Expression val)
		{
			Assignments.Add (new Assignment { Name = ident, Value = val });
		}
	}

	class VariableExpression : Expression
	{
		public readonly string Name;
		public VariableExpression (string name)
		{
			Name = name;
		}
		public override object Eval (EvalEnv env)
		{
			return env.Lookup (Name);
		}
	}

	class MemberExpression : Expression
	{
		public readonly Expression Object;
		public readonly string Name;
		public MemberExpression (Expression obj, string name)
		{
			Object = obj;
			Name = name;
		}
		public override object Eval (EvalEnv env)
		{
			var obj = Object.Eval (env);
			if (obj == null) {
				throw new NullReferenceException ("Object is null when accessing '" + Name + "'");
			}

			var members = obj.GetType ().GetMember (Name);
			if (members == null || members.Length == 0) {
				throw new MissingMemberException (obj.GetType ().FullName, Name);
			}

			var member = members[0];
			var prop = member as PropertyInfo;
			if (prop != null) {
				return prop.GetValue (obj, null);
			}
			else {
				var field = member as FieldInfo;
				if (field != null) {
					return field.GetValue (obj);
				}
				else {
					throw new NotSupportedException (member.MemberType.ToString ());
				}
			}
		}
	}

	enum TokenType : int
	{
		Dot = '.',
		Comma = ',',
		Colon = ':',

		LeftParen = '(',
		RightParen = ')',
		LeftCurly = '{',
		RightCurly = '}',

		Add = '+',
		Subtract = '-',
		Multiply = '*',
		Divide = '/',
		
		Assign = '=',

		String = 1000,
		Number = 1001,
		Identifier = 1002,

		Equal = 2000,
		NotEqual = 2001,

		LessThan = 2002,
		LessThanOrEqual = 2003,
		GreaterThan = 2004,
		GreaterThanOrEqual = 2005,

		LogicalOr = 3000,
		BitwiseOr = 3001,
		LogicalAnd = 3002,
		BitwiseAnd = 3003,
		LogicalNot = 3004,
	}

	class Token
	{
		string _src;
		int _startIndex;
		int _length;
		string _stringValue;

		public TokenType Type { get; private set; }

		public Token (TokenType type, string src, int startIndex, int length)
		{
			Type = type;
			_src = src;
			_startIndex = startIndex;
			_length = length;
		}

		public override string ToString ()
		{
			if (_stringValue == null) {
				_stringValue = _src.Substring (_startIndex, _length);
			}
			return _stringValue;
		}

		public static IEnumerable<Token> Tokenize (string src)
		{
			if (string.IsNullOrEmpty (src)) {
				yield break;
			}

			var p = 0;
			var end = src.Length;

			while (p < end) {

				while (p < end && char.IsWhiteSpace (src[p])) {
					p += 1;
				}
				if (p >= end) {
					yield break;
				}

				var ch = src[p];

				if (ch == '{' || ch == '(' || ch == '}' || ch == ')' || ch == ',' || ch == ':' || ch == '+' || ch == '*' || ch == '/' || ch == '-') {
					yield return new Token ((TokenType)ch, src, p, 1);
					p += 1;
				}
				else if (ch == '.') {
					if (p + 1 < end && char.IsDigit (src[p + 1])) {
						yield return TokenizeNumber (src, ref p);
					}
					else {
						yield return new Token (TokenType.Dot, src, p, 1);
						p += 1;
					}
				}
				else if (ch == '=') {
					if (p + 1 < end && src[p + 1] == '=') {
						yield return new Token (TokenType.Equal, src, p, 2);
						p += 2;
					}
					else {
						yield return new Token (TokenType.Assign, src, p, 1);
						p += 1;
					}
				}
				else if (ch == '<') {
					if (p + 1 < end && src[p + 1] == '=') {
						yield return new Token (TokenType.LessThanOrEqual, src, p, 2);
						p += 2;
					}
					else {
						yield return new Token (TokenType.LessThan, src, p, 1);
						p += 1;
					}
				}
				else if (ch == '>') {
					if (p + 1 < end && src[p + 1] == '=') {
						yield return new Token (TokenType.GreaterThanOrEqual, src, p, 2);
						p += 2;
					}
					else {
						yield return new Token (TokenType.GreaterThan, src, p, 1);
						p += 1;
					}
				}
				else if (ch == '|') {
					if (p + 1 < end && src[p + 1] == '|') {
						yield return new Token (TokenType.LogicalOr, src, p, 2);
						p += 2;
					}
					else {
						yield return new Token (TokenType.BitwiseOr, src, p, 1);
						p += 1;
					}
				}
				else if (ch == '&') {
					if (p + 1 < end && src[p + 1] == '&') {
						yield return new Token (TokenType.LogicalAnd, src, p, 2);
						p += 2;
					}
					else {
						yield return new Token (TokenType.BitwiseAnd, src, p, 1);
						p += 1;
					}
				}
				else if (ch == '!') {
					if (p + 1 < end && src[p + 1] == '=') {
						yield return new Token (TokenType.NotEqual, src, p, 2);
						p += 2;
					}
					else {
						yield return new Token (TokenType.LogicalNot, src, p, 1);
						p += 1;
					}
				}
				else if (ch == '\"') {
					yield return TokenizeString (src, ref p);
				}
				else if (ch == '\'') {
					yield return TokenizeString (src, ref p);
				}
				else if (char.IsDigit (ch)) {
					yield return TokenizeNumber (src, ref p);
				}
				else if (ch == '_' || ch == '$' || char.IsLetter (ch)) {
					yield return TokenizeIdentifier (src, ref p);
				}
				else {
					throw new ParseException ("Unexpected character '" + ch + "'");
				}
			}
		}

		static Token TokenizeString (string src, ref int p)
		{
			var startIndex = p + 1;
			var startChar = src[p];
			var end = src.Length;

			var sb = new StringBuilder ();

			p++;
			while (p < end) {
				var ch = src[p];
				if (ch == '\\') {
					if (p + 1 < end) {
						var ch1 = src[p + 1];
						switch (ch1) {
						case 'r':
							sb.Append ('\r');
							break;
						case 'n':
							sb.Append ('\n');
							break;
						case 't':
							sb.Append ('\t');
							break;
						case '\'':
							sb.Append ('\'');
							break;
						case '\"':
							sb.Append ('\"');
							break;
						default:
							sb.Append (ch1);
							break;
						}
						p += 2;
					}
					else {
						sb.Append (ch);
						p += 1;						
					}
				}
				else if (ch == startChar) {
					break;
				}
				else {
					sb.Append (ch);
					p += 1;
				}
			}

			var length = p - startIndex;
			p++;

			return new Token (TokenType.String, src, startIndex, length) { _stringValue = sb.ToString (), };
		}

		static Token TokenizeNumber (string src, ref int p)
		{
			var startIndex = p;
			var end = src.Length;

			var gotDot = false;
			var gotE = false;
			var gotEMinus = false;

			while (p < end) {
				var ch = src[p];
				if (char.IsDigit (ch)) {
					p++;
				}
				else if (ch == '.') {
					if (gotDot) {
						break;
					}
					else {
						gotDot = true;
						p++;
					}
				}
				else if (ch == '-') {
					if (p == startIndex) {
						p++;
					}
					else if (gotE) {
						if (gotEMinus) {
							break;
						}
						else {
							gotEMinus = true;
							p++;
						}
					}
					else {
						break;
					}
				}
				else if (ch == 'e' || ch == 'E') {
					if (gotE) {
						break;
					}
					else {
						gotE = true;
						p++;
					}
				}
				else if (ch == 'f') {
					p++;
					break;
				}
				else {
					break;
				}
			}

			var length = p - startIndex;
			return new Token (TokenType.Number, src, startIndex, length);
		}

		static Token TokenizeIdentifier (string src, ref int p)
		{
			var startIndex = p;
			var end = src.Length;

			while (p < end) {
				var ch = src[p];
				if (char.IsLetterOrDigit (ch) || ch == '_' || ch == '$') {
					p++;
				}
				else {
					break;
				}
			}

			var length = p - startIndex;
			return new Token (TokenType.Identifier, src, startIndex, length);
		}
	}

	public class StringMatrix
	{
		public List<List<string>> Rows;

		public StringMatrix (int numRows, int numCols)
		{
			Rows = new List<List<string>> ();
			for (var ri = 0; ri < numRows; ri++) {
				var row = new List<string> ();
				for (var ci = 0; ci < numCols; ci++) {
					row.Add ("");
				}
				Rows.Add (row);
			}
		}

		StringMatrix ()
		{
		}

		static bool NeedsQuoting (string src)
		{
			for (var i = 0; i < src.Length; i++) {
				if (src[i] < ' ') return true;
			}
			return false;
		}

		public string Tsv
		{
			get
			{
				var sb = new StringBuilder ();
				foreach (var r in Rows) {
					var head = "";
					foreach (var c in r) {
						sb.Append (head);
						if (NeedsQuoting (c)) {
							sb.Append ('\"');
							foreach (var ch in c) {
								if (ch == '\"') {
									sb.Append ("\"\"");
								}
								else {
									sb.Append (ch);
								}
							}
							sb.Append ('\"');
						}
						else {
							sb.Append (c);
						}
						head = "\t";
					}
					sb.Append ("\r\n");
				}
				return sb.ToString ();
			}
		}

		public static StringMatrix FromTsv (string src)
		{
			var p = 0;
			var end = src.Length;

			var rows = new List<List<string>> ();
			var row = new List<string> ();
			rows.Add (row);

			while (p < end) {
				var ch = src[p];
				if (ch == '\"') {
					row.Add (ParseQuotedTsvCell (src, ref p));
				}
				else if (ch == '\r') {
					if (p + 1 < end && src[p + 1] == '\n') {
						p += 2;
					}
					else {
						p += 1;
					}
					row = new List<string> ();
					rows.Add (row);
				}
				else if (ch == '\n') {
					p += 1;
					row = new List<string> ();
					rows.Add (row);
				}
				else {
					row.Add (ParseNormalTsvCell (src, ref p));
				}
			}

			if (row.Count == 0) {
				rows.RemoveAt (rows.Count - 1);
			}

			return new StringMatrix { Rows = rows, };
		}

		static string ParseQuotedTsvCell (string src, ref int p)
		{
			var val = new StringBuilder ();
			var startIndex = p;
			var end = src.Length;
			p++; // Move past the \"
			var neededQuotes = false;
			while (p < end) {

				var ch = src[p];
				if (ch == '\"') {
					if (p < end && src[p + 1] == '\"') {
						neededQuotes = true;
						val.Append ('\"');
						p += 2;
					}
					else {
						break;
					}
				}
				else {
					neededQuotes = neededQuotes || src[p] < ' ';
					val.Append (src[p]);
					p++;
				}
			}
			p++; // Move past the \"
			p++; // Move past the \t
			var r = val.ToString ();
			return neededQuotes ? r : '\"' + r + '\"';
		}

		static string ParseNormalTsvCell (string src, ref int p)
		{
			var val = new StringBuilder ();
			var startIndex = p;
			var end = src.Length;
			while (p < end) {
				var ch = src[p];
				if (ch == '\t') {
					p++;
					break;
				}
				else if (ch == '\r' || ch == '\n') {
					break;
				}
				else {
					val.Append (src[p]);
					p++;
				}
			}
			return val.ToString ();
		}
	}
}
