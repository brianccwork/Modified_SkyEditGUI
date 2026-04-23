Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices

''' <summary>
''' For detecting devices and receiving device notifications.
''' </summary>
Partial Friend NotInheritable Class DeviceManagement

    Const MODULE_NAME As String = "DeviceManagement"

    'Write a line to portal_debug.log.
    'This is used by the portal connection code so I can see exactly
    'what Windows is returning during HID device enumeration.
    Public Shared Sub DebugWrite(ByVal msg As String)
        Try
            Dim debugPath As String = IO.Path.Combine(Application.StartupPath, "portal_debug.log")
            IO.File.AppendAllText(
                debugPath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") & " | " & msg & Environment.NewLine)
        Catch
            'ignore logging failures so it doesnt stop program
        End Try
    End Sub

    ''' <summary>
    ''' Compares two device path names.
    ''' </summary>
    Friend Function DeviceNameMatch _
     (ByVal m As Message,
     ByVal mydevicePathName As String) _
     As Boolean

        Dim deviceNameString As String = ""
        Dim stringSize As Int32

        Try
            Dim devBroadcastDeviceInterface As New DEV_BROADCAST_DEVICEINTERFACE_1()
            Dim devBroadcastHeader As New DEV_BROADCAST_HDR()

            Marshal.PtrToStructure(m.LParam, devBroadcastHeader)

            If (devBroadcastHeader.dbch_devicetype = DBT_DEVTYP_DEVICEINTERFACE) Then

                stringSize = Convert.ToInt32((devBroadcastHeader.dbch_size - 32) / 2)

                Array.Resize(devBroadcastDeviceInterface.dbcc_name, stringSize)

                Marshal.PtrToStructure(m.LParam, devBroadcastDeviceInterface)

                deviceNameString = New String(devBroadcastDeviceInterface.dbcc_name, 0, stringSize)

                If (String.Compare(deviceNameString, mydevicePathName, True) = 0) Then
                    Return True
                Else
                    Return False
                End If
            End If

        Catch
            Throw
        End Try

        Return False
    End Function

    ''' <summary>
    ''' Use SetupDi API functions to retrieve the device path name of an
    ''' attached device that belongs to a device interface class.
    ''' Also added a bunch of logging to help debug portal connection issues
    ''' </summary>
    Friend Function FindDeviceFromGuid _
     (ByVal myGuid As Guid,
     ByRef devicePathName() As String) _
     As Boolean

        Dim bufferSize As Int32 = 0
        Dim detailDataBuffer As IntPtr = IntPtr.Zero
        Dim deviceFound As Boolean = False
        Dim deviceInfoSet As IntPtr = IntPtr.Zero
        Dim lastDevice As Boolean = False
        Dim memberIndex As Int32 = 0
        Dim myDeviceInterfaceData As SP_DEVICE_INTERFACE_DATA
        Dim pdevicePathName As IntPtr = IntPtr.Zero
        Dim success As Boolean

        Try
            'Log the start of HID enumeration.
            DebugWrite("FindDeviceFromGuid() started")

            deviceInfoSet = SetupDiGetClassDevs(
                myGuid,
                IntPtr.Zero,
                IntPtr.Zero,
                DIGCF_PRESENT Or DIGCF_DEVICEINTERFACE)

            If deviceInfoSet = IntPtr.Zero OrElse deviceInfoSet.ToInt64 = -1 Then
                DebugWrite("SetupDiGetClassDevs failed or returned invalid handle")
                Return False
            End If

            myDeviceInterfaceData.cbSize = Marshal.SizeOf(myDeviceInterfaceData)

            Do
                success = SetupDiEnumDeviceInterfaces(
                    deviceInfoSet,
                    IntPtr.Zero,
                    myGuid,
                    memberIndex,
                    myDeviceInterfaceData)

                If Not success Then
                    'No more matching HID devices were found.
                    lastDevice = True
                    DebugWrite("SetupDiEnumDeviceInterfaces finished at memberIndex=" & memberIndex.ToString())
                Else
                    bufferSize = 0

                    SetupDiGetDeviceInterfaceDetail(
                        deviceInfoSet,
                        myDeviceInterfaceData,
                        IntPtr.Zero,
                        0,
                        bufferSize,
                        IntPtr.Zero)

                    'Allocate the buffer that will hold SP_DEVICE_INTERFACE_DETAIL_DATA.
                    detailDataBuffer = Marshal.AllocHGlobal(bufferSize)

                    'Write the cbSize value in a way that works for the current process architecture.
                    Marshal.WriteInt32(detailDataBuffer, If(IntPtr.Size = 8, 8, 6))

                    success = SetupDiGetDeviceInterfaceDetail(
                        deviceInfoSet,
                        myDeviceInterfaceData,
                        detailDataBuffer,
                        bufferSize,
                        bufferSize,
                        IntPtr.Zero)

                    If success Then
                        'The first 4 bytes are the cbSize field.
                        'The device path string begins immediately after that.
                        pdevicePathName = IntPtr.Add(detailDataBuffer, 4)

                        If devicePathName Is Nothing Then
                            ReDim devicePathName(0)
                        ElseIf memberIndex >= devicePathName.Length Then
                            ReDim Preserve devicePathName(memberIndex)
                        End If

                        devicePathName(memberIndex) = Marshal.PtrToStringAuto(pdevicePathName)

                        If Not String.IsNullOrEmpty(devicePathName(memberIndex)) Then
                            'Log every HID path that Windows returns so we can see
                            'whether the portal is being exposed as a HID device.
                            DebugWrite("HID ENUM: " & devicePathName(memberIndex))
                            deviceFound = True
                        Else
                            DebugWrite("HID ENUM: [empty path at index " & memberIndex.ToString() & "]")
                        End If
                    Else
                        DebugWrite("SetupDiGetDeviceInterfaceDetail failed at memberIndex=" & memberIndex.ToString())
                    End If

                    If detailDataBuffer <> IntPtr.Zero Then
                        Marshal.FreeHGlobal(detailDataBuffer)
                        detailDataBuffer = IntPtr.Zero
                    End If
                End If

                memberIndex += 1

            Loop Until (lastDevice = True)

            DebugWrite("FindDeviceFromGuid() returning " & deviceFound.ToString())
            Return deviceFound

        Catch ex As Exception
            DebugWrite("FindDeviceFromGuid() exception: " & ex.ToString())
            Throw

        Finally
            If detailDataBuffer <> IntPtr.Zero Then
                Marshal.FreeHGlobal(detailDataBuffer)
            End If

            If Not (deviceInfoSet = IntPtr.Zero) Then
                SetupDiDestroyDeviceInfoList(deviceInfoSet)
            End If
        End Try
    End Function

    ''' <summary>
    ''' Requests to receive a notification when a device is attached or removed.
    ''' </summary>
    Friend Function RegisterForDeviceNotifications _
     (ByVal devicePathName As String,
     ByVal formHandle As IntPtr,
     ByVal classGuid As Guid,
     ByRef deviceNotificationHandle As IntPtr) _
     As Boolean

        Dim devBroadcastDeviceInterface As DEV_BROADCAST_DEVICEINTERFACE =
            New DEV_BROADCAST_DEVICEINTERFACE()
        Dim devBroadcastDeviceInterfaceBuffer As IntPtr
        Dim size As Int32

        Try
            size = Marshal.SizeOf(devBroadcastDeviceInterface)
            devBroadcastDeviceInterface.dbcc_size = size
            devBroadcastDeviceInterface.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE
            devBroadcastDeviceInterface.dbcc_reserved = 0
            devBroadcastDeviceInterface.dbcc_classguid = classGuid

            devBroadcastDeviceInterfaceBuffer = Marshal.AllocHGlobal(size)

            Marshal.StructureToPtr(devBroadcastDeviceInterface, devBroadcastDeviceInterfaceBuffer, True)

            deviceNotificationHandle = RegisterDeviceNotification(
                formHandle,
                devBroadcastDeviceInterfaceBuffer,
                DEVICE_NOTIFY_WINDOW_HANDLE)

            Marshal.PtrToStructure(devBroadcastDeviceInterfaceBuffer, devBroadcastDeviceInterface)

            'NOTE Maybe use this to not have an error (if an error shows up in future)
            'If deviceNotificationHandle = IntPtr.Zero Then
            If (deviceNotificationHandle.ToInt32 = IntPtr.Zero.ToInt32) Then
                Return False
            Else
                Return True
            End If

        Catch
            Throw

        Finally
            If Not (devBroadcastDeviceInterfaceBuffer = IntPtr.Zero) Then
                Marshal.FreeHGlobal(devBroadcastDeviceInterfaceBuffer)
            End If
        End Try
    End Function

    ''' <summary>
    ''' Requests to stop receiving notification messages.
    ''' </summary>
    Friend Sub StopReceivingDeviceNotifications _
     (ByVal deviceNotificationHandle As IntPtr)

        Try
            UnregisterDeviceNotification(deviceNotificationHandle)
        Catch
            Throw
        End Try
    End Sub

End Class