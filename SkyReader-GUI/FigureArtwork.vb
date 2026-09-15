Option Strict On
Option Explicit On

Imports System.IO
Imports System.Linq
Imports System.Text.RegularExpressions

Friend NotInheritable Class FigureArtwork
    Private ReadOnly root As String
    Private ReadOnly paths As New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly editionPaths As New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)

    Friend Sub New()
        Dim directory As DirectoryInfo = New DirectoryInfo(Application.StartupPath)
        For depth As Integer = 0 To 4
            If directory Is Nothing Then Exit For
            Dim candidate As String = Path.Combine(directory.FullName, "Images")
            If IO.Directory.Exists(candidate) Then
                root = candidate
                Exit For
            End If
            directory = directory.Parent
        Next
        If root Is Nothing Then Return
        Try
            For Each file As String In IO.Directory.EnumerateFiles(root, "*.png", SearchOption.AllDirectories)
                Dim relative As String = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                Dim game As String = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)(0)
                Dim key As String = game & "/" & Normalize(Path.GetFileNameWithoutExtension(file))
                If Not paths.ContainsKey(key) Then paths(key) = New List(Of String)()
                paths(key).Add(file)
                Dim editionKey As String = game & "/" & WithoutEdition(Normalize(Path.GetFileNameWithoutExtension(file)))
                If Not editionPaths.ContainsKey(editionKey) Then editionPaths(editionKey) = New List(Of String)()
                editionPaths(editionKey).Add(file)
            Next
        Catch ex As IOException
            DeviceManagement.DebugWrite("Artwork index: " & ex.Message)
        Catch ex As UnauthorizedAccessException
            DeviceManagement.DebugWrite("Artwork index: " & ex.Message)
        End Try
    End Sub

    Friend Shared Function Normalize(name As String) As String
        Dim value As String = name.ToLowerInvariant()
        value = Regex.Replace(value.Normalize(System.Text.NormalizationForm.FormD), "\p{Mn}", "")
        value = Regex.Replace(value, "^(sobersu|fruitsnack)'s\s+", "")
        value = Regex.Replace(value, "_logo(?:_v\d+)?$", "")
        value = Regex.Replace(value, "_(giant|mini)$", "")
        value = Regex.Replace(value, "_lc$", " lightcore")
        value = Regex.Replace(value, "_series(\d+)$", " series $1")
        value = value.Replace("(employee edition)", "").Replace("splitfire", "spitfire")
        value = value.Replace("crystal clear starcast", "clear starcast")
        value = value.Replace("flarewolf", "flare wolf")
        value = Regex.Replace(value, "\((top|bottom)\)", "")
        value = Regex.Replace(value, "[^a-z0-9]+", " ").Trim()
        'Hyphens and apostrophes in display names and filenames vary.
        value = value.Replace("jet vac", "jetvac").Replace("dive clops", "diveclops")
        value = value.Replace("eye brawl", "eyebrawl").Replace("ro bow", "robow")
        Return String.Join(" ", value.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries).OrderBy(Function(word) word, StringComparer.Ordinal))
    End Function

    Private Shared Function WithoutEdition(name As String) As String
        'Some uniquely named upgrades include an edition suffix only in the PNG.
        Return String.Join(" ", name.Split(" "c).Where(Function(word) word <> "series" AndAlso word <> "lightcore" AndAlso Not Regex.IsMatch(word, "^\d+$")))
    End Function

    Friend Function Load(gameName As String, figureName As String) As Image
        Dim folder As String = ""
        Select Case gameName
            Case "Spyro's Adventure" : folder = "SSA"
            Case "Giants" : folder = "SG"
            Case "Swap Force" : folder = "SSF"
            Case "Trap Team", "Traps" : folder = "STT"
            Case "SuperChargers", "Vehicles", "Vehicle" : folder = "SSC"
            Case "Imaginators", "Imaginators Crystals" : folder = "SI"
        End Select
        Dim matches As List(Of String) = Nothing
        If paths.TryGetValue(folder & "/" & Normalize(figureName), matches) AndAlso matches.Count = 1 Then
            Dim found As Image = TryLoad(matches(0))
            If found IsNot Nothing Then Return found
        End If
        Dim normalized As String = Normalize(figureName)
        If Not Regex.IsMatch(normalized, "\b(series|lightcore)\b") AndAlso
           editionPaths.TryGetValue(folder & "/" & normalized, matches) AndAlso matches.Count = 1 Then
            Dim found As Image = TryLoad(matches(0))
            If found IsNot Nothing Then Return found
        End If
        If root IsNot Nothing Then
            For Each fallback As String In New String() {
                Path.Combine(root, "ERROR", "Error_Figure.png"),
                Path.Combine(root, "ERROR) Error_Figure.png")}
                Dim found As Image = TryLoad(fallback)
                If found IsNot Nothing Then Return found
            Next
        End If
        'The PNGs are external assets; a missing/corrupt fallback must not crash UI.
        Return Nothing
    End Function

    Private Shared Function TryLoad(path As String) As Image
        Try
            If Not File.Exists(path) Then Return Nothing
            Using stream As FileStream = File.OpenRead(path), source As Image = Image.FromStream(stream)
                Return New Bitmap(source)
            End Using
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is ArgumentException OrElse
                                   TypeOf ex Is OutOfMemoryException OrElse TypeOf ex Is UnauthorizedAccessException
            Return Nothing
        End Try
    End Function
End Class
