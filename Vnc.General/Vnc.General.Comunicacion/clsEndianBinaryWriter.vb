Imports System.IO

Public NotInheritable Class clsEndianBinaryWriter
    Inherits BinaryWriter

    Sub New(ByVal input As Stream)
        MyBase.New(input)
    End Sub

    Public Overrides Sub Write(value As UShort)
        FlipAndWrite(BitConverter.GetBytes(value))
    End Sub

    Public Overrides Sub Write(value As Short)
        FlipAndWrite(BitConverter.GetBytes(value))
    End Sub

    Public Overrides Sub Write(value As UInteger)
        FlipAndWrite(BitConverter.GetBytes(value))
    End Sub

    Public Overrides Sub Write(value As Integer)
        FlipAndWrite(BitConverter.GetBytes(value))
    End Sub

    Public Overrides Sub Write(value As ULong)
        FlipAndWrite(BitConverter.GetBytes(value))
    End Sub

    Public Overrides Sub Write(value As Long)
        FlipAndWrite(BitConverter.GetBytes(value))
    End Sub

    Private Sub FlipAndWrite(b() As Byte)
        Array.Reverse(b)
        MyBase.Write(b)
    End Sub

End Class
