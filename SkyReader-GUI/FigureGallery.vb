Option Strict On
Option Explicit On

'Presents independent collapsible game sections; selection is artwork only and never changes tag data.
Friend Class SkyGameBrowser
    Inherits UserControl
    Private ReadOnly stack As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True,
        .AutoSizeMode = AutoSizeMode.GrowAndShrink, .ColumnCount = 1, .Margin = New Padding(0)}
    Friend Property SelectedGame As String
    Friend Property SelectedFigure As String
    Friend Event PreviewChanged As EventHandler

    Friend Sub New()
        DoubleBuffered = True
        AutoScroll = True
        BackColor = SkyAssets.Panel
        stack.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        Controls.Add(stack)
    End Sub

    'Build only the sections relevant to this editor; all entries remain artwork-only.
    Friend Sub LoadCatalog(Optional mode As ArtworkBrowserMode = ArtworkBrowserMode.Skylanders)
        SuspendLayout()
        stack.SuspendLayout()
        Try
            For Each child As Control In stack.Controls.Cast(Of Control)().ToArray()
                child.Dispose()
            Next
            stack.Controls.Clear()
            stack.RowStyles.Clear()
            stack.RowCount = 0
            SelectedGame = Nothing
            SelectedFigure = Nothing
            For Each category As ArtworkBrowserSection In EditorArtworkCatalog.Sections(mode)
                Dim title As String = category.Title
                Dim game As String = category.Game
                Dim section As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True,
                    .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    .ColumnCount = 1, .RowCount = 2, .Margin = New Padding(2, 1, 2, 1)}
                section.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                section.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                section.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                Dim header As New Button With {.Text = title & "  +", .Dock = DockStyle.Top,
                    .Height = CInt(32 * DeviceDpi / 96.0F), .Font = SkyAssets.UiFont(7.0F),
                    .FlatStyle = FlatStyle.Flat, .BackColor = category.BackColor,
                    .ForeColor = EditorArtworkCatalog.HeaderInk(category.BackColor), .UseVisualStyleBackColor = False}
                header.FlatAppearance.BorderSize = 0
                SkySounds.AttachButton(header)
                Dim entries As New SkyArtworkListBox With {.Dock = DockStyle.Top,
                    .Font = SkyAssets.UiFont(7.0F), .BackColor = SkyAssets.White, .ForeColor = Color.Black,
                    .BorderStyle = BorderStyle.None, .IntegralHeight = False, .Visible = False, .HorizontalScrollbar = True}
                entries.Items.AddRange(category.Names.Cast(Of Object)().ToArray())
                entries.FitToItems()
                AddHandler header.Resize, Sub(sender, e) SkyPolish.RoundControl(header, 10)
                AddHandler header.Click, Sub(sender, e)
                                             entries.Visible = Not entries.Visible
                                             header.Text = title & If(entries.Visible, "  −", "  +")
                                         End Sub
                AddHandler entries.MouseMove, Sub(sender, e)
                                                  Dim index As Integer = entries.IndexFromPoint(e.Location)
                                                  If index >= 0 AndAlso index <> entries.SelectedIndex Then entries.SelectedIndex = index
                                              End Sub
                AddHandler entries.SelectedIndexChanged, Sub(sender, e)
                                                             If entries.SelectedIndex < 0 Then Return
                                                             SelectedGame = game
                                                             SelectedFigure = CStr(entries.SelectedItem)
                                                             RaiseEvent PreviewChanged(Me, EventArgs.Empty)
                                                         End Sub
                section.Controls.Add(header, 0, 0)
                section.Controls.Add(entries, 0, 1)
                Dim row As Integer = stack.RowCount
                stack.RowCount = row + 1
                stack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                stack.Controls.Add(section, 0, row)
            Next
        Finally
            stack.ResumeLayout(True)
            ResumeLayout(True)
        End Try
    End Sub
End Class


'Fit short artwork lists to their rows; retain a bounded, scrollable viewport for long lists.
'Used by Skylanders, Vehicles, and Imaginators / CYOS through the shared browser.
Friend NotInheritable Class SkyArtworkListBox
    Inherits ListBox

    Private fitting As Boolean
    Private measuredWidth As Integer = -1

    Friend Sub FitToItems()
        If fitting OrElse IsDisposed Then Return
        fitting = True
        Try
            measuredWidth = Width
            Dim rowHeight As Integer = Math.Max(1, ItemHeight)
            Dim rowLimit As Integer = Math.Max(1, CInt(Math.Floor(180.0 * DeviceDpi / 96.0 / rowHeight)))
            Dim visibleRows As Integer = Math.Min(Math.Max(1, Items.Count), rowLimit)
            Dim horizontalHeight As Integer = 0
            If HorizontalScrollbar Then
                Dim textWidth As Integer = 0
                For Each item As Object In Items
                    textWidth = Math.Max(textWidth, TextRenderer.MeasureText(GetItemText(item), Font).Width)
                Next
                HorizontalExtent = textWidth
                'These browser lists are borderless. Reserve scrollbar height only when needed.
                Dim availableWidth As Integer = Math.Max(0, Width -
                    If(Items.Count > visibleRows, SystemInformation.VerticalScrollBarWidth, 0))
                If textWidth > availableWidth Then horizontalHeight = SystemInformation.HorizontalScrollBarHeight
            End If
            Dim requiredHeight As Integer = visibleRows * rowHeight + horizontalHeight
            If Height <> requiredHeight Then Height = requiredHeight
        Finally
            fitting = False
        End Try
    End Sub

    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        MyBase.OnSizeChanged(e)
        If Width <> measuredWidth Then FitToItems()
    End Sub

    Protected Overrides Sub OnFontChanged(e As EventArgs)
        MyBase.OnFontChanged(e)
        FitToItems()
    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        FitToItems()
    End Sub

    Protected Overrides Sub OnDpiChangedAfterParent(e As EventArgs)
        MyBase.OnDpiChangedAfterParent(e)
        FitToItems()
    End Sub
End Class
