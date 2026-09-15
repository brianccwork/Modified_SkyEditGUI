Option Strict On
Option Explicit On

Imports System.Linq
Imports System.Threading

Public Class frmModifier
    Inherits Form

    Private ReadOnly connectButton As Button = SimpleUi.Action("Connect portal")
    Private ReadOnly readButton As Button = SimpleUi.Action("Read figure")
    Private ReadOnly saveButton As Button = SimpleUi.Action("Save changes")
    Private ReadOnly backButton As Button = SimpleUi.Action("Back")
    Private ReadOnly goldInput As New NumericUpDown()
    Private ReadOnly levelInput As New NumericUpDown()
    Private ReadOnly loadedName As Label = SimpleUi.Caption("No figure read yet")
    Private ReadOnly status As Label = SimpleUi.Caption("Connect your portal, then read a figure.")
    Private ReadOnly previewName As Label = SimpleUi.Caption("Figures")
    Private ReadOnly imageBox As New PictureBox()
    Private ReadOnly gallery As New ListBox()
    Private ReadOnly artwork As New FigureArtwork()
    Private ReadOnly vehicleMode As Boolean
    Private session As SimpleFigureSession
    Private busy As Boolean

    Public Sub New(Optional editVehicle As Boolean = False)
        vehicleMode = editVehicle
        SimplePortal.Disconnect()
        Text = If(vehicleMode, "SkyGUI - Vehicle Gearbits", "SkyGUI - XP / Level Modifier")
        Name = "frmModifier"
        Font = SimpleUi.Body
        BackColor = SimpleUi.Sky
        ClientSize = New Size(1040, 680)
        MinimumSize = New Size(850, 620)
        StartPosition = FormStartPosition.CenterScreen
        AutoScaleDimensions = New SizeF(96, 96)
        AutoScaleMode = AutoScaleMode.Dpi
        DoubleBuffered = True

        Dim page As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 3, .Padding = New Padding(20)}
        page.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 43))
        page.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 57))
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, 76))
        page.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, 64))
        Dim heading As Label = SimpleUi.Caption(If(vehicleMode, "Vehicle Gearbits", "XP / Level Modifier"))
        heading.Font = SimpleUi.Heading
        heading.AutoSize = False
        page.Controls.Add(heading, 0, 0)
        page.SetColumnSpan(heading, 2)

        Dim editor As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 8, .BackColor = Color.White, .Margin = New Padding(8), .Padding = New Padding(12)}
        For Each height As Integer In New Integer() {58, 58, 68, 28, 54, 28, 54, 58}
            editor.RowStyles.Add(New RowStyle(SizeType.Absolute, height))
        Next
        editor.AutoScroll = True
        editor.Controls.Add(connectButton, 0, 0)
        editor.Controls.Add(readButton, 0, 1)
        loadedName.Font = SimpleUi.ActionFont
        loadedName.AutoSize = False
        editor.Controls.Add(loadedName, 0, 2)
        editor.Controls.Add(SimpleUi.Caption(If(vehicleMode, "Gearbits", "Gold")), 0, 3)
        goldInput.Maximum = 65000
        goldInput.ThousandsSeparator = True
        levelInput.Minimum = 1
        levelInput.Maximum = 20
        For Each input As NumericUpDown In New NumericUpDown() {goldInput, levelInput}
            input.Font = SimpleUi.NumberFont
            input.Dock = DockStyle.Fill
            input.Margin = New Padding(8, 2, 8, 4)
        Next
        editor.Controls.Add(goldInput, 0, 4)
        If Not vehicleMode Then
            editor.Controls.Add(SimpleUi.Caption("XP / Level"), 0, 5)
            editor.Controls.Add(levelInput, 0, 6)
        Else
            Dim hint As Label = SimpleUi.Caption("Gearbits range: 0 - 33,000")
            hint.AutoSize = False
            editor.Controls.Add(hint, 0, 5)
            editor.SetRowSpan(hint, 2)
        End If
        editor.Controls.Add(saveButton, 0, 7)

        Dim browser As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4, .BackColor = Color.White, .Padding = New Padding(12), .Margin = New Padding(8)}
        browser.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))
        browser.RowStyles.Add(New RowStyle(SizeType.Percent, 60))
        browser.RowStyles.Add(New RowStyle(SizeType.Percent, 40))
        browser.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))
        previewName.AutoSize = False
        previewName.Font = SimpleUi.ActionFont
        imageBox.Dock = DockStyle.Fill
        imageBox.SizeMode = PictureBoxSizeMode.Zoom
        imageBox.BackColor = Color.FromArgb(239, 248, 254)
        gallery.Dock = DockStyle.Fill
        gallery.Font = SimpleUi.Body
        gallery.BorderStyle = BorderStyle.None
        gallery.IntegralHeight = False
        Dim arrows As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2}
        arrows.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        arrows.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        Dim previous As Button = SimpleUi.Action("Previous")
        Dim following As Button = SimpleUi.Action("Next")
        arrows.Controls.Add(previous, 0, 0)
        arrows.Controls.Add(following, 1, 0)
        browser.Controls.Add(previewName, 0, 0)
        browser.Controls.Add(imageBox, 0, 1)
        browser.Controls.Add(gallery, 0, 2)
        browser.Controls.Add(arrows, 0, 3)
        page.Controls.Add(editor, 0, 1)
        page.Controls.Add(browser, 1, 1)
        page.Controls.Add(backButton, 0, 2)
        status.AutoSize = False
        page.Controls.Add(status, 1, 2)
        Controls.Add(page)

        AddHandler connectButton.Click, AddressOf ConnectPortal
        AddHandler readButton.Click, AddressOf ReadFigure
        AddHandler saveButton.Click, AddressOf SaveFigure
        AddHandler backButton.Click, Sub(sender, e) Close()
        AddHandler gallery.SelectedIndexChanged, AddressOf ShowPreview
        AddHandler previous.Click, Sub(sender, e) MovePreview(-1)
        AddHandler following.Click, Sub(sender, e) MovePreview(1)
        AddHandler imageBox.MouseEnter, Sub(sender, e) gallery.Focus()
        AddHandler imageBox.MouseWheel, AddressOf BrowseWithWheel
        AddHandler gallery.MouseWheel, AddressOf BrowseWithWheel
        AddHandler imageBox.Paint, AddressOf PaintMissingImage
        SimpleUi.StyleButtons(Me)
        RefreshActions()
    End Sub

    Private Sub RefreshActions()
        connectButton.Enabled = Not busy
        readButton.Enabled = Not busy AndAlso Portal.blnPortal
        saveButton.Enabled = Not busy AndAlso Portal.blnPortal AndAlso session IsNot Nothing AndAlso session.CanEdit
        goldInput.Enabled = saveButton.Enabled
        levelInput.Enabled = saveButton.Enabled
        backButton.Enabled = Not busy
    End Sub

    Private Sub ConnectPortal(sender As Object, e As EventArgs)
        If busy Then Return
        busy = True
        ClearSession()
        RefreshActions()
        Try
            If SimplePortal.Connect() Then
                status.Text = "Portal connected. Place one figure on it, then select Read figure."
            Else
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

    Private Async Sub ReadFigure(sender As Object, e As EventArgs)
        If busy Then Return
        busy = True
        ClearSession()
        loadedName.Text = "Reading figure..."
        status.Text = "Keep one figure on the portal while it is read."
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

    Private Async Sub SaveFigure(sender As Object, e As EventArgs)
        If busy OrElse session Is Nothing OrElse Not session.CanEdit Then Return
        If goldInput.Value = session.GoldValue AndAlso levelInput.Value = session.LevelValue Then
            status.Text = "There are no changes to save."
            Return
        End If
        busy = True
        RefreshActions()
        status.Text = "Saving to " & session.FigureName & ". Keep the figure on the portal."
        Try
            Dim original As Byte() = session.Original
            Dim updated As Byte() = session.BuildSave(goldInput.Value, levelInput.Value)
            Using timeout As New CancellationTokenSource(TimeSpan.FromSeconds(60))
                Dim verified As Byte() = Await SimplePortal.SaveAsync(original, updated, timeout.Token, vehicleMode)
                DisplaySession(New SimpleFigureSession(verified, vehicleMode))
            End Using
            status.Text = If(vehicleMode, "Gearbits saved and read back successfully.", "Gold and Level saved and read back successfully.")
        Catch ex As Exception
            FailOperation(ex)
        Finally
            busy = False
            RefreshActions()
        End Try
    End Sub

    Private Sub DisplaySession(value As SimpleFigureSession)
        session = value
        loadedName.Text = "Loaded: " & value.FigureName
        goldInput.Maximum = value.GoldMaximum
        levelInput.Maximum = value.LevelMaximum
        goldInput.Value = Math.Min(value.GoldValue, goldInput.Maximum)
        levelInput.Value = Math.Max(levelInput.Minimum, Math.Min(value.LevelValue, levelInput.Maximum))
        gallery.BeginUpdate()
        gallery.Items.Clear()
        For Each caption As String In value.Catalog
            gallery.Items.Add(caption)
        Next
        gallery.SelectedItem = value.FigureName
        gallery.EndUpdate()
        status.Text = If(value.CanEdit, If(vehicleMode, "Vehicle loaded. Change Gearbits, then save.", "Figure loaded. Change Gold or Level, then save."),
            If(value.IsUnsafe, "Unsafe figure data. Saving is disabled.", If(vehicleMode, "Read a supported vehicle to edit Gearbits.", "This figure is preview-only here. Use its matching editor.")))
    End Sub

    Private Sub ShowFigureWarnings()
        If session Is Nothing Then Return
        If session.IsSensei Then FigureWarnings.ShowWarning(Me, "Sensei initialization", FigureWarnings.SenseiText)
        If session.IsUnsafe Then
            Dim message As String = If(session.IsVehicle,
                "This vehicle has uninitialized or unsafe save data. Keep a backup. Initialize it in Skylanders SuperChargers and collect Gearbits, then read it again. If recovery requires an in-game reset, remember that resetting clears progress.",
                FigureWarnings.UnsafeText)
            FigureWarnings.ShowWarning(Me, If(session.IsVehicle, "Vehicle unsafe to write", "Skylander corrupted or unsafe to write"), message)
        End If
    End Sub

    Private Sub ShowPreview(sender As Object, e As EventArgs)
        If session Is Nothing OrElse gallery.SelectedItem Is Nothing Then Return
        Dim name As String = Convert.ToString(gallery.SelectedItem)
        previewName.Text = If(name = session.FigureName, "Loaded: ", "Preview: ") & name
        Dim old As Image = imageBox.Image
        imageBox.Image = artwork.Load(session.GameName, name)
        If old IsNot Nothing Then old.Dispose()
        imageBox.Invalidate()
    End Sub

    Private Sub MovePreview(direction As Integer)
        If gallery.Items.Count = 0 Then Return
        gallery.SelectedIndex = (Math.Max(0, gallery.SelectedIndex) + direction + gallery.Items.Count) Mod gallery.Items.Count
    End Sub

    Private Sub BrowseWithWheel(sender As Object, e As MouseEventArgs)
        If e.Delta = 0 Then Return
        MovePreview(If(e.Delta > 0, -1, 1))
        Dim handled As HandledMouseEventArgs = TryCast(e, HandledMouseEventArgs)
        If handled IsNot Nothing Then handled.Handled = True
    End Sub

    Private Sub PaintMissingImage(sender As Object, e As PaintEventArgs)
        If imageBox.Image IsNot Nothing Then Return
        TextRenderer.DrawText(e.Graphics, If(session Is Nothing, "Read a figure to see its icon", "Figure image unavailable"),
            SimpleUi.Body, imageBox.ClientRectangle, SimpleUi.Navy,
            TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.WordBreak)
    End Sub

    Private Sub FailOperation(ex As Exception)
        SimplePortal.Disconnect()
        ClearSession()
        loadedName.Text = "Read a figure to continue"
        status.Text = If(TypeOf ex Is OperationCanceledException, "The portal did not respond in time. Reconnect and read the figure again.", ex.Message)
        DeviceManagement.DebugWrite("Simple modifier: " & ex.ToString())
        FigureWarnings.ShowWarning(Me, If(TypeOf ex Is IO.InvalidDataException, "Figure unsafe to write", "Portal operation interrupted"), status.Text)
    End Sub

    Private Sub ClearSession()
        session = Nothing
        loadedName.Text = "No figure read yet"
        previewName.Text = "Figures"
        gallery.Items.Clear()
        Dim previous As Image = imageBox.Image
        imageBox.Image = Nothing
        If previous IsNot Nothing Then previous.Dispose()
        imageBox.Invalidate()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If busy Then
            e.Cancel = True
            status.Text = "Please wait for the portal operation to finish."
            Return
        End If
        SimplePortal.Disconnect()
        MyBase.OnFormClosing(e)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso imageBox.Image IsNot Nothing Then
            imageBox.Image.Dispose()
            imageBox.Image = Nothing
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
