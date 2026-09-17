Option Strict On
Option Explicit On

Imports System.IO
Imports System.Drawing.Text
Imports System.Runtime.InteropServices

'Assets are optional at build time. Keep the font collection alive for all UI fonts.
Friend NotInheritable Class SkyAssets
    Friend Shared ReadOnly Panel As Color = Color.FromArgb(218, 220, 224)
    Friend Shared ReadOnly Ink As Color = Color.Black
    Friend Shared ReadOnly Dark As Color = Color.FromArgb(158, 160, 164)
    Friend Shared ReadOnly Bright As Color = Color.FromArgb(120, 126, 135)
    Friend Shared ReadOnly White As Color = Color.FromArgb(239, 244, 238)
    Friend Shared ReadOnly Sand As Color = Color.FromArgb(179, 179, 167)
    Friend Shared ReadOnly Glow As Color = Color.FromArgb(239, 244, 238)
    Private Shared ReadOnly privateFonts As New PrivateFontCollection()
    Private Shared ReadOnly family As FontFamily = LoadFamily()
    Private Shared ReadOnly fonts As New Dictionary(Of Single, Font)()
    Friend Shared ReadOnly PortalImage As Image = LoadStill("PortalofPower.png")
    Friend Shared ReadOnly Logo As Image = LoadStill("SkylandersLogo.png")

    'Declares the Windows GDI call used to register the bundled font privately for this process.
    <DllImport("gdi32.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function AddFontResourceEx(filename As String, flags As UInteger, reserved As IntPtr) As Integer
    End Function

    'Keeps this shared helper from being instantiated.
    Private Sub New()
    End Sub

    'Searches the startup folder and its parents for the requested Images/UIDesign asset.
    Friend Shared Function AssetPath(filename As String) As String
        Dim directory As New DirectoryInfo(Application.StartupPath)
        'Also supports running from bin/Debug, bin/Release and framework subfolders.
        For depth As Integer = 0 To 4
            Dim candidate As String = Path.Combine(directory.FullName, "Images", "UIDesign", filename)
            If File.Exists(candidate) Then Return candidate
            directory = directory.Parent
            If directory Is Nothing Then Exit For
        Next
        Return Nothing
    End Function

    'Loads the bundled Markin font for GDI and GDI+ or falls back to the system sans-serif family.
    Private Shared Function LoadFamily() As FontFamily
        Try
            Dim filename As String = AssetPath("markin-lt-regular-regular_ufonts.com.ttf")
            If filename IsNot Nothing Then
                'FR_PRIVATE makes the font available to native WinForms/GDI controls
                'as well as GDI+ without installing it system-wide.
                If AddFontResourceEx(filename, &H10UI, IntPtr.Zero) > 0 Then
                    privateFonts.AddFontFile(filename)
                    If privateFonts.Families.Length > 0 Then Return privateFonts.Families(0)
                End If
            End If
        Catch ex As Exception
            Diagnostics.Debug.WriteLine("UI font: " & ex.Message)
        End Try
        Return FontFamily.GenericSansSerif
    End Function

    'Returns a cached regular font at the requested size plus the shared four-point increase.
    Friend Shared Function UiFont(size As Single) As Font
        size += 4.0F
        SyncLock fonts
            If Not fonts.ContainsKey(size) Then fonts.Add(size, New Font(family, size, FontStyle.Regular, GraphicsUnit.Point))
            Return fonts(size)
        End SyncLock
    End Function

    'Applies the optional balloon icon to non Developer windows and disposes the cloned icon with its owner.
    Friend Shared Sub ApplyWindowIcon(window As Form)
        If TypeOf window Is frmMain Then Return
        Try
            Dim filename As String = AssetPath("Balloon_Icon.ico")
            If filename Is Nothing Then Return
            Dim balloon As Icon
            Using source As New Icon(filename)
                balloon = DirectCast(source.Clone(), Icon)
            End Using
            window.Icon = balloon
            AddHandler window.Disposed, Sub(sender, e) balloon.Dispose()
        Catch ex As Exception
            Diagnostics.Debug.WriteLine("Window icon: " & ex.Message)
        End Try
    End Sub

    'Loads a detached bitmap copy of an optional static UI image.
    Private Shared Function LoadStill(filename As String) As Image
        Try
            Dim resolved As String = AssetPath(filename)
            If resolved IsNot Nothing Then
                Using source As Image = Image.FromFile(resolved)
                    Return New Bitmap(source)
                End Using
            End If
        Catch ex As Exception
            Diagnostics.Debug.WriteLine("UI image: " & ex.Message)
        End Try
        Return Nothing
    End Function
End Class
