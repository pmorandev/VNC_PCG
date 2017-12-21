Imports System.Net.Sockets
Imports System.Threading
Imports Vnc.General.Enumerador

Public Class clsTCPServerStateObject
    Public m_WorkSocket As Socket = Nothing
    Public m_BytesReceived As Integer = 0
    Public m_BytesSent As Integer = 0
    Public m_MsgSize As Integer = -1
    Public m_Buffer() As Byte
    Public m_EventHandler As AutoResetEvent
    Public m_TCPStatus As EnumServerStatus = EnumServerStatus.Disconnected
    Public m_dtTimeStamp As DateTime = DateTime.Now
End Class
