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

