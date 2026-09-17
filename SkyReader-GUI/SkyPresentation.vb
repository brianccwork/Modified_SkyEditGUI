Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Runtime.CompilerServices

Friend NotInheritable Class SkyPresentation
    Private Shared ReadOnly styled As New ConditionalWeakTable(Of Button, Object)()
    Private Shared ReadOnly sandTexture As Bitmap = MakeSandTexture()

    'Builds a small reusable sand-colored texture for the shared buttons.
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

    'Keeps this shared helper from being instantiated.
    Private Sub New()
    End Sub

    'Builds a rounded rectangle path bounded by the available control dimensions.
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

    'Applies shared button styling and registers the repaint handlers used for interaction feedback.
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

    'Invalidates the button when a mouse press or release changes its appearance.
    Private Shared Sub RefreshMouse(sender As Object, e As MouseEventArgs)
        DirectCast(sender, Control).Invalidate()
    End Sub

    'Invalidates the button after hover, focus, or enabled state changes.
    Private Shared Sub RefreshButton(sender As Object, e As EventArgs)
        DirectCast(sender, Control).Invalidate()
    End Sub

    'Updates the button's rounded clipping region when its size changes. (Added this to allow poeple to scale the program).
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

    'Draws the textured button, interaction border, caption, and any feature-specific icon.
    Private Shared Sub PaintButton(sender As Object, e As PaintEventArgs)
        Dim button As Button = DirectCast(sender, Button)
        If button.Width < 12 OrElse button.Height < 12 Then Return
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim scale As Single = button.DeviceDpi / 96.0F
        Dim hover As Boolean = button.Enabled AndAlso button.ClientRectangle.Contains(button.PointToClient(Control.MousePosition))
        Dim down As Boolean = hover AndAlso (Control.MouseButtons And MouseButtons.Left) <> MouseButtons.None
        'Give Save Changes and Save Villain the greeeen
        If button.Text.Replace("&", "").Trim().Equals("Save Changes", StringComparison.OrdinalIgnoreCase) OrElse
            button.Text.Replace("&", "").Trim().Equals("Save Villain", StringComparison.OrdinalIgnoreCase) Then
            g.Clear(SkyAssets.SaveGreen)
        Else
            g.Clear(SkyAssets.Sand)
            Using texture As New TextureBrush(sandTexture, WrapMode.Tile)
                g.FillRectangle(texture, button.ClientRectangle)
            End Using
        End If
        'this is how you control the buttons down, hover, relaxed, and awaiting shades.
        Using tint As New SolidBrush(Color.FromArgb(If(down, 55, If(hover, 28, If(button.Enabled, 0, 20))), If(down, SkyAssets.Dark, SkyAssets.White)))
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

'Enables double buffering for shared table layouts.
Friend Class SkyLayoutPanel
    Inherits TableLayoutPanel
    'Enables double buffering for this table layout.
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

    'Loads the cloud GIF and frame timing information, then starts playback when visible.
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
                    'A single frame GIF may omit frame delay metadata.
                End Try
                Dim total As Long = 0
                For index As Integer = 0 To frameCount - 1
                    Dim duration As Long = 100
                    If delays IsNot Nothing AndAlso delays.Length >= (index + 1) * 4 Then
                        duration = CLng(BitConverter.ToUInt32(delays, index * 4)) * 10
                    End If
                    'Zero elay frames have no usable timing so lets take note for something like this.
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

    'Starts or pauses cloud animation according to visibility and minimized state.
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

    'Updates cloud playback when the form becomes visible or hidden.
    Protected Overrides Sub OnVisibleChanged(e As EventArgs)
        MyBase.OnVisibleChanged(e)
        UpdatePlayback()
    End Sub

    'Marks the cloud buffer for rebuilding and updates playback after a window resize.
    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        bufferDirty = True
        UpdatePlayback()
        Invalidate(True)
    End Sub

    'Chooses the current GIF frame from elapsed time and queues a repaint only when it changes.
    Private Sub AdvanceFrame(sender As Object, e As EventArgs)
        If cloud Is Nothing OrElse frameEnds Is Nothing Then Return
        Dim elapsed As Long = playback.ElapsedMilliseconds Mod frameEnds(frameCount - 1)
        Dim nextFrame As Integer = Array.BinarySearch(frameEnds, elapsed + 1)
        If nextFrame < 0 Then nextFrame = Not nextFrame
        If nextFrame = selectedFrame Then Return
        selectedFrame = nextFrame
        cloud.SelectActiveFrame(Imaging.FrameDimension.Time, selectedFrame)
        bufferDirty = True
        'late ticks select the current
        'frame rather than accumulating stale frames
        Invalidate(True)
    End Sub

    'Renders the selected cloud frame through a reusable buffer or uses the normal background fallback.
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

    'Stops animation and releases the timer, frame buffer, GIF, and retained image stream.
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

'Paints circular figure artwork and portal artwork together in one preview control.
Friend Class SkyPortalPreview
    Inherits PictureBox
    'Uses the fitted portal image as the circle anchor when enabled for the trap page.
    Friend Property AnchorFigureToPortal As Boolean

    'Initializes the buffered portal preview and its default background.
    Friend Sub New()
        DoubleBuffered = True
        BackColor = SkyAssets.Panel
    End Sub

    'Draws the fitted portal and clips figure artwork into the circular preview above it.
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.HighQualityBicubic
        Dim portalBounds As New RectangleF(Width * 0.12F, Height * 0.57F, Width * 0.76F, Height * 0.42F)
        Dim fittedPortal As RectangleF = portalBounds
        If SkyAssets.PortalImage IsNot Nothing Then
            fittedPortal = FitBounds(SkyAssets.PortalImage, portalBounds)
            g.DrawImage(SkyAssets.PortalImage, fittedPortal)
        Else
            Using glow As New SolidBrush(Color.FromArgb(70, SkyAssets.Bright)), rim As New Pen(SkyAssets.Sand, 3)
                g.FillEllipse(glow, portalBounds)
                g.DrawEllipse(rim, portalBounds)
            End Using
        End If
        Dim diameter As Single = Math.Min(Width * 0.65F, Height * 0.72F)
        'Anchor against the rendered image, not its letterboxed allocation.
        Dim figureTop As Single = If(AnchorFigureToPortal, Math.Max(0, fittedPortal.Y + fittedPortal.Height * 0.35F - diameter), Height * 0.03F)
        Dim figureBounds As New RectangleF((Width - diameter) / 2, figureTop, diameter, diameter)
        'Use the default portal artwork when there is no figure image, without owning the cached asset.
        Dim displayImage As Image = If(Image, SkyDecor.Asset("PortalPlaceholder.png"))
        If displayImage Is Nothing Then Return
        Dim state As Drawing2D.GraphicsState = g.Save()
        Try
            Using circle As New GraphicsPath()
                circle.AddEllipse(figureBounds)
                g.SetClip(circle)
                DrawFit(g, displayImage, figureBounds)
            End Using
        Finally
            g.Restore(state)
        End Try
        'Add a four pixel black border to the placeholder only.
        If Image Is Nothing Then
            Dim thickness As Single = 5.0F * DeviceDpi / 96.0F
            Dim bounds As RectangleF = figureBounds
            bounds.Inflate(-thickness / 2, -thickness / 2)
            Using border As New Pen(Color.Black, thickness)
                g.DrawEllipse(border, bounds)
            End Using
        End If
        'Do not call PictureBox.OnPaint: it would paint the image a second time.
    End Sub

    'Calculates an aspect-preserving image rectangle centered inside its allotted space.
    Private Shared Function FitBounds(source As Image, bounds As RectangleF) As RectangleF
        Dim ratio As Single = Math.Min(bounds.Width / source.Width, bounds.Height / source.Height)
        Dim w As Single = source.Width * ratio
        Dim h As Single = source.Height * ratio
        Return New RectangleF(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h)
    End Function

    'Draws the source image into its calculated bounds.
    Private Shared Sub DrawFit(g As Graphics, source As Image, bounds As RectangleF)
        g.DrawImage(source, FitBounds(source, bounds))
    End Sub
End Class
