Option Strict On
Option Explicit On

Partial Public Class frmMain
    'Creates and initializes the hidden Developer controls needed by the shared parsers only once.
    Friend Sub EnsureEditorInitialized()
        If skyThemeApplied Then Return
        Dim nativeHandle As IntPtr = Handle
        PerformAutoScale()
        OnLoad(EventArgs.Empty)
    End Sub

    'Cancels closing while any Developer portal worker is still running.
    Private Sub KeepPortalOperationOpen(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If bgReadPortal.IsBusy OrElse bgReadPortalDuo.IsBusy OrElse bgWritePortal.IsBusy OrElse bgWritePortalDuo.IsBusy Then
            e.Cancel = True
            SaldeStatus.Text = "Wait for the portal operation to finish before returning home."
        End If
    End Sub
End Class