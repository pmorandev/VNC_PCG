Imports System.Text
Imports System.Threading
Imports Vnc.General.Comunicacion
Imports Vnc.General.Enumerador

Public Class clsVncServer
    Implements IDisposable

    Private WithEvents m_oTcpServer As clsTCPServer

    Private m_iVersionMajor As Integer = 0
    Private m_iVersionMenor As Integer = 0
    Private m_sRfbVersion As String = "RFB 003.008"
    Private m_sNombreServer As String
    Private m_bCerrando As Boolean = False
    Private m_oTerminalHandler As Thread

    Private m_eEstadoTerminal As EnumEstadoVncServer = EnumEstadoVncServer.NoInicializado
    Private m_eEstadoTCP As EnumServerStatus = EnumServerStatus.Idle
    Private m_Events(1) As AutoResetEvent
    Private m_evConectado As AutoResetEvent = New AutoResetEvent(False)
    Private m_evFinalizado As AutoResetEvent = New AutoResetEvent(False)

    Private m_bDisposed As Boolean = False

    Public Event ServerStatusChanged As EventHandler(Of clsVncServerEventArgs)

#Region "Rutinas de Inicializacion"

    Public Sub New(ByVal sNombreServer As String)
        Try
            If String.IsNullOrEmpty(sNombreServer) Then Throw New ArgumentNullException("sNombreServer", "El nombre del servidor no puede ser nulo o vacio")

            m_sNombreServer = sNombreServer
            m_oTcpServer = New clsTCPServer(5900)
        Catch ex As Exception
            Throw
        End Try
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.m_bDisposed Then
            If disposing Then
                'aqui se dispose los managed resources

                If Not IsNothing(m_oTcpServer) Then
                    m_oTcpServer.Dispose()
                    m_oTcpServer = Nothing
                End If
            End If

            'aqui se dispose los unmanaged resources
        End If

        m_bDisposed = True
    End Sub
#End Region

#Region "Rutinas de Inicializacion Handler RFB"

    Public Sub IniciarServerlHandler()
        Try
            m_bCerrando = False

            m_oTerminalHandler = New Thread(AddressOf ThreadServerHandler)
            m_oTerminalHandler.IsBackground = True
            m_oTerminalHandler.Start()

        Catch ex As Exception
            Throw

        End Try

    End Sub

    Public Sub ThreadServerHandler()
        Dim iIndex As Integer
        Dim bLoop As Boolean = True

        Try
            m_Events(0) = m_evConectado
            m_Events(1) = m_evFinalizado
            'm_Events(2) = m_hLocal

            m_eEstadoTerminal = EnumEstadoVncServer.EsperandoConexion
            CheckTerminalMode()

            m_oTcpServer.HacerAccept()

            'El thread se mantiene en un loop esperando los siguientes eventos: Salir, y Desconectar.
            'Si no hay un evento despues de 5 segundos, procesa un timeout
            While bLoop
                iIndex = WaitHandle.WaitAny(m_Events, 5000, False)
                Select Case iIndex

                    Case WaitHandle.WaitTimeout
                        'Si hay un timeout esperando un evento, hace lo siguiente:

                        '1. Si no está conectado a la estación, inicia el proceso de aceptación de la estación
                        If Not m_bCerrando Then
                            If m_eEstadoTerminal = EnumEstadoVncServer.EsperandoConexion Then
                                m_oTcpServer.HacerAccept()
                            End If
                        End If

                    Case 0
                        m_eEstadoTerminal = EnumEstadoVncServer.EsperandoProtocolVersion
                        CheckTerminalMode()
                        SendVersionProtocoloRFB()
                    Case 1

                        'Si ay una nueva conexion
                        'Case 1
                        '    ''Si hay un evento de Desconectar, hace lo siguiente:
                        '    'm_evLog.Escribir(m_sClase, sRutina, m_sProceso, clsLogEntryEventTypeEnum.Debug, PrepararMensaje("Thread de Terminal recibio evento de desconectar"))

                        '    ''1. Setea la variable de Cerrando
                        '    ''2. Inicia el proceso de desconexión del SrvCom y la Estación
                        '    'm_bCerrando = True
                        '    'm_TCPServer.Finalizar()

                        'Case 2
                        '    ''Si hay un evento de que no hay mas links disponibles, hace lo siguiente:
                        '    'm_evLog.Escribir(m_sClase, sRutina, m_sProceso, clsLogEntryEventTypeEnum.Debug, PrepararMensaje("Thread de Terminal recibio evento que no hay links disponibles"))

                        '    ''1. Inicia el proceso de desconexión del SrvCom y la Estación
                        '    'm_TCPServer.Finalizar()

                End Select
            End While

        Catch ex As Exception
            Throw

        End Try
        'm_hFinalizado.Set()
    End Sub

#End Region

#Region "Rutinas de Handler TCPServer"

    Private Sub m_TCPServer_EventoTCPServer(ByVal sender As Object, ByVal e As clsTCPServerEventArgs) Handles m_oTcpServer.EventoTCPServer
        Try
            Select Case e.Evento
                Case EnumServerEvent.TCPError
                    'm_evLog.WriteEntry(m_sProceso & ": " & PrepararMensaje(e.Mensaje), EventLogEntryType.Information, 0, 1)

                Case EnumServerEvent.Enviado


                Case EnumServerEvent.Recibido
                    Select Case m_eEstadoTerminal
                        Case EnumEstadoVncServer.EsperandoProtocolVersion
                            ReadVersionProtocoloRFB(e.Buffer)
                            SendSecurityProtocoloRFB()
                    End Select

                Case EnumServerEvent.Conectado
                    m_eEstadoTCP = EnumServerStatus.Connected
                    m_evConectado.Set()

                Case EnumServerEvent.Cerrado
                    m_eEstadoTerminal = EnumEstadoVncServer.Desconectando
                    If m_bCerrando Then
                        m_eEstadoTerminal = EnumEstadoVncServer.Desconectando
                        m_evFinalizado.Set()
                    Else
                        m_eEstadoTerminal = EnumEstadoVncServer.EsperandoConexion
                    End If

            End Select

        Catch ex As Exception

        End Try
    End Sub

#End Region

#Region "Rutinas de Protocolo RFB"

    Private Sub SendVersionProtocoloRFB()
        Try
            Dim cSalto As Char = System.Convert.ToChar(System.Convert.ToUInt32("0A", 16))
            Dim sVersion() As Char = {"R", "F", "B", " ", "0", "0", "3", ".", "0", "0", "8", cSalto}
            Dim sRes() As Byte = System.Text.Encoding.ASCII.GetBytes(sVersion)
            m_oTcpServer.Escritura.Write(sRes)
            m_oTcpServer.Escritura.Flush()
        Catch ex As Exception
            Throw
        End Try
    End Sub

    Private Sub SendSecurityProtocoloRFB()
        Try
            Dim b() As Byte
            If m_iVersionMenor = 3 Then
                m_oTcpServer.Escritura.Write(UInt32.Parse("1"))
            Else
                Dim types() As Byte = {1}
                Dim bit As Byte = CByte(CUInt(types.Length))
                m_oTcpServer.Escritura.Write(bit)

                For i As Integer = 0 To types.Length - 1
                    m_oTcpServer.Escritura.Write(BitConverter.GetBytes(1))
                Next

                m_oTcpServer.Escritura.Flush()

                If m_iVersionMenor >= 7 Then m_oTcpServer.Memoria.ReadByte()

                If m_iVersionMenor = 8 Then
                    m_oTcpServer.Escritura.Write(CUInt(0))
                End If
            End If

        Catch ex As Exception
            Throw
        End Try
    End Sub

    Private Sub ReadVersionProtocoloRFB(ByVal bBuffer() As Byte)
        Try
            Dim verMajor As String = ""
            Dim verMinor As String = ""
            Dim b() As Byte = bBuffer
            If Convert.ToInt32(b(0)).Equals(Int32.Parse("52", System.Globalization.NumberStyles.HexNumber)) And
               Convert.ToInt32(b(1)).Equals(Int32.Parse("46", System.Globalization.NumberStyles.HexNumber)) And
               Convert.ToInt32(b(2)).Equals(Int32.Parse("42", System.Globalization.NumberStyles.HexNumber)) And
               Convert.ToInt32(b(3)).Equals(Int32.Parse("20", System.Globalization.NumberStyles.HexNumber)) And
               Convert.ToInt32(b(4)).Equals(Int32.Parse("30", System.Globalization.NumberStyles.HexNumber)) And
               Convert.ToInt32(b(5)).Equals(Int32.Parse("30", System.Globalization.NumberStyles.HexNumber)) And
               Convert.ToInt32(b(6)).Equals(Int32.Parse("33", System.Globalization.NumberStyles.HexNumber)) And
               Convert.ToInt32(b(7)).Equals(Int32.Parse("2E", System.Globalization.NumberStyles.HexNumber)) And
               (Convert.ToInt32(b(8)).Equals(Int32.Parse("30", System.Globalization.NumberStyles.HexNumber)) Or
                Convert.ToInt32(b(8)).Equals(Int32.Parse("38", System.Globalization.NumberStyles.HexNumber))) And
               (Convert.ToInt32(b(9)).Equals(Int32.Parse("30", System.Globalization.NumberStyles.HexNumber)) Or
                Convert.ToInt32(b(9)).Equals(Int32.Parse("38", System.Globalization.NumberStyles.HexNumber))) And
               (Convert.ToInt32(b(10)).Equals(Int32.Parse("33", System.Globalization.NumberStyles.HexNumber)) Or
                Convert.ToInt32(b(10)).Equals(Int32.Parse("36", System.Globalization.NumberStyles.HexNumber)) Or
                Convert.ToInt32(b(10)).Equals(Int32.Parse("37", System.Globalization.NumberStyles.HexNumber)) Or
                Convert.ToInt32(b(10)).Equals(Int32.Parse("38", System.Globalization.NumberStyles.HexNumber)) Or
                Convert.ToInt32(b(10)).Equals(Int32.Parse("10", System.Globalization.NumberStyles.HexNumber)) And
               Convert.ToInt32(b(11)).Equals(Int32.Parse("0A", System.Globalization.NumberStyles.HexNumber))) Then

                m_iVersionMajor = 3

                Select Case Convert.ToInt32(b(10))
                    Case Int32.Parse("33", System.Globalization.NumberStyles.HexNumber)
                    Case Int32.Parse("36", System.Globalization.NumberStyles.HexNumber)
                        m_iVersionMenor = 3
                    Case Int32.Parse("37", System.Globalization.NumberStyles.HexNumber)
                        m_iVersionMenor = 7
                    Case Int32.Parse("38", System.Globalization.NumberStyles.HexNumber)
                        m_iVersionMenor = 8
                    Case Int32.Parse("39", System.Globalization.NumberStyles.HexNumber)
                        m_iVersionMenor = 8
                End Select

            Else
                Throw New NotSupportedException("Only versions 3.3, 3.7, and 3.8 of the RFB Protocol are supported.")
            End If

        Catch ex As Exception
            m_evFinalizado.Set()
        End Try
    End Sub


#End Region

#Region "Rutinas Varias"

    Private Sub CheckTerminalMode()
        Try
            Dim oServerEventArgs As clsVncServerEventArgs = New clsVncServerEventArgs(m_eEstadoTerminal.ToString, m_eEstadoTerminal)
            RaiseEvent ServerStatusChanged(Me, oServerEventArgs)

        Catch ex As Exception


        End Try
    End Sub

#End Region

End Class

#Region "clsVncServerEventArgs"

Public Class clsVncServerEventArgs
    Inherits EventArgs

    Private m_eModo As EnumEstadoVncServer
    Private m_sDescripcion As String

    Sub New(ByVal sDescripcion As String, ByVal eModo As EnumEstadoVncServer)
        m_sDescripcion = sDescripcion
        m_eModo = eModo
    End Sub

    Public ReadOnly Property Modo As EnumEstadoVncServer
        Get
            Return m_eModo
        End Get
    End Property

    Public ReadOnly Property Descripcion() As String
        Get
            Return m_sDescripcion
        End Get
    End Property

End Class

#End Region
