Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports Vnc.General.Enumerador

Public Class clsTCPServer
    Implements IDisposable

    Private m_iPuerto As Integer
    Private m_bDisposed As Boolean = False

    Private m_sockListen As Socket
    Private m_sockWork As Socket
    Private m_bListenInicializado As Boolean = False
    Private m_bWorkInicializado As Boolean = False
    Private m_bFinalizando As Boolean = False

    Private m_oStream As NetworkStream
    Private m_oWriter As clsEndianBinaryWriter
    Private m_oReader As clsEndianBinaryReader
    Private m_sockLock As New Object
    Public Event EventoTCPServer As EventHandler(Of clsTCPServerEventArgs)

#Region "Rutinas de Inicialización y Propiedades"

    Public Sub New(ByVal iPuerto As Integer)
        Try
            m_iPuerto = iPuerto

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

                If Not IsNothing(m_sockWork) Then
                    m_sockWork.Dispose()
                    m_sockWork = Nothing
                End If

                If Not IsNothing(m_sockListen) Then
                    m_sockListen.Dispose()
                    m_sockListen = Nothing
                End If
            End If

            'aqui se dispose los unmanaged resources
        End If

        m_bDisposed = True
    End Sub

    Public ReadOnly Property Memoria As NetworkStream
        Get
            Return m_oStream
        End Get
    End Property

    Public ReadOnly Property Escritura As clsEndianBinaryWriter
        Get
            Return m_oWriter
        End Get
    End Property

    Public ReadOnly Property Lectura As clsEndianBinaryReader
        Get
            Return m_oReader
        End Get
    End Property

    Public ReadOnly Property SockWork As Socket
        Get
            Return m_sockWork
        End Get
    End Property

#End Region

#Region "Rutinas de Accept"

    Public Function HacerAccept() As Boolean
        Dim bResult As Boolean = False

        Try
            'Log(m_oLogEventos, "clsTCPServer", "HacerAccept", m_sProceso, clsLogEntryEventTypeEnum.Debug, PrepararMensaje("Entrando a HacerAccept"))

            If m_bListenInicializado Then
                'Log(m_oLogEventos, "clsTCPServer", "HacerAccept", m_sProceso, clsLogEntryEventTypeEnum.Debug, PrepararMensaje("Proceso de Accept ya fue inicializado anteriormente"))
                bResult = True
                Exit Try
            End If

            'Log(m_oLogEventos, "clsTCPServer", "HacerAccept", m_sProceso, clsLogEntryEventTypeEnum.Debug, PrepararMensaje("Inicializado"))
            m_bListenInicializado = True

            Dim ipLocalEndPoint As IPEndPoint = New IPEndPoint(IPAddress.Any, Me.m_iPuerto)
            m_sockListen = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            m_sockListen.NoDelay = True
            m_sockListen.Bind(ipLocalEndPoint)
            'Dim lingerOption As New LingerOption(True, 1)
            m_sockListen.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, True)
            m_sockListen.Listen(1000)

            'Espera la siguiente conexión asincronicamente
            m_sockListen.BeginAccept(New AsyncCallback(AddressOf AcceptCallback), m_sockListen)

            bResult = True

        Catch oe As ObjectDisposedException


        Catch es As SocketException
            Finalizar()
        Catch ex As Exception
            Finalizar()
        End Try

        Return bResult
    End Function

    Private Sub AcceptCallback(ByVal arResult As IAsyncResult)
        Dim oEstado As clsTCPServerStateObject
        Dim e As clsTCPServerEventArgs

        Try
            If Not (m_bListenInicializado) Then
                Exit Try
            End If

            Dim sockListener As Socket = CType(arResult.AsyncState, Socket)
            If IsNothing(sockListener) Then
                m_bListenInicializado = False
                Exit Try
            End If

            Dim sockWork As Socket = sockListener.EndAccept(arResult)
            m_sockWork = sockWork
            m_sockWork.NoDelay = True

            oEstado = New clsTCPServerStateObject()
            oEstado.m_WorkSocket = sockWork
            oEstado.m_TCPStatus = EnumServerStatus.Idle
            m_bWorkInicializado = True

            sockListener.Close()
            sockListener = Nothing
            m_bListenInicializado = False

            'Se genera evento de que la conexion esta lista
            e = New clsTCPServerEventArgs(EnumServerEvent.Conectado, Nothing)
            OnEventoTCPServer(e)

            'Deja el socket esperando recibir con el fin de detectar desconexiones
            m_oStream = New NetworkStream(m_sockWork, True)
            m_oWriter = New clsEndianBinaryWriter(m_oStream)
            m_oReader = New clsEndianBinaryReader(m_oStream)
            'HacerRead(oEstado)

        Catch oe As ObjectDisposedException
            Throw

        Catch es As SocketException
            e = New clsTCPServerEventArgs(EnumServerEvent.TCPError, Nothing)
            OnEventoTCPServer(e)
            Finalizar()

        Catch ex As Exception
            e = New clsTCPServerEventArgs(EnumServerEvent.TCPError, Nothing)
            OnEventoTCPServer(e)
            Finalizar()
        End Try
    End Sub

#End Region

#Region "Rutinas de Read"

    Public Function HacerRead(ByVal oEstado As clsTCPServerStateObject) As Boolean
        Dim bResult As Boolean = False

        Try
            'Log(m_oLogEventos, "clsTCPServer", "HacerRead", m_sProceso, clsLogEntryEventTypeEnum.Debug, PrepararMensaje("Entrando a HacerRead"))

            oEstado.m_dtTimeStamp = DateTime.Now
            oEstado.m_TCPStatus = EnumServerStatus.Reading
            ReDim oEstado.m_Buffer(0)
            ReDim oEstado.m_Buffer(oEstado.m_MsgSize)
            oEstado.m_BytesReceived = 0

            m_oReader.BaseStream.BeginRead(oEstado.m_Buffer, 0, oEstado.m_MsgSize, New AsyncCallback(AddressOf ReadCallback), oEstado)
            'm_oStream.BeginRead(oEstado.m_Buffer, 0, oEstado.m_MsgSize, New AsyncCallback(AddressOf ReadCallback), oEstado)
            'm_sockWork.BeginReceive(oEstado.m_Buffer, 0, oEstado.m_MsgSize, SocketFlags.None, New AsyncCallback(AddressOf ReadCallback), oEstado)
            bResult = True

        Catch oe As ObjectDisposedException

        Catch es As SocketException
            Finalizar()

        Catch ex As Exception
            Finalizar()

        End Try

        Return bResult
    End Function

    Public Sub ReadCallback(ByVal ar As IAsyncResult)
        Dim oEstado As clsTCPServerStateObject
        Dim e As clsTCPServerEventArgs

        Try
            oEstado = CType(ar.AsyncState, clsTCPServerStateObject)
            If IsNothing(oEstado) Then
                Exit Try
            End If

            If m_bFinalizando Then
                Exit Try
            End If

            Dim sockWork As Socket = oEstado.m_WorkSocket
            If IsNothing(sockWork) Then
                Exit Try
            End If

            Dim iBytesRead As Integer = m_oReader.BaseStream.EndRead(ar)
            If (iBytesRead = 0) Then
                ' No se recibió nada en el buffer porque se cerró la conexión
                Finalizar()
                Exit Try
            End If

            oEstado.m_BytesReceived = oEstado.m_BytesReceived + iBytesRead
            'Se está esperando recibir el mensaje

            If (oEstado.m_BytesReceived = oEstado.m_MsgSize) Then
                'Ya se recibio el mensaje completo
                Monitor.Enter(oEstado)
                Monitor.Exit(oEstado)
                oEstado.m_dtTimeStamp = DateTime.Now

                e = New clsTCPServerEventArgs(EnumServerEvent.Recibido, oEstado.m_Buffer)
                OnEventoTCPServer(e)

                'HacerRead(oEstado)
            Else
                'No se ha recibido el mensaje completo, continua esperando el resto
                m_oStream.BeginRead(oEstado.m_Buffer, oEstado.m_BytesReceived, oEstado.m_Buffer.Length - oEstado.m_BytesReceived, New AsyncCallback(AddressOf ReadCallback), oEstado)
            End If

        Catch oe As ObjectDisposedException

        Catch es As SocketException
            e = New clsTCPServerEventArgs(EnumServerEvent.TCPError, Nothing)
            OnEventoTCPServer(e)
            Finalizar()

        Catch ex As Exception
            e = New clsTCPServerEventArgs(EnumServerEvent.TCPError, Nothing)
            OnEventoTCPServer(e)
            Finalizar()

        End Try
    End Sub

#End Region

#Region "Rutinas de Send"

    Public Function HacerSend(ByVal bBuffer() As Byte) As Boolean
        Dim bresult As Boolean = False

        Try
            m_oStream.Write(bBuffer, 0, bBuffer.Length)
            m_oStream.Flush()
            'Dim oEstado As clsTCPServerStateObject = New clsTCPServerStateObject()
            'oEstado.m_WorkSocket = m_sockWork
            'oEstado.m_dtTimeStamp = DateTime.Now
            'oEstado.m_TCPStatus = EnumServerStatus.Writing
            'oEstado.m_BytesSent = 0

            'oEstado.m_MsgSize = bBuffer.Length - 1
            'ReDim oEstado.m_Buffer(oEstado.m_MsgSize)

            'm_sockWork.BeginSend(oEstado.m_Buffer, 0, oEstado.m_MsgSize, SocketFlags.None, New AsyncCallback(AddressOf SendCallback), oEstado)
            'bresult = True

        Catch oe As ObjectDisposedException

        Catch es As SocketException
            Finalizar()

        Catch ex As Exception
            Finalizar()

        End Try

        Return bresult
    End Function

    Private Sub SendCallback(ByVal ar As IAsyncResult)
        Dim oEstado As clsTCPServerStateObject
        Dim e As clsTCPServerEventArgs

        Try
            oEstado = CType(ar.AsyncState, clsTCPServerStateObject)
            If IsNothing(oEstado) Then
                Exit Try
            End If

            If m_bFinalizando Then
                Exit Try
            End If

            Dim sockWork As Socket = oEstado.m_WorkSocket
            If IsNothing(sockWork) Then
                Exit Try
            End If

            oEstado.m_TCPStatus = EnumServerStatus.Writing

            Dim iBytesSent As Integer = sockWork.EndSend(ar)
            If (iBytesSent = 0) Then
                ' No se envió nada porque se cerró la conexión
                Finalizar()
                Exit Try
            End If

            oEstado.m_BytesSent = oEstado.m_BytesSent + iBytesSent

            'Estamos enviando el mensaje
            If (oEstado.m_BytesSent = oEstado.m_MsgSize) Then
                oEstado.m_TCPStatus = EnumServerStatus.Idle
                e = New clsTCPServerEventArgs(EnumServerEvent.Enviado, Nothing)
                OnEventoTCPServer(e)
            Else
                sockWork.BeginSend(oEstado.m_Buffer, oEstado.m_BytesSent, oEstado.m_Buffer.Length - oEstado.m_BytesSent, SocketFlags.None, New AsyncCallback(AddressOf SendCallback), oEstado)
            End If

        Catch oe As ObjectDisposedException


        Catch es As SocketException
            e = New clsTCPServerEventArgs(EnumServerEvent.TCPError, Nothing)
            OnEventoTCPServer(e)
            Finalizar()

        Catch ex As Exception
            e = New clsTCPServerEventArgs(EnumServerEvent.TCPError, Nothing)
            OnEventoTCPServer(e)
            Finalizar()

        End Try
    End Sub

#End Region

#Region "Rutinas de Close"

    Public Sub Finalizar()
        'Dim bCerrado As Boolean = False

        Try
            If m_bListenInicializado Then
                m_bListenInicializado = False

                m_bFinalizando = False
                SyncLock m_sockLock
                    m_sockListen.Close()
                    'Thread.Sleep(1000)
                    m_sockListen = Nothing
                    'bCerrado = True
                End SyncLock
            End If

            If m_bWorkInicializado Then
                m_bWorkInicializado = False

                If Me.Conectado Then
                    m_bFinalizando = False
                    SyncLock m_sockLock
                        m_sockWork.Close()
                        'Thread.Sleep(1000)
                        m_sockWork = Nothing
                        'bCerrado = True
                    End SyncLock
                End If
            End If

            'If bCerrado Then
            Dim e As clsTCPServerEventArgs = New clsTCPServerEventArgs(EnumServerEvent.Cerrado, Nothing)
            OnEventoTCPServer(e)
            'End If

        Catch oe As ObjectDisposedException

        Catch es As SocketException


        Catch ex As Exception


        End Try
    End Sub

#End Region

#Region "Rutinas de manejo de eventos"

    Protected Overridable Sub OnEventoTCPServer(ByVal e As clsTCPServerEventArgs)
        RaiseEvent EventoTCPServer(Me, e)
    End Sub

#End Region

#Region "Rutinas Varias"

    ''' <summary>
    ''' Esta función determina si el socket está conectado o no
    ''' </summary>
    ''' <returns>True si está conectado, False si no está conectado</returns>
    ''' <remarks></remarks>
    Public Function Conectado() As Boolean
        Dim sRutina As String = "Conectado"
        Dim sBuffer As String
        Dim bResult As Boolean = False

        Try
            If IsNothing(m_sockWork) Then
                bResult = False
                Exit Try
            End If

            'Busca a ver si el socket tiene marcado que hubo un evento de lectura
            Dim bPolled As Boolean
            SyncLock m_sockLock
                bPolled = m_sockWork.Poll(1, SelectMode.SelectRead)
            End SyncLock

            If IsNothing(m_sockWork) Then
                bResult = False
                Exit Try
            End If

            If Not m_sockWork.Connected Then
                bResult = False
                Exit Try
            End If

            If IsNothing(m_sockWork) Then
                bResult = False
                Exit Try
            End If

            'Busca a ver cuantos bytes están esperando para ser recibidos
            Dim a As Int32
            SyncLock m_sockLock
                a = m_sockWork.Available
            End SyncLock

            'Si el socket tiene marcado que hubo evento de lectura pero tiene 0 bytes esperando, 
            'quiere decir que el socket esta cerrado. Cualquier otro caso, el socket está abierto.
            If bPolled And a = 0 Then
                bResult = False
            Else
                bResult = True
            End If

        Catch oe As ObjectDisposedException

        Catch es As SocketException
            Throw

        Catch ex As Exception
            Throw

        End Try

        Conectado = bResult
    End Function

    Public Function TraducirError(ByVal iError As Integer) As String

        Select Case iError
            Case SocketError.AccessDenied
                TraducirError = "Permiso denegado; se intento accesar socket sin tener los permisos necesarios."

            Case SocketError.AddressAlreadyInUse
                TraducirError = "Dirección está usada; el puerto esta siendo usado por otra aplicación."

            Case SocketError.AddressFamilyNotSupported
                TraducirError = "La dirección no es compatible con el protocolo."

            Case SocketError.AddressNotAvailable
                TraducirError = "Dirección no puede ser asignada; la dirección no es válida."

            Case SocketError.AlreadyInProgress
                TraducirError = "Operación ya se está ejecutando."

            Case SocketError.ConnectionAborted
                TraducirError = "La conección ha sido abortada por el software en la otra computadora, posiblemente por timeout o error de protocolo."

            Case SocketError.ConnectionRefused
                TraducirError = "La conección ha sido denegada por un servicio inactivo en la otra computadora."

            Case SocketError.ConnectionReset
                TraducirError = "La conección ha sido reseteada por la otra computadora."

            Case SocketError.DestinationAddressRequired
                TraducirError = "Se requiere la dirección destino; el valor está en blanco."

            Case SocketError.Disconnecting
                TraducirError = "Desconexión en proceso."

            Case SocketError.Fault
                TraducirError = "Mala dirección; el buffer recibió un puntero invalido o es muy pequeño."

            Case SocketError.HostDown
                TraducirError = "La otra computadora se cayó; la actividad de red con la otra computadora no ha sido iniciada."

            Case SocketError.HostNotFound
                TraducirError = "La otra computadora no se encontró."

            Case SocketError.HostUnreachable
                TraducirError = "No hay una ruta a la otra computadora."

            Case SocketError.InProgress
                TraducirError = "La operación se está ejecutando; solo una transacción se puede ejecutar al mismo tiempo."

            Case SocketError.Interrupted
                TraducirError = "El comando ha sido interrumpido."

            Case SocketError.InvalidArgument
                TraducirError = "Argumento no valido; el socket no ha hecho un paso previo aun."

            Case SocketError.IOPending
                TraducirError = "IO pendiente"

            Case SocketError.IsConnected
                TraducirError = "EL socket ya está conectado."

            Case SocketError.MessageSize
                TraducirError = "El mensaje es mas grande de lo esperado."

            Case SocketError.NetworkDown
                TraducirError = "El subsistema de red ha fallado."

            Case SocketError.NetworkReset
                TraducirError = "La conección fue reseteada por fallo detectado por la actividad keep-alive."

            Case SocketError.NetworkUnreachable
                TraducirError = "La red no puede ser localizada; el software no sabe la ruta a la otra computadora."

            Case SocketError.NoBufferSpaceAvailable
                TraducirError = "No hay suficiente espacio de buffer disponible."

            Case SocketError.NoData
                TraducirError = "No se encuentra el servidor en el DNS"

            Case SocketError.NoRecovery
                TraducirError = "No se puede recuperar del error o no encuentra la base de datos"

            Case SocketError.NotConnected
                TraducirError = "Puerto no esta conectado."

            Case SocketError.NotInitialized
                TraducirError = "El socket no está conectado."

            Case SocketError.NotSocket
                TraducirError = "La operación se ejecuto en un objeto que no es un socket."

            Case SocketError.OperationAborted
                TraducirError = "La operación fue abortada por el objeto."

            Case SocketError.OperationNotSupported
                TraducirError = "La familia del protocolo no es soportado por el sistema."

            Case SocketError.ProcessLimit
                TraducirError = "El límite de procesos corriendo ha sido sobrepasado."

            Case SocketError.ProtocolNotSupported
                TraducirError = "El protocolo no ha sido configurado o no existe."

            Case SocketError.ProtocolOption
                TraducirError = "Se paso una opcion invalida al protocolo."

            Case SocketError.ProtocolType
                TraducirError = "El protocolo es incorrecto para este puerto."

            Case SocketError.Shutdown
                TraducirError = "No se puede seguir enviando porque el comando SHUTDOWN cerró el socket."

            Case SocketError.SocketError
                TraducirError = "Se recibio un error no especificado."

            Case SocketError.SocketNotSupported
                TraducirError = "El tipo de socket no es soportado por el tipo de dirección."

            Case SocketError.Success
                TraducirError = "Operacion realizada correctamente"

            Case SocketError.SystemNotReady
                TraducirError = "El subsistema de red no esta listo."

            Case SocketError.TimedOut
                TraducirError = "La conección dio timeout porque no hubo respuesta después de un período de tiempo."

            Case SocketError.TooManyOpenSockets
                TraducirError = "Hay demasiados puertos abiertos"

            Case SocketError.TryAgain
                TraducirError = "Intente nuevamente."

            Case SocketError.TypeNotFound
                TraducirError = "La clase no se encontró."

            Case SocketError.VersionNotSupported
                TraducirError = "La versión de Windows Sockets no es soportada."

            Case SocketError.WouldBlock
                TraducirError = "La operación causaría un bloqueo del proceso"

            Case Else
                TraducirError = "No se puede determinar el error"
        End Select
    End Function

#End Region

End Class

#Region "clsTCPServerEventArgs"

Public Class clsTCPServerEventArgs
    Inherits EventArgs
    Private m_bBuffer() As Byte
    Private m_eEvento As EnumServerEvent
    Private m_eMsgVncCliente As EnumClientVNCMensajes

    Public Sub New(ByVal eEvento As EnumServerEvent, ByVal bBuffer() As Byte)
        m_eEvento = eEvento
        m_bBuffer = bBuffer
    End Sub

    Public Property MensajeVncCliente As EnumClientVNCMensajes
        Get
            Return m_eMsgVncCliente
        End Get
        Set(value As EnumClientVNCMensajes)
            m_eMsgVncCliente = value
        End Set
    End Property

    Public ReadOnly Property Evento() As EnumServerEvent
        Get
            Return m_eEvento
        End Get
    End Property

    Public ReadOnly Property Buffer As Byte()
        Get
            Return m_bBuffer
        End Get
    End Property
End Class

#End Region
