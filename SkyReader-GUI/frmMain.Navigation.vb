Option Strict On
Option Explicit On

Partial Public Class frmMain
    'The existing parsers use this default form's controls. Initialize it without
    'showing it; the guarded OnLoad hook runs the original Load handler once.
    Friend Sub EnsureEditorInitialized()
        If skyThemeApplied Then Return
        Dim nativeHandle As IntPtr = Handle
        PerformAutoScale()
        OnLoad(EventArgs.Empty)
    End Sub

    Private Sub KeepPortalOperationOpen(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If bgReadPortal.IsBusy OrElse bgReadPortalDuo.IsBusy OrElse bgWritePortal.IsBusy OrElse bgWritePortalDuo.IsBusy Then
            e.Cancel = True
            SaldeStatus.Text = "Wait for the portal operation to finish before returning home."
        End If
    End Sub
End Class
