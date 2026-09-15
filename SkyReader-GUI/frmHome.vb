Option Strict On
Option Explicit On

Public Class frmHome
    Inherits Form

    Private ReadOnly buttons As New TableLayoutPanel()
    Private ReadOnly title As New Label()
    Private closingApplication As Boolean

    Public Sub New()
        Text = "SkyGUI"
        Name = "frmHome"
        Font = SimpleUi.Body
        BackColor = SimpleUi.Sky
        ClientSize = New Size(1000, 660)
        MinimumSize = New Size(700, 560)
        StartPosition = FormStartPosition.CenterScreen
        AutoScaleDimensions = New SizeF(96, 96)
        AutoScaleMode = AutoScaleMode.Dpi
        BackgroundImageLayout = ImageLayout.Stretch
        DoubleBuffered = True

        title.Text = "SkyGUI"
        title.Font = SimpleUi.Heading
        title.ForeColor = SimpleUi.Navy
        title.TextAlign = ContentAlignment.MiddleCenter
        title.BackColor = Color.Transparent
        buttons.ColumnCount = 2
        buttons.RowCount = 3
        buttons.BackColor = Color.Transparent
        buttons.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        buttons.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        For index As Integer = 0 To 2
            buttons.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F / 3.0F))
        Next
        Dim modifier As Button = SimpleUi.Action("XP / Level Modifier")
        Dim traps As Button = SimpleUi.Action("Traps")
        Dim vehicles As Button = SimpleUi.Action("Vehicles")
        Dim developer As Button = SimpleUi.Action("Developer")
        Dim imaginators As Button = SimpleUi.Action("Imaginators")
        developer.BackColor = SimpleUi.Navy
        For Each placeholder As Button In New Button() {traps, imaginators}
            placeholder.Enabled = False
            placeholder.BackColor = Color.FromArgb(162, 208, 237)
        Next
        buttons.Controls.Add(modifier, 0, 0)
        buttons.SetColumnSpan(modifier, 2)
        buttons.Controls.Add(traps, 0, 1)
        buttons.Controls.Add(vehicles, 1, 1)
        buttons.Controls.Add(developer, 0, 2)
        buttons.Controls.Add(imaginators, 1, 2)
        Controls.Add(title)
        Controls.Add(buttons)
        AddHandler modifier.Click, AddressOf OpenModifier
        AddHandler vehicles.Click, AddressOf OpenVehicles
        AddHandler developer.Click, AddressOf OpenDeveloper
        AddHandler Resize, AddressOf PositionContent
        SimpleUi.StyleButtons(Me)
        PositionContent(Me, EventArgs.Empty)
    End Sub

    Private Sub PositionContent(sender As Object, e As EventArgs)
        Dim scale As Single = DeviceDpi / 96.0F
        buttons.Size = New Size(Math.Min(CInt(660 * scale), ClientSize.Width - CInt(40 * scale)), CInt(342 * scale))
        buttons.Location = New Point((ClientSize.Width - buttons.Width) \ 2, Math.Max(CInt(120 * scale), (ClientSize.Height - buttons.Height) \ 2))
        title.SetBounds(0, buttons.Top - CInt(85 * scale), ClientSize.Width, CInt(60 * scale))
    End Sub

    Private Sub OpenDeveloper(sender As Object, e As EventArgs)
        frmMain.EnsureEditorInitialized()
        If Not Portal.blnPortal Then frmMain.lockPortalControls()
        RemoveHandler frmMain.FormClosed, AddressOf ReturnHome
        AddHandler frmMain.FormClosed, AddressOf ReturnHome
        frmMain.Show()
        frmMain.Activate()
        Hide()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        FigureWarnings.ShowWarning(Me, "Edit safely - keep backups",
            "I am not responsible for any damage done to figures. Please edit and modify safely, and keep backups of your figures before making changes.")
    End Sub

    Private Sub OpenVehicles(sender As Object, e As EventArgs)
        Dim editor As New frmModifier(True)
        AddHandler editor.FormClosed, AddressOf ReturnHome
        editor.Show()
        Hide()
    End Sub

    Private Sub OpenModifier(sender As Object, e As EventArgs)
        Dim modifier As New frmModifier()
        AddHandler modifier.FormClosed, AddressOf ReturnHome
        modifier.Show()
        Hide()
    End Sub

    Private Sub ReturnHome(sender As Object, e As FormClosedEventArgs)
        If Not closingApplication AndAlso Not IsDisposed Then
            Show()
            Activate()
        End If
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        closingApplication = True
        SimplePortal.Disconnect()
        MyBase.OnFormClosing(e)
    End Sub
End Class
