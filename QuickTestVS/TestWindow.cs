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
			RunRows (Enumerable.Range (0, Grid.Rows.Count - 1));
		}

		private void button1_Click (object sender, EventArgs e)
		{
			SyncWithCode ();
		}

		public void SyncWithCode ()
		{
			var doc = ApplicationObject.ActiveDocument;
			if (doc == null) return;

			var sel = (TextSelection)doc.Selection;
			var p = sel.ActivePoint;
			var funcElm = (CodeFunction2)p.CodeElement[vsCMElement.vsCMElementFunction];

			if (funcElm == null) {
				var propElm = (CodeProperty2)p.CodeElement[vsCMElement.vsCMElementProperty];
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

			if (funcElm == null) return;

			if (_funcElm == null || funcElm.FullName != _funcElm.FullName) {
				_funcElm = funcElm;

				SetMember ();

				DisplayFunction ();
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
			var memberName = _funcElm.FullName;
			_tests = repo.GetMemberTests (memberName);
			foreach (var t in _tests.Tests) {
				t.TestType = _testType;
				t.Member = memberName;
			}
		}

		void RunTests (TestPlan inTests)
		{
			var testsPath = Path.GetTempFileName ();
			inTests.Save (testsPath);

			var info = new System.Diagnostics.ProcessStartInfo {
				FileName = @"C:\Projects\QuickTest\QuickTestRunner\bin\Debug\QuickTestRunner.exe",
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
					UpdateRowColor (i);
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
			for (var i = 0; i < Grid.Rows.Count - 1; i++) {
				stats[(int)GetRowResult (i)]++;
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

		void UpdateRowColor (int rowIndex)
		{
			/*var r = GetRowResult (rowIndex);

			if (r == TestResult.Unknown) {
				SetRowUnkownColor (rowIndex);
			}
			else if (r == TestResult.Fail) {
				Grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Firebrick;
			}
			else {
				Grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Green;
			}*/
		}

		void SetRowUnkownColor (int rowIndex)
		{
			Grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Black;
		}

		void RunRows (IEnumerable<int> rowIndices)
		{
			if (_funcElm == null) return;

			var plan = new TestPlan ();

			var nargs = _paramInfos.Count;

			foreach (var rowIndex in rowIndices) {
				var t = ParseRow (rowIndex);
				plan.Tests.Add (t);
				SetRowUnkownColor (rowIndex);
			}

			plan.AssemblyPath = GetAssemblyPath (_funcElm.ProjectItem.ContainingProject);
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
					UpdateRowColor (index);
					index++;
				}
			}
		}

		void BindRow (int rowIndex)
		{
			BindRow (Grid.Rows[rowIndex]);
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

			vals[_failColIndex] = t.FailInfo;

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
				CellTemplate = new DefaultImageCell { Image = AllImages.Images[NewStatusIcon], },
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
					ParameterType = _funcElm.Type.AsFullName,
				};
				_paramInfos.Add (pi);
				Grid.Columns.Add (pi.Name, pi.Name);
			}
			else {
				foreach (CodeParameter2 p in _funcElm.Parameters) {
					var pi = new ParamInfo {
						Name = p.Name,
						ColIndex = Grid.Columns.Count,
						ParameterType = p.Type.AsFullName,
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
			if (_funcElm == null) return;

			var row = Grid.Rows[e.RowIndex];

			if (e.ColumnIndex == _resultColIndex) {
				row.Cells[_expectedColIndex].Value = row.Cells[_resultColIndex].Value;
			}
			if (row.Tag == null) {
				var t = new Test {
					Id = Guid.NewGuid (),
				};
				_tests.Tests.Add (t);
				row.Tag = t;
			}
			RunRows (new[] { e.RowIndex });
		}

		private void Grid_RowsAdded (object sender, DataGridViewRowsAddedEventArgs e)
		{
		}

		private void Grid_RowsRemoved (object sender, DataGridViewRowsRemovedEventArgs e)
		{
		}
	}
}
