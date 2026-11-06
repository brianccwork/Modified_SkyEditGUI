Option Strict On
Option Explicit On

Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text

'Optional MP3 effects. MCI provides asynchronous playback without an extra audio dependency.
Friend NotInheritable Class SkySounds
    Friend Const Back As String = "Back.mp3"
    Friend Const ConnectPortal As String = "Connect-Portal.mp3"
    Friend Const ConnectPortalFail As String = "Connect-Portal-Fail.mp3"
    Friend Const HelpPortal As String = "Help-Portal.mp3"
    Friend Const ProblemNoticeRead As String = "Problem-Notice-Read.mp3"
    Friend Const SaveChanges As String = "Save-Changes.mp3"
    Friend Const SuccessfulRead As String = "Succesful-Read.mp3"

    Private Const Channel As String = "SkyGuiEffect"
    Private Shared ReadOnly random As New Random()
    Private Shared ReadOnly attached As New ConditionalWeakTable(Of Button, Object)()
    Private Shared ReadOnly attachedMenuItems As New ConditionalWeakTable(Of ToolStripItem, Object)()
    Private Shared ReadOnly cleanupTimer As New System.Windows.Forms.Timer With {.Interval = 200}
    Private Shared opened As Boolean

    <DllImport("winmm.dll", EntryPoint:="mciSendStringW", CharSet:=CharSet.Unicode, ExactSpelling:=True)>
    Private Shared Function SendCommand(command As String, result As StringBuilder,
                                       resultLength As Integer, callback As IntPtr) As UInteger
    End Function

    Shared Sub New()
        AddHandler cleanupTimer.Tick, AddressOf ReleaseFinishedSound
        AddHandler Application.ApplicationExit, AddressOf ApplicationExiting
    End Sub

    Private Sub New()
    End Sub

    'Search beside the executable and project, including bin/Debug and a sibling SFX folder.
    Private Shared Function FindSound(filename As String) As String
        Dim directory As New DirectoryInfo(Application.StartupPath)
        For depth As Integer = 0 To 4
            Dim candidate As String = Path.Combine(directory.FullName, "SFX", filename)
            If File.Exists(candidate) Then Return candidate
            directory = directory.Parent
            If directory Is Nothing Then Exit For
        Next
        Return Nothing
    End Function

    'Use one channel so a result or notice replaces any earlier button effect.
    'No wait flag is used: playback never waits for the clip to finish on the UI thread.
    Friend Shared Sub Play(filename As String)
        Try
            Dim resolved As String = FindSound(filename)
            If resolved Is Nothing Then Return
            CloseSound()
            Dim quote As String = ChrW(34)
            Dim errorCode As UInteger = SendCommand("open " & quote & resolved & quote &
                " type mpegvideo alias " & Channel, Nothing, 0, IntPtr.Zero)
            If errorCode <> 0 Then
                Diagnostics.Debug.WriteLine("SFX open failed: " & filename & " (" & errorCode.ToString() & ")")
                Return
            End If
            opened = True
            If SendCommand("play " & Channel & " from 0", Nothing, 0, IntPtr.Zero) <> 0 Then
                CloseSound()
                Return
            End If
            cleanupTimer.Start()
        Catch ex As Exception
            'Missing files, codecs, or audio devices must never interrupt portal or editor work.
            Diagnostics.Debug.WriteLine("SFX: " & ex.Message)
            CloseSound()
        End Try
    End Sub

    Friend Shared Sub PlayRandomButton()
        Play("Random-Button-Press-" & random.Next(1, 5).ToString() & ".mp3")
    End Sub

    'Keep this separate from visual styling so gallery headers retain their game colors.
    Friend Shared Sub AttachButton(button As Button)
        Dim marker As Object = Nothing
        If attached.TryGetValue(button, marker) Then Return
        attached.Add(button, New Object())
        AddHandler button.Click, AddressOf ButtonClicked
    End Sub

    Private Shared Sub ButtonClicked(sender As Object, e As EventArgs)
        Dim button As Button = DirectCast(sender, Button)
        PlayClick(button.Text, button.Name)
    End Sub

    Friend Shared Sub AttachMenuItem(item As ToolStripItem)
        If TypeOf item Is ToolStripSeparator Then Return
        Dim dropdown As ToolStripDropDownItem = TryCast(item, ToolStripDropDownItem)
        If dropdown IsNot Nothing AndAlso dropdown.HasDropDownItems Then Return
        Dim marker As Object = Nothing
        If attachedMenuItems.TryGetValue(item, marker) Then Return
        attachedMenuItems.Add(item, New Object())
        AddHandler item.Click, AddressOf MenuItemClicked
    End Sub

    Private Shared Sub MenuItemClicked(sender As Object, e As EventArgs)
        Dim item As ToolStripItem = DirectCast(sender, ToolStripItem)
        PlayClick(item.Text, item.Name)
    End Sub

    Private Shared Sub PlayClick(text As String, name As String)
        Dim caption As String = text.Replace("&", "").Trim().ToLowerInvariant()
        Select Case caption
            Case "back", "go back", "back to home", "back to main menu", "close"
                Play(Back)
            Case "help portal"
                Play(HelpPortal)
            Case Else
                'Operation results own their sounds; a late Click handler must not overwrite them.
                If caption.StartsWith("connect", StringComparison.Ordinal) OrElse
                   caption.StartsWith("read", StringComparison.Ordinal) OrElse
                   caption.StartsWith("save", StringComparison.Ordinal) OrElse
                   caption.StartsWith("write", StringComparison.Ordinal) OrElse
                   name.Equals("btnRaw", StringComparison.OrdinalIgnoreCase) OrElse
                   name.Equals("OpenToolStripMenuItem", StringComparison.OrdinalIgnoreCase) Then Return
                PlayRandomButton()
        End Select
    End Sub

    Private Shared Sub ReleaseFinishedSound(sender As Object, e As EventArgs)
        If Not opened Then
            cleanupTimer.Stop()
            Return
        End If
        Try
            Dim mode As New StringBuilder(32)
            If SendCommand("status " & Channel & " mode", mode, mode.Capacity, IntPtr.Zero) <> 0 OrElse
               Not mode.ToString().Trim().Equals("playing", StringComparison.OrdinalIgnoreCase) Then CloseSound()
        Catch ex As Exception
            Diagnostics.Debug.WriteLine("SFX cleanup: " & ex.Message)
            CloseSound()
        End Try
    End Sub

    Private Shared Sub CloseSound()
        cleanupTimer.Stop()
        If Not opened Then Return
        Try
            SendCommand("close " & Channel, Nothing, 0, IntPtr.Zero)
        Catch ex As Exception
            Diagnostics.Debug.WriteLine("SFX close: " & ex.Message)
        Finally
            opened = False
        End Try
    End Sub

    Private Shared Sub ApplicationExiting(sender As Object, e As EventArgs)
        CloseSound()
        cleanupTimer.Dispose()
    End Sub
End Class
