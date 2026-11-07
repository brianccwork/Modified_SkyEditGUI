Option Strict On
Option Explicit On

Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text

'Effects are decoded to PCM WAV ahead of time and loaded once before the home page opens.
'Click playback uses pinned memory: no disk search, MP3 decoder, MCI open/close, or timer.
Friend NotInheritable Class SkySounds
    'Keep the existing public names so all portal/editor result sounds remain compatible.
    Friend Const Back As String = "Back.mp3"
    Friend Const ConnectPortal As String = "Connect-Portal.mp3"
    Friend Const ConnectPortalFail As String = "Connect-Portal-Fail.mp3"
    Friend Const HelpPortal As String = "Help-Portal.mp3"
    Friend Const ProblemNoticeRead As String = "Problem-Notice-Read.mp3"
    Friend Const SaveChanges As String = "Save-Changes.mp3"
    Friend Const SuccessfulRead As String = "Succesful-Read.mp3"

    Friend Enum VolumeLevel
        Full = 0
        Medium = 1
        Low = 2
        Silent = 3
    End Enum

    Private Shared volume As VolumeLevel = VolumeLevel.Medium
    Private Shared ReadOnly random As New Random()
    Private Shared ReadOnly gate As New Object()
    Private Shared ReadOnly sounds As New Dictionary(Of String, CachedSound)(StringComparer.OrdinalIgnoreCase)
    Private Shared ReadOnly attached As New ConditionalWeakTable(Of Button, PressState)()
    Private Shared initialized As Boolean
    Private Shared exiting As Boolean
    Private Const PlaybackFlags As UInteger = &H7UI 'SND_ASYNC Or SND_NODEFAULT Or SND_MEMORY

    <DllImport("winmm.dll", EntryPoint:="PlaySoundW", ExactSpelling:=True)>
    Private Shared Function PlayMemory(sound As IntPtr, moduleHandle As IntPtr, flags As UInteger) As Boolean
    End Function

    Shared Sub New()
        AddHandler Application.ApplicationExit, AddressOf ApplicationExiting
    End Sub

    Private Sub New()
    End Sub

    Friend Shared ReadOnly Property CurrentVolume As VolumeLevel
        Get
            Return volume
        End Get
    End Property

    'Every launch starts at Medium. This changes only SkyGUI effects, never system volume.
    Friend Shared Sub CycleVolume()
        SyncLock gate
            volume = CType((CInt(volume) + 1) Mod 4, VolumeLevel)
            StopSound()
        End SyncLock
    End Sub

    'Called during home construction so even the first button press is already cached.
    Friend Shared Sub Initialize()
        SyncLock gate
            If initialized OrElse exiting Then Return
            initialized = True
            Dim names As String() = {Back, ConnectPortal, ConnectPortalFail, HelpPortal,
                ProblemNoticeRead, SaveChanges, SuccessfulRead, "Random-Button-Press-1.mp3",
                "Random-Button-Press-2.mp3", "Random-Button-Press-3.mp3", "Random-Button-Press-4.mp3"}
            For Each filename As String In names
                Try
                    Dim resolved As String = FindSound(Path.ChangeExtension(filename, ".wav"))
                    If resolved IsNot Nothing Then sounds.Add(filename, New CachedSound(File.ReadAllBytes(resolved)))
                Catch ex As Exception
                    'A missing or damaged effect must never interrupt portal or editor work.
                    Diagnostics.Debug.WriteLine("SFX preload " & filename & ": " & ex.Message)
                End Try
            Next
        End SyncLock
    End Sub

    'Search only at startup; supports bin/Debug, bin/Release, and parent SFX folders.
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

    'One asynchronous channel: operation results replace earlier ordinary button effects.
    Friend Shared Sub Play(filename As String)
        Initialize()
        SyncLock gate
            If exiting OrElse volume = VolumeLevel.Silent Then Return
            Dim sound As CachedSound = Nothing
            If Not sounds.TryGetValue(filename, sound) Then Return
            Try
                If Not PlayMemory(sound.Pointer(volume), IntPtr.Zero, PlaybackFlags) Then
                    Diagnostics.Debug.WriteLine("SFX playback unavailable: " & filename)
                End If
            Catch ex As Exception
                Diagnostics.Debug.WriteLine("SFX: " & ex.Message)
            End Try
        End SyncLock
    End Sub

    Friend Shared Sub PlayRandomButton()
        Play("Random-Button-Press-" & random.Next(1, 5).ToString() & ".mp3")
    End Sub

    'Play on mouse-down, before navigation or expensive Click handlers run.
    'Click remains the fallback for keyboard, accessibility, and PerformClick activation.
    Friend Shared Sub AttachButton(button As Button)
        Dim state As PressState = Nothing
        If attached.TryGetValue(button, state) Then Return
        attached.Add(button, New PressState())
        AddHandler button.MouseDown, AddressOf ButtonPressed
        AddHandler button.MouseUp, AddressOf ButtonReleased
        AddHandler button.Click, AddressOf ButtonClicked
    End Sub

    Private NotInheritable Class PressState
        Friend PlayedOnPress As Boolean
    End Class

    Private Shared Sub ButtonPressed(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Return
        Dim button As Button = DirectCast(sender, Button)
        attached.GetOrCreateValue(button).PlayedOnPress = True
        PlayClick(button.Text)
    End Sub

    Private Shared Sub ButtonReleased(sender As Object, e As MouseEventArgs)
        attached.GetOrCreateValue(DirectCast(sender, Button)).PlayedOnPress = False
    End Sub

    Private Shared Sub ButtonClicked(sender As Object, e As EventArgs)
        Dim button As Button = DirectCast(sender, Button)
        Dim state As PressState = attached.GetOrCreateValue(button)
        If state.PlayedOnPress Then
            state.PlayedOnPress = False
            Return
        End If
        PlayClick(button.Text)
    End Sub

    Private Shared Sub PlayClick(text As String)
        Dim caption As String = text.Replace("&", "").Trim().ToLowerInvariant()
        Select Case caption
            Case "back", "go back", "back to home", "back to main menu", "close"
                Play(Back)
            Case "help portal"
                Play(HelpPortal)
            Case Else
                'Keep result sounds tied to completion, rather than implying success on press.
                If caption.StartsWith("connect", StringComparison.Ordinal) OrElse
                   caption.StartsWith("read", StringComparison.Ordinal) OrElse
                   caption.StartsWith("save", StringComparison.Ordinal) OrElse
                   caption.StartsWith("write", StringComparison.Ordinal) Then Return
                PlayRandomButton()
        End Select
    End Sub

    Private Shared Sub StopSound()
        Try
            PlayMemory(IntPtr.Zero, IntPtr.Zero, 0UI)
        Catch ex As Exception
            Diagnostics.Debug.WriteLine("SFX stop: " & ex.Message)
        End Try
    End Sub

    Private Shared Sub ApplicationExiting(sender As Object, e As EventArgs)
        SyncLock gate
            exiting = True
            'Stop native playback before releasing buffers used by SND_ASYNC.
            StopSound()
            For Each sound As CachedSound In sounds.Values
                sound.Dispose()
            Next
            sounds.Clear()
        End SyncLock
    End Sub

    'Three immutable, pinned WAV buffers per effect. Volume scaling happens once, at startup.
    Private NotInheritable Class CachedSound
        Implements IDisposable

        Private ReadOnly soundHandles(2) As GCHandle

        Friend Sub New(wave As Byte())
            Dim dataOffset As Integer = 0
            Dim dataLength As Integer = 0
            Using reader As New BinaryReader(New MemoryStream(wave, False), Encoding.ASCII)
                If Encoding.ASCII.GetString(reader.ReadBytes(4)) <> "RIFF" Then Throw New InvalidDataException("Expected RIFF WAV.")
                reader.ReadUInt32()
                If Encoding.ASCII.GetString(reader.ReadBytes(4)) <> "WAVE" Then Throw New InvalidDataException("Expected WAVE.")
                Dim pcm16 As Boolean = False
                While reader.BaseStream.Position + 8 <= wave.Length
                    Dim chunk As String = Encoding.ASCII.GetString(reader.ReadBytes(4))
                    Dim length As UInteger = reader.ReadUInt32()
                    Dim start As Long = reader.BaseStream.Position
                    If start + CLng(length) > wave.Length Then Throw New InvalidDataException("Truncated WAV.")
                    If chunk = "fmt " Then
                        If length < 16UI Then Throw New InvalidDataException("Invalid WAV format.")
                        Dim format As UShort = reader.ReadUInt16()
                        reader.ReadBytes(12)
                        pcm16 = format = 1US AndAlso reader.ReadUInt16() = 16US
                    ElseIf chunk = "data" Then
                        dataOffset = CInt(start)
                        dataLength = CInt(length)
                    End If
                    reader.BaseStream.Position = start + CLng(length) + CLng(length And 1UI)
                End While
                If Not pcm16 OrElse dataLength = 0 OrElse dataLength Mod 2 <> 0 Then
                    Throw New InvalidDataException("Effects require 16-bit PCM WAV.")
                End If
            End Using
            Try
                Dim gains As Double() = {1.0, 0.65, 0.3}
                For level As Integer = 0 To 2
                    Dim buffer As Byte() = DirectCast(wave.Clone(), Byte())
                    If level > 0 Then
                        For offset As Integer = dataOffset To dataOffset + dataLength - 2 Step 2
                            Dim sample As Short = BitConverter.ToInt16(buffer, offset)
                            Dim scaled As Short = CShort(Math.Round(sample * gains(level)))
                            Dim bits As Integer = CInt(scaled) And &HFFFF
                            buffer(offset) = CByte(bits And &HFF)
                            buffer(offset + 1) = CByte(bits >> 8)
                        Next
                    End If
                    soundHandles(level) = GCHandle.Alloc(buffer, GCHandleType.Pinned)
                Next
            Catch
                Dispose()
                Throw
            End Try
        End Sub

        Friend Function Pointer(level As VolumeLevel) As IntPtr
            Return soundHandles(CInt(level)).AddrOfPinnedObject()
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            For index As Integer = 0 To soundHandles.Length - 1
                If soundHandles(index).IsAllocated Then soundHandles(index).Free()
            Next
        End Sub
    End Class
End Class
