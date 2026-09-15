Option Strict On
Option Explicit On

Friend NotInheritable Class SimpleUi
    Friend Shared ReadOnly Sky As Color = Color.FromArgb(193, 229, 249)
    Friend Shared ReadOnly Blue As Color = Color.FromArgb(35, 117, 175)
    Friend Shared ReadOnly Navy As Color = Color.FromArgb(20, 47, 79)
    Friend Shared ReadOnly Body As New Font("Segoe UI", 10.0F)
    Friend Shared ReadOnly Heading As New Font("Segoe UI", 25.0F, FontStyle.Bold)
    Friend Shared ReadOnly ActionFont As New Font("Segoe UI", 13.0F, FontStyle.Bold)
    Friend Shared ReadOnly NumberFont As New Font("Segoe UI", 20.0F)

    Private Sub New()
    End Sub

    Friend Shared Function Action(caption As String) As Button
        Return New Button With {.Text = caption, .Font = ActionFont, .BackColor = Blue,
            .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Dock = DockStyle.Fill,
            .Margin = New Padding(8), .Cursor = Cursors.Hand, .UseVisualStyleBackColor = False}
    End Function

    Friend Shared Function Caption(text As String) As Label
        Return New Label With {.Text = text, .Font = Body, .ForeColor = Navy,
            .Dock = DockStyle.Fill, .AutoSize = True, .Margin = New Padding(8, 3, 8, 3),
            .TextAlign = ContentAlignment.MiddleLeft}
    End Function

    Friend Shared Sub StyleButtons(parent As Control)
        For Each control As Control In parent.Controls
            Dim button As Button = TryCast(control, Button)
            If button IsNot Nothing Then
                button.FlatAppearance.BorderSize = 0
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(53, 143, 204)
                button.FlatAppearance.MouseDownBackColor = Navy
            End If
            If control.HasChildren Then StyleButtons(control)
        Next
    End Sub
End Class
