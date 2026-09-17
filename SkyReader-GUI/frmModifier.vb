Option Strict On
Option Explicit On
Imports System.Threading

Public Class frmModifier
    Inherits Form

    Private ReadOnly connectButton As Button = SimpleUi.Action("Connect Portal")
    Private ReadOnly readButton As Button = SimpleUi.Action("Read Figure")
    Private ReadOnly saveButton As Button = SimpleUi.Action("Save Changes")
    Private ReadOnly backButton As Button = SimpleUi.Action("Back")
    Private ReadOnly goldInput As New NumericUpDown()
    Private ReadOnly levelInput As New NumericUpDown()
    Private ReadOnly loadedName As Label = SimpleUi.Caption("No figure read yet")
    Private ReadOnly status As Label = SimpleUi.Caption("Connect your portal, then read a figure.")
    Private ReadOnly previewName As Label = SimpleUi.Caption("Current Scanned Figure on Portal of Power")
    Private ReadOnly imageBox As New SkyPortalPreview()
    Private ReadOnly gallery As New SkyGameBrowser()
    Private ReadOnly waiting As PictureBox = SkyDecor.Badge("Waiting.ico")
    Private ReadOnly connectionHelp As Label = SimpleUi.Caption("Only Non-Xbox Portals are Compatible with the Program." & vbCrLf &
        "If you are struggling connecting please follow the Zadig process in the Main Menu, Help Portal option.")
    Private saving As Boolean
    Private connectionFailed As Boolean
    Private ReadOnly artwork As New FigureArtwork()
    Private ReadOnly vehicleMode As Boolean
    Private session As SimpleFigureSession
    Private busy As Boolean

    'Builds the shared character or vehicle editor and connects its actions and preview events.
    Public Sub New(Optional editVehicle As Boolean = False)
        vehicleMode = editVehicle
        SimplePortal.Disconnect()
        Text = If(vehicleMode, "SkyGUI - Vehicle Gearbits", "SkyGUI - Level and Gold Modifier")
        Name = "frmModifier"
        SkyAssets.ApplyWindowIcon(Me)
        Font = SimpleUi.Body
        BackColor = SimpleUi.Sky
        ClientSize = New Size(1160, 880)
        MinimumSize = New Size(850, 620)
        StartPosition = FormStartPosition.CenterScreen
        AutoScaleDimensions = New SizeF(96, 96)
        AutoScaleMode = AutoScaleMode.Dpi
        DoubleBuffered = True
        BackgroundImage = SkyDecor.Asset("Shattered_Background.png")
        BackgroundImageLayout = ImageLayout.Stretch
        AutoScroll = True
        AutoScrollMinSize = New Size(1100, 956)
        Dim work As Rectangle = Screen.FromControl(Me).WorkingArea
        ClientSize = New Size(Math.Min(ClientSize.Width, work.Width - 48), Math.Min(ClientSize.Height, work.Height - 80))

        Dim page As New SkyLayoutPanel With {.BackColor = Color.Transparent, .Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 4, .Padding = New Padding(20)}
        page.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 43))
        page.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 57))
        page.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        page.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        page.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, 116))
        Dim heading As Label = SimpleUi.Caption(If(vehicleMode, "Vehicle Gearbits", "Level and Gold Modifier"))
        heading.Font = SimpleUi.Heading
        heading.AutoSize = False
        heading.BackColor = SkyAssets.Panel
        SkyElevation.CompactTitle(heading)
        page.Controls.Add(heading, 0, 0)
        page.SetColumnSpan(heading, 2)

        Dim editor As New SkyCardLayout With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 8, .BackColor = SkyAssets.Panel, .Margin = New Padding(8), .Padding = New Padding(12)}
        For Each height As Integer In New Integer() {70, 70, 82, 54, 70, 54, 70, 74}
            editor.RowStyles.Add(New RowStyle(SizeType.Absolute, height))
        Next
        editor.AutoScroll = True
        editor.Controls.Add(connectButton, 0, 0)
        editor.Controls.Add(readButton, 0, 1)
        loadedName.Font = SimpleUi.ActionFont
        loadedName.AutoSize = False
        editor.Controls.Add(loadedName, 0, 2)
        editor.Controls.Add(SkyDecor.FieldLabel(If(vehicleMode, "Gearbits", "Gold"), If(vehicleMode, "Gearbit.ico", "Gold.ico")), 0, 3)
        goldInput.Maximum = 65000
        goldInput.ThousandsSeparator = True
        levelInput.Minimum = 1
        levelInput.Maximum = 20
        For Each input As NumericUpDown In New NumericUpDown() {goldInput, levelInput}
            input.Font = SimpleUi.NumberFont
            input.BackColor = SkyAssets.Panel
            input.ForeColor = SkyAssets.Ink
            input.Dock = DockStyle.Fill
            input.Margin = New Padding(8, 4, 8, 8)
        Next
        editor.Controls.Add(goldInput, 0, 4)
        If Not vehicleMode Then
            editor.Controls.Add(SkyDecor.FieldLabel("Level", "XP.ico"), 0, 5)
            editor.Controls.Add(levelInput, 0, 6)
        Else
            Dim hint As Label = SimpleUi.Caption("Gearbits range: 0 - 33,000")
            hint.AutoSize = False
            editor.Controls.Add(hint, 0, 5)
            editor.SetRowSpan(hint, 2)
        End If
        editor.Controls.Add(saveButton, 0, 7)

        Dim browser As New SkyCardLayout With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3,
            .BackColor = SkyAssets.Panel, .Padding = New Padding(12), .Margin = New Padding(8)}
        browser.RowStyles.Add(New RowStyle(SizeType.Absolute, 126))
        browser.RowStyles.Add(New RowStyle(SizeType.Percent, 60))
        browser.RowStyles.Add(New RowStyle(SizeType.Percent, 40))
        previewName.AutoSize = False
        previewName.Font = SimpleUi.ActionFont
        imageBox.Dock = DockStyle.Fill
        imageBox.BackColor = SkyAssets.Panel
        gallery.Dock = DockStyle.Fill
        gallery.Font = SimpleUi.Body
        gallery.BackColor = SkyAssets.Panel
        gallery.ForeColor = SkyAssets.Ink
        browser.Controls.Add(previewName, 0, 0)
        browser.Controls.Add(imageBox, 0, 1)
        browser.Controls.Add(gallery, 0, 2)
        page.Controls.Add(editor, 0, 1)
        page.Controls.Add(browser, 1, 1)
        page.Controls.Add(backButton, 0, 2)
        status.AutoSize = True
        status.Dock = DockStyle.Fill
        status.MaximumSize = New Size(470, 0)
        status.Padding = New Padding(8)
        status.Margin = New Padding(0)
        Dim statusCard As New SkyCardLayout With {.Dock = DockStyle.None, .Anchor = AnchorStyles.Top Or AnchorStyles.Left,
            .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink, .ColumnCount = 2, .RowCount = 1,
            .Padding = New Padding(8), .Margin = New Padding(8, 8, 8, 12), .BackColor = SkyAssets.Panel}
        statusCard.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        statusCard.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 44))
        statusCard.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        statusCard.Controls.Add(status, 0, 0)
        statusCard.Controls.Add(waiting, 1, 0)
        waiting.Visible = False
        page.Controls.Add(statusCard, 1, 2)
        SkyElevation.CompactTitle(connectionHelp)
        connectionHelp.AutoSize = True
        connectionHelp.MaximumSize = New Size(1000, 0)
        connectionHelp.BackColor = SkyAssets.Panel
        connectionHelp.Visible = False
        page.Controls.Add(connectionHelp, 0, 3)
        page.SetColumnSpan(connectionHelp, 2)
        Controls.Add(page)

        AddHandler connectButton.Click, AddressOf ConnectPortal
        AddHandler readButton.Click, AddressOf ReadFigure
        AddHandler saveButton.Click, AddressOf SaveFigure
        AddHandler backButton.Click, Sub(sender, e) Close()
        AddHandler gallery.PreviewChanged, Sub(sender, e)
                                               If Not busy AndAlso session Is Nothing Then ShowPreview(Me, EventArgs.Empty)
                                           End Sub
        SimpleUi.StyleButtons(Me)
        LoadGallery(Me, EventArgs.Empty)
        RefreshActions()
    End Sub

    'Enables or disables page actions according to connection, loaded data, and operation state.
    Private Sub RefreshActions()
        connectButton.Enabled = Not busy
        readButton.Enabled = Not busy AndAlso Portal.blnPortal
        saveButton.Enabled = Not busy AndAlso Portal.blnPortal AndAlso session IsNot Nothing AndAlso session.CanEdit
        goldInput.Enabled = saveButton.Enabled
        levelInput.Enabled = saveButton.Enabled
        backButton.Enabled = Not busy
        waiting.Visible = saving
        connectionHelp.Visible = connectionFailed
        gallery.Enabled = Not busy
    End Sub

    'portal connection and updates the page with connection status or troubleshooting guidance.
    Private Sub ConnectPortal(sender As Object, e As EventArgs)
        If busy Then Return
        busy = True
        ClearSession()
        RefreshActions()
        Try
            If SimplePortal.Connect() Then
                connectionFailed = False
                status.Text = "Portal connected. Place one figure on it, then select Read figure."
            Else
                connectionFailed = True
                connectionHelp.Visible = True
                status.Text = "Portal not found."
                MessageBox.Show(Me, "No portal was found. Connect your portal and try again.", "Connect portal", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            FailOperation(ex)
        Finally
            busy = False
            RefreshActions()
        End Try
    End Sub

    'Reads one figure under a timeout and creates a new character or vehicle session.
    Private Async Sub ReadFigure(sender As Object, e As EventArgs)
        If busy Then Return
        busy = True
        ClearSession()
        loadedName.Text = "Reading figure..."
        status.Text = "Keep one figure on the portal. Swap Force: keep both halves assembled while the bottom and then the top are read."
        RefreshActions()
        Try
            Using timeout As New CancellationTokenSource(TimeSpan.FromSeconds(30))
                Dim bytes As Byte() = Await SimplePortal.ReadFigureAsync(timeout.Token)
                DisplaySession(New SimpleFigureSession(bytes, vehicleMode))
                ShowFigureWarnings()
            End Using
        Catch ex As Exception
            FailOperation(ex)
        Finally
            busy = False
            RefreshActions()
        End Try
    End Sub

    'Prepares the requested values and writes them through the portal with a timeout and readback verification.
    Private Async Sub SaveFigure(sender As Object, e As EventArgs)
        If busy OrElse session Is Nothing OrElse Not session.CanEdit Then Return
        If goldInput.Value = session.GoldValue AndAlso levelInput.Value = session.LevelValue AndAlso Not session.NeedsChecksumRepair Then
            status.Text = "There are no changes to save."
            Return
        End If
        busy = True
        saving = True
        RefreshActions()
        status.Text = "Saving to " & session.FigureName & ". Keep the figure on the portal."
        Try
            Dim original As Byte() = session.Original
            Dim updated As Byte() = session.BuildSave(goldInput.Value, levelInput.Value)
            Using timeout As New CancellationTokenSource(TimeSpan.FromSeconds(60))
                Dim verified As Byte() = Await SimplePortal.SaveAsync(original, updated, timeout.Token, vehicleMode, session.RequiresFullEncryption)
                DisplaySession(New SimpleFigureSession(verified, vehicleMode))
            End Using
            status.Text = If(vehicleMode, "Gearbits saved and read back successfully.", "Level and Gold saved and read back successfully.")
        Catch ex As Exception
            FailOperation(ex)
        Finally
            saving = False
            busy = False
            RefreshActions()
        End Try
    End Sub

    'Copies the scanned name, editable values, and limits into the simplified editor controls.
    Private Sub DisplaySession(value As SimpleFigureSession)
        session = value
        loadedName.Text = "Loaded: " & value.FigureName & If(SimplePortal.IsSwapTop(value.Original), " (top half)", "")
        goldInput.Maximum = value.GoldMaximum
        levelInput.Maximum = value.LevelMaximum
        goldInput.Value = Math.Min(value.GoldValue, goldInput.Maximum)
        levelInput.Value = Math.Max(levelInput.Minimum, Math.Min(value.LevelValue, levelInput.Maximum))
        ShowPreview(Me, EventArgs.Empty)
        status.Text = If(value.CanEdit, If(vehicleMode, "Vehicle loaded. Change Gearbits, then save.", "Figure loaded. Change Gold or Level, then save."),
            If(value.IsUnsafe, "Unsafe figure data. Saving is disabled. Please press Connect Portal and try again.", If(vehicleMode, "Read a supported vehicle to edit Gearbits.", "This figure is preview-only here. Use its matching editor if one exists on a different page.")))
        If value.CanEdit AndAlso SimplePortal.IsSwapTop(value.Original) Then
            status.Text = "Swap Force top half loaded. Gold, and Level changes save to the top; the bottom stays unchanged."
        End If
        If value.CanEdit AndAlso value.NeedsChecksumRepair Then
            status.Text = "Figure loaded. Change Gold or Level, then save."
        End If
    End Sub

    'Displays the relevant unsafe data or Sensei initialization guidance for the loaded session.
    Private Sub ShowFigureWarnings()
        If session Is Nothing Then Return
        If session.IsSensei Then FigureWarnings.ShowWarning(Me, "Sensei initialization", FigureWarnings.SenseiText)
        If session.IsUnsafe Then
            Dim message As String = If(session.IsVehicle,
                "This vehicle has uninitialized or unsafe save data. Keep a backup. Initialize it in Skylanders SuperChargers and collect at least 1 Gearbits, then read it again. If recovery requires an in-game reset, remember that resetting clears progress.",
                FigureWarnings.UnsafeText)
            FigureWarnings.ShowWarning(Me, If(session.IsVehicle, "Vehicle unsafe to write", "Skylander corrupted or unsafe to write"), message)
        End If
    End Sub

    'Builds the expandable game and figure list used for pre-scan artwork browsing.
    Private Sub LoadGallery(sender As Object, e As EventArgs)
        gallery.LoadCatalog()
        ShowPreview(Me, EventArgs.Empty)
    End Sub

    'Displays scanned artwork or the currently browsed preview and disposes the previous image.
    Private Sub ShowPreview(sender As Object, e As EventArgs)
        Dim old As Image = imageBox.Image
        If session IsNot Nothing Then
            previewName.Text = "Current Scanned Figure on Portal of Power" & vbCrLf & session.FigureName
            imageBox.Image = artwork.Load(If(session.IsVehicle, "Vehicles", session.GameName), session.FigureName)
        ElseIf gallery.SelectedFigure IsNot Nothing Then
            previewName.Text = "Preview: " & gallery.SelectedFigure & vbCrLf & "No scanned figure. Hover the list to preview artwork."
            imageBox.Image = artwork.Load(gallery.SelectedGame, gallery.SelectedFigure)
        Else
            previewName.Text = "Current Scanned Figure on Portal of Power" & vbCrLf & "No scanned figure. Expand a game and hover a name to preview."
            imageBox.Image = Nothing
        End If
        If old IsNot Nothing Then old.Dispose()
        imageBox.Invalidate()
    End Sub

    'reports a failed or timed-out operation and clears stale editor session state.
    Private Sub FailOperation(ex As Exception)
        SimplePortal.Disconnect()
        connectionFailed = True
        connectionHelp.Visible = True
        ClearSession()
        loadedName.Text = "Read a figure to continue"
        status.Text = If(TypeOf ex Is OperationCanceledException, "The portal did not respond in time. Reconnect and read the figure again.", ex.Message)
        DeviceManagement.DebugWrite("Simple modifier: " & ex.ToString())
        FigureWarnings.ShowWarning(Me, If(TypeOf ex Is IO.InvalidDataException, "Figure unsafe to write", "Portal operation interrupted"), status.Text)
    End Sub

    'Clears the scanned session so later actions cannot reuse old figure data.
    Private Sub ClearSession()
        session = Nothing
        loadedName.Text = "No figure read yet"
        'The artwork browser remains populated after a disconnect or failed read.
        ShowPreview(Me, EventArgs.Empty)
    End Sub

    'Prevents closing during a portal operation and disconnects when the editor can close.
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If busy Then
            e.Cancel = True
            status.Text = "Please wait for the portal operation to finish."
            Return
        End If
        SimplePortal.Disconnect()
        MyBase.OnFormClosing(e)
    End Sub

    'Disposes the current preview image when the editor is disposed.
    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso imageBox.Image IsNot Nothing Then
            imageBox.Image.Dispose()
            imageBox.Image = Nothing
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
