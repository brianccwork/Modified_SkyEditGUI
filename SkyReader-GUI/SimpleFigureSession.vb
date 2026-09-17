Option Strict On
Option Explicit On

Imports System.IO

'Bridges the simple page to the established figure parser and Gold/Level writers.
'Artwork browsing never calls SelectFigure and never changes this session.
Friend NotInheritable Class SimpleFigureSession
    'returns a cloned copy of the scanned bytes so callers cannot overwrite the stored snapshot.
    Friend Property Original As Byte()
        Get
            Return If(raw Is Nothing, Nothing, DirectCast(raw.Clone(), Byte()))
        End Get
        Private Set(value As Byte())
            raw = value
        End Set
    End Property
    Private raw As Byte()
    'Records whether the scanned figure uses the vehicle data layout.
    Friend ReadOnly Property IsVehicle As Boolean
    'Records whether the scanned figure needs the separate Sensei rules.
    Friend ReadOnly Property IsSensei As Boolean
    'Records whether the loaded data failed the applicable editor safety checks.
    Friend ReadOnly Property IsUnsafe As Boolean
    Private ReadOnly vehicleMode As Boolean
    'Allows an otherwise unchanged save when legacy initialization metadata still needs repair. (Keep an eye on this NOTE)
    Friend ReadOnly Property NeedsChecksumRepair As Boolean
    'Records whether regular plaintext payload must be encrypted before a portal write.
    Friend ReadOnly Property RequiresFullEncryption As Boolean
    'Indicates whether the current page is allowed to modify the scanned figure.
    Friend ReadOnly Property CanEdit As Boolean
    'Stores the scanned catalog name independently of browsed artwork.
    Friend ReadOnly Property FigureName As String
    'Stores the game category determined from the scanned figure.
    Friend ReadOnly Property GameName As String
    'Stores the loaded Gold or Gearbits value shown by the simplified page.
    Friend ReadOnly Property GoldValue As Decimal
    'Stores the loaded level shown by the simplified page.
    Friend ReadOnly Property LevelValue As Decimal
    'Stores the allowed Gold or Gearbits limit for the loaded figure.
    Friend ReadOnly Property GoldMaximum As Decimal
    'tores the level limit used to configure the page input.
    Friend ReadOnly Property LevelMaximum As Decimal
    'Stores the identified category names available to the session.
    Friend ReadOnly Property Catalog As New List(Of String)

    'Parses a scanned snapshot, applies type-specific validation, and records its values and edit limits.
    Friend Sub New(bytes As Byte(), Optional editVehicle As Boolean = False)
        vehicleMode = editVehicle
        If bytes Is Nothing OrElse bytes.Length <> 1024 Then Throw New InvalidDataException("Read a valid figure first.")
        frmMain.EnsureEditorInitialized()
        Original = DirectCast(bytes.Clone(), Byte())
        FigureIO.WholeFile = DirectCast(bytes.Clone(), Byte())
        Portal.blnAccess = False
        MiFare.Detection()
        If Portal.blnAccess Then Throw New InvalidDataException(FigureWarnings.UnsafeText)
        frmMain.lstCharacters.SelectedIndex = -1
        frmMain.Enable_Controls()
        FigureIO.Parse_Figure(False)
        IsVehicle = FigureIO.BlnVehicle
        IsSensei = FigureIO.blnSensei
        GameName = Convert.ToString(frmMain.cmbGame.SelectedItem)
        FigureName = If(frmMain.lstCharacters.SelectedItem Is Nothing, "Unknown figure", Convert.ToString(frmMain.lstCharacters.SelectedItem))
        If Not IsSensei AndAlso Not IsVehicle AndAlso Not FigureIO.blnTrap AndAlso Not FigureIO.blnCrystal AndAlso
            {"Spyro's Adventure", "Giants", "Swap Force", "Trap Team", "SuperChargers"}.Contains(GameName) Then
            DecodeRegular(Original)
            Figures.Area0orArea1()
            Global.SkyReader_GUI.Gold.GetGold()
            Exp.GetEXP()
        End If
        GoldValue = frmMain.numGold.Value
        LevelValue = frmMain.numLevel.Value
        GoldMaximum = frmMain.numGold.Maximum
        LevelMaximum = frmMain.numLevel.Maximum
        Dim supportedGames As String() = {"Spyro's Adventure", "Giants", "Swap Force", "Trap Team", "SuperChargers", "Imaginators"}
        CanEdit = Not FigureIO.blnTrap AndAlso Not FigureIO.BlnVehicle AndAlso Not FigureIO.blnCrystal AndAlso
                  supportedGames.Contains(GameName) AndAlso
                  frmMain.lstCharacters.SelectedItem IsNot Nothing AndAlso frmMain.numGold.Enabled AndAlso frmMain.numLevel.Enabled AndAlso
                  frmMain.Save_Enc_ToolStripMenuItem.Enabled
        'Only catalog-recognized legacy characters may initialize missing payload checksums.
        'Imaginators/Senseis, vehicles, traps and crystals retain their existing policy.
        Dim legacy As Boolean = CanEdit AndAlso Not IsSensei AndAlso Not vehicleMode AndAlso
            {"Spyro's Adventure", "Giants", "Swap Force", "Trap Team", "SuperChargers"}.Contains(GameName)
        RequiresFullEncryption = legacy AndAlso Not FigureIO.blnEncrypted
        If IsVehicle Then
            Using vehicle As New frmVehicles()
                GoldValue = vehicle.ReadGearbitsForSimpleEditor()
                GoldMaximum = 33000D
                LevelValue = 1D
                LevelMaximum = 1D
                IsUnsafe = Not vehicle.IsSafeForSimpleEditor() OrElse
                    frmMain.picSerial.BackColor <> Color.Green OrElse frmMain.picHeader.BackColor <> Color.Green
            End Using
            CanEdit = vehicleMode AndAlso Not IsUnsafe AndAlso FigureName <> "Unknown figure"
        Else
            IsUnsafe = Not FigureIO.blnTrap AndAlso Not FigureIO.blnCrystal AndAlso
                supportedGames.Contains(GameName) AndAlso FigureWarnings.HasUnsafeCharacterData(legacy)
            CanEdit = CanEdit AndAlso Not vehicleMode AndAlso Not IsUnsafe
            NeedsChecksumRepair = legacy AndAlso CanEdit AndAlso
                (RequiresFullEncryption OrElse Not RegularCharacterData.Initialized(FigureIO.WholeFile))
        End If
        If Not CanEdit Then
            GoldValue = 0D
            LevelValue = 1D
        End If
        For Each item As Object In frmMain.lstCharacters.Items
            Dim caption As String = Convert.ToString(item)
            If Not caption.StartsWith("--", StringComparison.Ordinal) Then Catalog.Add(caption)
        Next
        If Not Catalog.Contains(FigureName) Then Catalog.Insert(0, FigureName)
    End Sub

    'Decodes validated regular character data and updates the shared buffer and checksum indicators.
    Private Shared Sub DecodeRegular(bytes As Byte())
        Dim encrypted As Boolean
        FigureIO.WholeFile = RegularCharacterData.Decode(bytes, encrypted)
        FigureIO.blnEncrypted = encrypted
        CRC16CCITT.Checksums()
    End Sub

    'Builds a verified type specific save from the scanned snapshot without using gallery selections as data.
    Friend Function BuildSave(gold As Decimal, level As Decimal) As Byte()
        If Not CanEdit Then Throw New InvalidOperationException("This figure cannot be edited on this page.")
        If gold < 0 OrElse gold > GoldMaximum OrElse level < 1 OrElse level > LevelMaximum Then
            Throw New ArgumentOutOfRangeException("The requested values are outside this editor's limits.")
        End If
        If gold = GoldValue AndAlso level = LevelValue AndAlso Not NeedsChecksumRepair Then Return Original

        If Not IsVehicle AndAlso Not IsSensei AndAlso GameName <> "Imaginators" Then
            Dim result = RegularCharacterData.Save(Original, gold, level)
            Dim check As New SimpleFigureSession(result)
            If Not check.CanEdit OrElse check.GoldValue <> gold OrElse check.LevelValue <> level Then
                Throw New InvalidDataException("Prepared Gold/Level did not match the requested values. Nothing was written.")
            End If
            Return result
        End If

        'Reload the scanned bytes, never a browsed gallery selection or stale buffer.
        Dim fresh As New SimpleFigureSession(Original, vehicleMode)
        If Not fresh.CanEdit Then Throw New InvalidOperationException("Read the figure again.")
        Dim plain As Byte() = DirectCast(FigureIO.WholeFile.Clone(), Byte())
        Try
            If IsVehicle Then
                Using vehicle As New frmVehicles()
                    vehicle.ApplyGearbitsForSimpleEditor(gold)
                End Using
                'The legacy region copy includes access trailers: restore them before encryption.
                For block As Integer = 0 To 63
                    If block < 8 OrElse block Mod 4 = 3 Then
                        Array.Copy(plain, block * 16, FigureIO.WholeFile, block * 16, 16)
                    End If
                Next
                FigureIO.Encrypt()
                Dim vehicleResult As Byte() = Original
                For Each block As Integer In SimplePortal.VehicleBlocks
                    Array.Copy(FigureIO.WholeFile, block * 16, vehicleResult, block * 16, 16)
                Next
                Return vehicleResult
            End If
            frmMain.numGold.Value = gold
            frmMain.numLevel.Value = level
            If gold <> GoldValue OrElse NeedsChecksumRepair Then Global.SkyReader_GUI.Gold.WriteGold()
            If level <> LevelValue OrElse NeedsChecksumRepair Then
                Exp.WriteEXP()
            End If
            'Both Gold/Level copies are updated in place. Preserve the independent
            'region counters so unrelated progress never switches to an older copy.
            If IsSensei Then Figures.SetArea0AndArea1()
            CRC16CCITT.WriteCheckSums()
            'Header and access bytes are not part of this editor's write surface.
            Array.Copy(plain, 0, FigureIO.WholeFile, 0, &H80)
            Dim prepared As Byte() = DirectCast(FigureIO.WholeFile.Clone(), Byte())
            FigureIO.Encrypt()
            Dim encrypted As Byte() = FigureIO.WholeFile
            Dim result As Byte() = Original
            For Each block As Integer In If(RequiresFullEncryption, SimplePortal.LegacyPayloadBlocks, New Integer() {8, 17, 36, 45})
                If RequiresFullEncryption AndAlso
                    prepared.Skip(block * 16).Take(16).All(Function(value) value = 0) Then
                    Array.Clear(result, block * 16, 16)
                Else
                    Array.Copy(encrypted, block * 16, result, block * 16, 16)
                End If
            Next
            If Not IsSensei Then
                'Validate the exact assembled output, not just the temporary plaintext buffer.
                Dim check As New SimpleFigureSession(result)
                If Not check.CanEdit OrElse FigureWarnings.HasUnsafeCharacterData() OrElse check.GoldValue <> gold OrElse check.LevelValue <> level Then
                    Throw New InvalidDataException("The prepared save did not pass checksum verification. Nothing was written.")
                End If
            End If
            Return result
        Finally
            FigureIO.WholeFile = plain
            Figures.Area0orArea1()
            If Not IsVehicle Then
                frmMain.numGold.Value = GoldValue
                frmMain.numLevel.Value = LevelValue
            End If
        End Try
    End Function
End Class

'Pre-Imaginators character save records. Runes layout: seven main blocks and
'four extended blocks, with independently selected alternate copies.
Friend NotInheritable Class RegularCharacterData
    Private Shared ReadOnly MainBlocks As Integer() = {8, 9, 10, 12, 13, 14, 16}
    Private Shared ReadOnly ExtraBlocks As Integer() = {17, 18, 20, 21}
    Private Shared ReadOnly Levels As Integer() = {0, 1000, 2200, 3800, 6000, 9000, 13000, 18200, 24800, 33000, 42700, 53900, 66600, 80800, 96500, 113700, 132400, 152600, 174300, 197500}

    'Calculates the CRC-16 checksum used to validate or rebuild save records.
    Private Shared Function Crc(data As Byte()) As UShort
        Dim value As Integer = &HFFFF
        For Each item In data
            value = value Xor (CInt(item) << 8)
            For bit As Integer = 0 To 7
                value = If((value And &H8000) <> 0, (value << 1) Xor &H1021, value << 1) And &HFFFF
            Next
        Next
        Return CUShort(value)
    End Function

    'Collects the specified 16-byte payload blocks into one contiguous record.
    Private Shared Function Blocks(data As Byte(), indices As IEnumerable(Of Integer)) As Byte()
        Return indices.SelectMany(Function(block) data.Skip(block * 16).Take(16)).ToArray()
    End Function

    'Derives a block-specific AES key from the header and transforms one payload block.
    Private Shared Function Crypt(data As Byte(), block As Integer, encrypt As Boolean) As Byte()
        Dim magic = System.Text.Encoding.ASCII.GetBytes(" Copyright (C) 2010 Activision. All Rights Reserved. ")
        Using hash = System.Security.Cryptography.MD5.Create(), cipher = System.Security.Cryptography.Aes.Create()
            cipher.Key = hash.ComputeHash(data.Take(32).Concat(New Byte() {CByte(block)}).Concat(magic).ToArray())
            cipher.Mode = System.Security.Cryptography.CipherMode.ECB
            cipher.Padding = System.Security.Cryptography.PaddingMode.None
            Using transform = If(encrypt, cipher.CreateEncryptor(), cipher.CreateDecryptor())
                Return transform.TransformFinalBlock(data, block * 16, 16)
            End Using
        End Using
    End Function

    'Checks main and extended record checksums while allowing genuinely empty records.
    Private Shared Function Valid(data As Byte()) As Boolean
        For Each shift In New Integer() {0, 28}
            Dim main = Blocks(data, MainBlocks.Select(Function(block) block + shift))
            Dim extra = Blocks(data, ExtraBlocks.Select(Function(block) block + shift))
            If main.Any(Function(value) value <> 0) Then
                Dim header = main.Take(16).ToArray()
                header(14) = 5 : header(15) = 0
                If BitConverter.ToUInt16(main, 14) <> Crc(header) Then Return False
                If BitConverter.ToUInt16(main, 12) <> Crc(main.Skip(16).Take(48).ToArray()) Then Return False
                If BitConverter.ToUInt16(main, 10) <> Crc(main.Skip(64).Take(48).Concat(New Byte(223) {}).ToArray()) Then Return False
            End If
            If extra.Any(Function(value) value <> 0) Then
                Dim expected = BitConverter.ToUInt16(extra, 0)
                extra(0) = 6 : extra(1) = 1
                If expected <> Crc(extra) Then Return False
            End If
        Next
        Return True
    End Function

    'Tests plaintext and decrypted candidates and rejects damaged or ambiguous regular character encoding.
    Friend Shared Function Decode(raw As Byte(), ByRef encrypted As Boolean) As Byte()
        If raw Is Nothing OrElse raw.Length <> 1024 Then Throw New InvalidDataException("Read a complete character first.")
        Dim decoded = DirectCast(raw.Clone(), Byte())
        For Each block In SimplePortal.LegacyPayloadBlocks
            If raw.Skip(block * 16).Take(16).All(Function(value) value = 0) Then Continue For
            Array.Copy(Crypt(raw, block, False), 0, decoded, block * 16, 16)
        Next
        Dim plainValid = Valid(raw)
        Dim encryptedValid = Valid(decoded)
        If (Not plainValid AndAlso Not encryptedValid) OrElse (plainValid AndAlso encryptedValid AndAlso Not raw.SequenceEqual(decoded)) Then
            Throw New InvalidDataException("The character save data is damaged or its encoding cannot be verified. Restore a known-good backup or reset/initialize it in-game before editing.")
        End If
        encrypted = Not plainValid
        Return If(encrypted, decoded, DirectCast(raw.Clone(), Byte()))
    End Function

    'Chooses the active main or extended copy, defaulting to the opposite copy for a blank first save.
    Private Shared Function Active(data As Byte(), blockA As Integer, blockB As Integer, sequence As Integer) As Integer
        Dim a = data.Skip(blockA * 16).Take(16).Any(Function(value) value <> 0)
        Dim b = data.Skip(blockB * 16).Take(16).Any(Function(value) value <> 0)
        If Not a Then Return 1 'Empty tag chooses B so the first save writes A.
        If Not b Then Return 0
        If ((CInt(data(blockA * 16 + sequence)) + 1) And &HFF) = data(blockB * 16 + sequence) Then Return 1
        Return 0
    End Function

    'Checks that the active record marks extended data as present and contains an extended header.
    Friend Shared Function Initialized(data As Byte()) As Boolean
        Dim main = &H80 + Active(data, 8, 36, 9) * &H1C0
        Dim extra = &H110 + Active(data, 17, 45, 2) * &H1C0
        Return data(main + &H16) <> 0 AndAlso data.Skip(extra).Take(16).Any(Function(value) value <> 0)
    End Function

    'Returns the payload blocks permitted for this session's next save.
    Friend Shared Function WriteBlocks(raw As Byte()) As Integer()
        Dim encrypted As Boolean
        Dim data = Decode(raw, encrypted)
        If Not encrypted Then Return SimplePortal.LegacyPayloadBlocks
        Dim mainShift = (1 - Active(data, 8, 36, 9)) * 28
        Dim extraShift = (1 - Active(data, 17, 45, 2)) * 28
        Return MainBlocks.Select(Function(block) block + mainShift).Concat(ExtraBlocks.Select(Function(block) block + extraShift)).ToArray()
    End Function

    'Copies active regular records to alternate slots, updates Gold/Level and metadata, and verifies the encrypted result.
    Friend Shared Function Save(raw As Byte(), gold As Decimal, level As Decimal) As Byte()
        If gold <> Decimal.Truncate(gold) OrElse gold < 0 OrElse gold > 65000 OrElse level <> Decimal.Truncate(level) OrElse level < 1 OrElse level > 20 Then Throw New ArgumentOutOfRangeException("Gold/Level")
        Dim encrypted As Boolean
        Dim data = Decode(raw, encrypted)
        Dim mainSource = Active(data, 8, 36, 9)
        Dim extraSource = Active(data, 17, 45, 2)
        Dim main = Blocks(data, MainBlocks.Select(Function(block) block + mainSource * 28))
        Dim extra = Blocks(data, ExtraBlocks.Select(Function(block) block + extraSource * 28))
        main(9) = CByte((CInt(main(9)) + 1) And &HFF)
        extra(2) = CByte((CInt(extra(2)) + 1) And &HFF)
        main(&H16) = 1 'Runes' region-count initialization, required for a fresh save.
        Array.Copy(BitConverter.GetBytes(CUShort(gold)), 0, main, 3, 2)
        Dim remaining = Levels(CInt(level) - 1)
        Dim xp1 = Math.Min(remaining, 33000) : remaining -= xp1
        Dim cap2 = If((BitConverter.ToUInt16(raw, &H1C) >> 12) > 1, 63500, 65535)
        Dim xp2 = Math.Min(remaining, cap2) : remaining -= xp2
        Array.Copy(BitConverter.GetBytes(xp1), 0, main, 0, 3)
        Array.Copy(BitConverter.GetBytes(xp2), 0, extra, 3, 2)
        Array.Copy(BitConverter.GetBytes(remaining), 0, extra, 8, 4)
        extra(0) = 6 : extra(1) = 1
        Array.Copy(BitConverter.GetBytes(Crc(extra)), 0, extra, 0, 2)
        Array.Copy(BitConverter.GetBytes(Crc(main.Skip(64).Take(48).Concat(New Byte(223) {}).ToArray())), 0, main, 10, 2)
        Array.Copy(BitConverter.GetBytes(Crc(main.Skip(16).Take(48).ToArray())), 0, main, 12, 2)
        main(14) = 5 : main(15) = 0
        Array.Copy(BitConverter.GetBytes(Crc(main.Take(16).ToArray())), 0, main, 14, 2)
        Dim targetMain = MainBlocks.Select(Function(block) block + (1 - mainSource) * 28).ToArray()
        Dim targetExtra = ExtraBlocks.Select(Function(block) block + (1 - extraSource) * 28).ToArray()
        For index As Integer = 0 To main.Length \ 16 - 1
            Array.Copy(main, index * 16, data, targetMain(index) * 16, 16)
        Next
        For index As Integer = 0 To extra.Length \ 16 - 1
            Array.Copy(extra, index * 16, data, targetExtra(index) * 16, 16)
        Next
        Dim result = DirectCast(raw.Clone(), Byte())
        Dim recordTargets = targetMain.Concat(targetExtra).ToArray()
        Dim targets = If(encrypted, recordTargets, SimplePortal.LegacyPayloadBlocks.Where(Function(block) recordTargets.Contains(block) OrElse data.Skip(block * 16).Take(16).Any(Function(value) value <> 0)).ToArray())
        For Each block In targets
            'Encrypt every block of the written records, even zero plaintext,
            'as Runes SaveBlocks and hegyak's Encrypt do.
            Array.Copy(Crypt(data, block, True), 0, result, block * 16, 16)
        Next
        Dim verifyEncrypted As Boolean
        Dim verified = Decode(result, verifyEncrypted)
        If Not verifyEncrypted OrElse Not verified.SequenceEqual(data) Then Throw New InvalidDataException("Prepared character data failed independent readback validation. Nothing was written.")
        Return result
    End Function
End Class
