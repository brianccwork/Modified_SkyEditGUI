Option Strict On
Option Explicit On

Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.Win32.SafeHandles

'Windows HID transport only. The portal protocol and figure policies live above
'it in SimplePortal and the sessions; no form is created to connect a device.
Friend NotInheritable Class PortalTransport
    Private Const ReportSize As Integer = 33
    Private Shared stream As FileStream
    Private Shared handle As SafeFileHandle

    Private Sub New()
    End Sub

    Friend Shared ReadOnly Property IsOpen As Boolean
        Get
            Return stream IsNot Nothing AndAlso handle IsNot Nothing AndAlso Not handle.IsInvalid AndAlso Not handle.IsClosed
        End Get
    End Property

    Friend Shared Function Open() As Boolean
        Close()
        Dim hidGuid As Guid
        HidD_GetHidGuid(hidGuid)
        For Each path In DevicePaths(hidGuid)
            If path.IndexOf("VID_1430", StringComparison.OrdinalIgnoreCase) < 0 OrElse
                path.IndexOf("PID_0150", StringComparison.OrdinalIgnoreCase) < 0 Then Continue For
            Using candidate = CreateFile(path, &HC0000000UI, 3, IntPtr.Zero, 3, 0, IntPtr.Zero)
                If candidate.IsInvalid Then Continue For
                Dim attributes As New HidAttributes With {.Size = Marshal.SizeOf(GetType(HidAttributes))}
                If HidD_GetAttributes(candidate, attributes) = 0 OrElse attributes.VendorId <> &H1430 OrElse attributes.ProductId <> &H150 Then Continue For
            End Using
            handle = CreateFile(path, &HC0000000UI, 3, IntPtr.Zero, 3, &H40000000UI, IntPtr.Zero)
            If handle.IsInvalid Then
                handle.Dispose() : handle = Nothing
                Continue For
            End If
            Try
                stream = New FileStream(handle, FileAccess.ReadWrite, ReportSize, True)
                Flush()
                Return True
            Catch
                Close()
                Throw
            End Try
        Next
        Return False
    End Function

    Friend Shared Sub Close()
        Try
            If stream IsNot Nothing Then stream.Dispose()
        Finally
            stream = Nothing
            If handle IsNot Nothing Then handle.Dispose()
            handle = Nothing
        End Try
    End Sub

    Friend Shared Sub Send(report As Byte())
        If Not IsOpen Then Throw New IOException("Connect a portal first.")
        If report Is Nothing OrElse report.Length <> ReportSize Then Throw New ArgumentException("An HID report must contain 33 bytes.")
        If HidD_SetOutputReport(handle, report(0), report.Length) = 0 Then
            Throw New IOException("The portal did not accept the request. Reconnect it and try again.", New Win32Exception(Marshal.GetLastWin32Error()))
        End If
    End Sub

    Friend Shared Sub Flush()
        If Not IsOpen Then Throw New IOException("Connect a portal first.")
        If HidD_FlushQueue(handle) = 0 Then Throw New IOException("The portal queue could not be cleared. Reconnect it and try again.")
    End Sub

    Friend Shared Async Function ReadAsync(token As CancellationToken) As Task(Of Byte())
        If Not IsOpen Then Throw New IOException("Connect the portal again before reading.")
        Dim report(ReportSize - 1) As Byte, offset As Integer = 0
        Do While offset < report.Length
            Dim received = Await stream.ReadAsync(report, offset, report.Length - offset, token)
            If received = 0 Then Throw New IOException("The portal was disconnected.")
            offset += received
        Loop
        Return report
    End Function

    'The native device-interface string starts after the four-byte size field,
    'even on x64, where the structure's required cbSize value is eight.
    Private Shared Function DevicePaths(hidGuid As Guid) As List(Of String)
        Dim paths As New List(Of String)
        Dim devices = SetupDiGetClassDevs(hidGuid, IntPtr.Zero, IntPtr.Zero, &H12UI)
        If devices = New IntPtr(-1) Then Throw New Win32Exception(Marshal.GetLastWin32Error())
        Try
            Dim index As UInteger = 0
            Do
                Dim entry As New DeviceInterfaceData With {.Size = Marshal.SizeOf(GetType(DeviceInterfaceData))}
                If Not SetupDiEnumDeviceInterfaces(devices, IntPtr.Zero, hidGuid, index, entry) Then
                    Dim code = Marshal.GetLastWin32Error()
                    If code = 259 Then Exit Do 'ERROR_NO_MORE_ITEMS
                    Throw New Win32Exception(code)
                End If
                Dim required As Integer = 0
                SetupDiGetDeviceInterfaceDetail(devices, entry, IntPtr.Zero, 0, required, IntPtr.Zero)
                If required <= 0 Then Throw New Win32Exception(Marshal.GetLastWin32Error())
                Dim buffer = Marshal.AllocHGlobal(required)
                Try
                    Marshal.WriteInt32(buffer, If(IntPtr.Size = 8, 8, 6))
                    If Not SetupDiGetDeviceInterfaceDetail(devices, entry, buffer, required, required, IntPtr.Zero) Then
                        Throw New Win32Exception(Marshal.GetLastWin32Error())
                    End If
                    Dim path = Marshal.PtrToStringUni(IntPtr.Add(buffer, 4))
                    If Not String.IsNullOrEmpty(path) Then paths.Add(path)
                Finally
                    Marshal.FreeHGlobal(buffer)
                End Try
                index += 1UI
            Loop
        Finally
            SetupDiDestroyDeviceInfoList(devices)
        End Try
        Return paths
    End Function

    Friend Shared Sub Log(message As String)
        Diagnostics.Debug.WriteLine(message)
    End Sub

    <StructLayout(LayoutKind.Sequential)>
    Private Structure HidAttributes
        Friend Size As Integer
        Friend VendorId As UShort
        Friend ProductId As UShort
        Friend Version As UShort
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure DeviceInterfaceData
        Friend Size As Integer
        Friend ClassGuid As Guid
        Friend Flags As UInteger
        Friend Reserved As IntPtr
    End Structure

    <DllImport("hid.dll")>
    Private Shared Sub HidD_GetHidGuid(ByRef value As Guid)
    End Sub
    <DllImport("hid.dll", SetLastError:=True)>
    Private Shared Function HidD_GetAttributes(device As SafeFileHandle, ByRef attributes As HidAttributes) As Byte
    End Function
    <DllImport("hid.dll", SetLastError:=True)>
    Private Shared Function HidD_SetOutputReport(device As SafeFileHandle, ByRef report As Byte, length As Integer) As Byte
    End Function
    <DllImport("hid.dll", SetLastError:=True)>
    Private Shared Function HidD_FlushQueue(device As SafeFileHandle) As Byte
    End Function
    <DllImport("kernel32.dll", EntryPoint:="CreateFileW", ExactSpelling:=True, CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function CreateFile(path As String, access As UInteger, share As UInteger, security As IntPtr,
                                       disposition As UInteger, flags As UInteger, template As IntPtr) As SafeFileHandle
    End Function
    <DllImport("setupapi.dll", EntryPoint:="SetupDiGetClassDevsW", ExactSpelling:=True, CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function SetupDiGetClassDevs(ByRef classGuid As Guid, enumerator As IntPtr, parent As IntPtr, flags As UInteger) As IntPtr
    End Function
    <DllImport("setupapi.dll", SetLastError:=True)>
    Private Shared Function SetupDiEnumDeviceInterfaces(devices As IntPtr, info As IntPtr, ByRef classGuid As Guid,
                                                       index As UInteger, ByRef entry As DeviceInterfaceData) As Boolean
    End Function
    <DllImport("setupapi.dll", EntryPoint:="SetupDiGetDeviceInterfaceDetailW", ExactSpelling:=True, CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function SetupDiGetDeviceInterfaceDetail(devices As IntPtr, ByRef entry As DeviceInterfaceData, buffer As IntPtr,
                                                           size As Integer, ByRef required As Integer, info As IntPtr) As Boolean
    End Function
    <DllImport("setupapi.dll", SetLastError:=True)>
    Private Shared Function SetupDiDestroyDeviceInfoList(devices As IntPtr) As Boolean
    End Function
End Class
