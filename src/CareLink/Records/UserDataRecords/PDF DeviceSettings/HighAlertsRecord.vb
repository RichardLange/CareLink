﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Public Class HighAlertsRecord
    Private _snoozeTime As New TimeSpan(1, 0, 0)

    Public Sub New(snoozeOn As String, snoozeTime As TimeSpan, sTable As StringTable)
        If snoozeOn = "Off" Then
            Me.SnoozeOn = "Off"
        Else
            Me.SnoozeTime = snoozeTime
            Me.SnoozeOn = snoozeOn
        End If
        Dim valueUnits As String = ""
        For Each e As IndexClass(Of StringTable.Row) In sTable.Rows.WithIndex
            Dim s As StringTable.Row = e.Value
            If e.IsFirst Then
                ' get value units
                Continue For
            End If
            Me.HighAlert.Add(New HighAlertRecord(s, valueUnits))
        Next
    End Sub

    Public Sub New()
    End Sub

    Public Property HighAlert As New List(Of HighAlertRecord)

    Public WriteOnly Property SnoozeTime As TimeSpan
        Set
            _snoozeTime = Value
        End Set
    End Property

    Public Property SnoozeOn As String = "Off"

    Public Overrides Function ToString() As String
        Return If(Me.SnoozeOn = "On", $"{_snoozeTime.Hours} hr", "Off")
    End Function

End Class
