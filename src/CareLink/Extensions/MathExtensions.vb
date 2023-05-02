﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports System.Runtime.CompilerServices

Friend Module MathExtensions

    <Extension>
    Friend Function RoundSingle(singleValue As Single, decimalDigits As Integer) As Single

        Return CSng(Math.Round(singleValue, decimalDigits))
    End Function

    <Extension>
    Friend Function RoundToSingle(doubleValue As Double, decimalDigits As Integer) As Single

        Return CSng(Math.Round(doubleValue, decimalDigits))
    End Function

    <Extension>
    Public Function ParseSingle(valueString As String, Optional decimalDigits As Integer = -1) As Single
        If valueString Is Nothing Then
            Return Single.NaN
        End If
        If valueString.Contains(","c) AndAlso valueString.Contains("."c) Then
            Throw New ArgumentException($"{NameOf(valueString)} = {valueString}, contains both a comma and period.", NameOf(valueString))
        End If
        valueString = valueString.Replace(",", ".")
        Dim returnSingle As Single
        If decimalDigits = -1 Then
            Dim index As Integer = valueString.IndexOf(".")
            If index = -1 Then
                decimalDigits = 0
            Else
                decimalDigits = valueString.Substring(index).Length
            End If
        End If
        If Single.TryParse(valueString, NumberStyles.Number, usDataCulture, returnSingle) Then
        Else
            Return Single.NaN
        End If
        Return If(decimalDigits = 3, returnSingle.RoundTo025, returnSingle.RoundSingle(decimalDigits))
    End Function

    Public Function ParseSingle(valueObject As Object, Optional decimalDigits As Integer = -1) As Single
        If valueObject Is Nothing Then
            Return Single.NaN
        End If

        Dim returnSingle As Single
        Select Case True
            Case TypeOf valueObject Is String
                Return CStr(valueObject).ParseSingle(decimalDigits)
            Case TypeOf valueObject Is Single
                returnSingle = CSng(valueObject)
            Case TypeOf valueObject Is Double
                returnSingle = CSng(valueObject)
            Case TypeOf valueObject Is Decimal
                returnSingle = CSng(valueObject)
            Case Else
                Throw UnreachableException($"{NameOf(valueObject)} of type {valueObject.GetType.Name} is unknown in ParseSingle")
        End Select

        Return If(decimalDigits = 3, returnSingle.RoundTo025, returnSingle.RoundSingle(decimalDigits))
    End Function

    <Extension>
    Public Function RoundTo025(originalValue As Single) As Single
        If Single.IsNaN(originalValue) Then
            Return Double.NaN
        End If
        Return CSng(Math.Floor(Math.Round(originalValue, 3, MidpointRounding.ToZero) / 0.025D) * 0.025D)
    End Function

    <Extension>
    Public Function TryParseSingle(valueString As String, ByRef result As Single, <CallerMemberName> Optional memberName As String = Nothing, <CallerLineNumber()> Optional sourceLineNumber As Integer = 0) As Boolean
        If valueString.Contains(","c) AndAlso valueString.Contains("."c) Then
            Throw New Exception($"{NameOf(valueString)} = {valueString}, and contains both comma and period in Line {sourceLineNumber} in {memberName}.")
        End If

        If Single.TryParse(valueString.Replace(",", "."), NumberStyles.Number, usDataCulture, result) Then
            result = result.RoundSingle(10)
            Return True
        End If
        If Single.TryParse(valueString, NumberStyles.Number, CurrentUICulture, result) Then
            result = result.RoundSingle(10)
            Return True
        End If
        result = Single.NaN
        Return False
    End Function

End Module
