Option Explicit On
Option Strict On
Imports System.Net.Http
Imports System.Net.Formatting
Imports PlanetWars.Shared

Public Class GameSession
    Private _authToken As String
    Private _gameId As Integer
    Private _myId As Integer
    Private _timeToNextTurn As Double
    Private _endpoint As HttpClient

    Private _isActive As Boolean

    Public Sub New(ByVal gameStart As LogonResult, ByVal endpoint As HttpClient)
        _authToken = gameStart.AuthToken
        _gameId = gameStart.GameId
        _myId = gameStart.Id
        _timeToNextTurn = gameStart.GameStart.Subtract(DateTime.UtcNow).TotalMilliseconds
        _endpoint = endpoint

        Console.WriteLine("Time to next turn, game start: {0}", _timeToNextTurn.ToString)
    End Sub

    Public Async Function Play() As Task
        Dim status As StatusResult

        Dim agentBrain As New BrainA()

        Dim moveList As List(Of MoveRequest)

        _isActive = True

        While _isActive
            status = Await GetGameStatus()

            Console.WriteLine("*******************************")

            If status.IsGameOver Then
                _isActive = False
                Console.WriteLine("The game is over.")
                Console.WriteLine(status.Status)
                _endpoint.Dispose()
                Exit Function
            End If

            Console.WriteLine("It is turn {0}", status.CurrentTurn)

            'Figure out what moves to make
            moveList = agentBrain.AssessSituation(status, _myId)

            'Agent rain doesn't know about game particulars. Add those now
            For Each move As MoveRequest In moveList
                move.AuthToken = _authToken
                move.GameId = _gameId
            Next

            'Send the moves to the server
            Await SubmitMoves(moveList)

            'Find out how long until next turn, then wait for it
            _timeToNextTurn = status.NextTurnStart.Subtract(DateTime.UtcNow).Milliseconds
            Console.WriteLine("Moves submitted! There is {0}ms left in the turn.", _timeToNextTurn.ToString)
            If _timeToNextTurn > 0 Then
                Await Task.Delay(CInt(_timeToNextTurn))
            End If


        End While
    End Function

    Private Async Function GetGameStatus() As Task(Of StatusResult)
        Dim result As StatusResult
        Dim request As New StatusRequest()
        request.GameId = _gameId

        Dim response = Await _endpoint.PostAsJsonAsync("api/status", request)
        result = Await response.Content.ReadAsAsync(Of StatusResult)

        If Not result.Success Then
            Console.WriteLine(String.Format("Could not get game status: {0}", result.Message))
            Throw New Exception("Problem getting game status")
        End If

        Return result

    End Function

    Private Async Function SubmitMoves(ByVal moves As List(Of MoveRequest)) As Task
        Dim response = Await _endpoint.PostAsJsonAsync("api/move", moves)
        Dim results = Await response.Content.ReadAsAsync(Of List(Of MoveResult))

        For Each mr As MoveResult In results
            Console.WriteLine(String.Format("Move: {0}", mr.Message))
        Next


    End Function


End Class
