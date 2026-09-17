'Applies the runtime theme, layout adjustments, and custom painting to original Developer windows.
Option Explicit On
Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

'Presentation only. Existing controls, their parents and their data are retained.
'Change this palette to update every editor together.
Friend NotInheritable Class SkyTheme
    Friend Shared ReadOnly Navy As Color = SkyAssets.Panel
    Friend Shared ReadOnly Ocean As Color = SkyAssets.Panel
    Friend Shared ReadOnly Cyan As Color = SkyAssets.Bright
    Friend Shared ReadOnly Gold As Color = SkyAssets.Glow
    Friend Shared ReadOnly Canvas As Color = SkyAssets.Panel
    Friend Shared ReadOnly Surface As Color = SkyAssets.Panel
    Friend Shared ReadOnly Ink As Color = SkyAssets.Ink
    Friend Shared ReadOnly Muted As Color = SkyAssets.Ink
    Friend Shared ReadOnly Border As Color = SkyAssets.Sand
    Friend Shared ReadOnly Selection As Color = SkyAssets.Sand

    Private Shared ReadOnly BodyFont As Font = SkyAssets.UiFont(9.5F)
    Private Shared ReadOnly StrongFont As Font = SkyAssets.UiFont(9.5F)
    Private Shared ReadOnly TitleFont As Font = SkyAssets.UiFont(22.0F)
    Private Shared ReadOnly CodeFont As Font = SkyAssets.UiFont(10.0F)

    Private Const WidthScale As Single = 1.8F
    Private Const HeightScale As Single = 1.6F
    Private Const HeaderHeight As Integer = 98

    'Keeps this shared helper from being instantiated.
    Private Sub New()
    End Sub

    'Applies the Developer theme once, then attaches painting and layout refresh handlers.
    Friend Shared Sub Apply(window As Form)
        SkyAssets.ApplyWindowIcon(window)
        window.SuspendLayout()
        Try
            Dim originalSize As Size = window.ClientSize
            Dim header As Integer = Pixels(window, HeaderHeight)

            'Bounds have already received the original form's DPI/font scaling.
            'Expand those bounds once; do not change the form's autoscale settings.
            ScaleControls(window, header)
            window.BackColor = Canvas
            window.ForeColor = Ink
            StyleChildren(window)

            Dim contentSize As New Size(CInt(originalSize.Width * WidthScale),
                                        CInt(originalSize.Height * HeightScale) + header)
            If TypeOf window Is frmLog Then
                contentSize = New Size(Pixels(window, 800), Pixels(window, 540))
                window.Padding = New Padding(Pixels(window, 16), header + Pixels(window, 16),
                                             Pixels(window, 16), Pixels(window, 16))
            End If

            'Native scrolling keeps the existing layout reachable on small displays.
            Dim workArea As Rectangle = Screen.FromControl(window).WorkingArea
            Dim chrome As Size = window.Size - window.ClientSize
            window.AutoScroll = True
            window.AutoScrollMinSize = contentSize
            window.ClientSize = New Size(Math.Min(contentSize.Width, Math.Max(1, workArea.Width - chrome.Width - Pixels(window, 24))),
                                         Math.Min(contentSize.Height, Math.Max(1, workArea.Height - chrome.Height - Pixels(window, 24))))

            PolishLayout(window)
            AddHandler window.Paint, AddressOf PaintHeader
            AddHandler window.Resize, AddressOf RefreshWindow
            AddHandler window.Scroll, AddressOf RefreshWindow
        Finally
            window.ResumeLayout(True)
        End Try
    End Sub

    'recursively scales original controls and applies the required vertical layout offset.
    Private Shared Sub ScaleControls(parent As Control, topOffset As Integer)
        For Each child As Control In parent.Controls
            If TypeOf child Is ToolStrip Then Continue For

            If child.Dock = DockStyle.None Then
                Dim bounds As Rectangle = child.Bounds
                child.SetBounds(CInt(bounds.X * WidthScale),
                                CInt(bounds.Y * HeightScale) + topOffset,
                                CInt(bounds.Width * WidthScale),
                                CInt(bounds.Height * HeightScale))
            End If

            'Do not style or rearrange native implementation children (e.g. spinners).
            If TypeOf child Is GroupBox OrElse TypeOf child Is Panel Then
                ScaleControls(child, 0)
            End If
        Next
    End Sub

    'Applies control-specific colors, fonts, and painting to the Developer control tree.
    Private Shared Sub StyleChildren(parent As Control)
        For Each child As Control In parent.Controls
            'These colors are live checksum/area results, not decoration.
            If TypeOf child Is PictureBox Then Continue For

            child.Font = BodyFont
            child.ForeColor = Ink

            If TypeOf child Is GroupBox Then
                Dim group As GroupBox = DirectCast(child, GroupBox)
                group.BackColor = Surface
                group.Font = StrongFont
                group.FlatStyle = FlatStyle.Flat
                AddHandler group.Paint, AddressOf PaintGroup
                StyleChildren(group)
            ElseIf TypeOf child Is Button Then
                StyleButton(DirectCast(child, Button))
            ElseIf TypeOf child Is Label Then
                child.BackColor = Color.Transparent
                child.ForeColor = Muted
            ElseIf TypeOf child Is ComboBox Then
                Dim combo As ComboBox = DirectCast(child, ComboBox)
                combo.BackColor = Surface
                combo.FlatStyle = FlatStyle.Flat
            ElseIf TypeOf child Is NumericUpDown Then
                Dim number As NumericUpDown = DirectCast(child, NumericUpDown)
                number.BackColor = Surface
                number.BorderStyle = BorderStyle.FixedSingle
            ElseIf TypeOf child Is RichTextBox Then
                Dim log As RichTextBox = DirectCast(child, RichTextBox)
                log.BackColor = Navy
                log.ForeColor = SkyAssets.Ink
                log.Font = CodeFont
                log.BorderStyle = BorderStyle.None
            ElseIf TypeOf child Is TextBox Then
                Dim input As TextBox = DirectCast(child, TextBox)
                input.BackColor = Surface
                input.BorderStyle = BorderStyle.FixedSingle
            ElseIf TypeOf child Is ListBox Then
                Dim list As ListBox = DirectCast(child, ListBox)
                list.BackColor = Surface
                list.BorderStyle = BorderStyle.FixedSingle
                list.IntegralHeight = False
                list.DrawMode = DrawMode.OwnerDrawFixed
                list.ItemHeight = Pixels(list, 34)
                AddHandler list.DrawItem, AddressOf PaintListItem
            ElseIf TypeOf child Is CheckBox OrElse TypeOf child Is RadioButton Then
                child.BackColor = Color.Transparent
            ElseIf TypeOf child Is ToolStrip Then
                Dim strip As ToolStrip = DirectCast(child, ToolStrip)
                strip.BackColor = Navy
                strip.ForeColor = Ink
                strip.Renderer = New SkyMenuRenderer()
                StyleMenuItems(strip.Items)
            ElseIf TypeOf child Is Panel Then
                child.BackColor = Canvas
                StyleChildren(child)
            End If
        Next
    End Sub

    'Applies shared button styling and registers the repaint handlers used for interaction feedback.
    Private Shared Sub StyleButton(button As Button)
        button.Font = StrongFont
        SkyPresentation.StyleButton(button)
    End Sub

    'Paints a styled group box frame and its caption.
    Private Shared Sub PaintGroup(sender As Object, e As PaintEventArgs)
        Dim group As GroupBox = DirectCast(sender, GroupBox)
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        e.Graphics.Clear(group.Parent.BackColor)
        Using path As GraphicsPath = RoundedRectangle(New Rectangle(0, 0, group.Width - 1, group.Height - 1), Pixels(group, 8)),
              fill As New SolidBrush(Surface),
              outline As New Pen(Border)
            e.Graphics.FillPath(fill, path)
            e.Graphics.DrawPath(outline, path)
        End Using
        Dim heading As New Rectangle(Pixels(group, 10), Pixels(group, 2), group.Width - Pixels(group, 20), Pixels(group, 20))
        TextRenderer.DrawText(e.Graphics, group.Text, StrongFont, heading,
                              If(group.Enabled, Ink, SystemColors.GrayText),
                              TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
    End Sub

    'Draws character list entries with the appropriate selection colors and text.
    Private Shared Sub PaintListItem(sender As Object, e As DrawItemEventArgs)
        Dim list As ListBox = DirectCast(sender, ListBox)
        If e.Index < 0 OrElse e.Index >= list.Items.Count Then Return
        Dim selected As Boolean = (e.State And DrawItemState.Selected) <> 0
        Dim caption As String = list.GetItemText(list.Items(e.Index))
        Dim heading As Boolean = caption.StartsWith("--", StringComparison.Ordinal)
        Using fill As New SolidBrush(If(selected, Selection, Surface))
            e.Graphics.FillRectangle(fill, e.Bounds)
        End Using
        If selected Then
            Using accent As New SolidBrush(Gold)
                e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y, Pixels(list, 3), e.Bounds.Height)
            End Using
        End If
        Dim textBounds As Rectangle = e.Bounds
        textBounds.X += Pixels(list, 10)
        textBounds.Width -= Pixels(list, 18)
        Dim textColor As Color = If(list.Enabled, If(selected, SkyAssets.Dark, Ink), SystemColors.GrayText)
        TextRenderer.DrawText(e.Graphics, caption, If(heading, StrongFont, list.Font), textBounds, textColor,
                              TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPrefix)
        e.DrawFocusRectangle()
    End Sub

    'Applies the shared font and colors recursively to menu items and dropdowns.
    Private Shared Sub StyleMenuItems(items As ToolStripItemCollection)
        For Each item As ToolStripItem In items
            item.Font = BodyFont
            Dim dropdown As ToolStripDropDownItem = TryCast(item, ToolStripDropDownItem)
            If dropdown IsNot Nothing AndAlso dropdown.HasDropDownItems Then
                dropdown.DropDown.BackColor = Surface
                dropdown.DropDown.ForeColor = Ink
                dropdown.DropDown.Renderer = New SkyMenuRenderer()
                StyleMenuItems(dropdown.DropDownItems)
            End If
        Next
    End Sub

    'Adjusts the original windows' control positions and widths for the runtime theme.
    Private Shared Sub PolishLayout(window As Form)
        Dim main As frmMain = TryCast(window, frmMain)
        If main IsNot Nothing Then
            Dim inset As Integer = Pixels(main, 16)
            For Each button As Button In New Button() {main.btnTraps, main.btnVehicles, main.btnCrystals}
                button.Left = inset
                button.Width = main.grpEditor.ClientSize.Width - inset * 2
                button.Height = Pixels(main, 38)
            Next
            'Long status descriptions continue using the original status label.
            main.SaldeStatus.Font = BodyFont
        End If

        Dim traps As frmTraps = TryCast(window, frmTraps)
        If traps IsNot Nothing Then
            'Allow long captions to wrap in the existing right-hand action column.
            Dim buttons As Button() = {traps.btnVil, traps.btnIDTrap}
            Dim left As Integer = traps.grpVillian6.Right + Pixels(traps, 10)
            Dim width As Integer = window.AutoScrollMinSize.Width - left - Pixels(traps, 12)
            For index As Integer = 0 To buttons.Length - 1
                buttons(index).SetBounds(left, traps.grpVillian6.Bottom - Pixels(traps, 90) + index * Pixels(traps, 48),
                                         width, Pixels(traps, 42))
            Next
        End If
    End Sub

    'Draws the Developer window's themed header area and title decoration.
    Private Shared Sub PaintHeader(sender As Object, e As PaintEventArgs)
        Dim window As Form = DirectCast(sender, Form)
        If window.ClientSize.Width < 1 Then Return
        Dim top As Integer = If(window.MainMenuStrip Is Nothing, 0, window.MainMenuStrip.Bottom) + window.AutoScrollPosition.Y
        Dim bounds As New Rectangle(0, top, window.ClientSize.Width, Pixels(window, HeaderHeight))
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        Using fill As New LinearGradientBrush(bounds, Navy, Ocean, LinearGradientMode.Horizontal)
            e.Graphics.FillRectangle(fill, bounds)
        End Using
        Using accent As New SolidBrush(Gold)
            e.Graphics.FillRectangle(accent, 0, bounds.Bottom - Pixels(window, 3), bounds.Width, Pixels(window, 3))
        End Using

        Dim title As String
        Dim subtitle As String
        Select Case window.Name
            Case "frmMain"
                title = "SkyGUI"
                subtitle = "This is a tool that is no longer maintained other than for development and testing. Be Aware."
            Case "frmTraps"
                title = "Trap Editor"
                subtitle = "Captured villains and customizations"
            Case "frmVehicles"
                title = "Vehicles"
                subtitle = "Supercharge your collection"
            Case "frmCrystals"
                title = "Creation Crystals"
                subtitle = "Imaginator customization"
            Case "frmLog"
                title = "Activity Log"
                subtitle = "SkyGUI"
            Case Else
                title = "Areas"
                subtitle = "Save regions"
        End Select

        TextRenderer.DrawText(e.Graphics, title, TitleFont,
                              New Point(Pixels(window, 18), top + Pixels(window, 5)), Ink, TextFormatFlags.NoPadding)
        TextRenderer.DrawText(e.Graphics, subtitle, BodyFont,
                              New Point(Pixels(window, 20), top + Pixels(window, 58)), SkyAssets.Ink, TextFormatFlags.NoPadding)

        If window.Name <> "frmMain" AndAlso bounds.Width >= Pixels(window, 400) Then
            Dim ring As New Rectangle(bounds.Right - Pixels(window, 76), top + Pixels(window, 13), Pixels(window, 44), Pixels(window, 44))
            Using outer As New Pen(Cyan, Pixels(window, 2)), inner As New Pen(Color.FromArgb(140, Cyan), Pixels(window, 1))
                e.Graphics.DrawEllipse(outer, ring)
                ring.Inflate(-Pixels(window, 7), -Pixels(window, 7))
                e.Graphics.DrawEllipse(inner, ring)
            End Using
        End If
    End Sub

    'Invalidates the window so themed decoration is repainted after layout or scroll changes.
    Private Shared Sub RefreshWindow(sender As Object, e As EventArgs)
        DirectCast(sender, Control).Invalidate()
    End Sub

    'Converts a logical size to pixels using the control's current DPI.
    Private Shared Function Pixels(control As Control, value As Integer) As Integer
        Return CInt(value * control.DeviceDpi / 96.0F)
    End Function

    'Builds a rounded path used to paint Developer UI surfaces.
    Private Shared Function RoundedRectangle(bounds As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim diameter As Integer = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)))
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90)
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90)
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90)
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    'Draws menu item text using the Developer theme state colors.
    Private NotInheritable Class SkyMenuRenderer
        Inherits ToolStripProfessionalRenderer

        'Installs the shared menu color table and disables rounded menu dropdown edges.
        Friend Sub New()
            MyBase.New(New SkyMenuColors())
            RoundedEdges = False
        End Sub

        'Chooses menu text colors for disabled, selected, and normal menu states.
        Protected Overrides Sub OnRenderItemText(e As ToolStripItemTextRenderEventArgs)
            If Not e.Item.Enabled Then
                e.TextColor = SystemColors.GrayText
            ElseIf e.Item.Selected OrElse e.Item.Pressed Then
                e.TextColor = SkyAssets.Dark
            Else
                e.TextColor = Ink
            End If
            MyBase.OnRenderItemText(e)
        End Sub
    End Class

    'Supplies the shared menu and status-strip color table.
    Private NotInheritable Class SkyMenuColors
        Inherits ProfessionalColorTable

        'Uses the supplied theme colors instead of system menu colors.
        Friend Sub New()
            UseSystemColors = False
        End Sub

        'Supplies the theme color for menu strip gradient begin.
        Public Overrides ReadOnly Property MenuStripGradientBegin As Color
            Get
                Return Navy
            End Get
        End Property
        'Supplies the theme color for menu strip gradient end.
        Public Overrides ReadOnly Property MenuStripGradientEnd As Color
            Get
                Return Navy
            End Get
        End Property
        'Supplies the theme color for tool strip drop down background.
        Public Overrides ReadOnly Property ToolStripDropDownBackground As Color
            Get
                Return Surface
            End Get
        End Property
        'Supplies the theme color for image margin gradient begin.
        Public Overrides ReadOnly Property ImageMarginGradientBegin As Color
            Get
                Return Surface
            End Get
        End Property
        'Supplies the theme color for image margin gradient middle.
        Public Overrides ReadOnly Property ImageMarginGradientMiddle As Color
            Get
                Return Surface
            End Get
        End Property
        'Supplies the theme color for image margin gradient end.
        Public Overrides ReadOnly Property ImageMarginGradientEnd As Color
            Get
                Return Surface
            End Get
        End Property
        'Supplies the theme color for menu item selected.
        Public Overrides ReadOnly Property MenuItemSelected As Color
            Get
                Return Selection
            End Get
        End Property
        'Supplies the theme color for menu item selected gradient begin.
        Public Overrides ReadOnly Property MenuItemSelectedGradientBegin As Color
            Get
                Return Selection
            End Get
        End Property
        'Supplies the theme color for menu item selected gradient end.
        Public Overrides ReadOnly Property MenuItemSelectedGradientEnd As Color
            Get
                Return Selection
            End Get
        End Property
        'Supplies the theme color for menu item pressed gradient begin.
        Public Overrides ReadOnly Property MenuItemPressedGradientBegin As Color
            Get
                Return Selection
            End Get
        End Property
        'Supplies the theme color for menu item pressed gradient middle.
        Public Overrides ReadOnly Property MenuItemPressedGradientMiddle As Color
            Get
                Return Selection
            End Get
        End Property
        'Supplies the theme color for menu item pressed gradient end.
        Public Overrides ReadOnly Property MenuItemPressedGradientEnd As Color
            Get
                Return Selection
            End Get
        End Property
        'Supplies the theme color for menu item border.
        Public Overrides ReadOnly Property MenuItemBorder As Color
            Get
                Return Border
            End Get
        End Property
        'Supplies the theme color for menu border.
        Public Overrides ReadOnly Property MenuBorder As Color
            Get
                Return Border
            End Get
        End Property
        'Supplies the theme color for separator dark.
        Public Overrides ReadOnly Property SeparatorDark As Color
            Get
                Return Border
            End Get
        End Property
        'Supplies the theme color for separator light.
        Public Overrides ReadOnly Property SeparatorLight As Color
            Get
                Return Surface
            End Get
        End Property
        'Supplies the theme color for status strip gradient begin.
        Public Overrides ReadOnly Property StatusStripGradientBegin As Color
            Get
                Return Navy
            End Get
        End Property
        'Supplies the theme color for status strip gradient end.
        Public Overrides ReadOnly Property StatusStripGradientEnd As Color
            Get
                Return Navy
            End Get
        End Property
    End Class
End Class
