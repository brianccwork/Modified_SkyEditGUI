Option Strict On
Option Explicit On

Friend NotInheritable Class FigureWarnings
    Friend Const UnsafeText As String = "This Skylander may be corrupted or unsafe to write. Keep a backup before attempting recovery. Please reset the figure in a compatible Skylanders game, then collect at least 1 gold so the game saves fresh data. Resetting clears progress. Read the figure again before editing."
    Friend Const SenseiText As String = "Senseis must first be initialized in-game before editing. Place the Sensei in Skylanders Imaginators, walk around, and collect at least 1 gold so the game saves valid Sensei data to the NFC tag."

    Private Sub New()
    End Sub

    'Use the existing parser's checksum results; do not attempt automatic repair.
    Friend Shared Function HasUnsafeCharacterData() As Boolean
        For Each indicator As PictureBox In New PictureBox() {
            frmMain.picSerial, frmMain.picHeader,
            frmMain.picArea0Type1, frmMain.picArea0Type2, frmMain.picArea0Type3, frmMain.picArea0Type4,
            frmMain.picArea1Type1, frmMain.picArea1Type2, frmMain.picArea1Type3, frmMain.picArea1Type4}
            If indicator.BackColor <> Color.Green Then Return True
        Next
        Return False
    End Function

    Friend Shared Sub ShowWarning(owner As IWin32Window, heading As String, message As String)
        Using popup As New Form()
            popup.Text = heading
            popup.Font = SimpleUi.Body
            popup.BackColor = SimpleUi.Sky
            popup.ClientSize = New Size(570, 340)
            popup.AutoScaleDimensions = New SizeF(96, 96)
            popup.AutoScaleMode = AutoScaleMode.Dpi
            popup.FormBorderStyle = FormBorderStyle.FixedDialog
            popup.StartPosition = FormStartPosition.CenterParent
            popup.MinimizeBox = False
            popup.MaximizeBox = False
            popup.ShowInTaskbar = False
            Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(20)}
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 58))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 64))
            Dim title As Label = SimpleUi.Caption(heading)
            title.AutoSize = False
            title.Font = SimpleUi.ActionFont
            Dim body As New TextBox With {.Text = message, .Multiline = True, .ReadOnly = True,
                .BorderStyle = BorderStyle.None, .BackColor = SimpleUi.Sky, .ForeColor = SimpleUi.Navy,
                .Dock = DockStyle.Fill, .ScrollBars = ScrollBars.Vertical, .Margin = New Padding(8)}
            Dim confirm As Button = SimpleUi.Action("I understand")
            confirm.DialogResult = DialogResult.OK
            layout.Controls.Add(title, 0, 0)
            layout.Controls.Add(body, 0, 1)
            layout.Controls.Add(confirm, 0, 2)
            popup.Controls.Add(layout)
            popup.AcceptButton = confirm
            popup.CancelButton = confirm
            SimpleUi.StyleButtons(popup)
            popup.ShowDialog(owner)
        End Using
    End Sub
End Class
