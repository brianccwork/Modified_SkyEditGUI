Option Strict On
Option Explicit On

'This file should later be merged, or organized properly into a file system but for now it'll live here.
Friend NotInheritable Class SkyPolish
    'Keeps this shared helper from being instantiated.
    Private Sub New()
    End Sub

    'Replaces the control's clipping region with a DPI-scaled rounded outline.
    Friend Shared Sub RoundControl(control As Control, radius As Single)
        If control.Width < 2 OrElse control.Height < 2 Then Return
        Using path As Drawing.Drawing2D.GraphicsPath = SkyPresentation.Rounded(
            New RectangleF(0, 0, control.Width, control.Height), radius * control.DeviceDpi / 96.0F)
            Dim previous As Region = control.Region
            control.Region = New Region(path)
            If previous IsNot Nothing Then previous.Dispose()
        End Using
    End Sub

    'Measures dialog text and sizes its layout to fit within the available screen area.
    Friend Shared Sub FitDialog(window As Form, heading As Label, body As Label)
        'Measure after DPI scaling and font inheritance. Warning text is a label,
        'not an editable/scrollable native text field.
        AddHandler window.Shown, Sub(sender, e)
            Dim scale As Single = window.DeviceDpi / 96.0F
            Dim work As Rectangle = Screen.FromControl(window).WorkingArea
            Dim width As Integer = Math.Min(CInt(760 * scale), work.Width - CInt(48 * scale))
            window.ClientSize = New Size(Math.Max(320, width), window.ClientSize.Height)
            window.PerformLayout()
            Dim textWidth As Integer = Math.Max(120, body.Width)
            Dim bodySize As Size = body.GetPreferredSize(New Size(textWidth, 0))
            Dim titleSize As Size = heading.GetPreferredSize(New Size(Math.Max(120, heading.Width), 0))
            Dim layout As TableLayoutPanel = DirectCast(body.Parent, TableLayoutPanel)
            layout.RowStyles(0).Height = Math.Max(CInt(60 * scale), titleSize.Height + heading.Margin.Vertical)
            Dim height As Integer = CInt(layout.RowStyles(0).Height + layout.RowStyles(2).Height) +
                bodySize.Height + body.Margin.Vertical + layout.Padding.Vertical + CInt(24 * scale)
            window.ClientSize = New Size(window.ClientSize.Width, Math.Max(CInt(260 * scale), height))
            window.Location = New Point(work.Left + (work.Width - window.Width) \ 2,
                work.Top + Math.Max(0, (work.Height - window.Height) \ 2))
        End Sub
    End Sub
End Class

'Provides a rounded gray layout panel with a parent-painted shadow.
Friend Class SkyCardLayout
    Inherits TableLayoutPanel
    'Initializes the rounded panel background and registers its shadow.
    Friend Sub New()
        DoubleBuffered = True
        BackColor = SkyAssets.Panel
        SkyElevation.Attach(Me)
    End Sub
    'Recomputes the rounded surface or text-card height after its size changes.
    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        MyBase.OnSizeChanged(e)
        SkyPolish.RoundControl(Me, 20)
    End Sub
End Class

'Compact, nonselectable text card; height follows wrapped text as width changes.
'Fits wrapped read-only text into a rounded, shadowed label.
Friend Class SkyTextCard
    Inherits Label
    Private fitting As Boolean

    'Initializes a read-only text label with buffered painting and a parent-owned shadow.
    Friend Sub New()
        AutoSize = False
        UseMnemonic = False
        SkyElevation.Attach(Me)
        Cursor = Cursors.Default
        SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    'Adjusts the card height to wrapped text and reapplies its rounded clipping region.
    Private Sub FitText()
        If fitting OrElse Width < 40 OrElse Font Is Nothing Then Return
        fitting = True
        Try
            Dim preferred As Size = GetPreferredSize(New Size(Width, 0))
            Height = Math.Max(48, preferred.Height)
            SkyPolish.RoundControl(Me, 16)
        Finally
            fitting = False
        End Try
    End Sub

    'Recomputes the rounded surface or text-card height after its size changes.
    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        MyBase.OnSizeChanged(e)
        FitText()
    End Sub
    'Refits the text card after its displayed text changes.
    Protected Overrides Sub OnTextChanged(e As EventArgs)
        MyBase.OnTextChanged(e)
        FitText()
    End Sub
    'Refits the text card after its font metrics change.
    Protected Overrides Sub OnFontChanged(e As EventArgs)
        MyBase.OnFontChanged(e)
        FitText()
    End Sub
End Class
