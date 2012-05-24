using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using QuickTest;
using System.Drawing;
using System.Globalization;

namespace QuickTest
{
	public partial class TestWindow : UserControl
	{
		public DTE2 ApplicationObject { get; set; }

		public TestWindow ()
		{
			InitializeComponent ();
		}

		CodeFunction2 _funcElm;
		MemberTests _tests;
		TestType _testType;

		public void OnBuildDone ()
		{
			RunAllRows ();
		}

		private void button1_Click (object sender, EventArgs e)
		{
			try {
				SyncWithCode (forceRun: true);
			}
			catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}
		}

		public void SyncWithCode (bool forceRun = false)
		{
			var doc = ApplicationObject.ActiveDocument;
			if (doc == null) return;

			//
			// Fetch the selected bit of code
			//
			var sel = (TextSelection)doc.Selection;
			var p = sel.ActivePoint;
			var funcElm = (CodeFunction2)p.CodeElement[vsCMElement.vsCMElementFunction];

			if (funcElm == null) {
				var propElm = (CodeProperty2)p.CodeElement[vsCMElement.vsCMElementProperty];
				if (propElm != null) {
					var getter = propElm.Getter;
					var setter = propElm.Setter;
					if (getter != null &&
						p.GreaterThan (getter.StartPoint) &&
						p.LessThan (getter.EndPoint)) {
						funcElm = (CodeFunction2)getter;
					}
					else if (setter != null &&
						p.GreaterThan (setter.StartPoint) &&
						p.LessThan (setter.EndPoint)) {
						funcElm = (CodeFunction2)setter;
					}
				}
			}

			//
			// Make sure it's .NET (C# and VB)
			//
			if (funcElm != null) {
				if (funcElm.Language != CodeModelLanguageConstants.vsCMLanguageCSharp &&
					funcElm.Language != CodeModelLanguageConstants.vsCMLanguageVB) {
					funcElm = null;
				}
			}

			if (funcElm == null) return;

			//
			// Handle it
			//
			var newFunc = false;

			if (_funcElm == null || GetMemberName (funcElm) != _tests.Member) {
				_funcElm = funcElm;

				SetMember ();

				DisplayFunction ();

				newFunc = true;
			}

			if (forceRun || newFunc) {
				RunAllRows ();
			}
		}

		string _projectPath;
		string _repoPath;
		TestRepo _projectRepo;

		TestRepo SetProject (Project project)
		{
			var projectPath = project.FullName;

			if (projectPath != _projectPath) {

				_projectPath = projectPath;

				var dir = Path.GetDirectoryName (projectPath);

				ProjectItem repoItem = null;
				foreach (ProjectItem i in project.ProjectItems) {
					if (Path.GetExtension (i.Name) == TestRepo.FileExtension) {
						repoItem = i;
					}
				}
				if (repoItem == null) {

					var filename = project.Name + TestRepo.FileExtension;
					_repoPath = Path.Combine (dir, filename);

					_projectRepo = new TestRepo ();
					_projectRepo.Save (_repoPath);

					project.ProjectItems.AddFromFile (_repoPath);
				}
				else {
					_repoPath = Path.Combine (dir, repoItem.Name);
					_projectRepo = TestRepo.Open (_repoPath);
				}
			}

			return _projectRepo;
		}

		private void SetMember ()
		{
			//
			// Analyze the member
			//
			var isSetter = _funcElm.FunctionKind == vsCMFunction.vsCMFunctionPropertySet;
			var isGetter = _funcElm.FunctionKind == vsCMFunction.vsCMFunctionPropertyGet;
			var isVoid = isSetter || _funcElm.Type.CodeType.FullName == "System.Void";

			if (isGetter) {
				_testType = TestType.PropertyGetter;
			}
			else if (isSetter) {
				_testType = TestType.PropertySetter;
			}
			else if (isVoid) {
				_testType = TestType.Procedure;
			}
			else {
				_testType = TestType.Function;
			}

			//
			// Fetch the tests
			//
			var repo = SetProject (_funcElm.ProjectItem.ContainingProject);
			var memberName = GetMemberName (_funcElm);
			_tests = repo.GetMemberTests (memberName);
			foreach (var t in _tests.Tests) {
				t.TestType = _testType;
				t.Member = memberName;
			}
		}

		class CodeElementInfo
		{
			public string Name;
			public vsCMElement Kind;
			public override string ToString ()
			{
				return Kind + " " + Name;
			}
			public bool IsType
			{
				get
				{
					return Kind == vsCMElement.vsCMElementClass ||
						Kind == vsCMElement.vsCMElementInterface ||
						Kind == vsCMElement.vsCMElementEnum ||
						Kind == vsCMElement.vsCMElementStruct ||
						Kind == vsCMElement.vsCMElementUnion;
				}
			}
		}

		static List<CodeElementInfo> GetChain (dynamic member)
		{
			var chain = new List<CodeElementInfo> ();
			try {
				dynamic e = member;
				while (e != null && (int)e.Kind <= 12) {
					chain.Add (new CodeElementInfo { Name = e.Name, Kind = (vsCMElement)(int)e.Kind });
					try {
						e = e.Parent;
					}
					catch (Exception) {
						try {
							e = e.Parent2;
						}
						catch (Exception) {
							e = null;
						}
					}
				}
			}
			catch (Exception) {
			}
			chain.Reverse ();
			return chain;
		}

		static string GetChainName (IEnumerable<CodeElementInfo> chainQuery)
		{
			var chain = chainQuery.ToList ();
			var sb = new StringBuilder ();
			for (var i = 0; i < chain.Count; i++) {
				if (sb.Length == 0) {
					sb.Append (chain[i].Name);
				}
				else {
					if (string.IsNullOrWhiteSpace (chain[i].Name)) {
					}
					else if (chain[i].Name == chain[i - 1].Name) {
					}
					else if (chain[i].IsType && chain[i - 1].IsType) {
						sb.Append ("+");
						sb.Append (chain[i].Name);
					}
					else {
						sb.Append (".");
						sb.Append (chain[i].Name);
					}
				}
			}
			return sb.ToString ();
		}

		static string GetMemberName (CodeFunction member)
		{
			var sb = new StringBuilder();
			sb.Append (GetChainName (GetChain (member)));

			var prop = member.Parent as CodeProperty;
			if (prop == null) {
				sb.Append ("(");
				var head = "";
				foreach (CodeParameter p in member.Parameters) {
					sb.Append (head);
					sb.Append (GetTypeName (p.Type));
					head = ", ";
				}
				sb.Append (")");
			}
			return sb.ToString ();
		}

		static string GetTypeName (CodeTypeRef type)
		{
			var name = "";
			try {
				var elm = type.CodeType;
				if (elm != null) {
					var chain = GetChain (elm);
					name = GetChainName (chain.TakeWhile (x => x.Kind == vsCMElement.vsCMElementNamespace || x.IsType));
				}
				else {
					name = type.AsFullName;
				}
			}
			catch (Exception) {
			}
			if (string.IsNullOrEmpty (name)) {
				name = type.AsString;
			}
			return name;
		}

		static string FindRunner ()
		{
			var asmLoc = typeof (TestWindow).Assembly.Location;
			var dir = Path.GetDirectoryName (asmLoc);

			var paths = new[] {
				Path.Combine (dir, "QuickTestRunner.exe"),
				Path.Combine (dir, "..\\..\\QuickTestRunner\\bin\\Debug\\QuickTestRunner.exe"),
				Path.Combine (dir, "..\\..\\QuickTestRunner\\bin\\Release\\QuickTestRunner.exe"),
			};

			var infos =  from p in paths
						 where File.Exists (p)
						 let fi = new FileInfo (p)
						 orderby fi.LastWriteTimeUtc descending
						 select p;

			return infos.FirstOrDefault ();
		}

		void RunTests (TestPlan inTests)
		{
			var testsPath = Path.GetTempFileName ();
			inTests.Save (testsPath);

			var runner = FindRunner ();
			if (string.IsNullOrEmpty (runner)) {
				MessageBox.Show ("Could not find QuickTestRunner.exe");
				return;
			}

			var info = new System.Diagnostics.ProcessStartInfo {
				FileName = runner,
				Arguments = "\"" + testsPath + "\"",
				RedirectStandardOutput = true,
				StandardOutputEncoding = Encoding.UTF8,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			var proc = System.Diagnostics.Process.Start (info);
			var output = proc.StandardOutput.ReadToEnd ();
			proc.WaitForExit (5000);

			try {
				var outTests = TestPlan.Open (new StringReader (output));

				foreach (var t in outTests.Tests) {
					_tests.GetTest (t.Id).RecordResults (t);
				}

				_projectRepo.Save (_repoPath);

				File.Delete (testsPath);
			}
			catch (Exception) {
			}

			BeginInvoke ((Action)delegate {
				Progress.Visible = false;
				if (proc.ExitCode == 0) {
					OutputTests ();
				}
				else {
					SetStatus (FailStatusIcon, "Execution Failed: " + output);
				}
			});
		}

		void SetStatus (int icon, string message)
		{
			StatusButton.ImageIndex = icon;
			StatusLabel.Text = message;
		}

		void OutputTests ()
		{
			for (var i = 0; i < Grid.Rows.Count; i++) {
				var row = Grid.Rows[i];
				if (row.Tag != null) {
					BindRow (i);
				}
			}

			UpdateStats ();
		}

		static string GetCellText (DataGridViewCell cell)
		{
			var v = cell.Value;
			return v != null ? Convert.ToString (v, CultureInfo.InvariantCulture) : "";
		}

		void UpdateStats ()
		{
			var stats = new int[3];
			foreach (var t in _tests.Tests) {
				stats[(int)t.Result]++;
			}

			var pass = stats[(int)TestResult.Pass];
			var fail = stats[(int)TestResult.Fail];
			var unk = stats[(int)TestResult.Unknown];

			var msg = "";
			if (unk > 0) {
				msg = string.Format ("Results: {0}/{1} passed, {2} unknown", pass, pass + fail + unk, unk);
			}
			else {
				msg = string.Format ("Results: {0}/{1} passed", pass, pass + fail);
			}

			var overall = SuccessStatusIcon;
			if (fail > 0) {
				overall = FailStatusIcon;
			}
			else if (unk > 0) {
				overall = UnknownStatusIcon;
			}

			SetStatus (overall, msg);
		}

		const int PassStatusIcon = 1;
		const int FailStatusIcon = 2;
		const int UnknownStatusIcon = 3;
		const int SuccessStatusIcon = 4;
		const int InfoStatusIcon = 5;
		const int NewStatusIcon = 6;

		TestResult GetRowResult (int rowIndex)
		{
			var row = Grid.Rows[rowIndex];
			var t = row.Tag as Test;
			if (t != null) {
				return t.Result;
			}
			else {
				return TestResult.Unknown;
			}
		}

		void RunAllRows ()
		{
			RunRows (Enumerable.Range (0, Grid.Rows.Count - 1));
		}

		void RunRows (IEnumerable<int> rowIndices)
		{
			if (_funcElm == null) return;

			var plan = new TestPlan ();

			//
			// Find all the tests that need to run
			//
			var nargs = _paramInfos.Count;

			foreach (var rowIndex in rowIndices) {
				var row = Grid.Rows[rowIndex];
				if (row.Tag == null) {
					var rt = new Test {
						Id = Guid.NewGuid (),
					};
					_tests.Tests.Add (rt);
					row.Tag = rt;
				}
				var t = ParseRow (rowIndex);
				plan.Tests.Add (t);
			}

			if (plan.Tests.Count == 0) return;

			//
			// Get the extra info needed by the test plan
			//
			var project = _funcElm.ProjectItem.ContainingProject;
			plan.AssemblyPath = GetAssemblyPath (project);
			try {
				var references = ((dynamic)project.Object).References;
				
				foreach (var item in references) {
					plan.References.Add (new TestAssemblyReference {
						Name = item.Name,
						Version = item.Version,
						Culture = item.Culture,
						PublicKeyToken = item.PublicKeyToken.ToString ().ToLowerInvariant (),
						Path = item.Path,
						StrongName = item.StrongName,
					});
				}
			}
			catch (Exception) {
			}

			//
			// Run it
			//
			Progress.Visible = true;
			SetStatus (InfoStatusIcon, "Test run in progress");
			ThreadPool.QueueUserWorkItem (delegate {
				RunTests (plan);
			});
		}

		static string GetAssemblyPath (Project project)
		{
			var fullPath = project.Properties.Item ("FullPath").Value.ToString ();
			var outputPath = project.ConfigurationManager.ActiveConfiguration.Properties.Item ("OutputPath").Value.ToString ();
			var outputDir = Path.Combine (fullPath, outputPath);
			var outputFileName = project.Properties.Item ("OutputFileName").Value.ToString ();
			var assemblyPath = Path.Combine (outputDir, outputFileName);
			return assemblyPath;
		}

		int _resultImageColIndex;
		int _thisColIndex;
		List<ParamInfo> _paramInfos;
		int _resultColIndex;
		int _expectedColIndex;
		int _assertColIndex;
		int _failColIndex;

		class ParamInfo
		{
			public string Name;
			public int ColIndex;
			public string ParameterType;
		}

		void DisplayFunction ()
		{
			//
			// Update the UI
			//
			MemberBox.Text = _tests.Member;

			BindGrid ();

			UpdateStats ();
		}

		void BindGrid ()
		{
			InitializeGrid ();

			Grid.Rows.Clear ();

			if (_tests.Tests.Count > 0) {

				Grid.Rows.Add (_tests.Tests.Count);

				var index = 0;
				foreach (var t in _tests.Tests) {
					var r = Grid.Rows[index];
					r.Tag = t;
					BindRow (index);
					index++;
				}
			}
		}

		void BindRow (int rowIndex)
		{
			BindRow (Grid.Rows[rowIndex]);
		}

		static string TrimCellText (string text)
		{
			var t = text;
			if (t.Length > 140) {
				t = t.Substring (0, 140) + "...";
			}
			return t;
		}

		void BindRow (DataGridViewRow row)
		{
			var vals = new object[Grid.Columns.Count];

			var t = (Test)row.Tag;
			if (t == null) throw new InvalidOperationException ("Row has no associated Test");

			if (t.Result == TestResult.Pass) {
				vals[_resultImageColIndex] = AllImages.Images[PassStatusIcon];
			}
			else if (t.Result == TestResult.Fail) {
				vals[_resultImageColIndex] = AllImages.Images[FailStatusIcon];
			}
			else if (t.Result == TestResult.Unknown) {
				vals[_resultImageColIndex] = AllImages.Images[UnknownStatusIcon];
			}

			if (!_funcElm.IsShared) {
				vals[_thisColIndex] = t.ThisString;
			}

			foreach (var p in _paramInfos) {
				var arg = t.GetArgument (p.Name);
				vals[p.ColIndex] = arg.ValueString;
			}

			if (_testType == TestType.Function || _testType == TestType.PropertyGetter) {
				vals[_resultColIndex] = t.ValueString;
				vals[_expectedColIndex] = t.ExpectedValueString;
			}

			vals[_assertColIndex] = t.AssertString;

			vals[_failColIndex] = TrimCellText (t.FailInfo);

			row.SetValues (vals);
		}

		class DefaultImageCell : DataGridViewImageCell
		{
			public Image Image;
			public override object DefaultNewRowValue
			{
				get
				{
					return Image;
				}
			}
		}

		Test ParseRow (int rowIndex)
		{
			var row = Grid.Rows[rowIndex];
			var t = (Test)row.Tag;
			if (t == null) throw new InvalidOperationException ("Row is not associated with a test");

			t.Member = _tests.Member;
			t.TestType = _testType;

			if (_thisColIndex >= 0) {
				t.ThisString = GetCellText (row.Cells[_thisColIndex]);
			}

			if (_expectedColIndex >= 0) {
				t.ExpectedValueString = GetCellText (row.Cells[_expectedColIndex]);
			}

			if (_assertColIndex >= 0) {
				t.AssertString = GetCellText (row.Cells[_assertColIndex]);
			}

			t.Arguments.Clear ();
			foreach (var p in _paramInfos) {
				var cell = row.Cells[p.ColIndex];
				t.Arguments.Add (new TestArgument {
					Name = p.Name,
					ValueString = GetCellText (cell),
					ValueType = p.ParameterType,
				});
			}

			return t;
		}

		private void InitializeGrid ()
		{
			Grid.Rows.Clear ();
			Grid.Columns.Clear ();
			_paramInfos = new List<ParamInfo> ();

			//
			// Image Column
			//
			_resultImageColIndex = Grid.Columns.Count;
			Grid.Columns.Add (new DataGridViewImageColumn {
				Width = 20,
				MinimumWidth = 20,
				ReadOnly = true,
				Image = AllImages.Images[NewStatusIcon],
				CellTemplate = new DefaultImageCell {
					Image = AllImages.Images[NewStatusIcon],
					Style = new DataGridViewCellStyle {
						Alignment = DataGridViewContentAlignment.TopCenter,
					},
				},
			});

			//
			// this Column
			//
			if (_funcElm.IsShared) {
				_thisColIndex = -1;
			}
			else {
				_thisColIndex = Grid.Columns.Count;
				Grid.Columns.Add ("this", "this");
			}

			//
			// Parameters Column
			//
			if (_testType == TestType.PropertySetter) {
				var pi = new ParamInfo {
					Name = "value",
					ColIndex = Grid.Columns.Count,
					ParameterType = GetTypeName (_funcElm.Type),
				};
				_paramInfos.Add (pi);
				Grid.Columns.Add (pi.Name, pi.Name);
			}
			else {
				foreach (CodeParameter2 p in _funcElm.Parameters) {
					var pi = new ParamInfo {
						Name = p.Name,
						ColIndex = Grid.Columns.Count,
						ParameterType = GetTypeName (p.Type),
					};
					_paramInfos.Add (pi);
					Grid.Columns.Add (pi.Name, pi.Name);
				}
			}

			//
			// Value and Expected Value Columns
			//			
			if (_testType == TestType.Procedure || _testType == TestType.PropertySetter) {
				_resultColIndex = -1;
				_expectedColIndex = -1;
			}
			else {
				_resultColIndex = Grid.Columns.Count;
				Grid.Columns.Add (_funcElm.Name, _funcElm.Name);
				Grid.Columns[_resultColIndex].ReadOnly = true;

				_expectedColIndex = Grid.Columns.Count;
				Grid.Columns.Add ("E" + _funcElm.Name, "E[" + _funcElm.Name + "]");
			}

			//
			// Assert Column
			//
			_assertColIndex = Grid.Columns.Count;
			Grid.Columns.Add ("Assert", "Assert");

			//
			// Fail Column
			//
			_failColIndex = Grid.Columns.Count;
			Grid.Columns.Add ("Fail", "Fail");
			Grid.Columns[_failColIndex].ReadOnly = true;
			Grid.Columns[_failColIndex].Width *= 2;

			//
			// Format
			//
			foreach (DataGridViewColumn col in Grid.Columns) {
				col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
			}
		}

		private void Grid_CellEndEdit (object sender, DataGridViewCellEventArgs e)
		{
			try {
				if (_funcElm == null) return;

				var row = Grid.Rows[e.RowIndex];

				RunRows (new[] { e.RowIndex });
			}
			catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}
		}

		private void Grid_RowsAdded (object sender, DataGridViewRowsAddedEventArgs e)
		{
		}

		private void Grid_RowsRemoved (object sender, DataGridViewRowsRemovedEventArgs e)
		{
		}

		private void deleteTestToolStripMenuItem_Click (object sender, EventArgs e)
		{
			try {
				var rowIndexes = Grid.SelectedCells.Cast<DataGridViewCell> ().Select (x => x.RowIndex).Distinct ();
				var tests = (from i in rowIndexes
							 let row = Grid.Rows[i]
							 let t = row.Tag as Test
							 where t != null
							 select new { Row = row, Test = t, }).ToArray ();

				foreach (var t in tests) {
					_tests.Tests.Remove (t.Test);
					Grid.Rows.Remove (t.Row);
				}
				UpdateStats ();
			}
			catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}
		}

		private void Grid_KeyDown (object sender, KeyEventArgs e)
		{
			try {
				if (e.Control && e.KeyCode == Keys.V) {
					int rowIndex = 0;
					int colIndex = 0;
					if (Grid.SelectedCells.Count > 0) {
						rowIndex = int.MaxValue;
						colIndex = int.MaxValue;
						foreach (DataGridViewCell c in Grid.SelectedCells) {
							rowIndex = Math.Min (c.RowIndex, rowIndex);
							colIndex = Math.Min (c.ColumnIndex, colIndex);
						}
					}
					PasteAt (rowIndex, colIndex);
					e.Handled = true;
				}
				else if (e.Control && e.KeyCode == Keys.C) {
					Copy (cut: false);
					e.Handled = true;
				}
				else if (e.Control && e.KeyCode == Keys.X) {
					var rows = new List<int> ();
					foreach (DataGridViewCell c in Grid.SelectedCells) {
						if (!Grid.Columns[c.ColumnIndex].ReadOnly) {
							rows.Add (c.RowIndex);
						}
					}
					Copy (cut: true);					
					RunRows (rows.Distinct ());
					e.Handled = true;
				}
				else if (e.KeyCode == Keys.Delete) {
					var rows = new List<int> ();
					foreach (DataGridViewCell c in Grid.SelectedCells) {
						if (!Grid.Columns[c.ColumnIndex].ReadOnly) {
							rows.Add (c.RowIndex);
							c.Value = null;
						}
					}
					RunRows (rows.Distinct ());
					e.Handled = true;
				}
				else if (e.Control && e.KeyCode == Keys.Return) {
					var c = Grid.SelectedCells.OfType<DataGridViewCell> ().FirstOrDefault ();
					if (c != null && !c.ReadOnly) {
						Grid.CurrentCell = c;
						Grid.BeginEdit (true);
						e.Handled = true;
					}
				}
			}
			catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}
		}

		void Copy (bool cut)
		{
			if (Grid.SelectedCells.Count == 0) return;

			var minRowIndex = int.MaxValue;
			var minColIndex = int.MaxValue;
			var maxRowIndex = int.MinValue;
			var maxColIndex = int.MinValue;
			foreach (DataGridViewCell c in Grid.SelectedCells) {
				minRowIndex = Math.Min (c.RowIndex, minRowIndex);
				minColIndex = Math.Min (c.ColumnIndex, minColIndex);
				maxRowIndex = Math.Max (c.RowIndex, maxRowIndex);
				maxColIndex = Math.Max (c.ColumnIndex, maxColIndex);
			}

			var m = new StringMatrix (maxRowIndex - minRowIndex + 1, maxColIndex - minColIndex + 1);

			foreach (DataGridViewCell c in Grid.SelectedCells) {
				var mri = c.RowIndex - minRowIndex;
				var mci = c.ColumnIndex - minColIndex;
				m.Rows[mri][mci] = GetCellText (c);
				if (cut && !c.ReadOnly) {
					c.Value = null;
				}
			}

			Clipboard.SetText (m.Tsv, TextDataFormat.UnicodeText);
		}

		void PasteAt (int rowIndex, int colIndex)
		{
			if (Grid.Columns.Count == 0) return;

			var clip = Clipboard.GetDataObject ();
			var data = clip.GetData (DataFormats.UnicodeText);
			if (data == null) return;
			var tsv = data.ToString ();

			var m = StringMatrix.FromTsv (tsv);

			var editedRows = new List<int> ();

			for (var mri = 0; mri < m.Rows.Count; mri++) {
				var ri = mri + rowIndex;
				while (ri >= Grid.Rows.Count - 1) { // -1 to keep the insert row
					Grid.Rows.Add ();
				}
				editedRows.Add (ri);
				var r = Grid.Rows[ri];
				var mr = m.Rows[mri];
				for (var mci = 0; mci < mr.Count; mci++) {
					var ci = colIndex + mci;
					if (ci >= Grid.Columns.Count) continue;
					if (Grid.Columns[ci].ReadOnly) continue;
					r.Cells[ci].Value = mr[mci];
				}
			}

			RunRows (editedRows);
		}

		private void StatusButton_Click (object sender, EventArgs e)
		{
			try {
				RunAllRows ();
			}
			catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}
		}

		private void Grid_DoubleClick (object sender, EventArgs e)
		{
			try {
				if (_funcElm == null) return;

				var dte = _funcElm.DTE;

				var c = Grid.SelectedCells.Cast<DataGridViewCell> ().FirstOrDefault ();
				if (c == null) return;
				if (c.ColumnIndex != _failColIndex) return;
				var row = Grid.Rows[c.RowIndex];
				var t = row.Tag as Test;
				if (t == null) return;

				if (!string.IsNullOrEmpty (t.FailInfo)) {
					var f = t.GetFailCodeReference ();
					if (f != null) {
						var window = _funcElm.DTE.ItemOperations.OpenFile (f.Path, Constants.vsViewKindCode);
						var doc = dte.ActiveDocument;
						var ts = doc.Selection as TextSelection;
						ts.GotoLine (f.Line);
						ts.SelectLine ();
					}
				}
			}
			catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}
		}

		private void UpdateTimer_Tick (object sender, EventArgs e)
		{
			try {
				if (this.Visible) {
					SyncWithCode (forceRun: false);
				}
			}
			catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}
		}
	}
}
