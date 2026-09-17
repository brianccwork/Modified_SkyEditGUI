Option Strict On
Option Explicit On

'Namespace-local replacement preserves existing MessageBox.Show call sites and
'DialogResult values while bringing app owned dialogs into the shared theme.
Friend NotInheritable Class MessageBox
    'Keeps this shared helper from being instantiated.
    Private Sub New()
    End Sub

    'Displays a themed message dialog while returning the expected DialogResult to the caller.
    Friend Shared Function Show(text As String, Optional caption As String = "Skylander Editor",
                                Optional buttons As MessageBoxButtons = MessageBoxButtons.OK,
                                Optional icon As MessageBoxIcon = MessageBoxIcon.None) As DialogResult
        Return Show(Form.ActiveForm, text, caption, buttons, icon)
    End Function

    'Displays a themed message dialog while returning the expected DialogResult to the caller.
    Friend Shared Function Show(owner As IWin32Window, text As String, caption As String,
                                Optional buttons As MessageBoxButtons = MessageBoxButtons.OK,
                                Optional icon As MessageBoxIcon = MessageBoxIcon.None) As DialogResult
        Dim choices As DialogResult()
        Select Case buttons
            Case MessageBoxButtons.YesNo : choices = {DialogResult.Yes, DialogResult.No}
            Case MessageBoxButtons.YesNoCancel : choices = {DialogResult.Yes, DialogResult.No, DialogResult.Cancel}
            Case MessageBoxButtons.OKCancel : choices = {DialogResult.OK, DialogResult.Cancel}
            Case MessageBoxButtons.RetryCancel : choices = {DialogResult.Retry, DialogResult.Cancel}
            Case MessageBoxButtons.AbortRetryIgnore : choices = {DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore}
            Case Else : choices = {DialogResult.OK}
        End Select
        Using dialog As New Form()
            dialog.Text = caption
            dialog.Font = SimpleUi.Body
            dialog.BackColor = SkyAssets.Panel
            SkyAssets.ApplyWindowIcon(dialog)
            dialog.ClientSize = New Size(760, 420)
            dialog.AutoScaleDimensions = New SizeF(96, 96)
            dialog.AutoScaleMode = AutoScaleMode.Dpi
            dialog.StartPosition = If(owner Is Nothing, FormStartPosition.CenterScreen, FormStartPosition.CenterParent)
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog
            dialog.MaximizeBox = False
            dialog.MinimizeBox = False
            dialog.ShowInTaskbar = False
            dialog.ControlBox = buttons <> MessageBoxButtons.YesNo AndAlso buttons <> MessageBoxButtons.AbortRetryIgnore
            Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .RowCount = 3, .ColumnCount = 1, .Padding = New Padding(20)}
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 90))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 84))
            Dim heading As Label = SimpleUi.Caption(caption)
            heading.Font = SimpleUi.ActionFont
            heading.AutoSize = False
            Dim body As New Label With {.Text = text, .AutoSize = False, .TabStop = False,
                .UseMnemonic = False, .Dock = DockStyle.Fill, .BackColor = SkyAssets.Panel,
                .ForeColor = SkyAssets.Ink, .Margin = New Padding(8), .TextAlign = ContentAlignment.MiddleLeft}
            Dim actions As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = choices.Length, .RowCount = 1}
            For index As Integer = 0 To choices.Length - 1
                actions.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F / choices.Length))
                Dim action As Button = SimpleUi.Action(choices(index).ToString())
                action.DialogResult = choices(index)
                actions.Controls.Add(action, index, 0)
                If index = 0 Then dialog.AcceptButton = action
                If choices(index) = DialogResult.Cancel OrElse choices(index) = DialogResult.OK Then dialog.CancelButton = action
            Next
            layout.Controls.Add(SkyDecor.WithBadge(heading, "Warning.ico", True), 0, 0)
            layout.Controls.Add(body, 0, 1)
            layout.Controls.Add(actions, 0, 2)
            dialog.Controls.Add(layout)
            SimpleUi.StyleButtons(dialog)
            SkyPolish.FitDialog(dialog, heading, body)
            Select Case icon
                Case MessageBoxIcon.Error : System.Media.SystemSounds.Hand.Play()
                Case MessageBoxIcon.Warning : System.Media.SystemSounds.Exclamation.Play()
                Case MessageBoxIcon.Information : System.Media.SystemSounds.Asterisk.Play()
                Case MessageBoxIcon.Question : System.Media.SystemSounds.Question.Play()
            End Select
            Return dialog.ShowDialog(owner)
        End Using
    End Function
End Class
