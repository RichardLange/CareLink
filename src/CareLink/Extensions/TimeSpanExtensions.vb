﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Friend Module TimeSpanExtensions

    <Extension>
    Public Function ToFormattedTimeSpan(tSpan As TimeSpan, Optional units As String = "") As String
        Dim r As String = ""
        If units.Contains("hr") Then
            units = If(tSpan.Hours = 0,
                       units,
                       units.Replace("hr", "hrs")
                      )
            r = $"{tSpan.Hours,2}:"
        End If
        If tSpan.Seconds > 0 AndAlso tSpan.Minutes > 0 Then
            r &= $"{tSpan.Minutes}:{tSpan.Seconds:D2}"
            units = If(tSpan.Minutes = 0,
                       "min",
                       "mins"
                      )
        ElseIf tSpan.Seconds > 0 Then
            r &= $"{tSpan.Seconds:D2}"
            units = If(tSpan.Minutes = 0,
                       "sec",
                       "sec"
                      )
        Else
            r &= $"{tSpan.Minutes:D2}"
            If Not units.Contains("hr") Then
                units = If(tSpan.Minutes = 0,
                           "min",
                           "mins"
                          )
            End If
        End If
        Return $"{r} {units}".TrimEnd
    End Function

End Module
