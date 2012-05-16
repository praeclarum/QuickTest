# Quick Test

Simple unit testing for easily-annoyed people.

## What?

You create state-based unit tests (inputs, transform, outputs) in a little window next to your code.
When you change the code, the tests re-run. When you move on to another function, so does Quick Test.

You don't:

* Create a test project.
* Make up names for tests.

You do:

* Test the code

## Yeah, but

Yeah, but it's better than not writing any unit tests at all.

## Installation

Add-ins are managed in Visual Studio under "Tools", "Add-in Manager". Once you have installed QuickTest (see below),
you will have to make sure it is enabled in this manager (make sure Start is checked). And then you will need to close and reopen Visual Studio
once or twice to get it to wake up. You will know that QuickTest is installed once there is a "Sync" menu item
added to your "Tools" menu. Sorry for the hassle, but this is my first time writing a VS add-in.

### Installer

1. Load QuickTest.sln
2. Build
3. Run QuickTestSetup\Debug\setup.exe

### Manual

1. Load QuickTest.sln in Visual Studio
2. Build.
3. Copy `QuickTestVS\bin\Debug\QuickTestVS.dll`, 
  `QuickTestVS\QuickTestVS.AddIn`, and
  `QuickTestRunner\bin\Debug\QuickTestRunner.exe` to
  `%USERPROFILE%\Documents\Visual Studio 2010\Addins`
4. Close Visual Studio and restart it
5. Select Tools, Sync Quick Test with Code to expose the window

## License

It's MIT. I still need to put that header on all the files.
