Option Explicit On

Imports Microsoft.Win32.SafeHandles
Imports SkyReader_GUI.Hid
Imports SkyReader_GUI.FileIO
Imports System.Runtime.InteropServices
Imports System.IO

Module hidControl

    Dim deviceStream As FileStream
    Dim MyHid As New Hid()
    Dim MyDeviceManagement As New DeviceManagement()
    Dim deviceNotificationHandle As IntPtr
    Dim myDevicePathName As String

    Const reportSize = 33

    Public Function FindThePortal() As SafeFileHandle

        Dim deviceFound As Boolean
        Dim devicePathName(127) As String
        Dim hidGuid As Guid
        Dim memberIndex As Int32
        Dim success As Boolean
        Dim preparsedData As IntPtr
        Dim myDeviceDetected As Boolean
        Dim hidHandle As SafeFileHandle = Nothing

        myDeviceDetected = False

        Try
            HidD_GetHidGuid(hidGuid)
            DeviceManagement.DebugWrite("FindThePortal() started")

            deviceFound = MyDeviceManagement.FindDeviceFromGuid(hidGuid, devicePathName)

            If deviceFound Then
                memberIndex = 0

                Do While memberIndex < devicePathName.Length AndAlso Not myDeviceDetected

                    If String.IsNullOrEmpty(devicePathName(memberIndex)) Then
                        memberIndex += 1
                        Continue Do
                    End If

                    Dim upperPath As String = devicePathName(memberIndex).ToUpperInvariant()
                    DeviceManagement.DebugWrite("Candidate HID Path: " & upperPath)

                    If upperPath.Contains("VID_1430") AndAlso upperPath.Contains("PID_0150") Then

                        ' IMPORTANT:
                        ' Some systems fail when opening the HID path with zero desired access.
                        ' Open it directly with read/write sharing.
                        hidHandle = CreateFile(
                            devicePathName(memberIndex),
                            GENERIC_READ Or GENERIC_WRITE,
                            FILE_SHARE_READ Or FILE_SHARE_WRITE,
                            IntPtr.Zero,
                            OPEN_EXISTING,
                            0,
                            IntPtr.Zero)

                        If Not (hidHandle Is Nothing) AndAlso Not hidHandle.IsInvalid Then
                            DeviceManagement.DebugWrite("CreateFile (query handle) succeeded")

                            MyHid.DeviceAttributes.Size = Marshal.SizeOf(MyHid.DeviceAttributes)
                            success = HidD_GetAttributes(hidHandle, MyHid.DeviceAttributes)

                            If success Then
                                DeviceManagement.DebugWrite(
                                    "Attributes VID=" & MyHid.DeviceAttributes.VendorID.ToString() &
                                    " PID=" & MyHid.DeviceAttributes.ProductID.ToString())

                                If MyHid.DeviceAttributes.VendorID = 5168 AndAlso MyHid.DeviceAttributes.ProductID = 336 Then
                                    myDeviceDetected = True
                                    myDevicePathName = devicePathName(memberIndex)
                                    DeviceManagement.DebugWrite("Portal candidate accepted: " & myDevicePathName)
                                Else
                                    DeviceManagement.DebugWrite("Rejected candidate due to VID/PID mismatch after open")
                                    hidHandle.Close()
                                End If
                            Else
                                DeviceManagement.DebugWrite("HidD_GetAttributes failed. LastError=" & Marshal.GetLastWin32Error().ToString())
                                hidHandle.Close()
                            End If
                        Else
                            DeviceManagement.DebugWrite("CreateFile (query handle) failed. LastError=" & Marshal.GetLastWin32Error().ToString())
                        End If
                    Else
                        DeviceManagement.DebugWrite("Rejected HID path because it is not VID_1430/PID_0150")
                    End If

                    memberIndex += 1
                Loop
            Else
                DeviceManagement.DebugWrite("FindDeviceFromGuid returned False")
            End If

            If myDeviceDetected Then
                MyDeviceManagement.RegisterForDeviceNotifications(
                    myDevicePathName,
                    frmMain.Handle,
                    hidGuid,
                    deviceNotificationHandle)

                HidD_GetPreparsedData(hidHandle, preparsedData)
                HidP_GetCaps(preparsedData, MyHid.Capabilities)

                If Not (preparsedData = IntPtr.Zero) Then
                    HidD_FreePreparsedData(preparsedData)
                End If

                If Not (hidHandle Is Nothing) AndAlso Not hidHandle.IsInvalid Then
                    hidHandle.Close()
                End If

                DeviceManagement.DebugWrite("Re-opening portal with read/write access: " & myDevicePathName)

                hidHandle = CreateFile(
                    myDevicePathName,
                    GENERIC_READ Or GENERIC_WRITE,
                    FILE_SHARE_READ Or FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    0,
                    IntPtr.Zero)

                If hidHandle Is Nothing OrElse hidHandle.IsInvalid Then
                    DeviceManagement.DebugWrite("CreateFile (read/write handle) failed. LastError=" & Marshal.GetLastWin32Error().ToString())
                    frmMain.lockPortalControls()
                    frmMain.SaldeStatus.Text = "Portal Open Failed!"
                    Portal.blnPortal = False
                    Return hidHandle
                End If

                DeviceManagement.DebugWrite("CreateFile (read/write handle) succeeded")

                deviceStream = New FileStream(hidHandle, FileAccess.ReadWrite, reportSize, False)

                HidD_FlushQueue(hidHandle)

                frmMain.unlockPortalControls()
                frmMain.SaldeStatus.Text = "Portal Connected!"
                Portal.blnPortal = True

                DeviceManagement.DebugWrite("Portal connected successfully")
            Else
                frmMain.lockPortalControls()
                frmMain.SaldeStatus.Text = "Portal Not Found!"
                Portal.blnPortal = False
                DeviceManagement.DebugWrite("Portal not found after scanning candidates")
            End If

            Return hidHandle

        Catch ex As Exception
            DeviceManagement.DebugWrite("FindThePortal() exception: " & ex.ToString())
            frmMain.lockPortalControls()
            frmMain.SaldeStatus.Text = "Portal Error!"
            Portal.blnPortal = False
            Throw
        End Try
    End Function

    Public Sub outputReport(ByVal hidHandle As SafeFileHandle, ByRef outReport As Byte())
        DeviceManagement.DebugWrite("outputReport() called")
        HidD_SetOutputReport(hidHandle, outReport(0), reportSize)
    End Sub

    Public Sub inputReport(ByVal hidHandle As SafeFileHandle, ByRef inReport As Byte())
        If Not (deviceStream Is Nothing) AndAlso deviceStream.CanRead Then
            deviceStream.Read(inReport, 0, reportSize)
        End If
    End Sub

    Public Sub flushHid(ByVal hidHandle As SafeFileHandle)
        HidD_FlushQueue(hidHandle)
    End Sub

    Public Function checkDevice(ByRef m As Message) As Boolean
        Try
            If String.IsNullOrEmpty(myDevicePathName) Then
                Return False
            End If

            Return MyDeviceManagement.DeviceNameMatch(m, myDevicePathName)

        Catch ex As Exception
            DeviceManagement.DebugWrite("checkDevice() exception: " & ex.ToString())
            Return False
        End Try
    End Function

    Public Sub CloseCommunications(ByRef hidHandle As SafeFileHandle)

        Try
            DeviceManagement.DebugWrite("CloseCommunications() called")

            If Not (deviceStream Is Nothing) Then
                deviceStream.Close()
            End If

            If Not (hidHandle Is Nothing) Then
                If Not hidHandle.IsInvalid Then
                    hidHandle.Close()
                End If
            End If
        Catch ex As Exception
            DeviceManagement.DebugWrite("CloseCommunications() exception: " & ex.ToString())
        End Try
    End Sub

End Module