Option Strict On
Option Explicit On

Public Class frmHome
    Inherits SkyCloudForm

    Private closingApplication As Boolean

    'Builds the landing layout, navigation buttons, and third-party image disclaimer.
    Public Sub New()
        Text = "Skylander Editor"
        Name = "frmHome"
        SkyAssets.ApplyWindowIcon(Me)
        Font = SimpleUi.Body
        BackColor = SkyAssets.Panel
        ClientSize = New Size(1000, 720)
        MinimumSize = New Size(740, 580)
        StartPosition = FormStartPosition.CenterScreen
        AutoScaleDimensions = New SizeF(96, 96)
        AutoScaleMode = AutoScaleMode.Dpi
        DoubleBuffered = True
        AutoScroll = True

        Dim page As New SkyLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink, .RowCount = 4,
            .ColumnCount = 1, .Padding = New Padding(24, 12, 24, 16), .BackColor = Color.Transparent}
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, 140))
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, 62))
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, 290))
        page.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim header As New SkyLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 3, .RowCount = 1,
            .BackColor = Color.Transparent, .Margin = New Padding(0)}
        header.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 152))
        header.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        header.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 152))
        Dim help As New SkyHelpShortcut With {.Size = New Size(116, 76), .Anchor = AnchorStyles.Top Or AnchorStyles.Left, .Margin = New Padding(4, 4, 8, 10)}
        header.Controls.Add(help, 0, 0)
        AddHandler help.Click, AddressOf OpenPortalHelp
        page.Controls.Add(header, 0, 0)
        Dim logo As New PictureBox With {.Dock = DockStyle.Fill, .Image = SkyAssets.Logo,
            .SizeMode = PictureBoxSizeMode.Zoom, .BackColor = Color.Transparent, .Margin = New Padding(20, 0, 20, 0)}
        If SkyAssets.Logo Is Nothing Then
            Dim fallback As Label = SimpleUi.Caption("Skylanders")
            fallback.Font = SimpleUi.Heading
            fallback.AutoSize = False
            fallback.TextAlign = ContentAlignment.MiddleCenter
            header.Controls.Add(fallback, 1, 0)
        Else
            header.Controls.Add(logo, 1, 0)
        End If
        Dim subtitle As Label = SimpleUi.Caption("Skylander Editor")
        subtitle.AutoSize = False
        subtitle.TextAlign = ContentAlignment.MiddleCenter
        subtitle.Font = SkyAssets.UiFont(23.0F)
        subtitle.ForeColor = Color.Black
        subtitle.BackColor = Color.Transparent
        page.Controls.Add(subtitle, 0, 1)

        Dim buttons As New SkyLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 3,
            .BackColor = Color.Transparent, .Margin = New Padding(65, 8, 65, 8)}
        buttons.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        buttons.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        For index As Integer = 0 To 2
            buttons.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F / 3.0F))
        Next
        Dim modifier As Button = SimpleUi.Action("Skylanders Level and Gold Modifier")
        Dim traps As Button = SimpleUi.Action("Traps")
        Dim vehicles As Button = SimpleUi.Action("Vehicles")
        Dim developer As Button = SimpleUi.Action("Advanced")
        Dim imaginators As Button = SimpleUi.Action("Imaginators")
        AddHandler traps.Click, AddressOf OpenTraps
        imaginators.Enabled = False
        buttons.Controls.Add(modifier, 0, 0)
        buttons.SetColumnSpan(modifier, 2)
        buttons.Controls.Add(traps, 0, 1)
        buttons.Controls.Add(vehicles, 1, 1)
        buttons.Controls.Add(developer, 0, 2)
        buttons.Controls.Add(SkyDecor.Development(imaginators), 1, 2)
        page.Controls.Add(buttons, 0, 2)

        Dim disclaimer As New SkyTextCard With {.Dock = DockStyle.Top,
            .BackColor = SkyAssets.Panel, .ForeColor = SkyAssets.Ink,
            .Font = SkyAssets.UiFont(8.5F), .TextAlign = ContentAlignment.MiddleCenter,
            .Margin = New Padding(12, 6, 12, 6), .Padding = New Padding(14, 10, 14, 10), .TabStop = False}
        disclaimer.Text = "Third-Party Images Disclaimer:" & vbCrLf &
            "This spreadsheet includes images of Skylanders figures sourced from third-party websites and creators." & vbCrLf &
            "These images are not licensed under (CC BY-NC-ND 4.0) and are used here only for informational or reference purposes." & vbCrLf & vbCrLf &
            "Skylanders is a trademark of Activision." & vbCrLf &
            "This project is not affiliated with or endorsed by Activision." & vbCrLf &
            "All character designs are © Activision and respective rights holders."
        SkyDecor.WarningOnCard(disclaimer)
        page.Controls.Add(disclaimer, 0, 3)
        Controls.Add(page)
        AddHandler modifier.Click, AddressOf OpenModifier
        AddHandler vehicles.Click, AddressOf OpenVehicles
        AddHandler developer.Click, AddressOf OpenDeveloper
        SimpleUi.StyleButtons(Me)
    End Sub

    'Hides the landing page and opens the portal troubleshooting window.
    Private Sub OpenPortalHelp(sender As Object, e As EventArgs)
        Dim help As New frmPortalHelp()
        AddHandler help.FormClosed, AddressOf ReturnHome
        help.Show()
        Hide()
    End Sub

    'Initializes and opens the original Developer editor from the landing page.
    Private Sub OpenDeveloper(sender As Object, e As EventArgs)
        frmMain.EnsureEditorInitialized()
        If Not Portal.blnPortal Then frmMain.lockPortalControls()
        RemoveHandler frmMain.FormClosed, AddressOf ReturnHome
        AddHandler frmMain.FormClosed, AddressOf ReturnHome
        frmMain.Show()
        frmMain.Activate()
        Hide()
    End Sub

    'Displays the startup responsibility warning once for this home window.
    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        FigureWarnings.ShowWarning(Me, "Edit Responsibly - BACKUP FIGURES",
            "The user is responsible for any damage done to figures. Please edit and modify safely, and keep 
            of your figures before making changes.")
    End Sub

    'Opens the villain workshop and arranges to return home when it closes.
    Private Sub OpenTraps(sender As Object, e As EventArgs)
        Dim editor As New frmTrapModifier()
        AddHandler editor.FormClosed, AddressOf ReturnHome
        editor.Show()
        Hide()
    End Sub

    'Opens the simplified modifier in vehicle Gearbits mode.
    Private Sub OpenVehicles(sender As Object, e As EventArgs)
        Dim editor As New frmModifier(True)
        AddHandler editor.FormClosed, AddressOf ReturnHome
        editor.Show()
        Hide()
    End Sub

    'Opens the simplified character Gold and Level editor.
    Private Sub OpenModifier(sender As Object, e As EventArgs)
        Dim modifier As New frmModifier()
        AddHandler modifier.FormClosed, AddressOf ReturnHome
        modifier.Show()
        Hide()
    End Sub

    'Shows and activates the landing page when a child editor closes.
    Private Sub ReturnHome(sender As Object, e As FormClosedEventArgs)
        If Not closingApplication AndAlso Not IsDisposed Then
            Show()
            Activate()
        End If
    End Sub

    'Marks the application as closing and disconnects the portal before the home window closes.
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        closingApplication = True
        SimplePortal.Disconnect()
        MyBase.OnFormClosing(e)
    End Sub
End Class
