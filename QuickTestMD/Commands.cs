using System;
using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace QuickTestMD
{
	class TestWindowPadContent : IPadContent
	{
		TestWindow _window;
		
		public TestWindowPadContent ()
		{
			_window = new TestWindow ();
			Control.ShowAll ();
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
		}
		#endregion

		#region IPadContent implementation
		public void Initialize (IPadWindow window)
		{
		}

		public void RedrawContent ()
		{
		}

		public Gtk.Widget Control {
			get {
				return _window;
			}
		}
		#endregion		
	}
	
	public class SyncHandler : CommandHandler
	{
		protected override void Run ()
		{
			var padId = "QuickTest.TestWindow";
			var pad = IdeApp.Workbench.Pads.FirstOrDefault (p => p.Id == padId);
			if (pad == null) {
				var padContent = new TestWindowPadContent ();
				pad = IdeApp.Workbench.AddPad (padContent, "QuickTest.TestWindow", "Quick Test", "", null);
			}
			pad.BringToFront (false);
		}
	}
}

