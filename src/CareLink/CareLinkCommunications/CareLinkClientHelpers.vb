﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Net
Imports System.Net.Http
Imports System.Runtime.CompilerServices
Imports System.Text

Imports CareLink

Public Module CareLinkClientHelpers

    Private Function DecodeResponse(response As HttpResponseMessage, ByRef lastErrorMessage As String, <CallerMemberName> Optional memberName As String = Nothing, <CallerLineNumber()> Optional sourceLineNumber As Integer = 0) As HttpResponseMessage
        Dim message As String
        If response?.IsSuccessStatusCode Then
            Dim resultText As String = response.ResultText
            lastErrorMessage = ExtractResponseData(resultText, "login_page.error.LoginFailed"">", "<")
            If lastErrorMessage = "" Then
                Debug.Print($"{NameOf(DecodeResponse)} success from {memberName}, line {sourceLineNumber}.")
                Return response
            End If
            Debug.Print($"{NameOf(DecodeResponse)} failed from {memberName}, line {sourceLineNumber}.")
            Return New HttpResponseMessage(HttpStatusCode.NetworkAuthenticationRequired)
        ElseIf response?.StatusCode = HttpStatusCode.BadRequest Then
            message = $"{NameOf(DecodeResponse)} failed with {HttpStatusCode.BadRequest}"
            lastErrorMessage = $"Login Failure {message}"
            Debug.Print($"{message} from {memberName}, line {sourceLineNumber}")
            Return response
        Else
            message = $"{NameOf(DecodeResponse)} failed, session response is {response?.StatusCode.ToString}"
            lastErrorMessage = message
            Debug.Print($"{message} from {memberName}, line {sourceLineNumber}")
            Return response
        End If
    End Function

    <Extension>
    Private Function ExtractResponseData(responseBody As String, startStr As String, endStr As String) As String
        If String.IsNullOrWhiteSpace(responseBody) Then
            Return ""
        End If

        Dim startIndex As Integer = responseBody.IndexOf(startStr, StringComparison.Ordinal)
        If startIndex = -1 Then
            Return ""
        End If
        startIndex += startStr.Length
        Dim endIndex As Integer = responseBody.IndexOf(endStr, startIndex, StringComparison.Ordinal)
        Return If(endIndex = -1,
                  "",
                  responseBody.Substring(startIndex, endIndex - startIndex).Replace("""", "")
                 )
    End Function

    Private Function ParseQsl(loginSessionResponse As HttpResponseMessage) As Dictionary(Of String, String)
        Dim result As New Dictionary(Of String, String)
        Dim absoluteUri As String = loginSessionResponse.RequestMessage.RequestUri.AbsoluteUri
        Dim splitAbsoluteUri() As String = absoluteUri.Split("&")
        For Each item As String In splitAbsoluteUri
            Dim splitItem() As String
            If result.Count = 0 Then
                item = item.Split("?")(1)
            End If
            splitItem = item.Split("=")
            result.Add(splitItem(0), splitItem(1))
        Next
        Return result
    End Function

    Friend Function DoConsent(ByRef httpClient As HttpClient, doLoginResponse As HttpResponseMessage, ByRef lastErrorMessage As String) As HttpResponseMessage

        ' Extract data for consent
        Dim doLoginRespBody As String = doLoginResponse.ResultText
        Dim url As New StringBuilder(doLoginRespBody.ExtractResponseData("<form action=", " "))
        Dim sessionId As String = doLoginRespBody.ExtractResponseData("<input type=""hidden"" name=""sessionID"" value=", ">")
        Dim sessionData As String = doLoginRespBody.ExtractResponseData("<input type=""hidden"" name=""sessionData"" value=", ">")
        lastErrorMessage = doLoginRespBody.ExtractResponseData("LoginFailed"">", "</p>")

        ' Send consent
        Dim form As New Dictionary(Of String, String) From {
            {"action", "consent"},
            {"sessionID", sessionId},
            {"sessionData", sessionData},
            {"response_type", "code"},
            {"response_mode", "query"}}
        ' Add header
        Dim consentHeaders As Dictionary(Of String, String) = s_commonHeaders.Clone()
        consentHeaders("Content-Type") = "application/x-www-form-urlencoded"

        Try
            Dim response As HttpResponseMessage = httpClient.Post(url, headers:=consentHeaders, data:=form)
            Return DecodeResponse(response, lastErrorMessage)
        Catch ex As Exception
            Dim message As String = $"__doConsent() failed with {ex.DecodeException()}"
            lastErrorMessage = message
            Debug.Print(message.Replace(vbCrLf, " "))
        End Try
        Return New HttpResponseMessage(HttpStatusCode.NotImplemented)
    End Function

    Friend Function DoLogin(ByRef httpClient As HttpClient, loginSessionResponse As HttpResponseMessage, userName As String, password As String, ByRef lastErrorMessage As String) As HttpResponseMessage

        Dim queryParameters As Dictionary(Of String, String) = ParseQsl(loginSessionResponse)
        Dim url As StringBuilder
        With loginSessionResponse.RequestMessage.RequestUri
            url = New StringBuilder($"{ .Scheme}://{ .Host}{Join(loginSessionResponse.RequestMessage.RequestUri.Segments, "")}")
        End With

        Dim webForm As New Dictionary(Of String, String) From {
            {"sessionID", queryParameters.GetValueOrDefault("sessionID")},
            {"sessionData", queryParameters.GetValueOrDefault("sessionData")},
            {"locale", "en"},
            {"action", "login"},
            {"username", userName},
            {"password", password},
            {"actionButton", "Log in"}}

        Dim payload As New Dictionary(Of String, String) From {
            {"country", queryParameters.GetValueOrDefault("CountryCode".ToLower)},
            {"locale", "en"},
            {"g-recaptcha-response", "abc"}
        }

        Dim response As HttpResponseMessage = Nothing
        Try
            response = httpClient.Post(url, s_commonHeaders, payload, webForm)
        Catch ex As Exception
            Stop
            lastErrorMessage = $"HTTP Response is not OK, {response?.StatusCode}"
        End Try
        Return If(response Is Nothing, Nothing, DecodeResponse(response, lastErrorMessage))
    End Function

    Friend Function NetworkUnavailable() As Boolean
        Return Not My.Computer.Network.IsAvailable
    End Function

End Module
