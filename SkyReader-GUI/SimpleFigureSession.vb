Option Strict On
Option Explicit On

Imports System.IO
Imports System.Linq

'Bridges the simple page to the established figure parser and Gold/EXP writers.
'Artwork browsing never calls SelectFigure and never changes this session.
Friend NotInheritable Class SimpleFigureSession
    Friend Property Original As Byte()
        Get
            Return If(raw Is Nothing, Nothing, DirectCast(raw.Clone(), Byte()))
        End Get
        Private Set(value As Byte())
            raw = value
        End Set
    End Property
    Private raw As Byte()
    Friend ReadOnly Property IsVehicle As Boolean
    Friend ReadOnly Property IsSensei As Boolean
    Friend ReadOnly Property IsUnsafe As Boolean
    Private ReadOnly vehicleMode As Boolean
    Friend ReadOnly Property CanEdit As Boolean
    Friend ReadOnly Property FigureName As String
    Friend ReadOnly Property GameName As String
    Friend ReadOnly Property GoldValue As Decimal
    Friend ReadOnly Property LevelValue As Decimal
    Friend ReadOnly Property GoldMaximum As Decimal
    Friend ReadOnly Property LevelMaximum As Decimal
    Friend ReadOnly Property Catalog As New List(Of String)

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
        GoldValue = frmMain.numGold.Value
        LevelValue = frmMain.numLevel.Value
        GoldMaximum = frmMain.numGold.Maximum
        LevelMaximum = frmMain.numLevel.Maximum
        Dim supportedGames As String() = {"Spyro's Adventure", "Giants", "Swap Force", "Trap Team", "SuperChargers", "Imaginators"}
        CanEdit = Not FigureIO.blnTrap AndAlso Not FigureIO.BlnVehicle AndAlso Not FigureIO.blnCrystal AndAlso
                  supportedGames.Contains(GameName) AndAlso
                  frmMain.lstCharacters.SelectedItem IsNot Nothing AndAlso frmMain.numGold.Enabled AndAlso frmMain.numLevel.Enabled AndAlso
                  frmMain.Save_Enc_ToolStripMenuItem.Enabled
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
                supportedGames.Contains(GameName) AndAlso FigureWarnings.HasUnsafeCharacterData(SimplePortal.IsSwapTop(Original))
            CanEdit = CanEdit AndAlso Not vehicleMode AndAlso Not IsUnsafe
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

    Friend Function BuildSave(gold As Decimal, level As Decimal) As Byte()
        If Not CanEdit Then Throw New InvalidOperationException("This figure cannot be edited on this page.")
        If gold < 0 OrElse gold > GoldMaximum OrElse level < 1 OrElse level > LevelMaximum Then
            Throw New ArgumentOutOfRangeException("The requested values are outside this editor's limits.")
        End If
        If gold = GoldValue AndAlso level = LevelValue Then Return Original

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
            If gold <> GoldValue Then Global.SkyReader_GUI.Gold.WriteGold()
            If level <> LevelValue Then
                'The existing non-Sensei writer leaves bucket 3 untouched at 11-15.
                'Clear that bucket on a downgrade before using its normal mapping.
                If Not FigureIO.blnSensei AndAlso level <= 15 Then
                    Array.Clear(FigureIO.WholeFile, &H118, 3)
                    Array.Clear(FigureIO.WholeFile, &H2D8, 3)
                End If
                Exp.WriteEXP()
            End If
            Figures.SetArea0AndArea1()
            CRC16CCITT.WriteCheckSums()
            'Header and access bytes are not part of this editor's write surface.
            Array.Copy(plain, 0, FigureIO.WholeFile, 0, &H80)
            FigureIO.Encrypt()
            Dim encrypted As Byte() = FigureIO.WholeFile
            Dim result As Byte() = Original
            For Each block As Integer In New Integer() {8, 17, 36, 45}
                Array.Copy(encrypted, block * 16, result, block * 16, 16)
            Next
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
