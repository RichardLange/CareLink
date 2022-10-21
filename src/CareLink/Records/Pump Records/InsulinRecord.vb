﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations.Schema

Public Class InsulinRecord
    Private _dateTime As Date

    <DisplayName("Record Number")>
    <Column(Order:=0, TypeName:="Integer")>
    Public Property RecordNumber As Integer

    <DisplayName("Type")>
    <Column(Order:=1, TypeName:="String")>
    Public Property type As String

    <DisplayName(NameOf(index))>
    <Column(Order:=2, TypeName:="Integer")>
    Public Property index As Integer

    <DisplayName("Kind")>
    <Column(Order:=3, TypeName:="String")>
    Public Property kind As String

    <DisplayName("Version")>
    <Column(Order:=4, TypeName:="Integer")>
    Public Property version As Integer

    <DisplayName(NameOf([dateTime]))>
    <Column(Order:=5, TypeName:="Date")>
    Public Property [dateTime] As Date
        Get
            Return _dateTime
        End Get
        Set
            _dateTime = Value
        End Set
    End Property

    <DisplayName("dateTime As String")>
    <Column(Order:=6, TypeName:="String")>
    Public Property dateTimeAsString As String

    <DisplayName("OA Date Time")>
    <Column(Order:=7, TypeName:="Double")>
    Public ReadOnly Property OAdateTime As OADate
        Get
            Return New OADate(_dateTime)
        End Get
    End Property

    <DisplayName(NameOf(relativeOffset))>
    <Column(Order:=8, TypeName:="Integer")>
    Public Property relativeOffset As Integer

    <DisplayName("Programmed Extended Amount")>
    <Column(Order:=9, TypeName:="Single")>
    Public Property programmedExtendedAmount As Single

    <DisplayName("Activation Type")>
    <Column(Order:=10, TypeName:="String", TypeName:="String")>
    Public Property activationType As String

    <DisplayName("delivered Extended Amount")>
    <Column(Order:=11, TypeName:="Single")>
    Public Property deliveredExtendedAmount As Single

    <DisplayName("Programmed Fast Amount")>
    <Column(Order:=12, TypeName:="Single")>
    Public Property programmedFastAmount As Single

    <DisplayName("Programmed Duration")>
    <Column(Order:=13, TypeName:="Integer")>
    Public Property programmedDuration As Integer

    <DisplayName("delivered Fast Amount")>
    <Column(Order:=14, TypeName:="Single")>
    Public Property deliveredFastAmount As Single

    <DisplayName(NameOf(id))>
    <Column(Order:=15, TypeName:="Integer")>
    Public Property id As Integer

    <DisplayName("Effective Duration")>
    <Column(Order:=16, TypeName:="Integer")>
    Public Property effectiveDuration As Integer

    <DisplayName(NameOf(completed))>
    <Column(Order:=17, TypeName:="Boolean")>
    Public Property completed As Boolean

    <DisplayName("Bolus Type")>
    <Column(Order:=18, TypeName:="String")>
    Public Property bolusType As String

End Class
