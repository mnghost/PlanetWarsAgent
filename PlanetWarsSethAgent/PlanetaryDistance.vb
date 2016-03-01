Public Class PlanetaryDistance
    Public Property Source As Integer
    Public Property Destination As Integer
    Public Property Distance As Integer

    Public Sub New(ByVal src As Integer, dest As Integer, distance As Integer)
        Me.Source = src
        Me.Destination = dest
        Me.Distance = distance
    End Sub
End Class
