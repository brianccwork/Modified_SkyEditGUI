Option Strict On
Option Explicit On

Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

'Creation Crystal Gold/Level editing, independent of the legacy form/global buffer.
'Layout references: the supplied SkylanderFormat.md CYOS section, Portal-To-Unity
'SpyroTag_CYOS.cs, and SkanderNET CreationCrystalFigure.cs (reference only).
'No SkanderNET/Portal-To-Unity runtime dependency is used.
Friend NotInheritable Class CrystalSession
    Private ReadOnly raw As Byte()
    Private ReadOnly record As Byte()
    Private ReadOnly activeArea As Integer
    Private ReadOnly encrypted As Boolean
    Friend Const MaximumLevel As Integer = 80
    Private Shared ReadOnly Levels As Integer() = CreateLevelThresholds()

    'The established XP curve through 20, followed by the constant final level interval.
    'Level 80 stores 1,589,500 XP; the game's unlocked level cap is independent of tag XP.
    Private Shared Function CreateLevelThresholds() As Integer()
        Dim result As Integer() = {0, 1000, 2200, 3800, 6000, 9000, 13000, 18200,
            24800, 33000, 42700, 53900, 66600, 80800, 96500, 113700, 132400, 152600, 174300, 197500}
        Dim interval As Integer = result(19) - result(18)
        Array.Resize(result, MaximumLevel)
        For index As Integer = 20 To result.Length - 1
            result(index) = result(index - 1) + interval
        Next
        Return result
    End Function
    Private Shared ReadOnly Elements As String() = {"Magic", "Water", "Air", "Undead", "Tech", "Fire", "Earth", "Life", "Dark", "Light"}
    Private Shared ReadOnly Classes As String() = {"None", "Knight", "Bowslinger", "Quickshot", "Ninja", "Brawler",
        "Smasher", "Sorcerer", "Swashbuckler", "Sentinel", "Bazooker"}

    Friend ReadOnly Property CanEdit As Boolean
    Friend ReadOnly Property Notice As String
    Friend ReadOnly Property HasValidSave As Boolean
    Friend ReadOnly Property FigureName As String
    Friend ReadOnly Property GoldValue As Decimal
    Friend ReadOnly Property LevelValue As Decimal = 1D
    Friend ReadOnly Property Experience As Integer
    Friend ReadOnly Property BattleClass As String = "None"
    Friend ReadOnly Property Original As Byte()
        Get
            Return DirectCast(raw.Clone(), Byte())
        End Get
    End Property

    Friend Sub New(bytes As Byte())
        If bytes Is Nothing OrElse bytes.Length <> 1024 Then Throw New InvalidDataException("Read a complete 1024-byte Creation Crystal first.")
        raw = DirectCast(bytes.Clone(), Byte())
        Dim id As Integer = Word(raw, &H10)
        If raw(&H12) <> 0 OrElse raw(&H13) <> 0 OrElse id < 680 OrElse id > 689 OrElse
           (Word(raw, &H1C) >> 12) <> 5 OrElse (Word(raw, &H1C) And &H200) = 0 Then
            Throw New InvalidDataException("This page edits Creation Crystals. Place a Creation Crystal containing an Imaginator on the portal.")
        End If
        FigureName = EditorArtworkCatalog.CrystalName(id, Word(raw, &H1C), Elements(id - 680) & " Crystal")
        If (raw(0) Xor raw(1) Xor raw(2) Xor raw(3)) <> raw(4) OrElse Crc(raw.Take(30).ToArray()) <> Word(raw, 30) Then
            Throw New InvalidDataException("The crystal's serial or header could not be verified. Writing is disabled.")
        End If
        For block As Integer = 3 To 63 Step 4
            Dim offset As Integer = block * 16 + 6
            If (raw(offset + 1) >> 4) <> ((raw(offset) And 15) Xor 15) OrElse
               (raw(offset + 2) And 15) <> ((raw(offset) >> 4) Xor 15) OrElse
               (raw(offset + 2) >> 4) <> ((raw(offset + 1) And 15) Xor 15) Then
                Throw New InvalidDataException("The crystal's access-control bytes could not be verified. Writing is disabled.")
            End If
        Next

        'Select an encoding only through all three applicable CYOS checksums.
        Dim candidates As New List(Of Tuple(Of Boolean, Byte(), Byte(), Boolean, Boolean))()
        For Each decode As Boolean In New Boolean() {True, False}
            Dim a As Byte() = ReadRecord(raw, 0, decode)
            Dim b As Byte() = ReadRecord(raw, 1, decode)
            Dim validA As Boolean = ValidRecord(a)
            Dim validB As Boolean = ValidRecord(b)
            If validA OrElse validB Then candidates.Add(Tuple.Create(decode, a, b, validA, validB))
        Next
        If candidates.Count = 0 Then
            Dim blank As Boolean = ReadRecord(raw, 0, False).All(Function(value) value = 0) AndAlso
                ReadRecord(raw, 1, False).All(Function(value) value = 0)
            Notice = If(blank, "Empty Creation Crystal. Create an Imaginator in Skylanders Imaginators, collect at least 1 gold, let the game save, then read it again.",
                "Neither crystal save area passed the CYOS checksums. Keep your backup; initialize/save the character in-game and read it again. Writing is disabled.")
            Return
        End If
        If candidates.Count <> 1 Then Throw New InvalidDataException("The crystal's save encoding is ambiguous. Writing is disabled.")
        Dim candidate = candidates(0)
        encrypted = candidate.Item1
        Dim areaA As Byte() = candidate.Item2, areaB As Byte() = candidate.Item3
        If candidate.Item4 AndAlso candidate.Item5 Then
            If ((CInt(areaA(9)) + 1) And 255) = areaB(9) Then
                activeArea = 1
            ElseIf ((CInt(areaB(9)) + 1) And 255) = areaA(9) OrElse areaA.SequenceEqual(areaB) Then
                activeArea = 0
            Else
                Notice = "The crystal's two save sequences are out of sync. Save the character in-game and read it again. Writing is disabled."
                Return
            End If
        Else
            activeArea = If(candidate.Item4, 0, 1)
            Notice = "One save area is valid; saving will refresh the alternate area from the current character."
        End If
        record = DirectCast(If(activeArea = 0, areaA, areaB).Clone(), Byte())
        HasValidSave = True
        GoldValue = Word(record, 3)
        Experience = CInt(record(0)) Or (CInt(record(1)) << 8) Or (CInt(record(2)) << 16)
        For index As Integer = 0 To Levels.Length - 1
            If Experience >= Levels(index) Then LevelValue = index + 1
        Next
        'Battle class is seven little-endian bits at bit 0x1D8 of the 0x45-byte CYOS struct.
        Dim classId As Integer = Bits(record, &H7C * 8 + &H1D8, 7)
        If classId < 1 OrElse classId > 10 OrElse record.Skip(&H7C).Take(&H45).All(Function(value) value = 0) Then
            Notice = "No supported, saved Imaginator battle class was found. Create the character in-game, collect at least 1 gold, let the game save, then read it again."
            Return
        End If
        BattleClass = Classes(classId)
        CanEdit = encrypted
        If Not encrypted Then
            Notice = "This is a decrypted backup. Read the encrypted crystal from the portal before saving."
        End If
    End Sub

    'Only the alternate complete 0x140-byte save record may be written, with its header last.
    Private Shared Function RecordBlocks(area As Integer) As Integer()
        Return Enumerable.Range(8 + area * 28, 26).Where(Function(block) block Mod 4 <> 3).ToArray()
    End Function

    Friend Function WriteBlocks(updated As Byte()) As Integer()
        ValidatePrepared(updated)
        Dim blocks As Integer() = RecordBlocks(1 - activeArea)
        Return blocks.Skip(1).Concat(blocks.Take(1)).ToArray()
    End Function

    'Gold-only saves leave all 24 XP bits untouched; the form passes Level only when it changes.
    Friend Function BuildSave(gold As Decimal, Optional level As Integer? = Nothing) As Byte()
        If Not CanEdit Then Throw New InvalidOperationException(If(Notice, "Read an initialized Creation Crystal first."))
        If gold <> Decimal.Truncate(gold) OrElse gold < 0 OrElse gold > 65535 Then Throw New ArgumentOutOfRangeException(NameOf(gold))
        If level.HasValue AndAlso (level.Value < 1 OrElse level.Value > MaximumLevel) Then Throw New ArgumentOutOfRangeException(NameOf(level))
        Dim xp As Integer = Experience
        If level.HasValue Then xp = Levels(level.Value - 1)
        If gold = GoldValue AndAlso xp = Experience Then Return Original
        Dim nextRecord As Byte() = DirectCast(record.Clone(), Byte())
        Array.Copy(BitConverter.GetBytes(CUShort(gold)), 0, nextRecord, 3, 2)
        If level.HasValue Then Array.Copy(BitConverter.GetBytes(xp), 0, nextRecord, 0, 3)
        nextRecord(9) = CByte((CInt(record(9)) + 1) And 255)
        UpdateChecksums(nextRecord)
        Dim result As Byte() = Original
        Dim blocks As Integer() = RecordBlocks(1 - activeArea)
        For index As Integer = 0 To blocks.Length - 1
            Dim payload As Byte() = nextRecord.Skip(index * 16).Take(16).ToArray()
            Array.Copy(Crypt(raw, blocks(index), payload, True), 0, result, blocks(index) * 16, 16)
        Next
        ValidatePrepared(result)
        Return result
    End Function

    'Independent portal preflight: forbid identity/access changes or character customization changes.
    Private Sub ValidatePrepared(updated As Byte())
        If Not CanEdit OrElse updated Is Nothing OrElse updated.Length <> 1024 Then Throw New InvalidDataException("Invalid crystal save.")
        If raw.SequenceEqual(updated) Then Return
        Dim permitted As Integer() = RecordBlocks(1 - activeArea)
        For index As Integer = 0 To 1023
            If raw(index) <> updated(index) AndAlso Not permitted.Contains(index \ 16) Then
                Throw New InvalidDataException("The crystal edit would change protected data. Nothing was written.")
            End If
        Next
        Dim prepared As Byte() = ReadRecord(updated, 1 - activeArea, True)
        If Not ValidRecord(prepared) OrElse prepared(9) <> CByte((CInt(record(9)) + 1) And 255) Then
            Throw New InvalidDataException("The prepared crystal record failed validation. Nothing was written.")
        End If
        For index As Integer = 0 To record.Length - 1
            If (index <= 4 OrElse index = 9 OrElse (index >= 10 AndAlso index <= 15)) Then Continue For
            If prepared(index) <> record(index) Then Throw New InvalidDataException("The edit would change the Imaginator's character or other progress. Nothing was written.")
        Next
        Dim gold As Integer = Word(prepared, 3)
        Dim xp As Integer = CInt(prepared(0)) Or (CInt(prepared(1)) << 8) Or (CInt(prepared(2)) << 16)
        If gold > 65535 OrElse (xp <> Experience AndAlso Not Levels.Contains(xp)) Then Throw New InvalidDataException("The requested crystal values are outside the supported range.")
        Dim check As New CrystalSession(updated)
        If Not check.CanEdit OrElse check.Experience <> xp OrElse check.GoldValue <> gold OrElse check.BattleClass <> BattleClass Then
            Throw New InvalidDataException("The prepared crystal could not be read back correctly. Nothing was written.")
        End If
    End Sub

    Private Shared Function ReadRecord(source As Byte(), area As Integer, decode As Boolean) As Byte()
        Dim result(&H13F) As Byte
        Dim blocks As Integer() = RecordBlocks(area)
        For index As Integer = 0 To blocks.Length - 1
            Dim payload As Byte() = source.Skip(blocks(index) * 16).Take(16).ToArray()
            If decode AndAlso payload.Any(Function(value) value <> 0) Then payload = Crypt(source, blocks(index), payload, False)
            Array.Copy(payload, 0, result, index * 16, 16)
        Next
        Return result
    End Function

    Private Shared Function Crypt(source As Byte(), block As Integer, payload As Byte(), encrypt As Boolean) As Byte()
        Using hash As MD5 = MD5.Create(), cipher As System.Security.Cryptography.Aes = System.Security.Cryptography.Aes.Create()
            cipher.Key = hash.ComputeHash(source.Take(32).Concat(New Byte() {CByte(block)}).
                Concat(Encoding.ASCII.GetBytes(" Copyright (C) 2010 Activision. All Rights Reserved. ")).ToArray())
            cipher.Mode = CipherMode.ECB
            cipher.Padding = PaddingMode.None
            Using transform As ICryptoTransform = If(encrypt, cipher.CreateEncryptor(), cipher.CreateDecryptor())
                Return transform.TransformFinalBlock(payload, 0, 16)
            End Using
        End Using
    End Function

    Private Shared Function ValidRecord(data As Byte()) As Boolean
        Dim header As Byte() = data.Take(16).ToArray()
        header(14) = 5 : header(15) = 0
        Return Word(data, 10) = Crc(data.Skip(&H40).Take(&H100).ToArray()) AndAlso
            Word(data, 12) = Crc(data.Skip(&H10).Take(&H30).ToArray()) AndAlso Word(data, 14) = Crc(header)
    End Function

    Private Shared Sub UpdateChecksums(data As Byte())
        Array.Copy(BitConverter.GetBytes(Crc(data.Skip(&H40).Take(&H100).ToArray())), 0, data, 10, 2)
        Array.Copy(BitConverter.GetBytes(Crc(data.Skip(&H10).Take(&H30).ToArray())), 0, data, 12, 2)
        Dim header As Byte() = data.Take(16).ToArray()
        header(14) = 5 : header(15) = 0
        Array.Copy(BitConverter.GetBytes(Crc(header)), 0, data, 14, 2)
    End Sub

    Private Shared Function Word(data As Byte(), offset As Integer) As Integer
        Return CInt(data(offset)) Or (CInt(data(offset + 1)) << 8)
    End Function

    Private Shared Function Bits(data As Byte(), start As Integer, count As Integer) As Integer
        Dim value As Integer = 0
        For index As Integer = 0 To count - 1
            value = value Or (((CInt(data((start + index) \ 8)) >> ((start + index) Mod 8)) And 1) << index)
        Next
        Return value
    End Function

    Private Shared Function Crc(data As Byte()) As UShort
        Dim value As Integer = &HFFFF
        For Each item As Byte In data
            value = value Xor (CInt(item) << 8)
            For bit As Integer = 0 To 7
                value = If((value And &H8000) <> 0, (value << 1) Xor &H1021, value << 1) And &HFFFF
            Next
        Next
        Return CUShort(value)
    End Function
End Class
