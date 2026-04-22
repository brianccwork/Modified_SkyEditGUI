Imports SkyReader_GUI.FigureIO

Public Class frmVehicles

    Private Const MaxGearBits As UShort = 33000US
    Private Const BlockSize As Integer = 16
    Private Const RegionCopyLength As Integer = &HE0

    Private NotInheritable Class VehicleSlot
        Public Property HeaderBase As Integer
        Public Property ExtendedBase As Integer
        Public Property Sequence1Offset As Integer
        Public Property Sequence2Offset As Integer
        Public Property GearBitsOffset As Integer
    End Class

    Private Shared ReadOnly Area0Slot As New VehicleSlot With {
        .HeaderBase = &H80,
        .ExtendedBase = &H110,
        .Sequence1Offset = &H89,
        .Sequence2Offset = &H112,
        .GearBitsOffset = &H118
    }

    Private Shared ReadOnly Area1Slot As New VehicleSlot With {
        .HeaderBase = &H240,
        .ExtendedBase = &H2D0,
        .Sequence1Offset = &H249,
        .Sequence2Offset = &H2D2,
        .GearBitsOffset = &H2D8
    }

    Private Sub frmVehicles_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        frmMain.Disable_Controls()

        ' Gearbits-only pass for now.
        cmbDecoration.Enabled = False
        cmbTopper.Enabled = False
        cmbNeon.Enabled = False
        cmbShout.Enabled = False
        numWeapon.Enabled = False
        numShield.Enabled = False

        Figures.CharacterID(0) = WholeFile(&H10)
        Figures.CharacterID(1) = WholeFile(&H11)
        Figures.CharacterVariant(0) = WholeFile(&H1C)
        Figures.CharacterVariant(1) = WholeFile(&H1D)

        Try
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

    Private Sub frmVehicles_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
        Dim result As DialogResult
        result = MessageBox.Show("Do you want to apply any changes made to this figure?", "Apply Changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            SaveVehicleGearBits()
        End If

        frmMain.Show()
        Dispose()
    End Sub

    Private Sub btnGoBack_Click(sender As Object, e As EventArgs) Handles btnGoBack.Click
        Dim result As DialogResult
        result = MessageBox.Show("Do you want to apply any changes made to this figure?", "Apply Changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            SaveVehicleGearBits()
        End If

        frmMain.Show()
        Dispose()
    End Sub

    Private Sub SaveVehicleGearBits()
        Dim requestedGearBits As UShort = CUShort(Math.Min(CInt(numGearbits.Value), CInt(MaxGearBits)))

        Dim activeSlot As VehicleSlot = SelectActiveSlot()
        Dim inactiveSlot As VehicleSlot = If(activeSlot Is Area0Slot, Area1Slot, Area0Slot)

        Figures.CharacterID(0) = WholeFile(&H10)
        Figures.CharacterID(1) = WholeFile(&H11)
        Figures.CharacterVariant(0) = WholeFile(&H1C)
        Figures.CharacterVariant(1) = WholeFile(&H1D)

        Buffer.BlockCopy(WholeFile, activeSlot.HeaderBase, WholeFile, inactiveSlot.HeaderBase, RegionCopyLength)

        WriteUInt16LE(inactiveSlot.GearBitsOffset, requestedGearBits)

        WholeFile(inactiveSlot.Sequence1Offset) = IncrementByte(WholeFile(activeSlot.Sequence1Offset))
        WholeFile(inactiveSlot.Sequence2Offset) = IncrementByte(WholeFile(activeSlot.Sequence2Offset))

        RewriteVehicleChecksums(inactiveSlot)

        WholeFile(&H10) = Figures.CharacterID(0)
        WholeFile(&H11) = Figures.CharacterID(1)
        WholeFile(&H1C) = Figures.CharacterVariant(0)
        WholeFile(&H1D) = Figures.CharacterVariant(1)

        frmMain.SaldeStatus.Text = "Vehicle gearbits updated in memory."
    End Sub

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

            Return Area0Slot
        End If

        If (Not area0Populated) AndAlso area1Populated Then
            Return Area1Slot
        End If

        Return Area0Slot
    End Function

    Private Function HasLikelyVehicleData(slot As VehicleSlot) As Boolean
        Dim gearBits As UShort = ReadUInt16LE(slot.GearBitsOffset)
        Return gearBits > 0US OrElse
               WholeFile(slot.Sequence1Offset) <> 0 OrElse
               WholeFile(slot.Sequence2Offset) <> 0
    End Function

    Private Function ReadUInt16LE(offset As Integer) As UShort
        Return CUShort(CInt(WholeFile(offset)) Or (CInt(WholeFile(offset + 1)) << 8))
    End Function

    Private Sub WriteUInt16LE(offset As Integer, value As UShort)
        WholeFile(offset) = CByte(value And &HFFUS)
        WholeFile(offset + 1) = CByte((value >> 8) And &HFFUS)
    End Sub

    Private Function IncrementByte(value As Byte) As Byte
        Return CByte((CInt(value) + 1) And &HFF)
    End Function

    Private Sub RewriteVehicleChecksums(slot As VehicleSlot)
        Dim headerBlock As Integer = slot.HeaderBase \ BlockSize

        ' CRC2 = non-ACB blocks 09, 0A, 0C
        Dim crc2Data() As Byte = CollectNonAcbBlocks(headerBlock + 1, 3)
        Dim crc2 As UShort = ComputeCrc16CcittFalse(crc2Data)
        WriteUInt16LE(slot.HeaderBase + &HC, crc2)

        ' CRC1 = non-ACB blocks 0D, 0E, 10 + 0xE0 zeros
        Dim crc1Core() As Byte = CollectNonAcbBlocks(headerBlock + 5, 3)
        Dim crc1Data(&H10F) As Byte
        Buffer.BlockCopy(crc1Core, 0, crc1Data, 0, crc1Core.Length)
        Dim crc1 As UShort = ComputeCrc16CcittFalse(crc1Data)
        WriteUInt16LE(slot.HeaderBase + &HA, crc1)

        ' CRC3 = first 14 bytes + 05 00
        Dim crc3Data(&HF) As Byte
        Buffer.BlockCopy(WholeFile, slot.HeaderBase, crc3Data, 0, &HE)
        crc3Data(&HE) = &H5
        crc3Data(&HF) = &H0
        Dim crc3 As UShort = ComputeCrc16CcittFalse(crc3Data)
        WriteUInt16LE(slot.HeaderBase + &HE, crc3)

        ' CRC4 = 06 01 + 0x3E bytes from extended region, skipping ACB
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

    Private Function IsAcbBlock(blockIndex As Integer) As Boolean
        Return (blockIndex Mod 4) = 3
    End Function

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