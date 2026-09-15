Option Strict On
Option Explicit On

Imports System.IO
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

'Uses the existing portal discovery and HID report format. No background worker
'touches the default Developer form. Requests are bounded and replies checked.
Friend NotInheritable Class SimplePortal
    'Only the two established vehicle payload regions; no identity/access blocks.
    Friend Shared ReadOnly Property VehicleBlocks As Integer()
        Get
            Return Enumerable.Range(8, 14).Concat(Enumerable.Range(36, 14)).Where(Function(block) block Mod 4 <> 3).ToArray()
        End Get
    End Property

    Private Sub New()
    End Sub

    Friend Shared Function Connect() As Boolean
        Disconnect()
        frmMain.EnsureEditorInitialized()
        Portal.portalHandle = hidControl.FindThePortal(True)
        Return Portal.blnPortal AndAlso Portal.portalHandle IsNot Nothing AndAlso
               Not Portal.portalHandle.IsInvalid AndAlso Not Portal.portalHandle.IsClosed
    End Function

    Friend Shared Sub Disconnect()
        hidControl.CloseCommunications(Portal.portalHandle)
        Portal.blnPortal = False
        Portal.BlnPortalUsed = False
    End Sub

    Private Shared Sub Send(report As Byte())
        If Not Portal.blnPortal OrElse Portal.portalHandle Is Nothing OrElse
           Portal.portalHandle.IsInvalid OrElse Portal.portalHandle.IsClosed Then
            Throw New IOException("Connect a portal first.")
        End If
        If Not Hid.HidD_SetOutputReport(Portal.portalHandle, report(0), report.Length) Then
            Throw New IOException("The portal did not accept the request. Reconnect it and try again.")
        End If
    End Sub

    Private Shared Async Function ActivateAsync(token As CancellationToken) As Task
        Dim report(32) As Byte
        hidControl.flushHid(Portal.portalHandle)
        report(1) = &H52
        Send(report)
        Await Task.Delay(50, token)
        report(1) = &H41
        report(2) = 1
        Send(report)
        Await Task.Delay(500, token)
    End Function

    Private Shared Async Function ReplyAsync(command As Byte, block As Integer, token As CancellationToken) As Task(Of Byte())
        Using timeout As CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token)
            timeout.CancelAfter(2500)
            Do
                Dim reply As Byte() = Await hidControl.ReadReportAsync(timeout.Token)
                If reply(1) <> command OrElse reply(3) <> block Then Continue Do
                If (reply(2) And &HF) <> 0 Then Continue Do
                If (reply(2) And &H10) = 0 Then
                    Throw New IOException("No readable figure was found. Place one figure on the portal and try again.")
                End If
                Return reply
            Loop
        End Using
    End Function

    Private Shared Async Function ReadBlockAsync(block As Integer, token As CancellationToken) As Task(Of Byte())
        Dim request(32) As Byte
        request(1) = &H51
        request(2) = &H20
        request(3) = CByte(block)
        Send(request)
        Dim reply As Byte() = Await ReplyAsync(&H51, block, token)
        Dim data(15) As Byte
        Array.Copy(reply, 4, data, 0, 16)
        Return data
    End Function

    Friend Shared Async Function ReadFigureAsync(token As CancellationToken) As Task(Of Byte())
        Await ActivateAsync(token)
        Dim data(1023) As Byte
        For block As Integer = 0 To 63
            Dim bytes As Byte() = Await ReadBlockAsync(block, token)
            Array.Copy(bytes, 0, data, block * 16, 16)
        Next
        'Reject a figure swap/removal partway through a read.
        Dim identity As Byte() = Await ReadBlockAsync(0, token)
        If Not identity.SequenceEqual(data.Take(16)) Then Throw New IOException("The figure changed while reading. Read it again.")
        Return data
    End Function

    Friend Shared Async Function SaveAsync(original As Byte(), updated As Byte(), token As CancellationToken, Optional vehicle As Boolean = False) As Task(Of Byte())
        If original.Length <> 1024 OrElse updated.Length <> 1024 Then Throw New InvalidDataException("Invalid figure data.")
        Dim current As Byte() = Await ReadFigureAsync(token)
        If Not current.SequenceEqual(original) Then
            Throw New IOException("The figure or its data changed. Read the figure again before saving.")
        End If

        'Only these four blocks contain the existing Gold/EXP fields and their
        'mirrored sequence/checksum bytes. Never write identity or signature data.
        Dim allowed As Integer() = If(vehicle, VehicleBlocks, New Integer() {8, 17, 36, 45})
        'Commit sequence/header blocks last, after their associated payload is verified.
        If vehicle Then allowed = allowed.Where(Function(block) block <> 8 AndAlso block <> 36).Concat(New Integer() {8, 36}).ToArray()
        For index As Integer = 0 To 1023
            If original(index) <> updated(index) AndAlso Not allowed.Contains(index \ 16) Then
                Throw New InvalidDataException("The edit would affect data outside the permitted editor blocks. Nothing was written.")
            End If
        Next

        For Each block As Integer In allowed
            Dim expected As Byte() = updated.Skip(block * 16).Take(16).ToArray()
            If expected.SequenceEqual(original.Skip(block * 16).Take(16)) Then Continue For
            Dim identity As Byte() = Await ReadBlockAsync(0, token)
            If Not identity.SequenceEqual(original.Take(16)) Then Throw New IOException("The figure was removed or changed while saving.")
            Dim request(32) As Byte
            request(1) = &H57
            request(2) = &H20
            request(3) = CByte(block)
            Array.Copy(expected, 0, request, 4, 16)
            Send(request)
            Await ReplyAsync(&H57, block, token)
            Await Task.Delay(100, token)
            Dim actual As Byte() = Await ReadBlockAsync(block, token)
            If Not actual.SequenceEqual(expected) Then Throw New IOException("The portal could not verify the saved data. Read the figure again.")
        Next
        Dim verified As Byte() = Await ReadFigureAsync(token)
        If Not verified.SequenceEqual(updated) Then Throw New IOException("The saved figure did not match the requested changes. Read it again.")
        Return verified
    End Function
End Class
