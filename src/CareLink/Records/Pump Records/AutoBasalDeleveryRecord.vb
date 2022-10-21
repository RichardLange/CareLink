﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations.Schema

Public Class AutoBasalDeliveryRecord
    Private _dateTime As Date

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

    <DisplayName("Bolus Amount")>
    <Column(Order:=10, TypeName:="Single")>
    Public Property bolusAmount As Single

    <DisplayName("dateTime As String")>
    <Column(Order:=6, TypeName:="String")>
    Public Property dateTimeAsString As String

    <DisplayName(NameOf(id))>
    <Column(Order:=9, TypeName:="Integer")>
    Public Property id As Integer

    <DisplayName(NameOf(index))>
    <Column(Order:=2, TypeName:="Integer")>
    Public Property index As Integer

    <DisplayName("Kind")>
    <Column(Order:=3, TypeName:="String")>
    Public Property kind As String

    <DisplayName("OA date Time")>
    <Column(Order:=7, TypeName:="Double")>
    Public ReadOnly Property OAdateTime As OADate
        Get
            Return New OADate(_dateTime)
        End Get
    End Property

    <DisplayName("Record Number")>
    <Column(Order:=0, TypeName:="Integer")>
    Public Property RecordNumber As Integer

    <DisplayName(NameOf(relativeOffset))>
    <Column(Order:=8, TypeName:="Integer")>
    Public Property relativeOffset As Integer

    <DisplayName("Type")>
    <Column(Order:=1, TypeName:="String")>
    Public Property type As String

    <DisplayName("Version")>
    <Column(Order:=4, TypeName:="Integer")>
    Public Property version As Integer

End Class
