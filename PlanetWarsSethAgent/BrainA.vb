Imports PlanetWars.Shared
Imports PlanetWarsSethAgent

Public Class BrainA
    Implements IAgentBrain

    Private _currentTurn As Integer
    Private _myId As Integer
    Private _gameStatus As StatusResult

    Private _myPopulation As Integer
    Private _myIndustrialCap As Integer
    Private _enemyPopulation As Integer
    Private _enemyIndustrialCap As Integer

    Private _myPlanets As List(Of Planet)
    Private _enemyPlanets As List(Of Planet)

    Private _planetaryDistances As List(Of PlanetaryDistance)

    Public Sub New()
        _currentTurn = 0

        _planetaryDistances = New List(Of PlanetaryDistance)
    End Sub

    Public Function AssessSituation(gs As StatusResult, myId As Integer) As List(Of MoveRequest) Implements IAgentBrain.AssessSituation
        Dim timeLeft As Integer

        Dim moveList As New List(Of MoveRequest)

        If gs.CurrentTurn <= _currentTurn Then
            'I should have already made moves for this turn. Get out.
            Console.WriteLine("I've already made moves for turn {0}", gs.CurrentTurn.ToString)
            Return moveList
        End If

        _currentTurn = gs.CurrentTurn
        _myId = myId
        _gameStatus = gs

        timeLeft = gs.EndOfCurrentTurn.Subtract(DateTime.UtcNow).Milliseconds
        Console.WriteLine("Assessing situation. I have {0}ms to make my decision", gs.EndOfCurrentTurn.Subtract(DateTime.UtcNow).Milliseconds)

        GetMyInfo(gs)
        GetEnemyInfo(gs)

        Console.WriteLine("My industrial capcity is {0}", _myIndustrialCap.ToString)
        Console.WriteLine("My population is {0}", _myPopulation.ToString)

        Console.WriteLine("Enemy industry is {0}", _enemyIndustrialCap)
        Console.WriteLine("Enemy population is {0}", _enemyPopulation)

        If _myIndustrialCap <= _enemyIndustrialCap Then
            'Time to grab a new planet
            Console.WriteLine("I'm not strong enough. Need to expand my empire.")

            For Each base As Planet In _myPlanets
                Dim targetId As Integer
                Dim minDist As Integer = 10000

                'Find the nearest planet I don't own to target
                For Each pd In _planetaryDistances.Where(Function(x As PlanetaryDistance) x.Source = base.Id And
                                                                Not (From mp In _myPlanets Select mp.Id).Contains(x.Destination)).ToList
                    If pd.Distance < minDist Then
                        minDist = pd.Distance
                        targetId = pd.Destination
                    End If
                Next

                Dim move As MoveRequest
                move = MakeMaxAttack(base.Id, targetId)

                If move.NumberOfShips <> 0 Then
                    moveList.Add(move)
                End If

            Next
        Else  'Defend and attack
            Console.WriteLine("I have the edge. Time to do something with it.")

            For Each p As Planet In _myPlanets
                'Check to see if the planet is being attacked
                Dim incomingFleets As List(Of Fleet)
                incomingFleets = gs.Fleets.Where(Function(f) f.OwnerId <> _myId And f.DestinationPlanetId = p.Id).ToList

                Dim targetId As Integer
                Dim minDist As Integer = 10000

                For Each pd In _planetaryDistances.Where(Function(x As PlanetaryDistance) x.Source = p.Id And
                                                                (From ep In _enemyPlanets Select ep.Id).Contains(x.Destination)).ToList
                    If pd.Distance < minDist Then
                        minDist = pd.Distance
                        targetId = pd.Destination
                    End If
                Next

                If incomingFleets.Count = 0 Then 'Planet is free, use it to attack nearest enemy

                    Dim move As MoveRequest
                    move = MakeMaxAttack(p.Id, targetId)

                    If move.NumberOfShips <> 0 Then
                        moveList.Add(move)
                    End If

                Else 'Planet is under attack, can it spare any fleets for attack?

                    'How close the nearest attacking fleet?
                    Dim turnsAway As Integer = 0

                    For Each flt As Fleet In incomingFleets
                        If turnsAway = 0 Then
                            turnsAway = flt.NumberOfTurnsToDestination
                        End If
                        If flt.NumberOfTurnsToDestination < turnsAway Then
                            turnsAway = flt.NumberOfTurnsToDestination
                        End If
                    Next

                    'How big is the nearest attack?
                    Dim nearestAttackers As List(Of Fleet) = incomingFleets.Where(Function(y) y.NumberOfTurnsToDestination = turnsAway).ToList
                    Dim attackSize As Integer

                    For Each attacker As Fleet In nearestAttackers
                        attackSize += attacker.NumberOfShips
                    Next

                    Dim retalCap As Integer = p.GrowthRate - attackSize

                    If retalCap > 0 Then
                        Console.WriteLine("Planet {0} can spare {1} ships.", p.Id, retalCap)
                        Dim move As New MoveRequest
                        move.SourcePlanetId = p.Id
                        move.DestinationPlanetId = targetId
                        move.NumberOfShips = retalCap

                        moveList.Add(move)
                    End If

                End If
            Next

        End If

        timeLeft = gs.EndOfCurrentTurn.Subtract(DateTime.UtcNow).Milliseconds
        Console.WriteLine("Decisions made with {0}ms to spare.", timeLeft.ToString)
        Return moveList
    End Function

    Private Sub GetMyInfo(ByVal gs As StatusResult)
        _myPlanets = gs.Planets.Where(Function(p As Planet) p.OwnerId = _myId).ToList

        'What is my empire total?
        _myIndustrialCap = 0
        _myPopulation = 0
        For Each p As Planet In _myPlanets
            _myIndustrialCap += p.GrowthRate
            _myPopulation += p.Size

            'Check to see if we've calculated distances for this planet yet
            If _planetaryDistances.Where(Function(x As PlanetaryDistance) x.Source = p.Id).Count = 0 Then
                'We have a new planet in our empire, get its distance from other places
                For Each x As Planet In gs.Planets.Where(Function(y As Planet) y.Id <> p.Id)
                    _planetaryDistances.Add(New PlanetaryDistance(p.Id, x.Id, p.Position.Distance(x.Position)))
                Next
            End If

        Next
    End Sub

    Private Sub GetEnemyInfo(ByVal gs As StatusResult)
        _enemyPlanets = gs.Planets.Where(Function(p As Planet) p.OwnerId <> _myId And p.OwnerId <> -1).ToList

        'What is the enemy's totals?
        _enemyIndustrialCap = 0
        _enemyPopulation = 0
        For Each p As Planet In _enemyPlanets
            _enemyIndustrialCap += p.GrowthRate
            _enemyPopulation += p.Size
        Next

    End Sub

    Private Function MakeMaxAttack(ByVal attacker As Integer, target As Integer) As MoveRequest
        Dim move As New MoveRequest
        Dim targetPlanet As Planet
        Dim attackingPlanet As Planet

        Dim targetSize As Integer
        Dim attackerCap As Integer

        move.SourcePlanetId = attacker
        move.DestinationPlanetId = target

        targetPlanet = _gameStatus.Planets.Where(Function(x) x.Id = target).FirstOrDefault
        attackingPlanet = _gameStatus.Planets.Where(Function(y) y.Id = attacker).FirstOrDefault

        attackerCap = attackingPlanet.GrowthRate
        targetSize = targetPlanet.Size

        If attackerCap >= targetSize Then
            move.NumberOfShips = targetSize
        Else
            move.NumberOfShips = attackerCap
        End If

        If move.NumberOfShips > 0 Then
            Console.WriteLine("***Making attack, {0} ships from {1} to {2}", move.NumberOfShips.ToString, move.SourcePlanetId.ToString, move.DestinationPlanetId.ToString)
        End If
        Return move
    End Function


End Class
