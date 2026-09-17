Option Strict On
Option Explicit On
Imports System.Threading

Public Class frmTrapModifier
    Inherits Form
    Private session As TrapSession
    Private busy As Boolean
    Private updating As Boolean
    Private ReadOnly artwork As New FigureArtwork()
    Private ReadOnly connectButton As Button = SimpleUi.Action("Connect portal")
    Private ReadOnly readButton As Button = SimpleUi.Action("Read trap")
    Private ReadOnly backButton As Button = SimpleUi.Action("Back")
    Private ReadOnly saveButton As Button = SimpleUi.Action("Save Villain")
    Private ReadOnly evolved As New CheckBox With {.Text = "Evolved", .AutoSize = True, .Dock = DockStyle.Top}
    Private ReadOnly variantValue As New CheckBox With {.Text = "Use villain variant", .AutoSize = True, .Dock = DockStyle.Top}
    Private ReadOnly choices As New ComboBox With {.Dock = DockStyle.Top, .DropDownStyle = ComboBoxStyle.DropDownList}
    Private ReadOnly summary As Label = SimpleUi.Caption("Place one trap in the portal's trap slot, then select Read trap.")
    Private ReadOnly status As Label = SimpleUi.Caption("Only non-Xbox portals are supported. For connection issues, use Help Portal on the main menu.")
    Private ReadOnly picture As New SkyPortalPreview With {.Dock = DockStyle.Fill, .BackColor = Color.Transparent, .AnchorFigureToPortal = True}
    Private ReadOnly waiting As PictureBox = SkyDecor.Badge("Waiting.ico")
    Private ReadOnly ids As New Collections.Generic.List(Of Integer)

    'Builds the trap workshop controls and wires its read, selection, and save events.
    'Initially, I actually was just going to integrate Textheads Revolve but once I spent time figuring out how the implementation
    'for the villian variants works I ended up integrating both and calling it workshop.
    Public Sub New()
        Text = If(TrapFeatures.AllowAssignment, "Traps - Villain Workshop", "Traps - Evolution")
        Name = "frmTrapModifier"
        Font = SimpleUi.Body
        BackColor = SkyAssets.Panel
        ForeColor = SkyAssets.Ink
        BackgroundImage = SkyDecor.Asset("Shattered_Background.png")
        BackgroundImageLayout = ImageLayout.Stretch
        SkyAssets.ApplyWindowIcon(Me)
        ClientSize = New Size(1060, 900)
        MinimumSize = New Size(900, 660)
        StartPosition = FormStartPosition.CenterScreen
        AutoScaleDimensions = New SizeF(96, 96)
        AutoScaleMode = AutoScaleMode.Dpi
        AutoScroll = True
        DoubleBuffered = True
        Dim page As New SkyLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 1, .RowCount = 4,
            .Padding = New Padding(16), .BackColor = Color.Transparent}
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, 90))
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, 82))
        page.RowStyles.Add(New RowStyle(SizeType.Absolute, If(TrapFeatures.AllowAssignment, 570, 410)))
        page.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim title As Label = SimpleUi.Caption(If(TrapFeatures.AllowAssignment, "Trap Villain Workshop", "Trap Villain Evolution"))
        title.Font = SimpleUi.Heading
        title.BackColor = SkyAssets.Panel
        SkyElevation.CompactTitle(title)
        page.Controls.Add(title, 0, 0)
        Dim navigation As New SkyLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 3, .BackColor = Color.Transparent}
        For i As Integer = 0 To 2
            navigation.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F / 3))
        Next
        navigation.Controls.Add(connectButton, 0, 0)
        navigation.Controls.Add(readButton, 1, 0)
        navigation.Controls.Add(backButton, 2, 0)
        page.Controls.Add(navigation, 0, 1)
        Dim content As New SkyLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .BackColor = Color.Transparent}
        content.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 62))
        content.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 38))
        Dim card As New SkyCardLayout With {.Dock = DockStyle.Fill, .ColumnCount = 1, .Padding = New Padding(14), .Margin = New Padding(8)}
        summary.AutoSize = True
        summary.MaximumSize = New Size(550, 0)
        AddRow(card, summary)
        If TrapFeatures.AllowAssignment Then
            AddRow(card, SimpleUi.Caption("Villain:"))
            choices.Margin = New Padding(8)
            AddRow(card, choices)
        End If
        Dim options As New SkyLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 2}
        options.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
        options.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        evolved.Margin = New Padding(8, 14, 8, 14)
        variantValue.Margin = New Padding(8, 14, 8, 14)
        variantValue.Text = "Variant"
        options.Controls.Add(evolved, 0, 0)
        options.Controls.Add(variantValue, 1, 0)
        variantValue.Visible = TrapFeatures.AllowAssignment
        AddRow(card, options)
        AddRow(card, saveButton, 88)
        content.Controls.Add(card, 0, 0)
        content.Controls.Add(picture, 1, 0)
        page.Controls.Add(content, 0, 2)
        Dim footer As New SkyCardLayout With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 2, .Padding = New Padding(12), .Margin = New Padding(8)}
        footer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        footer.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 42))
        status.MaximumSize = New Size(920, 0)
        footer.Controls.Add(status, 0, 0)
        footer.Controls.Add(waiting, 1, 0)
        page.Controls.Add(footer, 0, 3)
        Controls.Add(page)
        AddHandler connectButton.Click, AddressOf ConnectPortal
        AddHandler readButton.Click, AddressOf ReadTrap
        AddHandler saveButton.Click, AddressOf SaveTrap
        AddHandler variantValue.CheckedChanged, Sub(sender, e) RefreshPreview()
        AddHandler choices.SelectedIndexChanged, AddressOf SelectionChanged
        AddHandler backButton.Click, Sub(sender, e) Close()
        SimpleUi.StyleButtons(Me)
        RefreshActions()
    End Sub

    'Adds a control to a new layout row using a fixed height or automatic sizing.
    Private Shared Sub AddRow(card As TableLayoutPanel, control As Control, Optional height As Integer = 0)
        Dim row As Integer = card.RowCount
        card.RowCount += 1
        card.RowStyles.Add(New RowStyle(If(height = 0, SizeType.AutoSize, SizeType.Absolute), height))
        card.Controls.Add(control, 0, row)
    End Sub

    'Enables or disables page actions according to connection, loaded data, and operation state.
    Private Sub RefreshActions()
        connectButton.Enabled = Not busy
        readButton.Enabled = Not busy AndAlso Portal.blnPortal
        backButton.Enabled = Not busy
        Dim editable As Boolean = Not busy AndAlso Portal.blnPortal AndAlso session IsNot Nothing AndAlso session.CanWrite
        evolved.Enabled = editable AndAlso (TrapFeatures.AllowAssignment OrElse session.VillainId <> 0)
        choices.Enabled = editable AndAlso TrapFeatures.AllowAssignment
        saveButton.Enabled = editable AndAlso If(TrapFeatures.AllowAssignment, choices.SelectedIndex >= 0, session.VillainId <> 0)
        variantValue.Enabled = choices.Enabled AndAlso choices.SelectedIndex >= 0 AndAlso TrapCatalog.Variants.ContainsKey(ids(choices.SelectedIndex))
        waiting.Visible = busy
    End Sub

    'Attempts portal connection and updates the page with connection status or troubleshooting guidance.
    Private Sub ConnectPortal(sender As Object, e As EventArgs)
        ClearSession()
        Try
            status.Text = If(SimplePortal.Connect(), "Portal connected. Place one trap in its trap slot and select Read trap.",
                "Portal not found. Only non-Xbox portals are compatible. Follow the Zadig instructions in Help Portal.")
        Catch ex As Exception
            Failure(ex)
        End Try
        RefreshActions()
    End Sub
    'Reads the portal under a timeout and displays the resulting validated trap session.
    Private Async Sub ReadTrap(sender As Object, e As EventArgs)
        If busy Then Return
        ClearSession()
        busy = True : status.Text = "Reading trap. Keep it on the portal." : RefreshActions()
        Try
            Using timeout As New CancellationTokenSource(TimeSpan.FromSeconds(40))
                ShowSession(New TrapSession(Await SimplePortal.ReadFigureAsync(timeout.Token)))
            End Using
        Catch ex As Exception
            Failure(ex)
        Finally
            busy = False : RefreshActions()
        End Try
    End Sub
    'Loads the scanned trap's villain choices and flags into the workshop controls.
    Private Sub ShowSession(value As TrapSession)
        updating = True
        session = value
        summary.Text = value.TrapName & vbCrLf & If(value.VillainId = 0, "Empty trap - No active villian yet inside.",
            "Current villain: " & value.VillainName & vbCrLf & If(value.Evolved, "Evolved", "Not evolved"))
        evolved.Checked = value.Evolved
        ids.Clear() : choices.Items.Clear()
        For id As Integer = 1 To TrapCatalog.Names.Length - 1
            If TrapCatalog.Compatible(id, value.TrapId) Then
                ids.Add(id) : choices.Items.Add(TrapCatalog.Names(id))
            End If
        Next
        choices.SelectedIndex = If(ids.Contains(value.VillainId), ids.IndexOf(value.VillainId), -1)
        variantValue.Checked = value.IsVariant
        status.Text = If(String.IsNullOrEmpty(value.Notice), "Trap loaded. Choose a villain and options that villian may have, then select Save Villain.", value.Notice)
        updating = False
        RefreshPreview()
    End Sub
    'Replaces the preview image with artwork for the current trap or villain selection.
    Private Sub RefreshPreview()
        If updating OrElse session Is Nothing Then Return
        Dim name As String = session.TrapName
        If choices.SelectedIndex >= 0 AndAlso (session.VillainId <> 0 OrElse TrapFeatures.AllowAssignment) Then
            name = TrapCatalog.VillainName(ids(choices.SelectedIndex), variantValue.Checked)
        End If
        Dim old As Image = picture.Image
        picture.Image = artwork.Load("Traps", name)
        If old IsNot Nothing Then old.Dispose()
    End Sub
    'Refreshes the variant/evolution controls and artwork when the villain selection changes.
    Private Sub SelectionChanged(sender As Object, e As EventArgs)
        If updating Then Return
        variantValue.Checked = session IsNot Nothing AndAlso choices.SelectedIndex >= 0 AndAlso
            ids(choices.SelectedIndex) = session.VillainId AndAlso session.IsVariant
        RefreshActions()
        RefreshPreview()
    End Sub
    'Builds the selected villain update and waits for a verified portal write before refreshing the session.
    Private Async Sub SaveTrap(sender As Object, e As EventArgs)
        If busy OrElse session Is Nothing OrElse Not session.CanWrite Then Return
        busy = True : RefreshActions()
        Try
            Dim updated As Byte() = If(TrapFeatures.AllowAssignment,
                session.BuildSelection(ids(choices.SelectedIndex), variantValue.Checked, evolved.Checked), session.BuildEvolution(evolved.Checked))
            status.Text = "Writing and verifying the trap. Keep it on the portal."
            Using timeout As New CancellationTokenSource(TimeSpan.FromSeconds(60))
                Dim saved As Byte() = Await SimplePortal.SaveTrapAsync(session.Original, updated, timeout.Token)
                ShowSession(New TrapSession(saved))
            End Using
            status.Text = "Trap saved and verified. " & session.VillainName & If(session.Evolved, " - Evolved.", " - Not evolved.")
        Catch ex As Exception
            Failure(ex)
        Finally
            busy = False : RefreshActions()
        End Try
    End Sub

    'Shows the trap operation error, clears stale session data, and updates available actions.
    Private Sub Failure(ex As Exception)
        ClearSession()
        status.Text = If(TypeOf ex Is OperationCanceledException, "The portal timed out. Reconnect and read the trap again.", ex.Message)
        FigureWarnings.ShowWarning(Me, "Trap operation interrupted", status.Text)
    End Sub

    'Clears the scanned session so later actions cannot reuse old figure data.
    Private Sub ClearSession()
        session = Nothing
        ids.Clear() : choices.Items.Clear()
        summary.Text = "No trap loaded. Read a trap before editing."
        If picture.Image IsNot Nothing Then
            Dim old As Image = picture.Image : picture.Image = Nothing : old.Dispose()
        End If
    End Sub

    'Prevents closing during a trap operation and disconnects when the workshop can close.
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If busy Then
            e.Cancel = True
            status.Text = "Please wait for the portal operation to finish."
        Else
            ClearSession()
            SimplePortal.Disconnect()
        End If
        MyBase.OnFormClosing(e)
    End Sub
End Class
