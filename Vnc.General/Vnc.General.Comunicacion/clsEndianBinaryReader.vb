Imports System.IO
Imports System.Text

Public NotInheritable Class clsEndianBinaryReader
    Inherits BinaryReader

    Private buff(4) As Byte

    Sub New(ByVal input As Stream)
        MyBase.New(input)
    End Sub

    Sub New(ByVal input As Stream, encoding As Encoding)
        MyBase.New(input, encoding)
    End Sub

    Private Sub FillBuff(totalBytes As Integer)
        Dim bytesRead As Integer = 0
        Dim n As Integer = 0

        Do
            n = BaseStream.Read(buff, bytesRead, totalBytes - bytesRead)
            If n = 0 Then Throw New IOException("Unable to read next byte(s).")

            bytesRead += n

        Loop While (bytesRead < totalBytes)

    End Sub

    Public Overrides Function ReadUInt16() As UShort
        FillBuff(2)
        Dim Res As UShort = (CUInt(buff(1)) Or (CUInt(buff(0)) << 8))
        Return Res
    End Function

    Public Overrides Function ReadInt16() As Short
        FillBuff(2)
        Dim Res As Short = ((buff(1) And Int32.Parse("FF", System.Globalization.NumberStyles.HexNumber)) Or (buff(0) << 8))
        Return Res
    End Function

    Public Overrides Function ReadUInt32() As UInt32
        FillBuff(4)
        Dim Res As UInt32 = ((CUInt(buff(3)) And Int32.Parse("FF", System.Globalization.NumberStyles.HexNumber)) Or (CUInt(buff(2)) << 8) Or (CUInt(buff(1)) << 16) Or (CUInt(buff(0)) << 24))
        Return Res
    End Function

    Public Overrides Function ReadInt32() As Int32
        FillBuff(4)
        Dim Res As Int32 = (buff(3) Or (buff(2) << 8) Or (buff(1) << 16) Or (buff(0) << 24))
        Return Res
    End Function

    Public Sub HacerRead()
        Try

        Catch ex As Exception

        End Try
    End Sub

End Class
