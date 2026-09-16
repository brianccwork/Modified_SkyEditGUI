Option Strict On
Option Explicit On
Imports System.IO
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text

'Independent VB implementation of the trap save layout documented by Portal-To-Unity/Revolve.
Friend NotInheritable Class TrapSession
    Private ReadOnly raw As Byte()
    Private ReadOnly region As Byte()
    Private ReadOnly activeArea As Integer
    Friend ReadOnly Property TrapId As Integer
    Friend ReadOnly Property TrapName As String
    Friend ReadOnly Property VillainId As Integer
    Friend ReadOnly Property Evolved As Boolean
    Friend ReadOnly Property IsVariant As Boolean
    Friend ReadOnly Property CanWrite As Boolean
    Friend ReadOnly Property Notice As String
    Friend ReadOnly Property Original As Byte()
        Get
            Return DirectCast(raw.Clone(), Byte())
        End Get
    End Property
    Friend ReadOnly Property VillainName As String
        Get
            Return TrapCatalog.VillainName(VillainId, IsVariant)
        End Get
    End Property

    Friend Sub New(bytes As Byte())
        If bytes Is Nothing OrElse bytes.Length <> 1024 Then Throw New InvalidDataException("Read a complete 1024-byte trap first.")
        raw = DirectCast(bytes.Clone(), Byte())
        TrapId = Word(raw, 16)
        If TrapId < 210 OrElse TrapId > 220 Then Throw New InvalidDataException("This figure is not a Trap Team trap. Place a trap in the portal's trap slot.")
        TrapName = TrapCatalog.TrapName(TrapId, Word(raw, 28))
        If (raw(0) Xor raw(1) Xor raw(2) Xor raw(3)) <> raw(4) OrElse Crc(raw.Take(30).ToArray()) <> Word(raw, 30) Then
            Throw New InvalidDataException("The trap's serial or header is invalid. Writing is disabled.")
        End If
        For block As Integer = 3 To 63 Step 4
            Dim at As Integer = block * 16 + 6
            If (raw(at + 1) >> 4) <> ((raw(at) And 15) Xor 15) OrElse
               (raw(at + 2) And 15) <> ((raw(at) >> 4) Xor 15) OrElse
               (raw(at + 2) >> 4) <> ((raw(at + 1) And 15) Xor 15) Then
                Throw New InvalidDataException("The trap's access-control data is invalid. Writing is disabled.")
            End If
        Next
        Dim selected As Byte() = Nothing
        Dim selectedArea As Integer = 0
        Dim complete As Boolean = False
        For Each encrypted As Boolean In New Boolean() {True, False}
            Dim a As Byte() = ReadRegion(raw, 0, encrypted)
            Dim b As Byte() = ReadRegion(raw, 1, encrypted)
            Dim goodA As Boolean = ValidRegion(a)
            Dim goodB As Boolean = ValidRegion(b)
            If goodA OrElse goodB Then
                selectedArea = If(goodB AndAlso (Not goodA OrElse ((CInt(a(9)) + 1) And 255) = b(9)), 1, 0)
                selected = If(selectedArea = 0, a, b)
                complete = True
                If goodA <> goodB Then Notice = "Using the valid save area; the other area will be refreshed when you save."
                Exit For
            End If
            If selected Is Nothing Then
                If ValidPrimary(a) Then
                    selected = a : selectedArea = 0
                ElseIf ValidPrimary(b) Then
                    selected = b : selectedArea = 1
                End If
            End If
        Next
        If selected Is Nothing Then
            If ReadRegion(raw, 0, False).All(Function(v) v = 0) AndAlso ReadRegion(raw, 1, False).All(Function(v) v = 0) Then
                selected = New Byte(335) {}
                complete = True
                Notice = "Empty, uninitialized trap. Assign a compatible villain to create its first save."
            Else
                Throw New InvalidDataException("Neither trap save area could be verified. Restore a backup or initialize the trap in Trap Team before editing.")
            End If
        End If
        region = DirectCast(selected.Clone(), Byte())
        activeArea = selectedArea
        VillainId = region(16)
        Evolved = region(17) = 1
        IsVariant = VillainId <> 0 AndAlso region(7) = VillainId AndAlso TrapCatalog.Variants.ContainsKey(VillainId)
        CanWrite = complete AndAlso (VillainId = 0 OrElse TrapCatalog.Compatible(VillainId, TrapId)) AndAlso region(17) <= 1
        If Not CanWrite Then Notice = "The primary villain is readable, but cached data or villain metadata is invalid. This trap is read-only."
    End Sub

    Friend Function BuildEvolution(evolve As Boolean) As Byte()
        If Not CanWrite OrElse VillainId = 0 Then Throw New InvalidOperationException("Read a valid trap with a captured villain before evolving it.")
        Dim edited As Byte() = DirectCast(region.Clone(), Byte())
        edited(17) = If(evolve, CByte(1), CByte(0))
        Return BuildRegion(edited)
    End Function

    'Universal save preserves the existing record when only flags change.
    Friend Function BuildSelection(id As Integer, variantValue As Boolean, evolve As Boolean) As Byte()
        If id <> VillainId Then Return BuildAssignment(id, variantValue, evolve)
        If variantValue = IsVariant Then Return BuildEvolution(evolve)
        If Not TrapFeatures.AllowAssignment OrElse Not CanWrite Then Throw New InvalidOperationException("Variant editing is unavailable.")
        If Not TrapCatalog.Compatible(id, TrapId) OrElse (variantValue AndAlso Not TrapCatalog.Variants.ContainsKey(id)) Then
            Throw New InvalidOperationException("Choose a compatible villain and variant.")
        End If
        Dim edited As Byte() = DirectCast(region.Clone(), Byte())
        edited(17) = If(evolve, CByte(1), CByte(0))
        edited(0) = If(variantValue, CByte(1), CByte(0))
        edited(7) = If(variantValue, CByte(id), CByte(0))
        Return BuildRegion(edited)
    End Function

    Friend Function BuildAssignment(id As Integer, variantValue As Boolean, evolve As Boolean) As Byte()
        If Not TrapFeatures.AllowAssignment Then Throw New InvalidOperationException("Villain assignment is not included in the evolution-only update.")
        If Not CanWrite Then Throw New InvalidOperationException("Read a valid or empty trap before assigning a villain.")
        If Not TrapCatalog.Compatible(id, TrapId) Then Throw New InvalidOperationException("Choose a villain matching this trap's element.")
        If variantValue AndAlso Not TrapCatalog.Variants.ContainsKey(id) Then Throw New InvalidOperationException("This villain has no supported variant.")
        Dim edited As Byte() = DirectCast(region.Clone(), Byte())
        'Replace the primary record only. Preserve cached villains, their nicknames and unknown data.
        Array.Clear(edited, 16, 48)
        edited(16) = CByte(id)
        edited(17) = If(evolve, CByte(1), CByte(0))
        edited(0) = If(variantValue, CByte(1), CByte(0))
        edited(7) = If(variantValue, CByte(id), CByte(0))
        Dim ids As New Collections.Generic.HashSet(Of Byte) From {CByte(id)}
        For Each offset As Integer In New Integer() {64, 112, 160, 208, 240}
            If edited(offset) <> 0 Then ids.Add(edited(offset))
        Next
        edited(1) = CByte(ids.Count)
        Return BuildRegion(edited)
    End Function

    Private Function BuildRegion(edited As Byte()) As Byte()
        edited(9) = CByte((CInt(region(9)) + 1) And 255)
        PutWord(edited, 10, Crc(edited.Skip(64).ToArray()))
        PutWord(edited, 12, Crc(edited.Skip(16).Take(48).ToArray()))
        Dim header As Byte() = edited.Take(16).ToArray()
        header(14) = 5 : header(15) = 0
        PutWord(edited, 14, Crc(header))
        Dim result As Byte() = Original
        Dim blocks As Integer() = AreaBlocks(1 - activeArea)
        For i As Integer = 0 To blocks.Length - 1
            Dim encoded As Byte() = Crypt(raw, blocks(i), edited.Skip(i * 16).Take(16).ToArray(), True)
            Array.Copy(encoded, 0, result, blocks(i) * 16, 16)
        Next
        Dim check As New TrapSession(result)
        If Not check.CanWrite OrElse check.VillainId <> edited(16) OrElse check.Evolved <> (edited(17) = 1) Then
            Throw New InvalidDataException("The prepared trap save did not verify. Nothing was written.")
        End If
        Return result
    End Function

    Friend Function WriteBlocks(updated As Byte()) As Integer()
        If updated Is Nothing OrElse updated.Length <> 1024 Then Throw New InvalidDataException("Invalid trap save.")
        Dim allowed As Integer() = AreaBlocks(1 - activeArea)
        For i As Integer = 0 To 1023
            If updated(i) <> raw(i) AndAlso Not allowed.Contains(i \ 16) Then Throw New InvalidDataException("The edit would change protected trap data.")
        Next
        If Not CanWrite OrElse (Not TrapFeatures.AllowAssignment AndAlso VillainId = 0) Then Throw New InvalidOperationException("This trap cannot be edited in this mode.")
        Dim requested As Byte() = ReadRegion(updated, 1 - activeArea, True)
        If Not ValidRegion(requested) OrElse requested(9) <> ((CInt(region(9)) + 1) And 255) Then
            Throw New InvalidDataException("The new trap save area is invalid.")
        End If
        For i As Integer = 0 To region.Length - 1
            Dim permitted As Boolean = (i >= 9 AndAlso i <= 15) OrElse i = 17
            If TrapFeatures.AllowAssignment Then permitted = permitted OrElse i = 0 OrElse i = 1 OrElse i = 7 OrElse (i >= 16 AndAlso i <= 63)
            If Not permitted AndAlso requested(i) <> region(i) Then Throw New InvalidDataException("The edit would change cached villains or unrelated trap data.")
        Next
        Dim check As New TrapSession(updated)
        If Not check.CanWrite Then Throw New InvalidDataException("The requested trap save is invalid.")
        'Commit the new area's sequence/checksum header only after its payload is verified.
        Return allowed.Skip(1).Concat(allowed.Take(1)).ToArray()
    End Function

    Friend Shared Function AreaBlocks(area As Integer) As Integer()
        Return Enumerable.Range(If(area = 0, 8, 36), 28).Where(Function(b) b Mod 4 <> 3).ToArray()
    End Function
    Private Shared Function ReadRegion(bytes As Byte(), area As Integer, encrypted As Boolean) As Byte()
        Dim result As New Collections.Generic.List(Of Byte)
        For Each block As Integer In AreaBlocks(area)
            Dim data As Byte() = bytes.Skip(block * 16).Take(16).ToArray()
            result.AddRange(If(encrypted, Crypt(bytes, block, data, False), data))
        Next
        Return result.ToArray()
    End Function
    Private Shared Function Crypt(bytes As Byte(), block As Integer, data As Byte(), encrypt As Boolean) As Byte()
        Dim material As Byte() = bytes.Take(32).Concat(New Byte() {CByte(block)}).Concat(Encoding.ASCII.GetBytes(" Copyright (C) 2010 Activision. All Rights Reserved. ")).ToArray()
        Using hash As MD5 = MD5.Create(), cipher As System.Security.Cryptography.Aes = System.Security.Cryptography.Aes.Create()
            cipher.Key = hash.ComputeHash(material)
            cipher.Mode = CipherMode.ECB
            cipher.Padding = PaddingMode.None
            Using transform As ICryptoTransform = If(encrypt, cipher.CreateEncryptor(), cipher.CreateDecryptor())
                Return transform.TransformFinalBlock(data, 0, data.Length)
            End Using
        End Using
    End Function
    Private Shared Function ValidPrimary(r As Byte()) As Boolean
        Dim h As Byte() = r.Take(16).ToArray()
        h(14) = 5 : h(15) = 0
        Return Word(r, 14) = Crc(h) AndAlso Word(r, 12) = Crc(r.Skip(16).Take(48).ToArray())
    End Function
    Private Shared Function ValidRegion(r As Byte()) As Boolean
        Return ValidPrimary(r) AndAlso Word(r, 10) = Crc(r.Skip(64).ToArray())
    End Function
    Private Shared Function Word(b As Byte(), offset As Integer) As Integer
        Return CInt(b(offset)) Or (CInt(b(offset + 1)) << 8)
    End Function
    Private Shared Sub PutWord(b As Byte(), offset As Integer, value As Integer)
        b(offset) = CByte(value And 255) : b(offset + 1) = CByte(value >> 8)
    End Sub
    Private Shared Function Crc(b As Byte()) As Integer
        Dim value As Integer = &HFFFF
        For Each octet As Byte In b
            value = value Xor (CInt(octet) << 8)
            For bit As Integer = 0 To 7
                value = If((value And &H8000) <> 0, (value << 1) Xor &H1021, value << 1) And &HFFFF
            Next
        Next
        Return value
    End Function
End Class
