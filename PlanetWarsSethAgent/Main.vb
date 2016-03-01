Imports System

Module Main

    Sub Main()
        Dim epIndex As Integer
        Dim nameIndex As Integer
        Dim endPoint As String
        Dim playerName As String
        Dim gameAgent As PlanetWarAgent

        Dim args As String() = System.Environment.GetCommandLineArgs

        epIndex = args.ToList.IndexOf("-endpoint") + 1
        nameIndex = args.ToList.IndexOf("-name") + 1

        If epIndex <> 0 And args.Length > epIndex Then
            endPoint = args(epIndex)
        End If

        If nameIndex <> 0 And args.Length > nameIndex Then
            playerName = args(nameIndex)
        End If

        If endPoint = String.Empty Then
            Console.WriteLine("You must specify an endpoint to connect to!")
            Exit Sub
        End If

        gameAgent = New PlanetWarAgent(endPoint, playerName)
        gameAgent.Start.Wait()

    End Sub

End Module
