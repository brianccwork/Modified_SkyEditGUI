Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Runtime.CompilerServices

Friend NotInheritable Class SkyPresentation
    Private Shared ReadOnly styled As New ConditionalWeakTable(Of Button, Object)()
    Private Shared ReadOnly sandTexture As Bitmap = MakeSandTexture()

    Private Shared Function MakeSandTexture() As Bitmap
        Dim tile As New Bitmap(64, 64)
        Dim random As New Random(412)
        For y As Integer = 0 To 63
            For x As Integer = 0 To 63
                Dim shade As Integer = random.Next(-4, 5)
                tile.SetPixel(x, y, Color.FromArgb(179 + shade, 179 + shade, 167 + shade))
            Next
        Next
        Return tile
    End Function

    Private Sub New()
    End Sub

    Friend Shared Function Rounded(bounds As RectangleF, radius As Single) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim d As Single = Math.Max(1.0F, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)))
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90)
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90)
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90)
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Friend Shared Sub StyleButton(button As Button)
        Dim marker As Object = Nothing
        If styled.TryGetValue(button, marker) Then Return
        styled.Add(button, New Object())
        SkyElevation.Attach(button)
        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = 0
        button.UseVisualStyleBackColor = False
        button.BackColor = SkyAssets.Sand
        button.ForeColor = SkyAssets.Dark
        AddHandler button.Paint, AddressOf PaintButton
        AddHandler button.MouseEnter, AddressOf RefreshButton
        AddHandler button.MouseLeave, AddressOf RefreshButton
        AddHandler button.MouseDown, AddressOf RefreshMouse
        AddHandler button.MouseUp, AddressOf RefreshMouse
        AddHandler button.GotFocus, AddressOf RefreshButton
        AddHandler button.LostFocus, AddressOf RefreshButton
        AddHandler button.EnabledChanged, AddressOf RefreshButton
        AddHandler button.Resize, AddressOf ResizeButton
        ResizeButton(button, EventArgs.Empty)
    End Sub

    Private Shared Sub RefreshMouse(sender As Object, e As MouseEventArgs)
        DirectCast(sender, Control).Invalidate()
    End Sub

    Private Shared Sub RefreshButton(sender As Object, e As EventArgs)
        DirectCast(sender, Control).Invalidate()
    End Sub

    Private Shared Sub ResizeButton(sender As Object, e As EventArgs)
        Dim button As Button = DirectCast(sender, Button)
        If button.Width < 2 OrElse button.Height < 2 Then Return
        Using outline As GraphicsPath = Rounded(New RectangleF(0, 0, button.Width, button.Height), 16 * button.DeviceDpi / 96.0F)
            Dim previous As Region = button.Region
            button.Region = New Region(outline)
            If previous IsNot Nothing Then previous.Dispose()
        End Using
        button.Invalidate()
    End Sub

    Private Shared Sub PaintButton(sender As Object, e As PaintEventArgs)
        Dim button As Button = DirectCast(sender, Button)
        If button.Width < 12 OrElse button.Height < 12 Then Return
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim scale As Single = button.DeviceDpi / 96.0F
        Dim hover As Boolean = button.Enabled AndAlso button.ClientRectangle.Contains(button.PointToClient(Control.MousePosition))
        Dim down As Boolean = hover AndAlso (Control.MouseButtons And MouseButtons.Left) <> MouseButtons.None
        g.Clear(SkyAssets.Sand)
        Using texture As New TextureBrush(sandTexture, WrapMode.Tile)
            g.FillRectangle(texture, button.ClientRectangle)
        End Using
        Using tint As New SolidBrush(Color.FromArgb(If(down, 55, If(hover, 28, If(button.Enabled, 0, 95))), If(down, SkyAssets.Dark, SkyAssets.White)))
            g.FillRectangle(tint, button.ClientRectangle)
        End Using
        'Draw soft concentric strokes inside the region so the parent never clips the glow.
        Dim inset As Single = 5 * scale
        Using outline As GraphicsPath = Rounded(New RectangleF(inset, inset, Math.Max(1, button.Width - inset * 2), Math.Max(1, button.Height - inset * 2)), 12 * scale)
            If hover OrElse button.Focused Then
                For width As Integer = 10 To 4 Step -2
                    Using halo As New Pen(Color.FromArgb(22, SkyAssets.Glow), width * scale)
                        g.DrawPath(halo, outline)
                    End Using
                Next
            End If
            Using edge As New Pen(If(hover OrElse button.Focused, SkyAssets.Glow, Color.FromArgb(170, SkyAssets.Glow)), 1.5F * scale)
                g.DrawPath(edge, outline)
            End Using
        End Using
        Dim caption As Rectangle = Rectangle.Inflate(button.ClientRectangle, -CInt(10 * scale), -CInt(5 * scale))
        If TypeOf button Is SkyInactiveButton Then
            Dim iconSize As Integer = Math.Min(CInt(28 * scale), button.Height - CInt(16 * scale))
            Dim iconBounds As New Rectangle(button.Width - iconSize - CInt(14 * scale), (button.Height - iconSize) \ 2, iconSize, iconSize)
            Dim warning As Image = SkyDecor.Asset("Warning.ico")
            If warning IsNot Nothing Then g.DrawImage(warning, iconBounds)
            caption.Width = Math.Max(1, iconBounds.Left - caption.Left - CInt(8 * scale))
        End If
        If TypeOf button Is SkyHelpShortcut Then
            Dim iconSize As Integer = CInt(26 * scale)
            Dim iconBounds As New Rectangle((button.Width - iconSize) \ 2, CInt(9 * scale), iconSize, iconSize)
            Dim portal As Image = SkyDecor.Asset("Portal.ico")
            If portal IsNot Nothing Then
                g.DrawImage(portal, iconBounds)
            Else
                g.DrawIcon(SystemIcons.Question, iconBounds)
            End If
            caption.Y = iconBounds.Bottom + CInt(2 * scale)
            caption.Height = Math.Max(1, button.Height - caption.Y - CInt(8 * scale))
        End If
        TextRenderer.DrawText(g, button.Text, button.Font, caption, SkyAssets.Ink,
            TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.WordBreak)

    End Sub
End Class

Friend Class SkyLayoutPanel
    Inherits TableLayoutPanel
    Friend Sub New()
        DoubleBuffered = True
    End Sub
End Class

'One decoded GIF and one reusable, window-sized buffer. No queued frame callbacks
'or eager expansion of all GIF frames into bitmaps. Source delays control timing.
Public Class SkyCloudForm
    Inherits Form
    Private cloud As Image
    Private cloudStream As MemoryStream
    Private frameBuffer As Bitmap
    Private frameEnds As Long()
    Private frameCount As Integer
    Private selectedFrame As Integer = -1
    Private bufferDirty As Boolean = True
    Private ReadOnly playback As New Diagnostics.Stopwatch()
    Private ReadOnly frameTimer As New System.Windows.Forms.Timer With {.Interval = 15}

    Protected Overrides Sub OnLoad(e As EventArgs)
        Try
            Dim filename As String = SkyAssets.AssetPath("CloudBackground.gif")
            If filename IsNot Nothing Then
                cloudStream = New MemoryStream(File.ReadAllBytes(filename))
                cloud = Image.FromStream(cloudStream)
                frameCount = cloud.GetFrameCount(Imaging.FrameDimension.Time)
                ReDim frameEnds(frameCount - 1)
                Dim delays As Byte() = Nothing
                Try
                    delays = cloud.GetPropertyItem(&H5100).Value
                Catch ex As ArgumentException
                    'A single-frame GIF may omit frame-delay metadata.
                End Try
                Dim total As Long = 0
                For index As Integer = 0 To frameCount - 1
                    Dim duration As Long = 100
                    If delays IsNot Nothing AndAlso delays.Length >= (index + 1) * 4 Then
                        duration = CLng(BitConverter.ToUInt32(delays, index * 4)) * 10
                    End If
                    'Zero-delay frames have no usable timing; display those at 100ms.
                    If duration = 0 Then duration = 100
                    total += duration
                    frameEnds(index) = total
                Next
                selectedFrame = 0
                cloud.SelectActiveFrame(Imaging.FrameDimension.Time, selectedFrame)
                AddHandler frameTimer.Tick, AddressOf AdvanceFrame
                UpdatePlayback()
            End If
        Catch ex As Exception
            Diagnostics.Debug.WriteLine("Cloud background: " & ex.Message)
        End Try
        MyBase.OnLoad(e)
    End Sub

    Private Sub UpdatePlayback()
        If frameTimer Is Nothing Then Return
        If cloud IsNot Nothing AndAlso frameCount > 1 AndAlso Visible AndAlso WindowState <> FormWindowState.Minimized Then
            playback.Start()
            frameTimer.Start()
        Else
            playback.Stop()
            frameTimer.Stop()
        End If
    End Sub

    Protected Overrides Sub OnVisibleChanged(e As EventArgs)
        MyBase.OnVisibleChanged(e)
        UpdatePlayback()
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        bufferDirty = True
        UpdatePlayback()
        Invalidate(True)
    End Sub

    Private Sub AdvanceFrame(sender As Object, e As EventArgs)
        If cloud Is Nothing OrElse frameEnds Is Nothing Then Return
        Dim elapsed As Long = playback.ElapsedMilliseconds Mod frameEnds(frameCount - 1)
        Dim nextFrame As Integer = Array.BinarySearch(frameEnds, elapsed + 1)
        If nextFrame < 0 Then nextFrame = Not nextFrame
        If nextFrame = selectedFrame Then Return
        selectedFrame = nextFrame
        cloud.SelectActiveFrame(Imaging.FrameDimension.Time, selectedFrame)
        bufferDirty = True
        'Invalidate is coalesced by the message loop; late ticks select the current
        'frame rather than accumulating stale frames in a BeginInvoke queue.
        Invalidate(True)
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        If cloud Is Nothing OrElse ClientSize.Width < 1 OrElse ClientSize.Height < 1 Then
            MyBase.OnPaintBackground(e)
            Return
        End If
        If frameBuffer Is Nothing OrElse frameBuffer.Size <> ClientSize Then
            If frameBuffer IsNot Nothing Then frameBuffer.Dispose()
            frameBuffer = New Bitmap(ClientSize.Width, ClientSize.Height)
            bufferDirty = True
        End If
        If bufferDirty Then
            Using g As Graphics = Graphics.FromImage(frameBuffer)
                g.Clear(SkyAssets.Dark)
                g.InterpolationMode = InterpolationMode.Bilinear
                Dim ratio As Single = Math.Max(CSng(ClientSize.Width) / cloud.Width, CSng(ClientSize.Height) / cloud.Height)
                Dim w As Single = cloud.Width * ratio
                Dim h As Single = cloud.Height * ratio
                g.DrawImage(cloud, (ClientSize.Width - w) / 2, (ClientSize.Height - h) / 2, w, h)
            End Using
            bufferDirty = False
        End If
        e.Graphics.DrawImageUnscaled(frameBuffer, 0, 0)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            frameTimer.Stop()
            RemoveHandler frameTimer.Tick, AddressOf AdvanceFrame
            frameTimer.Dispose()
            playback.Stop()
            If frameBuffer IsNot Nothing Then frameBuffer.Dispose()
            If cloud IsNot Nothing Then cloud.Dispose()
            If cloudStream IsNot Nothing Then cloudStream.Dispose()
            frameBuffer = Nothing
            cloud = Nothing
            cloudStream = Nothing
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class

Friend Class SkyPortalPreview
    Inherits PictureBox
    Friend Property AnchorFigureToPortal As Boolean

    Friend Sub New()
        DoubleBuffered = True
        BackColor = SkyAssets.Panel
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.HighQualityBicubic
        Dim portalBounds As New RectangleF(Width * 0.12F, Height * 0.57F, Width * 0.76F, Height * 0.42F)
        If SkyAssets.PortalImage IsNot Nothing Then
            DrawFit(g, SkyAssets.PortalImage, portalBounds)
        Else
            Using glow As New SolidBrush(Color.FromArgb(70, SkyAssets.Bright)), rim As New Pen(SkyAssets.Sand, 3)
                g.FillEllipse(glow, portalBounds)
                g.DrawEllipse(rim, portalBounds)
            End Using
        End If
        Dim diameter As Single = Math.Min(Width * 0.65F, Height * 0.72F)
        Dim figureTop As Single = If(AnchorFigureToPortal, Math.Max(0, portalBounds.Y + portalBounds.Height * 0.18F - diameter), Height * 0.03F)
        Dim figureBounds As New RectangleF((Width - diameter) / 2, figureTop, diameter, diameter)
        If Image Is Nothing Then
            'Always show an icon, even when neither figure artwork nor ERROR PNG exists.
            Using fill As New SolidBrush(SkyAssets.Sand), rim As New Pen(SkyAssets.Ink, 2)
                g.FillEllipse(fill, figureBounds)
                g.DrawEllipse(rim, figureBounds)
            End Using
            TextRenderer.DrawText(g, "?", SimpleUi.Heading, Rectangle.Round(figureBounds), SkyAssets.Ink,
                TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
            Return
        End If
        Dim state As Drawing2D.GraphicsState = g.Save()
        Try
            Using circle As New GraphicsPath()
                circle.AddEllipse(figureBounds)
                g.SetClip(circle)
                DrawFit(g, Image, figureBounds)
            End Using
        Finally
            g.Restore(state)
        End Try
        'Do not call PictureBox.OnPaint: it would paint the image a second time.
    End Sub

    Private Shared Sub DrawFit(g As Graphics, source As Image, bounds As RectangleF)
        Dim ratio As Single = Math.Min(bounds.Width / source.Width, bounds.Height / source.Height)
        Dim w As Single = source.Width * ratio
        Dim h As Single = source.Height * ratio
        g.DrawImage(source, bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h)
    End Sub
End Class
