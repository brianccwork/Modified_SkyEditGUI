Option Strict On
Option Explicit On

Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

'The common 1 KiB tag boundary and on-tag primitives. MD5/AES-ECB are the toy
'format, not a choice of encryption for application secrets.
Friend NotInheritable Class FigureData
    Friend Const DumpSize As Integer = 1024
    Friend Shared ReadOnly PayloadBlocks As IReadOnlyList(Of Integer) = Array.AsReadOnly(Enumerable.Range(8, 56).
        Where(Function(block) block Mod 4 <> 3).ToArray())

    Private Sub New()
    End Sub

    Friend Shared Sub ValidateTag(bytes As Byte())
        If bytes Is Nothing OrElse bytes.Length <> DumpSize Then Throw New InvalidDataException("Read a complete 1024-byte figure first.")
        If (bytes(0) Xor bytes(1) Xor bytes(2) Xor bytes(3)) <> bytes(4) OrElse
            Crc(bytes.Take(30).ToArray()) <> Word(bytes, 30) Then
            Throw New InvalidDataException("The figure's serial or header is invalid. Writing is disabled.")
        End If
        For block As Integer = 3 To 63 Step 4
            Dim at As Integer = block * 16 + 6
            If (bytes(at + 1) >> 4) <> ((bytes(at) And 15) Xor 15) OrElse
                (bytes(at + 2) And 15) <> ((bytes(at) >> 4) Xor 15) OrElse
                (bytes(at + 2) >> 4) <> ((bytes(at + 1) And 15) Xor 15) Then
                Throw New InvalidDataException("The figure's access-control data is invalid. Writing is disabled.")
            End If
        Next
    End Sub

    Friend Shared Function Word(bytes As Byte(), at As Integer) As Integer
        Return CInt(bytes(at)) Or (CInt(bytes(at + 1)) << 8)
    End Function

    Friend Shared Function Crc(bytes As Byte()) As UShort
        Dim value As Integer = &HFFFF
        For Each octet As Byte In bytes
            value = value Xor (CInt(octet) << 8)
            For bit As Integer = 0 To 7
                value = If((value And &H8000) <> 0, (value << 1) Xor &H1021, value << 1) And &HFFFF
            Next
        Next
        Return CUShort(value)
    End Function

    Friend Shared Function Crypt(header As Byte(), block As Integer, payload As Byte(), encrypt As Boolean) As Byte()
        Dim material = header.Take(32).Concat(New Byte() {CByte(block)}).
            Concat(Encoding.ASCII.GetBytes(" Copyright (C) 2010 Activision. All Rights Reserved. ")).ToArray()
        Using hash = MD5.Create(), cipher = Aes.Create()
            cipher.Key = hash.ComputeHash(material)
            cipher.Mode = CipherMode.ECB
            cipher.Padding = PaddingMode.None
            Using transform = If(encrypt, cipher.CreateEncryptor(), cipher.CreateDecryptor())
                Return transform.TransformFinalBlock(payload, 0, payload.Length)
            End Using
        End Using
    End Function
End Class
