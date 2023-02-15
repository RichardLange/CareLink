﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text

''' <summary>
''' Helper Functions. Conversion, Validation
''' </summary>
Public Module DataTableHelper

    ''' <summary>
    ''' Returns a new string in which all occurrences of the single quote character in the current instance are replaced with a back-tick character.
    ''' </summary>
    Private Function EscapeSingleQuotes(Input As String) As String
        Return Input.Replace("'"c, "`"c) ' Replace with back-tick
    End Function

    ''' <summary>
    ''' Returns an enumerator, which supports a simple iteration over a collection of all the DataColumns in a specified DataTable.
    ''' </summary>
    Private Function GetDataTableColumns(Input As DataTable) As IEnumerable(Of DataColumn)
        If Input Is Nothing OrElse Input.Columns.Count < 1 Then
            Return New List(Of DataColumn)()
        End If
        Return Input.Columns.OfType(Of DataColumn)().ToList()
    End Function

    ''' <summary>
    ''' Enumerates a collection as delimited collection of strings.
    ''' </summary>
    ''' <typeparam name="T">The Type of the collection.</typeparam>
    ''' <param name="Collection">An Enumerator to a collection to populate the string.</param>
    ''' <param name="Prefix">The string to prefix the result.</param>
    ''' <param name="Delimiter">The string that will appear between each item in the specified collection.</param>
    ''' <param name="Postfix">The string to postfix the result.</param>
    Private Function ListToDelimitedString(Of T)(Collection As IEnumerable(Of T), Prefix As String, Delimiter As String, Postfix As String) As String
        If Collection Is Nothing OrElse Not Collection.Any() Then
            Return String.Empty
        End If

        Dim result As New StringBuilder()
        For Each item As T In Collection
            If result.Length <> 0 Then
                result.Append(Delimiter) ' Add comma
            End If

            result.Append(EscapeSingleQuotes(TryCast(item, String)))
        Next item
        If result.Length < 1 Then
            Return String.Empty
        End If

        result.Insert(0, Prefix)
        result.Append(Postfix)

        Return result.ToString()
    End Function

    ''' <summary>
    ''' Returns an list, which supports a simple iteration over a collection of all the DataRows in a specified DataTable.
    ''' </summary>
    Public Function GetDataTableRows(Input As DataTable) As IEnumerable(Of DataRow)
        If Not IsValidDataTable(Input) Then
            Return New List(Of DataRow)()
        End If
        Return Input.Rows.OfType(Of DataRow)().ToList()
    End Function

    ''' <summary>
    ''' Returns all the column names of the specified DataRow in a string delimited like and SQL INSERT INTO statement.
    ''' Example: ([FullName], [Gender], [BirthDate])
    ''' </summary>
    ''' <returns>A string formatted like the columns specified in an SQL 'INSERT INTO' statement.</returns>
    Public Function RowToColumnString(Row As DataRow) As String
        Dim collection As IEnumerable(Of String) = Row.ItemArray.Select(Function(item) TryCast(item, String))
        Return ListToDelimitedString(collection, "([", "], [", "])")
    End Function

    ''' <summary>
    ''' Returns all the values the specified DataRow in as a string delimited like and SQL INSERT INTO statement.
    ''' Example: ('John Doe', 'M', '10/3/1981'')
    ''' </summary>
    ''' <returns>A string formatted like the values specified in an SQL 'INSERT INTO' statement.</returns>
    Public Function RowToValueString(Row As DataRow) As String
        Dim collection As IEnumerable(Of String) = GetDataTableColumns(Row.Table).Select(Function(c) c.ColumnName)
        Return ListToDelimitedString(collection, "('", "', '", "')")
    End Function

End Module
