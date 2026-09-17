Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Runtime.CompilerServices

Friend NotInheritable Class SkyElevation
    Private Shared ReadOnly raised As New ConditionalWeakTable(Of Control, Object)()
    Private Shared ReadOnly parents As New ConditionalWeakTable(Of Control, Object)()
    'Keeps this shared helper from being instantiated.
    Private Sub New()
    End Sub

    'NRegisters a control for shadow painting and tracks changes that require its parent to redraw.
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

    'Hooks shadow painting once per parent and invalidates that parent when a raised control changes.
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

    'Draws soft rounded shadows behind registered visible child controls.
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

    'Sizes a title to its text and adds rounded edges and a parent-painted shadow.
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

'This class is really for development and just rendering things useless.
Friend Class SkyInactiveButton
    Inherits Button
    'Suppresses normal click activation for a feature that is still in development.
    Protected Overrides Sub OnClick(e As EventArgs)
        'Intentionally no navigation or click event while under development.
    End Sub
    'Prevents mouse click events from activating the inactive button.
    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
    End Sub
    'Prevents double click events from activating the inactive button.
    Protected Overrides Sub OnDoubleClick(e As EventArgs)
    End Sub
    'Consumes Enter and Space so the inactive button cannot be activated by keyboard.
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If e.KeyCode = Keys.Enter OrElse e.KeyCode = Keys.Space Then
            e.SuppressKeyPress = True
            Return
        End If
        MyBase.OnKeyDown(e)
    End Sub
    'Exposes the inactive button through an accessibility object that reports it as unavailable.
    Protected Overrides Function CreateAccessibilityInstance() As AccessibleObject
        Return New InactiveAccessibleObject(Me)
    End Function
    'Reports the placeholder button as unavailable to assistive technology.
    Private Class InactiveAccessibleObject
        Inherits Control.ControlAccessibleObject
        'Attaches the custom accessibility state to its placeholder button.
        Friend Sub New(owner As Button)
            MyBase.New(owner)
        End Sub
        'Prevents accessibility clients from invoking the inactive button's default action.
        Public Overrides Sub DoDefaultAction()
        End Sub
        'Reports this accessible control as a push button.
        Public Overrides ReadOnly Property Role As AccessibleRole
            Get
                Return AccessibleRole.PushButton
            End Get
        End Property
        'Adds the unavailable state to the accessible button's existing state flags.
        Public Overrides ReadOnly Property State As AccessibleStates
            Get
                Return MyBase.State Or AccessibleStates.Unavailable
            End Get
        End Property
    End Class
End Class
