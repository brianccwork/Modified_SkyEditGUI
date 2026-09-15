Option Strict On
Option Explicit On

Imports System.Collections.Generic
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

    Private Shared Async Function ReplyAsync(command As Byte, slot As Integer, block As Integer, token As CancellationToken) As Task(Of Byte())
        Using timeout As CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token)
            timeout.CancelAfter(2500)
            Do
                Dim reply As Byte() = Await hidControl.ReadReportAsync(timeout.Token)
                If reply Is Nothing OrElse reply.Length < 20 Then Continue Do
                If reply(1) <> command OrElse reply(3) <> block Then Continue Do
                If (reply(2) And &HF) <> slot Then Continue Do
                If (reply(2) And &H10) = 0 Then
                    Throw New IOException("No readable figure was found. Place one figure on the portal and try again.")
                End If
                Return reply
            Loop
        End Using
    End Function

    Private Shared Async Function ReadBlockAsync(slot As Integer, block As Integer, token As CancellationToken) As Task(Of Byte())
        Dim request(32) As Byte
        request(1) = &H51
        request(2) = CByte(slot)
        request(3) = CByte(block)
        Send(request)
        Dim reply As Byte() = Await ReplyAsync(&H51, slot, block, token)
        Dim data(15) As Byte
        Array.Copy(reply, 4, data, 0, 16)
        Return data
    End Function

    'S reports contain 16 two-bit presence fields, starting after the report ID
    'and command. Present=01, arriving=11; absent=00, departed=10.
    Private Shared Async Function PresentSlotsAsync(token As CancellationToken) As Task(Of Integer())
        hidControl.flushHid(Portal.portalHandle)
        Dim request(32) As Byte
        request(1) = &H53
        Send(request)
        Using timeout As CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token)
            timeout.CancelAfter(2500)
            Do
                Dim reply As Byte() = Await hidControl.ReadReportAsync(timeout.Token)
                If reply Is Nothing OrElse reply.Length < 6 OrElse reply(1) <> &H53 Then Continue Do
                Dim slots As New List(Of Integer)
                For slot As Integer = 0 To 15
                    Dim presence As Integer = (CInt(reply(2 + slot \ 4)) >> ((slot Mod 4) * 2)) And 3
                    If (presence And 1) <> 0 Then slots.Add(slot)
                Next
                Return slots.ToArray()
            Loop
        End Using
    End Function

    Friend Shared Function IsSwapTop(data As Byte()) As Boolean
        Dim id As Integer = CInt(data(&H10)) Or (CInt(data(&H11)) << 8)
        Return id >= 2000 AndAlso id <= 2015
    End Function

    Private Shared Function IsSwapBottom(data As Byte()) As Boolean
        Dim id As Integer = CInt(data(&H10)) Or (CInt(data(&H11)) << 8)
        Return id >= 1000 AndAlso id <= 1015
    End Function

    Private Shared Async Function HeaderAsync(slot As Integer, token As CancellationToken) As Task(Of Byte())
        Dim first As Byte() = Await ReadBlockAsync(slot, 0, token)
        Dim second As Byte() = Await ReadBlockAsync(slot, 1, token)
        Return first.Concat(second).ToArray()
    End Function

    Private Shared Async Function SelectSlotAsync(token As CancellationToken) As Task(Of Integer)
        Dim slots As Integer() = Await PresentSlotsAsync(token)
        If slots.Length = 0 Then Throw New IOException("No figure was found. Place one figure on the portal and read again.")
        Dim headers As New Dictionary(Of Integer, Byte())
        For Each slot As Integer In slots
            headers.Add(slot, Await HeaderAsync(slot, token))
        Next
        Dim tops As Integer() = slots.Where(Function(slot) IsSwapTop(headers(slot))).ToArray()
        If tops.Length = 1 AndAlso (slots.Length = 1 OrElse
           (slots.Length = 2 AndAlso slots.Any(Function(slot) IsSwapBottom(headers(slot))))) Then
            Return tops(0)
        End If
        If slots.Length = 1 AndAlso Not IsSwapBottom(headers(slots(0))) Then Return slots(0)
        If slots.All(Function(slot) IsSwapBottom(headers(slot))) Then
            Throw New IOException("Only a Swap Force bottom half was detected. Attach the top half and read again. Gold, XP and Level are stored on the top half.")
        End If
        Throw New IOException("More than one figure was detected. Leave only one figure (or one assembled Swap Force figure) on the portal and read again.")
    End Function

    Private Shared Async Function ReadSlotAsync(slot As Integer, token As CancellationToken) As Task(Of Byte())
        Dim header As Byte() = Await HeaderAsync(slot, token)
        Dim data(1023) As Byte
        For block As Integer = 0 To 63
            Dim bytes As Byte() = Await ReadBlockAsync(slot, block, token)
            Array.Copy(bytes, 0, data, block * 16, 16)
        Next
        Dim finalHeader As Byte() = Await HeaderAsync(slot, token)
        If Not header.SequenceEqual(data.Take(32)) OrElse Not header.SequenceEqual(finalHeader) Then
            Throw New IOException("The figure changed while reading. Read it again.")
        End If
        Return data
    End Function

    Friend Shared Async Function ReadFigureAsync(token As CancellationToken) As Task(Of Byte())
        Await ActivateAsync(token)
        Dim slot As Integer = Await SelectSlotAsync(token)
        Return Await ReadSlotAsync(slot, token)
    End Function

    Friend Shared Async Function SaveAsync(original As Byte(), updated As Byte(), token As CancellationToken, Optional vehicle As Boolean = False) As Task(Of Byte())
        If original Is Nothing OrElse updated Is Nothing OrElse original.Length <> 1024 OrElse updated.Length <> 1024 Then Throw New InvalidDataException("Invalid figure data.")
        If IsSwapBottom(original) Then Throw New InvalidDataException("Gold, XP and Level must be written to the Swap Force top half.")
        'Resolve the current slot again. Slot numbers may change across activation.
        'Do not reset or change slots after this preflight, including verification.
        Await ActivateAsync(token)
        Dim slot As Integer = Await SelectSlotAsync(token)
        Dim current As Byte() = Await ReadSlotAsync(slot, token)
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
            Dim identity As Byte() = Await HeaderAsync(slot, token)
            If Not identity.SequenceEqual(original.Take(32)) Then Throw New IOException("The figure was removed or changed while saving.")
            Dim request(32) As Byte
            request(1) = &H57
            request(2) = CByte(slot)
            request(3) = CByte(block)
            Array.Copy(expected, 0, request, 4, 16)
            Send(request)
            Await ReplyAsync(&H57, slot, block, token)
            Await Task.Delay(100, token)
            Dim actual As Byte() = Await ReadBlockAsync(slot, block, token)
            If Not actual.SequenceEqual(expected) Then Throw New IOException("The portal could not verify the saved data. Read the figure again.")
        Next
        Dim verified As Byte() = Await ReadSlotAsync(slot, token)
        If Not verified.SequenceEqual(updated) Then Throw New IOException("The saved figure did not match the requested changes. Read it again.")
        Return verified
    End Function
End Class
