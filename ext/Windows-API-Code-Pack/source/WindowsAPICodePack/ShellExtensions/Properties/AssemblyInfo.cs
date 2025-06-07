using System.Runtime.InteropServices;
using System.Security;

//Use the original security model
//See: http://msdn.microsoft.com/en-us/library/ee191569.aspx
[assembly: SecurityRules(SecurityRuleSet.Level1)]

// Setting ComVisible to false makes the types in this assembly not visible to COM components. If you need to access a type in this assembly
// from COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("dd26f0bf-d274-4c1a-8db2-f5d54ef255d3")]