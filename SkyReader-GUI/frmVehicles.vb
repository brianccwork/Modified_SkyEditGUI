Imports SkyReader_GUI.FigureIO

Public Class frmVehicles

    'Vehicle Gearbits have an in-game maximum of 33000.
    Private Const MaxGearBits As UShort = 33000US

    'Raw MiFare blocks are 16 bytes each.
    Private Const BlockSize As Integer = 16

    'Copy enough raw bytes from the active vehicle region to the inactive one
    'so the gearbits, sequence bytes, and CRC-related data all move together.
    Private Const RegionCopyLength As Integer = &HE0

    'This helper class describes one of the two mirrored vehicle data regions.
    'Groups the byte offsets used to read and update one vehicle save copy.
    Private NotInheritable Class VehicleSlot
        'Locates the main header of this vehicle save copy.
        Public Property HeaderBase As Integer
        'Locates the extended record of this vehicle save copy.
        Public Property ExtendedBase As Integer
        'Locates the main sequence counter for this vehicle copy.
        Public Property Sequence1Offset As Integer
        'Locates the extended sequence counter for this vehicle copy.
        Public Property Sequence2Offset As Integer
        'Locates the two-byte Gearbits value for this vehicle copy.
        Public Property GearBitsOffset As Integer
    End Class

    'Area 0 is the first mirrored vehicle region.
    Private Shared ReadOnly Area0Slot As New VehicleSlot With {
        .HeaderBase = &H80,
        .ExtendedBase = &H110,
        .Sequence1Offset = &H89,
        .Sequence2Offset = &H112,
        .GearBitsOffset = &H118
    }

    'Area 1 is the second mirrored vehicle region.
    Private Shared ReadOnly Area1Slot As New VehicleSlot With {
        .HeaderBase = &H240,
        .ExtendedBase = &H2D0,
        .Sequence1Offset = &H249,
        .Sequence2Offset = &H2D2,
        .GearBitsOffset = &H2D8
    }

    'Configures the vehicle window and reads Gearbits from the selected vehicle data copy.
    Private Sub frmVehicles_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        frmMain.Disable_Controls()

        'This pass is intentionally focused on Gearbits only.
        'The other vehicle editing controls are disabled until they are fully mapped and trusted.
        cmbDecoration.Enabled = False
        cmbTopper.Enabled = False
        cmbNeon.Enabled = False
        cmbShout.Enabled = False
        numWeapon.Enabled = False
        numShield.Enabled = False

        'Preserve the figure identity bytes so they are not disturbed by vehicle edits.
        Figures.CharacterID(0) = WholeFile(&H10)
        Figures.CharacterID(1) = WholeFile(&H11)
        Figures.CharacterVariant(0) = WholeFile(&H1C)
        Figures.CharacterVariant(1) = WholeFile(&H1D)

        Try
            'Determine which mirrored region is currently active and load its Gearbits value into the UI.
            Dim activeSlot As VehicleSlot = SelectActiveSlot()
            Dim currentGearBits As UShort = ReadUInt16LE(activeSlot.GearBitsOffset)

            If currentGearBits > MaxGearBits Then
                currentGearBits = MaxGearBits
            End If

            If currentGearBits > numGearbits.Maximum Then
                numGearbits.Value = numGearbits.Maximum
            Else
                numGearbits.Value = currentGearBits
            End If
        Catch
            numGearbits.Value = 0D
        End Try
    End Sub

    'Routes window closing through the vehicle editor's return-and-save handling.
    Private Sub frmVehicles_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
        Dim result As DialogResult
        result = MessageBox.Show("Do you want to apply any changes made to this figure?", "Apply Changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            SaveVehicleGearBits()
        End If

        frmMain.Show()
        Dispose()
    End Sub

    'Offers to apply Gearbits to the shared buffer before returning to the Developer window.
    Private Sub btnGoBack_Click(sender As Object, e As EventArgs) Handles btnGoBack.Click
        Dim result As DialogResult
        result = MessageBox.Show("Do you want to apply any changes made to this figure?", "Apply Changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            SaveVehicleGearBits()
        End If

        frmMain.Show()
        Dispose()
    End Sub

    'Returns Gearbits from the vehicle's selected active slot without opening the window.
    Friend Function ReadGearbitsForSimpleEditor() As Decimal
        Return CDec(ReadUInt16LE(SelectActiveSlot().GearBitsOffset))
    End Function

    'Checks vehicle values and rebuilt checksum bytes while restoring the original data buffer afterward.
    Friend Function IsSafeForSimpleEditor() As Boolean
        Dim original As Byte() = DirectCast(WholeFile.Clone(), Byte())
        Dim slot As VehicleSlot = SelectActiveSlot()
        Try
            If Not HasLikelyVehicleData(slot) OrElse ReadUInt16LE(slot.GearBitsOffset) > MaxGearBits Then Return False
            RewriteVehicleChecksums(slot)
            For Each offset As Integer In New Integer() {slot.HeaderBase + &HA, slot.HeaderBase + &HB,
                slot.HeaderBase + &HC, slot.HeaderBase + &HD, slot.HeaderBase + &HE, slot.HeaderBase + &HF,
                slot.ExtendedBase, slot.ExtendedBase + 1}
                If WholeFile(offset) <> original(offset) Then Return False
            Next
            Return True
        Finally
            WholeFile = original
        End Try
    End Function

    'Checks the requested Gearbits range and vehicle data before preparing the in-memory update.
    Friend Sub ApplyGearbitsForSimpleEditor(value As Decimal)
        If value < 0D OrElse value > MaxGearBits Then Throw New ArgumentOutOfRangeException(NameOf(value))
        If Not IsSafeForSimpleEditor() Then Throw New IO.InvalidDataException("Vehicle data is unsafe to write.")
        numGearbits.Value = value
        SaveVehicleGearBits()
    End Sub

    'Apply the current Gearbits value to the in-memory vehicle data.
    'This does not write to the portal by itself. The main form write step still does that later.
    Private Sub SaveVehicleGearBits()
        Dim requestedGearBits As UShort = CUShort(Math.Min(CInt(numGearbits.Value), CInt(MaxGearBits)))

        'Read the currently active mirrored region and choose the other region as the write target.
        Dim activeSlot As VehicleSlot = SelectActiveSlot()
        Dim inactiveSlot As VehicleSlot = If(activeSlot Is Area0Slot, Area1Slot, Area0Slot)

        'Preserve identity bytes so the figure ID and variant bytes are not changed by this editor.
        Figures.CharacterID(0) = WholeFile(&H10)
        Figures.CharacterID(1) = WholeFile(&H11)
        Figures.CharacterVariant(0) = WholeFile(&H1C)
        Figures.CharacterVariant(1) = WholeFile(&H1D)

        'Copy the active raw region into the inactive raw region first.
        'This mirrors how the games update one area, then advance the sequence so the new area becomes active.
        Buffer.BlockCopy(WholeFile, activeSlot.HeaderBase, WholeFile, inactiveSlot.HeaderBase, RegionCopyLength)

        'Write the new Gearbits value into the inactive region.
        WriteUInt16LE(inactiveSlot.GearBitsOffset, requestedGearBits)

        'Advance both sequence bytes in the inactive region so it becomes the newer active region.
        WholeFile(inactiveSlot.Sequence1Offset) = IncrementByte(WholeFile(activeSlot.Sequence1Offset))
        WholeFile(inactiveSlot.Sequence2Offset) = IncrementByte(WholeFile(activeSlot.Sequence2Offset))

        'Rebuild the vehicle-specific checksums for the region we just updated.
        RewriteVehicleChecksums(inactiveSlot)

        'Restore identity bytes as an extra safety step.
        WholeFile(&H10) = Figures.CharacterID(0)
        WholeFile(&H11) = Figures.CharacterID(1)
        WholeFile(&H1C) = Figures.CharacterVariant(0)
        WholeFile(&H1D) = Figures.CharacterVariant(1)

        frmMain.SaldeStatus.Text = "Vehicle gearbits updated in memory."
    End Sub

    'Decide which mirrored region is currently active.
    'The newer region is the one whose sequence byte is one step ahead.
    Private Function SelectActiveSlot() As VehicleSlot
        Dim area0Seq As Byte = WholeFile(Area0Slot.Sequence1Offset)
        Dim area1Seq As Byte = WholeFile(Area1Slot.Sequence1Offset)

        Dim area0Populated As Boolean = HasLikelyVehicleData(Area0Slot)
        Dim area1Populated As Boolean = HasLikelyVehicleData(Area1Slot)

        If area0Populated AndAlso area1Populated Then
            If IncrementByte(area0Seq) = area1Seq Then
                Return Area1Slot
            End If

            If IncrementByte(area1Seq) = area0Seq Then
                Return Area0Slot
            End If

            'Default to Area 0 if the sequence relationship is unclear.
            Return Area0Slot
        End If

        If (Not area0Populated) AndAlso area1Populated Then
            Return Area1Slot
        End If

        Return Area0Slot
    End Function

    'Decide whether a mirrored slot looks like it contains usable vehicle data.
    'Treats nonzero Gearbits or sequence bytes as evidence that a vehicle slot is populated.
    Private Function HasLikelyVehicleData(slot As VehicleSlot) As Boolean
        Dim gearBits As UShort = ReadUInt16LE(slot.GearBitsOffset)
        Return gearBits > 0US OrElse
               WholeFile(slot.Sequence1Offset) <> 0 OrElse
               WholeFile(slot.Sequence2Offset) <> 0
    End Function

    'Read a little-endian UInt16 from WholeFile.
    Private Function ReadUInt16LE(offset As Integer) As UShort
        Return CUShort(CInt(WholeFile(offset)) Or (CInt(WholeFile(offset + 1)) << 8))
    End Function

    'Write a little-endian UInt16 into WholeFile.
    Private Sub WriteUInt16LE(offset As Integer, value As UShort)
        WholeFile(offset) = CByte(value And &HFFUS)
        WholeFile(offset + 1) = CByte((value >> 8) And &HFFUS)
    End Sub

    'Increment a sequence byte with wraparound.
    Private Function IncrementByte(value As Byte) As Byte
        Return CByte((CInt(value) + 1) And &HFF)
    End Function

    'Rebuild the vehicle checksums for the chosen mirrored slot.
    'These are vehicle specific and are not handled the same way as ordinary figure edits.
    Private Sub RewriteVehicleChecksums(slot As VehicleSlot)
        Dim headerBlock As Integer = slot.HeaderBase \ BlockSize

        'CRC2 uses the non-ACB blocks immediately after the header region.
        Dim crc2Data() As Byte = CollectNonAcbBlocks(headerBlock + 1, 3)
        Dim crc2 As UShort = ComputeCrc16CcittFalse(crc2Data)
        WriteUInt16LE(slot.HeaderBase + &HC, crc2)

        'CRC1 uses later non-ACB blocks plus zero padding.
        Dim crc1Core() As Byte = CollectNonAcbBlocks(headerBlock + 5, 3)
        Dim crc1Data(&H10F) As Byte
        Buffer.BlockCopy(crc1Core, 0, crc1Data, 0, crc1Core.Length)
        Dim crc1 As UShort = ComputeCrc16CcittFalse(crc1Data)
        WriteUInt16LE(slot.HeaderBase + &HA, crc1)

        'CRC3 uses the first 14 bytes of the header plus the constant bytes 05 00.
        Dim crc3Data(&HF) As Byte
        Buffer.BlockCopy(WholeFile, slot.HeaderBase, crc3Data, 0, &HE)
        crc3Data(&HE) = &H5
        crc3Data(&HF) = &H0
        Dim crc3 As UShort = ComputeCrc16CcittFalse(crc3Data)
        WriteUInt16LE(slot.HeaderBase + &HE, crc3)

        'CRC4 uses 06 01 and the extended data region while skipping the access control block.
        Dim crc4Data(&H3F) As Byte
        crc4Data(0) = &H6
        crc4Data(1) = &H1

        Buffer.BlockCopy(WholeFile, slot.ExtendedBase + &H2, crc4Data, &H2, 14)
        Buffer.BlockCopy(WholeFile, slot.ExtendedBase + &H10, crc4Data, &H10, 16)
        Buffer.BlockCopy(WholeFile, slot.ExtendedBase + &H30, crc4Data, &H20, 16)
        Buffer.BlockCopy(WholeFile, slot.ExtendedBase + &H40, crc4Data, &H30, 16)

        Dim crc4 As UShort = ComputeCrc16CcittFalse(crc4Data)
        WriteUInt16LE(slot.ExtendedBase + &H0, crc4)
    End Sub

    'Collects the requested number of payload blocks while skipping access control trailers.
    Private Function CollectNonAcbBlocks(startBlock As Integer, nonAcbCount As Integer) As Byte()
        Dim result((nonAcbCount * BlockSize) - 1) As Byte
        Dim destPos As Integer = 0
        Dim currentBlock As Integer = startBlock
        Dim collected As Integer = 0

        While collected < nonAcbCount
            If Not IsAcbBlock(currentBlock) Then
                Buffer.BlockCopy(WholeFile, currentBlock * BlockSize, result, destPos, BlockSize)
                destPos += BlockSize
                collected += 1
            End If

            currentBlock += 1
        End While

        Return result
    End Function

    'MiFare access control blocks are the last block in each 4-block sector.
    Private Function IsAcbBlock(blockIndex As Integer) As Boolean
        Return (blockIndex Mod 4) = 3
    End Function

    'Compute CRC16-CCITT-FALSE for the supplied byte array.
    Private Function ComputeCrc16CcittFalse(data() As Byte) As UShort
        Dim crc As UInteger = &HFFFFUI

        For i As Integer = 0 To data.Length - 1
            crc = crc Xor (CUInt(data(i)) << 8)

            For bit As Integer = 0 To 7
                If (crc And &H8000UI) <> 0UI Then
                    crc = ((crc << 1) Xor &H1021UI) And &HFFFFUI
                Else
                    crc = (crc << 1) And &HFFFFUI
                End If
            Next
        Next

        Return CUShort(crc And &HFFFFUI)
    End Function

End Class