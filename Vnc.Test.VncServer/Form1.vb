Imports Vnc.General.ServerVNC

Public Class Form1

    Private m_oServerVNC As clsVncServer
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load
        Try
            m_oServerVNC = New clsVncServer("Principal")
            m_oServerVNC.IniciarServerlHandler()
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub
End Class
