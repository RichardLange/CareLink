﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations.Schema

Public Class BGReadingRecord
    Private _dateTime As Date

    <DisplayName(NameOf([dateTime]))>
    <Column(Order:=6, TypeName:="Date")>
    Public Property [dateTime] As Date
        Get
            Return _dateTime
        End Get
        Set
            _dateTime = Value
        End Set
    End Property

    <DisplayName("dateTime As String")>
    <Column(Order:=7, TypeName:="String")>
    Public Property dateTimeAsString As String

    <DisplayName(NameOf(index))>
    <Column(Order:=2, TypeName:="Integer")>
    Public Property index As Integer

    <DisplayName("Kind")>
    <Column(Order:=4, TypeName:="String")>
    Public Property kind As String

    <DisplayName("Record Number")>
    <Column(Order:=0, TypeName:="Integer")>
    Public Property RecordNumber As Integer

    <DisplayName(NameOf(relativeOffset))>
    <Column(Order:=9, TypeName:="Integer")>
    Public Property relativeOffset As Integer

    <DisplayName("Type")>
    <Column(Order:=1, TypeName:="Integer")>
    Public Property type As String

    <DisplayName("Value")>
    <Column(Order:=3, TypeName:="Single")>
    Public Property value As Single

    <DisplayName("Version")>
    <Column(Order:=5, TypeName:="Integer")>
    Public Property version As Integer

End Class
