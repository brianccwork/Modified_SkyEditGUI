Imports System.Linq
Imports SkyReader_GUI.FigureIO
Imports SkyReader_GUI.frmMain
'WARNING:
'We don't Try and Catch here.  Due to complexity of the values

'Note on Senseis, remember that before reading, make sure Sensei has been initialized in game with at
'least 1 gold, so that when reading all checks succeed and a write to both XP and Gold is possible
Public Class Exp
	'Senseis still use the original 197,500 total EXP level table.
	Private Shared ReadOnly SenseiLevelThresholds As Integer() = {
		0,
		1000,
		2200,
		3800,
		6000,
		9000,
		13000,
		18200,
		24800,
		33000,
		42700,
		53900,
		66600,
		80800,
		96500,
		113700,
		132400,
		152600,
		174300,
		197500
	}

	Private Shared Function ReadUInt16LE(ByVal offset As Integer) As Integer
		Return CInt(WholeFile(offset)) Or (CInt(WholeFile(offset + 1)) << 8)
	End Function

	Private Shared Function ReadUInt24LE(ByVal offset As Integer) As Integer
		Return CInt(WholeFile(offset)) Or (CInt(WholeFile(offset + 1)) << 8) Or (CInt(WholeFile(offset + 2)) << 16)
	End Function

	'Sensei EXP bucket 3 is treated as UInt32 so level 20 can store the full 101,000 remainder.
	Private Shared Function ReadUInt32LE(ByVal offset As Integer) As Integer
		Return CInt(WholeFile(offset)) Or (CInt(WholeFile(offset + 1)) << 8) Or (CInt(WholeFile(offset + 2)) << 16) Or (CInt(WholeFile(offset + 3)) << 24)
	End Function

	Private Shared Sub WriteUInt16LE(ByVal offset As Integer, ByVal value As Integer)
		Dim clampedValue As Integer = Math.Max(0, Math.Min(&HFFFF, value))
		WholeFile(offset) = CByte(clampedValue And &HFF)
		WholeFile(offset + 1) = CByte((clampedValue >> 8) And &HFF)
	End Sub

	Private Shared Sub WriteUInt24LE(ByVal offset As Integer, ByVal value As Integer)
		Dim clampedValue As Integer = Math.Max(0, Math.Min(&HFFFFFF, value))
		WholeFile(offset) = CByte(clampedValue And &HFF)
		WholeFile(offset + 1) = CByte((clampedValue >> 8) And &HFF)
		WholeFile(offset + 2) = CByte((clampedValue >> 16) And &HFF)
	End Sub

	'Sensei EXP bucket 3 is written as 4 bytes, not 3 bytes.
	Private Shared Sub WriteUInt32LE(ByVal offset As Integer, ByVal value As Integer)
		Dim clampedValue As Integer = Math.Max(0, value)
		WholeFile(offset) = CByte(clampedValue And &HFF)
		WholeFile(offset + 1) = CByte((clampedValue >> 8) And &HFF)
		WholeFile(offset + 2) = CByte((clampedValue >> 16) And &HFF)
		WholeFile(offset + 3) = CByte((clampedValue >> 24) And &HFF)
	End Sub

	Private Shared Function SenseiTotalExpToLevel(ByVal totalEXP As Integer) As Integer
		Dim level As Integer = 1
		Dim counter As Integer = 0

		Do Until counter = SenseiLevelThresholds.Length
			If totalEXP >= SenseiLevelThresholds(counter) Then
				level = counter + 1
			End If
			counter += 1
		Loop

		Return Math.Max(1, Math.Min(20, level))
	End Function

	Private Shared Function SenseiLevelToTotalExp(ByVal level As Integer) As Integer
		Dim safeLevel As Integer = Math.Max(1, Math.Min(20, level))
		Return SenseiLevelThresholds(safeLevel - 1)
	End Function

	Private Shared Sub SplitSenseiExperience(ByVal totalEXP As Integer, ByRef exp2011 As Integer, ByRef exp2012 As Integer, ByRef exp2013 As Integer)
		Dim remaining As Integer = Math.Max(0, totalEXP)

		exp2011 = Math.Min(33000, remaining)
		remaining -= exp2011

		exp2012 = Math.Min(63500, remaining)
		remaining -= exp2012

		exp2013 = Math.Max(0, remaining)
	End Sub

	Private Shared Sub GetSenseiEXP()
		'Sensei EXP bucket 3 must be read as UInt32 from &H118 / &H2D8.
		Dim totalArea0 As Integer = ReadUInt16LE(&H80) + ReadUInt16LE(&H113) + ReadUInt32LE(&H118)
		Dim totalArea1 As Integer = ReadUInt16LE(&H240) + ReadUInt16LE(&H2D3) + ReadUInt32LE(&H2D8)
		Dim totalEXP As Integer

		If Area0 > Area1 Then
			totalEXP = totalArea0
		ElseIf Area1 > Area0 Then
			totalEXP = totalArea1
		Else
			totalEXP = totalArea0
		End If

		frmMain.numLevel.Value = SenseiTotalExpToLevel(totalEXP)
	End Sub

	Private Shared Sub WriteSenseiEXP()
		Dim totalEXP As Integer = SenseiLevelToTotalExp(CInt(frmMain.numLevel.Value))
		Dim exp2011 As Integer
		Dim exp2012 As Integer
		Dim exp2013 As Integer

		'
		'Level 20 is 197,500 total EXP, split as 33,000 + 63,500 + 101,000.
		'bucket 1 = up to 33,000, bucket 2 = up to 63,500, bucket 3 = remainder.
		SplitSenseiExperience(totalEXP, exp2011, exp2012, exp2013)

		'Area 0 EXP
		WriteUInt16LE(&H80, exp2011)
		WriteUInt16LE(&H113, exp2012)
		'Write the full 4-byte third EXP bucket for Senseis.
		WriteUInt32LE(&H118, exp2013)

		'Area 1 EXP
		WriteUInt16LE(&H240, exp2011)
		WriteUInt16LE(&H2D3, exp2012)
		'Write the full 4-byte mirrored third EXP bucket for Senseis.
		WriteUInt32LE(&H2D8, exp2013)
	End Sub

    'Main and extended XP regions have independent sequence counters.
    Friend Shared Function RegularRegion(first As Integer, second As Integer, sequenceOffset As Integer) As Integer
        Dim a = WholeFile.Skip(first).Take(16).Any(Function(value) value <> 0)
        Dim b = WholeFile.Skip(second).Take(16).Any(Function(value) value <> 0)
        If Not a AndAlso b Then Return second
        If a AndAlso Not b Then Return first
        If ((CInt(WholeFile(first + sequenceOffset)) + 1) And &HFF) = WholeFile(second + sequenceOffset) Then Return second
        Return first
    End Function

    Shared Sub GetEXP()
        If blnSensei Then
            GetSenseiEXP()
            Return
        End If
        Dim main = RegularRegion(&H80, &H240, 9)
        Dim extended = RegularRegion(&H110, &H2D0, 2)
        Dim total As ULong = CULng(ReadUInt24LE(main))
        'A cleared region-count flag means later-game XP was reset by SSA.
        If (WholeFile(main + &H16) And 1) <> 0 Then
            total += CULng(ReadUInt16LE(extended + 3)) + CULng(BitConverter.ToUInt32(WholeFile, extended + 8))
        End If
        Dim level As Integer = 1
        For index As Integer = 0 To SenseiLevelThresholds.Length - 1
            If total >= CULng(SenseiLevelThresholds(index)) Then level = index + 1
        Next
        frmMain.numLevel.Value = Math.Min(CDec(level), frmMain.numLevel.Maximum)
    End Sub

    Shared Sub WriteEXP()
        If blnSensei Then
            WriteSenseiEXP()
            Return
        End If
        Dim level As Integer = CInt(frmMain.numLevel.Value)
        If level < 1 OrElse level > 20 Then Throw New ArgumentOutOfRangeException("level")
        Dim remaining As Integer = SenseiLevelThresholds(level - 1)
        Dim first = Math.Min(33000, remaining)
        remaining -= first
        Dim second = Math.Min(63500, remaining)
        remaining -= second
        For Each start As Integer In New Integer() {&H80, &H240}
            'Always replace the full 24-, 16- and 32-bit fields, including on downgrades.
            Array.Copy(BitConverter.GetBytes(first), 0, WholeFile, start, 3)
            Array.Copy(BitConverter.GetBytes(second), 0, WholeFile, start + &H93, 2)
            Array.Copy(BitConverter.GetBytes(remaining), 0, WholeFile, start + &H98, 4)
        Next
    End Sub
End Class
