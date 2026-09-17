Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports Microsoft.Win32.SafeHandles

''' <summary>
''' API declarations relating to file I/O.
''' </summary>

Friend NotInheritable Class FileIO

    Friend Const FILE_FLAG_OVERLAPPED As Int32 = &H40000000
    Friend Const FILE_SHARE_READ As Int32 = 1
    Friend Const FILE_SHARE_WRITE As Int32 = 2
    Friend Const GENERIC_READ As UInt32 = &H80000000UL
    Friend Const GENERIC_WRITE As UInt32 = &H40000000
    Friend Const INVALID_HANDLE_VALUE As Int32 = -1
    Friend Const OPEN_EXISTING As Int32 = 3
    Friend Const WAIT_TIMEOUT As Int32 = &H102
    Friend Const WAIT_OBJECT_0 As Int32 = 0

    <StructLayout(LayoutKind.Sequential)>
    Friend Class SECURITY_ATTRIBUTES
        Friend nLength As Int32
        Friend lpSecurityDescriptor As Int32
        Friend bInheritHandle As Int32
    End Class

    <DllImport("kernel32.dll", SetLastError:=True)>
    Shared Function CancelIo _
        (ByVal hFile As SafeFileHandle) _
            As Int32
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Shared Function CreateEvent _
        (ByVal SecurityAttributes As IntPtr,
        ByVal bManualReset As Boolean,
        ByVal bInitialState As Boolean,
        ByVal lpName As String) _
        As IntPtr
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Shared Function CreateFile _
     (ByVal lpFileName As String,
     ByVal dwDesiredAccess As UInt32,
     ByVal dwShareMode As Int32,
     ByVal lpSecurityAttributes As IntPtr,
     ByVal dwCreationDisposition As Int32,
     ByVal dwFlagsAndAttributes As Int32,
     ByVal hTemplateFile As Int32) _
     As SafeFileHandle
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Shared Function GetOverlappedResult _
     (ByVal hFile As SafeFileHandle,
     ByVal lpOverlapped As IntPtr,
     ByRef lpNumberOfBytesTransferred As Int32,
     ByVal bWait As Boolean) _
     As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Shared Function ReadFile _
     (ByVal hFile As SafeFileHandle,
     ByVal lpBuffer As IntPtr,
     ByVal nNumberOfBytesToRead As Int32,
     ByRef lpNumberOfBytesRead As Int32,
     ByVal lpOverlapped As IntPtr) _
     As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Shared Function WaitForSingleObject _
     (ByVal hHandle As IntPtr,
     ByVal dwMilliseconds As Int32) _
     As Int32
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Shared Function WriteFile _
     (ByVal hFile As SafeFileHandle,
     ByVal lpBuffer() As Byte,
     ByVal nNumberOfBytesToWrite As Int32,
     ByRef lpNumberOfBytesWritten As Int32,
     ByVal lpOverlapped As IntPtr) _
     As Boolean
    End Function

End Class

