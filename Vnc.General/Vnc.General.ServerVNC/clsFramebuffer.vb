Imports System
Imports System.Drawing

Public Class clsFramebuffer

    Private m_sNombre As String

    Private bpp As Integer
    Private depth As Integer
    Private bigEndian As Boolean
    Private trueColor As Boolean
    Private redMax As Integer
    Private greenMax As Integer
    Private blueMax As Integer
    Private redShift As Integer
    Private greenShift As Integer
    Private blueShift As Integer

    Private ReadOnly width As Integer
    Private ReadOnly height As Integer
    Public ReadOnly pixels() As Integer

    Private redFix As Integer = -1
    Private greenFix As Integer = -1
    Private blueFix As Integer = -1

    Private redMask As Integer = -1
    Private greenMask As Integer = -1
    Private blueMask As Integer = -1


    Sub New(width As Integer, height As Integer)
        Me.width = width
        Me.height = height

        Dim pixelCount As Integer = width * height
        ReDim Me.pixels(pixelCount)
    End Sub

    Public Property Pixel(index As Integer) As Integer
        Get
            Return pixels(index)
        End Get
        Set(value As Integer)
            pixels(index) = value
        End Set
    End Property

    Public ReadOnly Property Ancho As Integer
        Get
            Return width
        End Get
    End Property

    Public ReadOnly Property Alto As Integer
        Get
            Return height
        End Get
    End Property

    Public ReadOnly Property Rectangle As Rectangle
        Get
            Return New Rectangle(0, 0, width, height)
        End Get
    End Property

    Public Property BitsPerPixel As Integer
        Get
            Return bpp
        End Get
        Set(value As Integer)
            If value = 32 Or value = 16 Or value = 8 Then
                bpp = value
            Else
                Throw New ArgumentException("Wrong value for BitsPerPixel")
            End If
        End Set
    End Property

    Public Property ColorDepth As Integer
        Get
            Return Me.depth
        End Get
        Set(value As Integer)
            Me.depth = value
        End Set
    End Property

    Public Property UseBigEndian As Boolean
        Get
            Return bigEndian
        End Get
        Set(value As Boolean)
            bigEndian = value
        End Set
    End Property

    Public Property SupportTrueColor As Boolean
        Get
            Return trueColor
        End Get
        Set(value As Boolean)
            trueColor = value
        End Set
    End Property

    Public Property RedMaxValue As Integer
        Get
            Return redMax
        End Get
        Set(value As Integer)
            redMax = value
        End Set
    End Property

    Public Property GreenMaxValue As Integer
        Get
            Return greenMax
        End Get
        Set(value As Integer)
            greenMax = value
        End Set
    End Property

    Public Property BlueMaxValue As Integer
        Get
            Return blueMax
        End Get
        Set(value As Integer)
            blueMax = value
        End Set
    End Property

    Public Property RedShiftValue As Integer
        Get
            Return redShift
        End Get
        Set(value As Integer)
            redShift = value
        End Set
    End Property

    Public Property GreenShiftValue As Integer
        Get
            Return greenShift
        End Get
        Set(value As Integer)
            greenShift = value
        End Set
    End Property

    Public Property BlueShiftValue As Integer
        Get
            Return blueShift
        End Get
        Set(value As Integer)
            blueShift = value
        End Set
    End Property

    Public Property DesktopName As String
        Get
            Return m_sNombre
        End Get
        Set(value As String)
            m_sNombre = value
        End Set
    End Property

    Public Function ToPixelFormat() As Byte()
        Dim bRes(16) As Byte
        Try
            bRes(0) = Convert.ToByte(bpp)
            bRes(1) = Convert.ToByte(depth)
            bRes(2) = Convert.ToByte(IIf(bigEndian, 1, 0))
            bRes(3) = Convert.ToByte(IIf(trueColor, 1, 0))
            bRes(4) = Convert.ToByte((redMax >> 8) And Int32.Parse("FF", System.Globalization.NumberStyles.HexNumber))
            bRes(5) = Convert.ToByte(redMax And Int32.Parse("FF", System.Globalization.NumberStyles.HexNumber))
            bRes(6) = Convert.ToByte((greenMax >> 8) And Int32.Parse("FF", System.Globalization.NumberStyles.HexNumber))
            bRes(7) = Convert.ToByte(greenMax And Int32.Parse("FF", System.Globalization.NumberStyles.HexNumber))
            bRes(8) = Convert.ToByte((blueMax >> 8) And Int32.Parse("FF", System.Globalization.NumberStyles.HexNumber))
            bRes(9) = Convert.ToByte(blueMax And Int32.Parse("FF", System.Globalization.NumberStyles.HexNumber))
            bRes(10) = Convert.ToByte(redShift)
            bRes(11) = Convert.ToByte(greenShift)
            bRes(12) = Convert.ToByte(blueShift)
        Catch ex As Exception

        End Try

        Return bRes
    End Function

    Public Function FromPixelFormat(b() As Byte, width As Integer, height As Integer) As clsFramebuffer
        If b.Length <> 16 Then Throw New ArgumentException("Length of b must be 16 bytes.")

        Dim buffer As New clsFramebuffer(width, height)

        With buffer
            .BitsPerPixel = Convert.ToInt32(b(0))
            .ColorDepth = Convert.ToInt32(b(1))
            .UseBigEndian = (b(2) <> 0)
            .SupportTrueColor = (b(3) <> 0)
            .RedMaxValue = Convert.ToInt32(b(5) Or b(4) << 8)
            .GreenMaxValue = Convert.ToInt32(b(7) Or b(6) << 8)
            .BlueMaxValue = Convert.ToInt32(b(9) Or b(8) << 8)
            .RedShiftValue = Convert.ToInt32(b(10))
            .GreenShiftValue = Convert.ToInt32(b(11))
            .BlueShiftValue = Convert.ToInt32(b(12))
        End With

        Return buffer
    End Function

    Public Function TranslatePixel(pixel As Integer) As Integer
        If redFix <> -1 Then
            Return ((pixel And redMask) >> redFix << redShift) Or
                    ((pixel And greenMask) >> greenFix << greenShift) Or
                    ((pixel And blueMask) >> blueFix << blueShift)
        End If

        Return pixel
    End Function


End Class
