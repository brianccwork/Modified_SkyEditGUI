Option Explicit On
Option Strict On

'These partial classes add presentation before each original Load handler runs.
'No existing form code or designer-generated control declarations are changed.
'The guard also prevents repeated scaling if a window is hidden and shown again.

Partial Public Class frmMain
    Private skyThemeApplied As Boolean

    'Runs the original load handler and applies the shared theme once for this window.
    Protected Overrides Sub OnLoad(e As EventArgs)
        If skyThemeApplied Then Return
        If Not skyThemeApplied Then
            DoubleBuffered = True
            SkyTheme.Apply(Me)
            skyThemeApplied = True
        End If
        MyBase.OnLoad(e)
    End Sub
End Class

Partial Public Class frmTraps
    Private skyThemeApplied As Boolean

    'Runs the original load handler and applies the shared theme once for this window.
    Protected Overrides Sub OnLoad(e As EventArgs)
        If Not skyThemeApplied Then
            DoubleBuffered = True
            SkyTheme.Apply(Me)
            skyThemeApplied = True
        End If
        MyBase.OnLoad(e)
    End Sub
End Class

Partial Public Class frmVehicles
    Private skyThemeApplied As Boolean

    'Runs the original load handler and applies the shared theme once for this window.
    Protected Overrides Sub OnLoad(e As EventArgs)
        If Not skyThemeApplied Then
            DoubleBuffered = True
            SkyTheme.Apply(Me)
            skyThemeApplied = True
        End If
        MyBase.OnLoad(e)
    End Sub
End Class

Partial Public Class frmCrystals
    Private skyThemeApplied As Boolean

    'Runs the original load handler and applies the shared theme once for this window.
    Protected Overrides Sub OnLoad(e As EventArgs)
        If Not skyThemeApplied Then
            DoubleBuffered = True
            SkyTheme.Apply(Me)
            skyThemeApplied = True
        End If
        MyBase.OnLoad(e)
    End Sub
End Class

Partial Public Class frmLog
    Private skyThemeApplied As Boolean

    'Runs the original load handler and applies the shared theme once for this window.
    Protected Overrides Sub OnLoad(e As EventArgs)
        If Not skyThemeApplied Then
            DoubleBuffered = True
            SkyTheme.Apply(Me)
            skyThemeApplied = True
        End If
        MyBase.OnLoad(e)
    End Sub
End Class

Partial Public Class frmArea
    Private skyThemeApplied As Boolean

    'Runs the original load handler and applies the shared theme once for this window.
    Protected Overrides Sub OnLoad(e As EventArgs)
        If Not skyThemeApplied Then
            DoubleBuffered = True
            SkyTheme.Apply(Me)
            skyThemeApplied = True
        End If
        MyBase.OnLoad(e)
    End Sub
End Class
