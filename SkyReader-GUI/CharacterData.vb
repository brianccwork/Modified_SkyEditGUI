Option Strict On
Option Explicit On

Imports System.IO

'Characters and vehicles share two independently selected record pairs. Record
'copies contain payload only; identity, signatures and access trailers never move.
Friend NotInheritable Class CharacterData
    Private Shared ReadOnly MainBlocks As Integer() = {8, 9, 10, 12, 13, 14, 16}
    Private Shared ReadOnly ExtraBlocks As Integer() = {17, 18, 20, 21}
    Private Shared ReadOnly Levels As Integer() = {0, 1000, 2200, 3800, 6000, 9000, 13000, 18200, 24800, 33000,
        42700, 53900, 66600, 80800, 96500, 113700, 132400, 152600, 174300, 197500}

    Private Sub New()
    End Sub

    Private Shared Function Indices(extra As Boolean, bank As Integer) As Integer()
        Return If(extra, ExtraBlocks, MainBlocks).Select(Function(block) block + bank * 28).ToArray()
    End Function

    Private Shared Function Record(data As Byte(), extra As Boolean, bank As Integer) As Byte()
        Return Indices(extra, bank).SelectMany(Function(block) data.Skip(block * 16).Take(16)).ToArray()
    End Function

    'A sequence of zero follows 255. Never compare counters by numeric magnitude,
    'and never use the main counter to select extended data.
    Friend Shared Function ActiveBank(data As Byte(), Optional extra As Boolean = False) As Integer
        Dim a = Record(data, extra, 0), b = Record(data, extra, 1)
        If a.All(Function(value) value = 0) Then Return 1 'First save targets A.
        If b.All(Function(value) value = 0) Then Return 0
        Dim sequence = If(extra, 2, 9)
        If ((CInt(a(sequence)) + 1) And 255) = b(sequence) Then Return 1
        If ((CInt(b(sequence)) + 1) And 255) = a(sequence) OrElse a.SequenceEqual(b) Then Return 0
        Throw New InvalidDataException("The figure's save sequences are out of sync. Save it in-game and read it again before editing.")
    End Function

    Private Shared Function ValidRecord(bytes As Byte(), extra As Boolean, allowEmpty As Boolean) As Boolean
        If bytes.All(Function(value) value = 0) Then Return allowEmpty
        Dim header = bytes.Take(16).ToArray()
        If extra Then
            header = DirectCast(bytes.Clone(), Byte())
            header(0) = 6 : header(1) = 1
            Return FigureData.Word(bytes, 0) = FigureData.Crc(header)
        End If
        header(14) = 5 : header(15) = 0
        Return FigureData.Word(bytes, 14) = FigureData.Crc(header) AndAlso
            FigureData.Word(bytes, 12) = FigureData.Crc(bytes.Skip(16).Take(48).ToArray()) AndAlso
            FigureData.Word(bytes, 10) = FigureData.Crc(bytes.Skip(64).Take(48).Concat(New Byte(223) {}).ToArray())
    End Function

    Private Shared Function IsBlankVehicle(data As Byte()) As Boolean
        Return FigureData.PayloadBlocks.All(Function(block) data.Skip(block * 16).Take(16).All(Function(value) value = 0))
    End Function

    Private Shared Function Valid(data As Byte(), vehicle As Boolean) As Boolean
        If vehicle Then
            If IsBlankVehicle(data) Then Return True
            'An inactive damaged record may be replaced, but both selected records
            'must independently verify. Do not select a bank just because its CRC works.
            Try
                Return ValidRecord(Record(data, False, ActiveBank(data)), False, False) AndAlso
                    ValidRecord(Record(data, True, ActiveBank(data, True)), True, False)
            Catch ex As InvalidDataException
                Return False
            End Try
        End If
        For bank As Integer = 0 To 1
            If Not ValidRecord(Record(data, False, bank), False, True) OrElse
                Not ValidRecord(Record(data, True, bank), True, True) Then Return False
        Next
        Return True
    End Function

    Friend Shared Function Decode(raw As Byte(), ByRef encrypted As Boolean, Optional vehicle As Boolean = False) As Byte()
        If raw Is Nothing OrElse raw.Length <> FigureData.DumpSize Then Throw New InvalidDataException("Read a complete figure first.")
        Dim decoded = DirectCast(raw.Clone(), Byte())
        For Each block In FigureData.PayloadBlocks
            Dim payload = raw.Skip(block * 16).Take(16).ToArray()
            If payload.Any(Function(value) value <> 0) Then
                Array.Copy(FigureData.Crypt(raw, block, payload, False), 0, decoded, block * 16, 16)
            End If
        Next
        Dim plainValid = Valid(raw, vehicle), encryptedValid = Valid(decoded, vehicle)
        If (Not plainValid AndAlso Not encryptedValid) OrElse
            (plainValid AndAlso encryptedValid AndAlso Not raw.SequenceEqual(decoded)) Then
            Throw New InvalidDataException("The figure save data is damaged or its encoding cannot be verified. Writing is disabled.")
        End If
        encrypted = Not plainValid
        Dim result = If(encrypted, decoded, DirectCast(raw.Clone(), Byte()))
        'Validate sequence relationships once at the read boundary, before any edit.
        ActiveBank(result) : ActiveBank(result, True)
        Return result
    End Function

    Friend Shared Function Initialized(data As Byte()) As Boolean
        Dim main = Record(data, False, ActiveBank(data))
        Return main(&H16) <> 0 AndAlso Record(data, True, ActiveBank(data, True)).Any(Function(value) value <> 0)
    End Function

    Friend Shared Function HasBothRecords(data As Byte()) As Boolean
        Return Enumerable.Range(0, 2).All(Function(bank)
            Return Record(data, False, bank).Any(Function(value) value <> 0) AndAlso
                Record(data, True, bank).Any(Function(value) value <> 0)
        End Function)
    End Function

    Friend Shared Function Gold(data As Byte()) As Decimal
        Return Math.Min(65000, FigureData.Word(data, &H80 + ActiveBank(data) * &H1C0 + 3))
    End Function

    Friend Shared Function Gearbits(data As Byte()) As Decimal
        Return FigureData.Word(data, &H110 + ActiveBank(data, True) * &H1C0 + 8)
    End Function

    Friend Shared Function Experience(data As Byte(), sensei As Boolean) As ULong
        Dim main = Record(data, False, ActiveBank(data))
        Dim extra = Record(data, True, ActiveBank(data, True))
        Dim total = CULng(CInt(main(0)) Or (CInt(main(1)) << 8) Or (CInt(main(2)) << 16))
        If sensei OrElse (main(&H16) And 1) <> 0 Then
            total += CULng(FigureData.Word(extra, 3)) + CULng(BitConverter.ToUInt32(extra, 8))
        End If
        Return total
    End Function

    Friend Shared Function Level(data As Byte(), sensei As Boolean) As Decimal
        Dim xp = Experience(data, sensei), result As Integer = 1
        For index As Integer = 0 To Levels.Length - 1
            If xp >= CULng(Levels(index)) Then result = index + 1
        Next
        Return result
    End Function

    Friend Shared Function WriteBlocks(raw As Byte(), sensei As Boolean, vehicle As Boolean) As Integer()
        Dim encrypted As Boolean
        Dim data = Decode(raw, encrypted, vehicle)
        If sensei Then Return New Integer() {8, 17, 36, 45}
        If Not encrypted Then Return FigureData.PayloadBlocks.ToArray()
        If vehicle Then Return Indices(True, 1 - ActiveBank(data, True))
        Return Indices(False, 1 - ActiveBank(data)).Concat(Indices(True, 1 - ActiveBank(data, True))).ToArray()
    End Function

    Private Shared Sub RebuildChecksum(bytes As Byte(), extra As Boolean)
        If extra Then
            bytes(0) = 6 : bytes(1) = 1
            Array.Copy(BitConverter.GetBytes(FigureData.Crc(bytes)), 0, bytes, 0, 2)
        Else
            Array.Copy(BitConverter.GetBytes(FigureData.Crc(bytes.Skip(64).Take(48).Concat(New Byte(223) {}).ToArray())), 0, bytes, 10, 2)
            Array.Copy(BitConverter.GetBytes(FigureData.Crc(bytes.Skip(16).Take(48).ToArray())), 0, bytes, 12, 2)
            bytes(14) = 5 : bytes(15) = 0
            Array.Copy(BitConverter.GetBytes(FigureData.Crc(bytes.Take(16).ToArray())), 0, bytes, 14, 2)
        End If
    End Sub

    Private Shared Sub PutRecord(data As Byte(), bytes As Byte(), extra As Boolean, bank As Integer)
        Dim targets = Indices(extra, bank)
        For index As Integer = 0 To targets.Length - 1
            Array.Copy(bytes, index * 16, data, targets(index) * 16, 16)
        Next
    End Sub

    Private Shared Sub PutExperience(main As Byte(), extra As Byte(), level As Integer, secondCap As Integer)
        Dim remaining = Levels(level - 1)
        Dim first = Math.Min(remaining, 33000) : remaining -= first
        Dim second = Math.Min(remaining, secondCap) : remaining -= second
        Array.Copy(BitConverter.GetBytes(first), 0, main, 0, 3)
        Array.Copy(BitConverter.GetBytes(second), 0, extra, 3, 2)
        Array.Copy(BitConverter.GetBytes(remaining), 0, extra, 8, 4)
    End Sub

    Friend Shared Function SaveCharacter(raw As Byte(), goldValue As Decimal, levelValue As Decimal, sensei As Boolean) As Byte()
        If goldValue <> Decimal.Truncate(goldValue) OrElse goldValue < 0 OrElse goldValue > 65000 OrElse
            levelValue <> Decimal.Truncate(levelValue) OrElse levelValue < 1 OrElse levelValue > 20 Then Throw New ArgumentOutOfRangeException("Gold/Level")
        Dim encrypted As Boolean
        Dim data = Decode(raw, encrypted)
        Dim mainSource = ActiveBank(data), extraSource = ActiveBank(data, True)
        Dim changeGold = goldValue <> Gold(data), changeLevel = levelValue <> Level(data, sensei)
        If sensei Then
            If Not encrypted OrElse Not HasBothRecords(data) Then Throw New InvalidDataException("Initialize the Sensei in-game before editing.")
            For bank As Integer = 0 To 1
                Dim main = Record(data, False, bank), extra = Record(data, True, bank)
                If changeGold Then Array.Copy(BitConverter.GetBytes(CUShort(goldValue)), 0, main, 3, 2)
                If changeLevel Then PutExperience(main, extra, CInt(levelValue), 63500)
                'Gold/XP are mirrored in place. Keep each independent sequence and
                'all unrelated bytes; neither region may switch to an older copy.
                RebuildChecksum(main, False) : RebuildChecksum(extra, True)
                PutRecord(data, main, False, bank) : PutRecord(data, extra, True, bank)
            Next
        Else
            Dim main = Record(data, False, mainSource), extra = Record(data, True, extraSource)
            If changeLevel Then
                Dim secondCap = If((FigureData.Word(raw, &H1C) >> 12) > 1, 63500, 65535)
                PutExperience(main, extra, CInt(levelValue), secondCap)
            ElseIf (main(&H16) And 1) = 0 Then
                'SSA reset invalidates later-game XP. Keep the current first bucket,
                'but never reactivate stale later-game XP during initialization.
                Array.Clear(extra, 3, 2) : Array.Clear(extra, 8, 4)
            End If
            main(9) = CByte((CInt(main(9)) + 1) And 255)
            extra(2) = CByte((CInt(extra(2)) + 1) And 255)
            main(&H16) = 1
            Array.Copy(BitConverter.GetBytes(CUShort(goldValue)), 0, main, 3, 2)
            RebuildChecksum(main, False) : RebuildChecksum(extra, True)
            PutRecord(data, main, False, 1 - mainSource) : PutRecord(data, extra, True, 1 - extraSource)
        End If
        Dim targets = If(sensei, New Integer() {8, 17, 36, 45},
            Indices(False, 1 - mainSource).Concat(Indices(True, 1 - extraSource)).ToArray())
        Return Encode(raw, data, targets, encrypted, False)
    End Function

    Friend Shared Function SaveGearbits(raw As Byte(), value As Decimal) As Byte()
        If value <> Decimal.Truncate(value) OrElse value < 0 OrElse value > 33000 Then Throw New ArgumentOutOfRangeException(NameOf(value))
        Dim encrypted As Boolean
        Dim data = Decode(raw, encrypted, True)
        Dim source = ActiveBank(data, True), extra = Record(data, True, source)
        Array.Copy(BitConverter.GetBytes(CUShort(value)), 0, extra, 8, 2)
        extra(2) = CByte((CInt(extra(2)) + 1) And 255)
        RebuildChecksum(extra, True)
        PutRecord(data, extra, True, 1 - source)
        Dim targets = Indices(True, 1 - source)
        If Not encrypted AndAlso IsBlankVehicle(raw) Then
            'SuperChargers uses the main record's region count to read extended data.
            Dim main(MainBlocks.Length * 16 - 1) As Byte
            main(9) = 1
            main(&H16) = 1
            RebuildChecksum(main, False)
            PutRecord(data, main, False, 0)
            targets = Indices(False, 0).Concat(targets).ToArray()
        End If
        Return Encode(raw, data, targets, encrypted, True)
    End Function

    Private Shared Function Encode(raw As Byte(), data As Byte(), targets As Integer(), wasEncrypted As Boolean, vehicle As Boolean) As Byte()
        Dim result = DirectCast(raw.Clone(), Byte())
        'A plaintext initialization must encrypt every populated payload block;
        'ordinary saves touch only their selected writable records.
        If Not wasEncrypted Then targets = FigureData.PayloadBlocks.Where(Function(block)
            Return targets.Contains(block) OrElse data.Skip(block * 16).Take(16).Any(Function(value) value <> 0)
        End Function).ToArray()
        For Each block In targets
            Dim payload = data.Skip(block * 16).Take(16).ToArray()
            Array.Copy(FigureData.Crypt(data, block, payload, True), 0, result, block * 16, 16)
        Next
        Dim encrypted As Boolean
        Dim verified = Decode(result, encrypted, vehicle)
        If Not encrypted OrElse Not verified.SequenceEqual(data) Then Throw New InvalidDataException("Prepared figure data failed validation. Nothing was written.")
        Return result
    End Function
End Class
