Option Strict On
Option Explicit On

Friend NotInheritable Class SimpleUi
    Friend Shared ReadOnly Sky As Color = SkyAssets.Panel
    Friend Shared ReadOnly Blue As Color = SkyAssets.Bright
    Friend Shared ReadOnly Navy As Color = SkyAssets.Ink
    Friend Shared ReadOnly Body As Font = SkyAssets.UiFont(10.0F)
    Friend Shared ReadOnly Heading As Font = SkyAssets.UiFont(25.0F)
    Friend Shared ReadOnly ActionFont As Font = SkyAssets.UiFont(13.0F)
    Friend Shared ReadOnly NumberFont As Font = SkyAssets.UiFont(20.0F)

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
                SkyPresentation.StyleButton(button)
            End If
            If control.HasChildren Then StyleButtons(control)
        Next
    End Sub
End Class
