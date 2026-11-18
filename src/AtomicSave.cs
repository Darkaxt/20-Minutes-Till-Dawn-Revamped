using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
namespace TwentyMinutes.Modernization;

// The Windows/Proton player publishes an already flushed sibling with a single
// replacement rename. File.Replace's metadata-merging operation can fail with
// ERROR_UNABLE_TO_REMOVE_REPLACED (1175) during consecutive journal commits.
internal static class AtomicSave
{
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
 [return:MarshalAs(UnmanagedType.Bool)]
 static extern bool MoveFileEx(string existing,string destination,int flags);
 internal static void Commit(string writing,string destination)
 {
  const int ReplaceExisting=1,WriteThrough=8;
  if(!MoveFileEx(writing,destination,ReplaceExisting|WriteThrough))
   throw new IOException("Could not publish the saved state: "+destination,new Win32Exception(Marshal.GetLastWin32Error()));
 }
}
