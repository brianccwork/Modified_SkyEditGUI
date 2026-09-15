Option Strict On
Option Explicit On

Imports System.Runtime.CompilerServices
Imports System.Drawing.Drawing2D

Friend NotInheritable Class SkyElevation
    Private Shared ReadOnly raised As New ConditionalWeakTable(Of Control, Object)()
    Private Shared ReadOnly parents As New ConditionalWeakTable(Of Control, Object)()
    Private Sub New()
    End Sub

    Friend Shared Sub Attach(control As Control)
        Dim marker As Object = Nothing
        If raised.TryGetValue(control, marker) Then Return
        raised.Add(control, New Object())
        AddHandler control.ParentChanged, AddressOf RefreshParent
        AddHandler control.LocationChanged, AddressOf RefreshParent
        AddHandler control.SizeChanged, AddressOf RefreshParent
        AddHandler control.VisibleChanged, AddressOf RefreshParent
        RefreshParent(control, EventArgs.Empty)
    End Sub

    Private Shared Sub RefreshParent(sender As Object, e As EventArgs)
        Dim control As Control = DirectCast(sender, Control)
        Dim parent As Control = control.Parent
        If parent Is Nothing OrElse parent.IsDisposed Then Return
        Dim marker As Object = Nothing
        If Not parents.TryGetValue(parent, marker) Then
            parents.Add(parent, New Object())
            AddHandler parent.Paint, AddressOf PaintShadows
        End If
        parent.Invalidate()
    End Sub

    Private Shared Sub PaintShadows(sender As Object, e As PaintEventArgs)
        Dim parent As Control = DirectCast(sender, Control)
        Dim state As GraphicsState = e.Graphics.Save()
        Try
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            For Each child As Control In parent.Controls
                Dim marker As Object = Nothing
                If Not child.Visible OrElse Not raised.TryGetValue(child, marker) Then Continue For
                Dim scale As Single = child.DeviceDpi / 96.0F
                Dim bounds As New RectangleF(child.Left + 2 * scale, child.Top + 4 * scale, child.Width, child.Height)
                If bounds.Width < 2 OrElse bounds.Height < 2 Then Continue For
                Using path As GraphicsPath = SkyPresentation.Rounded(bounds, 16 * scale)
                    For width As Integer = 6 To 2 Step -2
                        Using soft As New Pen(Color.FromArgb(10, Color.Black), width * scale)
                            e.Graphics.DrawPath(soft, path)
                        End Using
                    Next
                    Using fill As New SolidBrush(Color.FromArgb(28, Color.Black))
                        e.Graphics.FillPath(fill, path)
                    End Using
                End Using
            Next
        Finally
            e.Graphics.Restore(state)
        End Try
    End Sub

    Friend Shared Sub CompactTitle(label As Label)
        label.Dock = DockStyle.None
        label.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        label.AutoSize = True
        label.Padding = New Padding(14, 6, 14, 6)
        label.Margin = New Padding(8, 6, 8, 10)
        AddHandler label.SizeChanged, Sub(sender, e) SkyPolish.RoundControl(label, 14)
        Attach(label)
    End Sub
End Class

'An inert button surface can receive hover tooltips without an extra wrapper.
Friend Class SkyInactiveButton
    Inherits Button
    Protected Overrides Sub OnClick(e As EventArgs)
        'Intentionally no navigation or click event while under development.
    End Sub
    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
    End Sub
    Protected Overrides Sub OnDoubleClick(e As EventArgs)
    End Sub
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If e.KeyCode = Keys.Enter OrElse e.KeyCode = Keys.Space Then
            e.SuppressKeyPress = True
            Return
        End If
        MyBase.OnKeyDown(e)
    End Sub
    Protected Overrides Function CreateAccessibilityInstance() As AccessibleObject
        Return New InactiveAccessibleObject(Me)
    End Function
    Private Class InactiveAccessibleObject
        Inherits Control.ControlAccessibleObject
        Friend Sub New(owner As Button)
            MyBase.New(owner)
        End Sub
        Public Overrides Sub DoDefaultAction()
        End Sub
        Public Overrides ReadOnly Property Role As AccessibleRole
            Get
                Return AccessibleRole.PushButton
            End Get
        End Property
        Public Overrides ReadOnly Property State As AccessibleStates
            Get
                Return MyBase.State Or AccessibleStates.Unavailable
            End Get
        End Property
    End Class
End Class
