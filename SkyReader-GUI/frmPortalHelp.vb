Option Strict On
Option Explicit On

Public Class frmPortalHelp
    Inherits Form

    'Builds the help page
    Public Sub New()
        Text = "Help Portal - Portal Connection Issues"
        Name = "frmPortalHelp"
        Font = SimpleUi.Body
        BackColor = SkyAssets.Panel
        BackgroundImage = SkyDecor.Asset("Shattered_Background.png")
        BackgroundImageLayout = ImageLayout.Stretch
        ForeColor = SkyAssets.Ink
        AutoScaleDimensions = New SizeF(96, 96)
        AutoScaleMode = AutoScaleMode.Dpi
        ClientSize = New Size(940, 760)
        MinimumSize = New Size(640, 480)
        StartPosition = FormStartPosition.CenterScreen
        DoubleBuffered = True
        SkyAssets.ApplyWindowIcon(Me)
        Dim work As Rectangle = Screen.FromControl(Me).WorkingArea
        ClientSize = New Size(Math.Min(ClientSize.Width, work.Width - 48), Math.Min(ClientSize.Height, work.Height - 80))

        Dim page As New SkyLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(16), .BackColor = Color.Transparent}
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, 88))
        page.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, 80))
        Dim title As Label = SimpleUi.Caption("Portal Connection Issues")
        title.Font = SimpleUi.Heading
        title.AutoSize = False
        title.BackColor = SkyAssets.Panel
        SkyElevation.CompactTitle(title)
        page.Controls.Add(title, 0, 0)

        Dim viewport As New SkyHelpViewport With {.Dock = DockStyle.Fill, .AutoScroll = True, .BackColor = SkyAssets.Panel}
        Dim guide As New SkyLayoutPanel With {.Dock = DockStyle.Top, .ColumnCount = 1, .RowCount = 0,
            .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink, .Padding = New Padding(12), .BackColor = SkyAssets.Dark}
        guide.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        AddCard(guide, "Before you begin", "Use a compatible non-Xbox portal. If it already connects, no driver changes are needed. " &
            "This program uses USB Input Device (HID). In the troubleshooting sequence below, WinUSB is an intermediate step; finish by selecting USB Input Device before trying SkyGUI again.")
        AddCard(guide, "1. Download Zadig", "Use the Open Zadig Website button below to get Zadig from its official website.")
        AddCard(guide, "2. Connect your portal", "Make sure the portal is securely connected to your computer, then launch Zadig.")
        AddCard(guide, "3. Select the portal in Zadig", "Open Options > List All Devices, then select Spyro Porta from the device list. Confirm you have selected the portal, not another USB device.")
        AddCard(guide, "4. Install WinUSB", "Select WinUSB as the target driver, then click Install Driver or Replace Driver, depending on the button shown. Wait until the operation finishes, then close Zadig.")
        AddCard(guide, "5. Open Device Manager", "Search for Device Manager in the Windows search bar and open it.")
        AddCard(guide, "6. Find Spyro Porta", "Expand Universal Serial Bus devices. Right-click Spyro Porta and choose Update driver. The device name or category may vary; locate the portal you changed in Zadig.")
        AddCard(guide, "7. Browse for a driver", "Select Browse my computer for drivers.")
        AddCard(guide, "8. Choose from available drivers", "Select Let me pick from a list of available drivers on my computer.")
        AddCard(guide, "9. Switch to USB Input Device", "Select USB Input Device and click Next to apply it. If it is not offered, stop here rather than choosing an unrelated driver.")
        AddCard(guide, "10. Try the portal again", "After Windows finishes, close Device Manager. Return to the main screen, open an editor, and choose Connect portal again. Reconnect the USB cable if needed.")
        viewport.Controls.Add(guide)
        page.Controls.Add(viewport, 0, 1)

        Dim actions As New SkyLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .BackColor = Color.Transparent}
        actions.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 62))
        actions.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 38))
        Dim download As Button = SimpleUi.Action("Open Zadig Website")
        Dim back As Button = SimpleUi.Action("Back")
        AddHandler download.Click, AddressOf OpenZadig
        AddHandler back.Click, Sub(sender, e) Close()
        actions.Controls.Add(download, 0, 0)
        actions.Controls.Add(back, 1, 0)
        page.Controls.Add(actions, 0, 2)
        Controls.Add(page)
        CancelButton = back
        SimpleUi.StyleButtons(Me)
    End Sub

    'Adds a wrapped instruction card to the Help Portal guide.
    Private Shared Sub AddCard(guide As TableLayoutPanel, title As String, body As String)
        Dim card As New SkyTextCard With {.Dock = DockStyle.Top, .Text = title & vbCrLf & body,
            .Font = SimpleUi.Body, .BackColor = SkyAssets.Panel, .ForeColor = SkyAssets.Ink,
            .Margin = New Padding(6, 4, 6, 10), .Padding = New Padding(14), .TabStop = False}
        Dim row As Integer = guide.RowCount
        guide.RowCount += 1
        guide.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        guide.Controls.Add(card, 0, row)
    End Sub

    'Opens the official Zadig website in the default browser and reports launch failures.
    Private Sub OpenZadig(sender As Object, e As EventArgs)
        Try
            Process.Start(New ProcessStartInfo("https://zadig.akeo.ie/") With {.UseShellExecute = True})
        Catch ex As Exception
            FigureWarnings.ShowWarning(Me, "Unable to open browser", "Open https://zadig.akeo.ie/ in your browser to download Zadig.")
        End Try
    End Sub
End Class

'Provides the compact Help Portal button and its accessibility description.
Friend Class SkyHelpShortcut
    Inherits Button

    'caption
    Friend Sub New()
        Text = "Help Portal"
        AccessibleName = "Help Portal"
        AccessibleDescription = "Open portal connection instructions."
        Font = SkyAssets.UiFont(8.0F)
        Cursor = Cursors.Hand
        TabStop = True
        FlatStyle = FlatStyle.Flat
        FlatAppearance.BorderSize = 0
        BackColor = SkyAssets.Sand
    End Sub

End Class

'Keeps Help Portal scrolling on an opaque buffered surface to avoid stale background pixels, this used to be sooo buggy
Friend Class SkyHelpViewport
    Inherits Panel

    'Enables buffered painting on an opaque background for the scrolling instructions.
    Friend Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        BackColor = SkyAssets.Panel
    End Sub

    'clears pale pixels after scrollbar movement
    Protected Overrides Sub OnScroll(e As ScrollEventArgs)
        MyBase.OnScroll(e)
        Invalidate(True)
    End Sub

    ' child repaint after wheel scrolling
    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        'Mouse-wheel scrolling does not always raise Scroll in WinForms.
        Invalidate(True)
    End Sub
End Class
