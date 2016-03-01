Imports PlanetWars.Shared

Public Interface IAgentBrain

    Function AssessSituation(ByVal gs As StatusResult, myID As Integer) As List(Of MoveRequest)

End Interface
