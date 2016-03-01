Imports System.Net.Http
Imports System.Net.Http.Headers
Imports PlanetWars.Shared

Public Class PlanetWarAgent

    Private _playerName As String
    Private _endPointAddr As String
    Private _endPoint As HttpClient


    Public Sub New(ByVal endPointAddr As String, ByVal playerName As String)
        If playerName <> String.Empty Then
            _playerName = playerName
        Else
            _playerName = "Nameless Bot"
        End If

        _endPointAddr = endPointAddr

    End Sub

    Public Sub Initialize()
        _endPoint = New HttpClient()
        _endPoint.BaseAddress = New Uri(_endPointAddr)
        _endPoint.DefaultRequestHeaders.Accept.Clear()
        _endPoint.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
    End Sub

    Public Async Function Start() As Task
        Dim logonResult As LogonResult
        Dim session As GameSession

        If _endPoint Is Nothing Then
            Initialize()
        End If

        logonResult = Await Logon()
        session = New GameSession(logonResult, _endPoint)

        Await session.Play()

    End Function

    Private Async Function Logon() As Task(Of LogonResult)

        Dim logonRequest = New LogonRequest()
        logonRequest.AgentName = _playerName

        Dim logonResponse = Await _endPoint.PostAsJsonAsync("api/logon", logonRequest)
        Dim logonResult = Await logonResponse.Content.ReadAsAsync(Of LogonResult)

        If Not logonResult.Success Then
            Console.WriteLine(String.Format("Unable to connect to endpoint: {0}", logonResult.Message))
            Throw New Exception("Could not connect to endpoint.")
        End If

        Return logonResult

    End Function

End Class
