Public Enum EnumTipoConnMode
    IP = 1
    DNS = 2
End Enum

Public Enum EnumServerStatus
    Disconnected = 0
    Accepting = 1
    Connecting = 2
    Idle = 4
    Reading = 8
    Writing = 16
    Disconnecting = 32
    Connected = Idle Or Reading Or Writing
End Enum

Public Enum EnumServerEvent
    Cerrado = 0
    Conectado = 1
    Recibido = 2
    Enviado = 3
    TCPError = 4
End Enum

Public Enum EnumEstadoVncServer
    NoInicializado = 0
    EsperandoConexion = 1
    EsperandoProtocolVersion = 2
    Conectado = 3
    Desconectando = 32
    Saliendo = 64
End Enum

Module mdEnumerador

End Module
