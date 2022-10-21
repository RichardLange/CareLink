﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Text

<DebuggerDisplay("{GetDebuggerDisplay(),nq}")>
Public Class supportedReportRecord

    Public Sub New(Values As Dictionary(Of String, String), recordnumber As Integer)
        If Values.Count <> 3 Then
            Throw New Exception($"{NameOf(supportedReportRecord)}({Values}) contains {Values.Count} entries, 3 expected.")
        End If
        Me.recordNumber = recordnumber
        Me.report = Values(NameOf(report))
        Me.onlyFor = kvpToString(LoadList(Values(NameOf(onlyFor)))).ToString.TrimStart(" "c).TrimEnd(",")
        Me.notFor = kvpToString(LoadList(Values(NameOf(notFor)))).ToString.TrimStart(" "c).TrimEnd(",")

    End Sub

#If True Then ' Prevent reordering

    <DisplayName("Record Number")>
    <Column(Order:=0)>
    Public Property recordNumber As Integer

    <DisplayName("Report")>
    <Column(Order:=1)>
    Public Property report As String

    <DisplayName("Only For")>
    <Column(Order:=2)>
    Public Property onlyFor As String

    <DisplayName("Not For")>
    <Column(Order:=3)>
    Public Property notFor As String

#End If  ' Prevent reordering

    Private Shared Function kvpToString(forList As List(Of Dictionary(Of String, String))) As StringBuilder
        Dim sb As New StringBuilder
        For Each dic As Dictionary(Of String, String) In forList
            For Each kvp As KeyValuePair(Of String, String) In dic
                sb.Append($" {kvp.Key}={kvp.Value},")
            Next
        Next

        Return sb
    End Function

    Private Function GetDebuggerDisplay() As String
        Return Me.report.ToString()
    End Function

End Class
