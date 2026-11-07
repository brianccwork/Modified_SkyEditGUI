Option Strict On
Option Explicit On
Imports System.IO
Imports System.Threading

'Portal protocol over Windows HID. Replies are matched by command, slot and block;
'reads are bounded and every saved block is read back before committing a record.
Friend NotInheritable Class SimplePortal
    Friend Shared ReadOnly Property IsConnected As Boolean
        Get
            Return PortalTransport.IsOpen
        End Get
    End Property

    'Keeps this shared helper from being instantiated.
    Private Sub New()
    End Sub

    'Closes any previous connection and asks the HID layer to find an asynchronous portal handle.
    Friend Shared Function Connect() As Boolean
        Return PortalTransport.Open()
    End Function

    Friend Shared Sub Disconnect()
        PortalTransport.Close()
    End Sub

    Private Shared Sub Send(report As Byte())
        PortalTransport.Send(report)
    End Sub

    'Resets and activates the portal with bounded delays before reading its slots.
    Private Shared Async Function ActivateAsync(token As CancellationToken) As Task
        Dim report(32) As Byte
        PortalTransport.Flush()
        report(1) = &H52
        Send(report)
        Await Task.Delay(50, token)
        report(1) = &H41
        report(2) = 1
        Send(report)
        Await Task.Delay(500, token)
    End Function

    'Waits for a matching command, slot, and block reply and rejects timeout or failed-read responses.
    Private Shared Async Function ReplyAsync(command As Byte, slot As Integer, block As Integer, token As CancellationToken) As Task(Of Byte())
        Using timeout As CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token)
            timeout.CancelAfter(2500)
            Do
                Dim reply As Byte() = Await PortalTransport.ReadAsync(timeout.Token)
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

    'Requests one 16-byte block from the specified portal slot.
    Private Shared Async Function ReadBlockAsync(slot As Integer, block As Integer, token As CancellationToken) As Task(Of Byte())
        Dim request(32) As Byte
        request(1) = &H51
        request(2) = CByte(&H20 Or slot)
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
        PortalTransport.Flush()
        Dim request(32) As Byte
        request(1) = &H53
        Send(request)
        Using timeout As CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token)
            timeout.CancelAfter(2500)
            Do
                Dim reply As Byte() = Await PortalTransport.ReadAsync(timeout.Token)
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

    'Recognizes the supported Swap Force top-half ID range in the unencrypted header.
    Friend Shared Function IsSwapTop(data As Byte()) As Boolean
        If data Is Nothing OrElse data.Length < 18 Then Return False
        Dim id As Integer = CInt(data(&H10)) Or (CInt(data(&H11)) << 8)
        Return ScannedFigureInfo.IsTopId(id)
    End Function

    'Recognizes the supported Swap Force bottom-half ID range in the unencrypted header.
    Private Shared Function IsSwapBottom(data As Byte()) As Boolean
        If data Is Nothing OrElse data.Length < 18 Then Return False
        Dim id As Integer = CInt(data(&H10)) Or (CInt(data(&H11)) << 8)
        Return ScannedFigureInfo.IsBottomId(id)
    End Function

    'eads the first two blocks used to identify a figure before and during an operation.
    Private Shared Async Function HeaderAsync(slot As Integer, token As CancellationToken) As Task(Of Byte())
        Dim first As Byte() = Await ReadBlockAsync(slot, 0, token)
        Dim second As Byte() = Await ReadBlockAsync(slot, 1, token)
        Return first.Concat(second).ToArray()
    End Function

    'Select the top directly: bottom payload readability must never gate top Gold/Level.
    'Allow a short arrival window when only a bottom or no figure is initially reported.
    Private Shared Async Function SelectSlotAsync(token As CancellationToken) As Task(Of Integer)
        Dim arrival As Diagnostics.Stopwatch = Diagnostics.Stopwatch.StartNew()
        Do
            Dim slots As Integer() = Await PresentSlotsAsync(token)
            If slots.Length = 0 Then
                If arrival.ElapsedMilliseconds < 1200 Then
                    Await Task.Delay(120, token)
                    Continue Do
                End If
                Throw New IOException("No figure was found. Reconnect the portal, place one figure on it, and read again.")
            End If
            Dim headers As New Dictionary(Of Integer, Byte())
            Dim awaitingHeader As Boolean = False
            For Each slot As Integer In slots
                Try
                    headers.Add(slot, Await HeaderAsync(slot, token))
                Catch ex As IOException When arrival.ElapsedMilliseconds < 1200
                    awaitingHeader = True
                    Exit For
                End Try
            Next
            If awaitingHeader Then
                Await Task.Delay(120, token)
                Continue Do
            End If
            Dim tops As Integer() = slots.Where(Function(slot) IsSwapTop(headers(slot))).ToArray()
            If tops.Length = 1 AndAlso (slots.Length = 1 OrElse
               (slots.Length = 2 AndAlso slots.Any(Function(slot) IsSwapBottom(headers(slot))))) Then
                'An assembled swapper may expose two slots. Only the top's full dump is needed.
                Return tops(0)
            End If
            If slots.Length = 1 Then
                If IsSwapBottom(headers(slots(0))) AndAlso arrival.ElapsedMilliseconds < 1200 Then
                    Await Task.Delay(120, token)
                    Continue Do
                End If
                'A bottom-only scan is displayed read-only; every save path still rejects it.
                Return slots(0)
            End If
            Throw New IOException("More than one figure was detected. Leave only one figure or one assembled Swap Force figure on the portal.")
        Loop
    End Function

    'Reads all 64 blocks and checks that the identity stayed the same throughout the scan.
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

    'Activates the portal, resolves a suitable slot, and reads a complete figure dump.
    Friend Shared Async Function ReadFigureAsync(token As CancellationToken) As Task(Of Byte())
        Await ActivateAsync(token)
        Dim slot As Integer = Await SelectSlotAsync(token)
        Return Await ReadSlotAsync(slot, token)
    End Function

    Friend Shared Async Function SaveAsync(original As Byte(), updated As Byte(), token As CancellationToken,
                                          Optional vehicle As Boolean = False) As Task(Of Byte())
        If IsSwapBottom(original) Then Throw New InvalidDataException("Gold and Level must be written to the Swapper Skylander top half.")
        Dim session As New SimpleFigureSession(original, vehicle)
        Dim blocks = session.WriteBlocks(updated)
        'The payload must land before any sequence/checksum headers commit it.
        Dim headers As Integer() = {8, 17, 36, 45}
        blocks = blocks.Where(Function(block) Not headers.Contains(block)).Concat(blocks.Where(Function(block) headers.Contains(block))).ToArray()
        Return Await WriteVerifiedAsync(original, updated, blocks, token)
    End Function

    Friend Shared Async Function SaveCrystalAsync(original As Byte(), updated As Byte(), token As CancellationToken) As Task(Of Byte())
        Dim session As New CrystalSession(original)
        Return Await WriteVerifiedAsync(original, updated, session.WriteBlocks(updated), token)
    End Function

    Friend Shared Async Function SaveTrapAsync(original As Byte(), updated As Byte(), token As CancellationToken) As Task(Of Byte())
        Dim session As New TrapSession(original)
        Return Await WriteVerifiedAsync(original, updated, session.WriteBlocks(updated), token)
    End Function

    'All writers use this transport: same snapshot, identity before every block,
    'acknowledged writes, per-block verification and a final complete readback.
    'The caller owns the figure-specific validation, write surface and commit order.
    Private Shared Async Function WriteVerifiedAsync(original As Byte(), updated As Byte(), blocks As Integer(), token As CancellationToken) As Task(Of Byte())
        original = DirectCast(original.Clone(), Byte())
        updated = DirectCast(updated.Clone(), Byte())
        token.ThrowIfCancellationRequested()
        If original.SequenceEqual(updated) Then Return DirectCast(original.Clone(), Byte())
        For index As Integer = 0 To FigureData.DumpSize - 1
            If original(index) <> updated(index) AndAlso Not blocks.Contains(index \ 16) Then
                Throw New InvalidDataException("The edit would change protected data. Nothing was written.")
            End If
        Next
        Await ActivateAsync(token)
        Dim slot = Await SelectSlotAsync(token)
        Dim current = Await ReadSlotAsync(slot, token)
        If Not current.SequenceEqual(original) Then Throw New IOException("The figure or its data changed. Read it again before saving.")
        For Each block In blocks
            token.ThrowIfCancellationRequested()
            Dim expected = updated.Skip(block * 16).Take(16).ToArray()
            If expected.SequenceEqual(original.Skip(block * 16).Take(16)) Then Continue For
            Dim identity = Await HeaderAsync(slot, token)
            If Not identity.SequenceEqual(original.Take(32)) Then Throw New IOException("The figure changed while saving. Read it again.")
            Dim request(32) As Byte
            request(1) = &H57
            request(2) = CByte(&H20 Or slot)
            request(3) = CByte(block)
            Array.Copy(expected, 0, request, 4, 16)
            token.ThrowIfCancellationRequested()
            Send(request)
            Await ReplyAsync(&H57, slot, block, token)
            Await Task.Delay(100, token)
            Dim actual = Await ReadBlockAsync(slot, block, token)
            If Not actual.SequenceEqual(expected) Then Throw New IOException("Write verification failed. Read the figure again.")
        Next
        Dim verified = Await ReadSlotAsync(slot, token)
        If Not verified.SequenceEqual(updated) Then Throw New IOException("The saved figure did not match the requested changes. Read it again.")
        Return verified
    End Function
End Class
