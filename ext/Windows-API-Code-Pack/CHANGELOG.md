### 2018-06-19  
**Core 1.1.3.3**  
**Shell 1.1.3.3**  
**ShellExtensions 1.1.3.3**  
**Sensors 1.1.3.3**  
**ExtendedLinguisticServices 1.1.3.3**  
- Retarget to .NET 4.5.2 instead of .NET 4.6.2. This is the oldest version of the .NET framework that is still under support.


### 2018-02-28  
**Core 1.1.3.2**  
**Shell 1.1.3.2**  
**ShellExtensions 1.1.3.2**  
**Sensors 1.1.3.2**  
**ExtendedLinguisticServices 1.1.3.2**  
- Fix dependencies between the packages

### 2018-02-27  
**Core 1.1.3.1**  
**Shell 1.1.3**  
**ShellExtensions 1.1.3**  
**Sensors 1.1.3**  
**ExtendedLinguisticServices 1.1.3**  
- Fix Enabled property not working properly #1
- Fixed marshaling errors and memory leaks with TaskDialogButton's #2
- Fix a bug where a ShellLink was not properly created #3
- Remove the border on the ExplorerBrowser control #4
- Fix the implementation of the property CheckSelect #5
- Fixed comctl32.dll version 6 problem. Added default button selector to TaskDialog, Signed the assemblies #6
- Fix Command link description display bug. #7
- Fixed bug in normalizing value of itemType variable. #8
- Fix NullReferenceException when setting label.Text while the dialog is open #9
- Fixed the pointer size to ensure proper memory access #10
- Fixes for NativeTaskDialog aybe#29
- Revert "Return correct value when closing". See aybe#41 for details #12

### 02/01/2016  
**Core 1.1.2**  
- TaskDialog icons were visible only when defined in Opened event
- TaskDialog custom/hyperlink button not closing dialog from within Click event
