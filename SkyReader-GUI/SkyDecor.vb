Option Strict On
Option Explicit On

Friend NotInheritable Class SkyDecor
    Private Shared ReadOnly images As New Dictionary(Of String, Image)(StringComparer.OrdinalIgnoreCase)
    'Keeps this shared helper from being instantiated.
    Private Sub New()
    End Sub

    'Caches a named decoration and supplies system icon fallbacks for missing warning or waiting icons.
    Friend Shared Function Asset(name As String) As Image
        If images.ContainsKey(name) Then Return images(name)
        Dim loaded As Image = Nothing
        Try
            Dim path As String = SkyAssets.AssetPath(name)
            If path IsNot Nothing Then
                If name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) Then
                    Using source As New Icon(path)
                        loaded = source.ToBitmap()
                    End Using
                Else
                    Using source As Image = Image.FromFile(path)
                        loaded = New Bitmap(source)
                    End Using
                End If
            End If
        Catch ex As Exception
            Diagnostics.Debug.WriteLine("Decoration: " & ex.Message)
        End Try
        If loaded Is Nothing AndAlso name = "Warning.ico" Then loaded = SystemIcons.Warning.ToBitmap()
        If loaded Is Nothing AndAlso name = "Waiting.ico" Then loaded = SystemIcons.Information.ToBitmap()
        images.Add(name, loaded)
        Return loaded
    End Function

    'Creates a transparent, scaled picture control for a named decoration.
    Friend Shared Function Badge(name As String) As PictureBox
        Return New PictureBox With {.Image = Asset(name), .Dock = DockStyle.Fill,
            .SizeMode = PictureBoxSizeMode.Zoom, .Margin = New Padding(4), .TabStop = False,
            .AccessibleName = name.Replace(".ico", ""), .BackColor = Color.Transparent}
    End Function

    'Places an icon beside an existing text control without changing the text itself.
    Friend Shared Function WithBadge(text As Control, name As String, Optional right As Boolean = False) As Control
        Dim row As New TableLayoutPanel With {.Dock = DockStyle.Fill, .RowCount = 1, .ColumnCount = 2,
            .Margin = New Padding(0), .BackColor = SkyAssets.Panel}
        row.ColumnStyles.Add(New ColumnStyle(If(right, SizeType.Percent, SizeType.Absolute), If(right, 100, 44)))
        row.ColumnStyles.Add(New ColumnStyle(If(right, SizeType.Absolute, SizeType.Percent), If(right, 44, 100)))
        row.Controls.Add(text, If(right, 0, 1), 0)
        row.Controls.Add(Badge(name), If(right, 1, 0), 0)
        Return row
    End Function

    'Replaces a placeholder button with an inert themed button and an in development tooltip. Lol. Update, I gave up on tooltips being useful xD
    Friend Shared Function Development(button As Button) As Control
        Dim inactive As New SkyInactiveButton With {.Text = button.Text, .Name = button.Name,
            .Font = button.Font, .Dock = button.Dock, .Margin = button.Margin,
            .TabStop = False, .Cursor = Cursors.Default, .AccessibleDescription = "In development."}
        button.Dispose()
        Dim tip As New ToolTip With {.ShowAlways = True}
        tip.SetToolTip(inactive, "In development.")
        AddHandler inactive.Disposed, Sub(sender, e) tip.Dispose()
        Return inactive
    End Function

    'Builds a text and icon row with spacing above the associated numeric input.
    Friend Shared Function FieldLabel(caption As String, icon As String) As Control
        Dim label As Label = SimpleUi.Caption(caption)
        label.AutoSize = False
        label.Margin = New Padding(8, 0, 8, 0)
        label.TextAlign = ContentAlignment.MiddleLeft
        Dim row As Control = WithBadge(label, icon)
        row.Margin = New Padding(0, 0, 0, 8)
        Return row
    End Function

    'Reserves right side padding and paints a warning icon in the card's upper right corner.
    Friend Shared Sub WarningOnCard(card As Control)
        card.Padding = New Padding(card.Padding.Left, card.Padding.Top, card.Padding.Right + 48, card.Padding.Bottom)
        AddHandler card.Paint, Sub(sender, e)
                                   Dim size As Integer = CInt(32 * card.DeviceDpi / 96.0F)
                                   Dim warning As Image = Asset("Warning.ico")
                                   If warning IsNot Nothing Then e.Graphics.DrawImage(warning, card.Width - size - 10, 10, size, size)
                               End Sub
    End Sub
End Class
