Option Strict On
Option Explicit On

Imports System.IO

'A scanned snapshot and the values permitted by one editor. Browsing artwork
'never changes this snapshot. No parser or writer depends on a hidden form.
Friend NotInheritable Class SimpleFigureSession
    Private ReadOnly raw As Byte()
    Private ReadOnly crystal As CrystalSession
    Private ReadOnly vehicleMode As Boolean
    Private ReadOnly crystalMode As Boolean

    Friend ReadOnly Property Original As Byte()
        Get
            Return DirectCast(raw.Clone(), Byte())
        End Get
    End Property
    Friend ReadOnly Property Identity As ScannedFigureInfo
    Friend ReadOnly Property IsPreviewOnly As Boolean
    Friend ReadOnly Property IsVehicle As Boolean
    Friend ReadOnly Property IsCrystal As Boolean
    Friend ReadOnly Property IsSensei As Boolean
    Friend ReadOnly Property IsUnsafe As Boolean
    Friend ReadOnly Property NeedsSenseiInitialization As Boolean
    Friend ReadOnly Property CanEdit As Boolean
    Friend ReadOnly Property NeedsChecksumRepair As Boolean
    Friend ReadOnly Property Notice As String
    Friend ReadOnly Property FigureName As String
    Friend ReadOnly Property GameName As String
    Friend ReadOnly Property GoldValue As Decimal
    Friend ReadOnly Property LevelValue As Decimal = 1D
    Friend ReadOnly Property GoldMaximum As Decimal = 65000D
    Friend ReadOnly Property LevelMaximum As Decimal = 20D

    Friend Sub New(bytes As Byte(), Optional editVehicle As Boolean = False, Optional editCrystal As Boolean = False)
        If bytes Is Nothing OrElse bytes.Length <> FigureData.DumpSize Then Throw New InvalidDataException("Read a complete figure first.")
        If editVehicle AndAlso editCrystal Then Throw New ArgumentException("Select one editor mode.")
        raw = DirectCast(bytes.Clone(), Byte())
        vehicleMode = editVehicle : crystalMode = editCrystal
        Identity = New ScannedFigureInfo(raw)
        FigureName = Identity.FigureName : GameName = Identity.GameName
        IsVehicle = Identity.Editor = FigureEditorKind.Vehicles
        IsCrystal = Identity.Editor = FigureEditorKind.Crystals
        IsSensei = GameName = "Imaginators" AndAlso Identity.Editor = FigureEditorKind.Skylanders
        Dim editorKind = If(editCrystal, FigureEditorKind.Crystals, If(editVehicle, FigureEditorKind.Vehicles, FigureEditorKind.Skylanders))
        If Not Identity.Matches(editorKind) Then
            IsPreviewOnly = True
            Notice = Identity.ReadOnlyMessage
            Return
        End If
        If editCrystal Then
            crystal = New CrystalSession(raw)
            GoldValue = crystal.GoldValue : LevelValue = crystal.LevelValue
            GoldMaximum = 65535D : LevelMaximum = CrystalSession.MaximumLevel
            CanEdit = crystal.CanEdit : Notice = crystal.Notice
            Return
        End If
        FigureData.ValidateTag(raw)
        Try
            Dim encrypted As Boolean
            Dim plain = CharacterData.Decode(raw, encrypted, editVehicle, IsSensei)
            If editVehicle Then
                GoldMaximum = 33000D : LevelMaximum = 1D
                GoldValue = CharacterData.Gearbits(plain)
                If GoldValue > GoldMaximum Then Throw New InvalidDataException("Vehicle Gearbits are outside the supported range.")
            Else
                If IsSensei Then
                    If Not CharacterData.CanEditSensei(plain, encrypted) Then
                        Throw New InvalidDataException("The Sensei's save data could not be verified. Saving is disabled.")
                    End If
                    'A verified blank Sensei needs its first in-game save before editing.
                    If Not encrypted Then
                        NeedsSenseiInitialization = True
                        Notice = FigureWarnings.SenseiText
                        Return
                    End If
                End If
                GoldValue = CharacterData.Gold(plain)
                LevelValue = CharacterData.Level(plain, IsSensei)
                NeedsChecksumRepair = Not IsSensei AndAlso (Not encrypted OrElse Not CharacterData.Initialized(plain))
            End If
            CanEdit = True
        Catch ex As InvalidDataException
            IsUnsafe = True
            Notice = ex.Message
            GoldValue = 0D : LevelValue = 1D
        End Try
    End Sub

    Friend Function BuildSave(gold As Decimal, level As Decimal) As Byte()
        If Not CanEdit Then Throw New InvalidOperationException(If(Notice, "Read a supported figure in its matching editor before saving."))
        If gold <> Decimal.Truncate(gold) OrElse level <> Decimal.Truncate(level) OrElse
            gold < 0 OrElse gold > GoldMaximum OrElse level < 1 OrElse level > LevelMaximum Then
            Throw New ArgumentOutOfRangeException("Gold/Level")
        End If
        If gold = GoldValue AndAlso level = LevelValue AndAlso Not NeedsChecksumRepair Then Return Original
        Dim result As Byte()
        If IsCrystal Then
            result = crystal.BuildSave(gold, If(level <> LevelValue, CType(CInt(level), Integer?), Nothing))
        ElseIf IsVehicle Then
            result = CharacterData.SaveGearbits(raw, gold)
        Else
            result = CharacterData.SaveCharacter(raw, gold, level, IsSensei)
        End If
        Dim check As New SimpleFigureSession(result, vehicleMode, crystalMode)
        If Not check.CanEdit OrElse check.GoldValue <> gold OrElse check.LevelValue <> level Then
            Throw New InvalidDataException("Prepared Gold/Level did not match the requested values. Nothing was written.")
        End If
        Return result
    End Function

    'The portal checks the exact deterministic edit again before any writes, not
    'merely that changed bytes happen to be inside an allowed block.
    Friend Function WriteBlocks(updated As Byte()) As Integer()
        If Not CanEdit OrElse updated Is Nothing OrElse updated.Length <> FigureData.DumpSize Then Throw New InvalidDataException("Invalid figure save.")
        If IsCrystal Then Return crystal.WriteBlocks(updated)
        Dim check As New SimpleFigureSession(updated, vehicleMode)
        If Not check.CanEdit OrElse Not BuildSave(check.GoldValue, check.LevelValue).SequenceEqual(updated) Then
            Throw New InvalidDataException("The prepared save changes unrelated or invalid data. Nothing was written.")
        End If
        Return CharacterData.WriteBlocks(raw, IsSensei, IsVehicle)
    End Function
End Class
