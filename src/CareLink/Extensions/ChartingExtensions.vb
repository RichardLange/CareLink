﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Windows.Forms.DataVisualization.Charting
Imports Octokit

Friend Module ChartingExtensions

    <Extension>
    Private Sub AddBasalPoint(basalSeries As Series, startX As OADate, StartY As Double, lineColor As Color, toolTip As String)
        If basalSeries.Points.Count > 0 AndAlso (Not basalSeries.Points.Last.IsEmpty) AndAlso New OADate(basalSeries.Points.Last.XValue).Within10Minutes(startX) Then
            basalSeries.Points.AddXY(basalSeries.Points.Last, Double.NaN)
            basalSeries.Points.Last().Color = Color.Transparent
            basalSeries.Points.Last().IsEmpty = True
        End If
        basalSeries.Points.AddXY(startX, StartY)
        If Double.IsNaN(StartY) Then
            basalSeries.Points.Last.Color = Color.Transparent
            basalSeries.Points.Last.MarkerSize = 0
            basalSeries.Points.Last.IsEmpty = True
        Else
            basalSeries.Points.Last.Color = lineColor
            basalSeries.Points.Last.ToolTip = toolTip
        End If

    End Sub

    <Extension>
    Private Sub AddBgReadingPoint(markerSeriesPoints As DataPointCollection, markerOADate As OADate, bgValueString As String, bgValue As Single)
        markerSeriesPoints.AddXY(markerOADate, bgValue)
        markerSeriesPoints.Last.BorderColor = Color.Gainsboro
        markerSeriesPoints.Last.Color = Color.FromArgb(5, Color.Gainsboro)
        markerSeriesPoints.Last.MarkerSize = 10
        markerSeriesPoints.Last.MarkerStyle = MarkerStyle.Circle
        If Not Single.IsNaN(bgValue) Then
            markerSeriesPoints.Last.ToolTip = $"Blood Glucose: Not used For calibration: {bgValueString} {BgUnitsString}"
        End If
    End Sub

    <Extension>
    Private Sub AddCalibrationPoint(markerSeriesPoints As DataPointCollection, markerOADate As OADate, bgValue As Single, entry As Dictionary(Of String, String))
        markerSeriesPoints.AddXY(markerOADate, bgValue)
        markerSeriesPoints.Last.BorderColor = Color.Red
        markerSeriesPoints.Last.Color = Color.FromArgb(5, Color.Red)
        markerSeriesPoints.Last.MarkerStyle = MarkerStyle.Circle
        markerSeriesPoints.Last.MarkerBorderWidth = 2
        markerSeriesPoints.Last.MarkerSize = 8
        markerSeriesPoints.Last.ToolTip = $"Blood Glucose: Calibration {If(CBool(entry("calibrationSuccess")), "accepted", "not accepted")}: {entry("value")} {BgUnitsString}"
    End Sub

    Private Function GetLimitsList(count As Integer) As Integer()
        Dim limitsIndexList(count) As Integer
        Dim limitsIndex As Integer = 0
        For i As Integer = 0 To limitsIndexList.GetUpperBound(0)
            If limitsIndex + 1 < s_listOfLimitRecords.Count AndAlso s_listOfLimitRecords(limitsIndex + 1).index < i Then
                limitsIndex += 1
            End If
            limitsIndexList(i) = limitsIndex
        Next
        Return limitsIndexList
    End Function

    <Extension>
    Private Sub PaintMarker(e As ChartPaintEventArgs, markerImage As Bitmap, markerDictionary As Dictionary(Of OADate, Single), noImageOffset As Boolean, paintOnY2 As Boolean)
        ' Draw the cloned portion of the Bitmap object.
        Dim halfHeight As Single = CSng(If(noImageOffset, 0, markerImage.Height / 2))
        Dim halfWidth As Single = CSng(markerImage.Width / 2)
        For Each markerKvp As KeyValuePair(Of OADate, Single) In markerDictionary
            Dim imagePosition As RectangleF = RectangleF.Empty
            imagePosition.X = CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.X, markerKvp.Key))
            If paintOnY2 Then
                imagePosition.Y = CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.Y2, markerKvp.Value))
            Else
                imagePosition.Y = CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.Y, markerKvp.Value))
            End If
            imagePosition.Width = markerImage.Width
            imagePosition.Height = markerImage.Height
            imagePosition = e.ChartGraphics.GetAbsoluteRectangle(imagePosition)
            imagePosition.Y -= halfHeight
            imagePosition.X -= halfWidth
            ' Draw image
            e.ChartGraphics.Graphics.DrawImage(markerImage, imagePosition.X, imagePosition.Y)
        Next
    End Sub

    <Extension>
    Private Sub PlotOnePoint(plotSeries As Series, sgOADateTime As OADate, bgValue As Single, mainLineColor As Color, HomePageMealRow As Double)
        Try
            With plotSeries.Points
                Dim sgDouble As Double = sgOADateTime
                If Single.IsNaN(bgValue) OrElse Math.Abs(bgValue - 0) < Single.Epsilon Then
                    .AddXY(sgDouble, HomePageMealRow)
                    .Last().Color = Color.Transparent
                    .Last().IsEmpty = True
                Else
                    If .Count > 0 AndAlso (Not .Last.IsEmpty) AndAlso New OADate(plotSeries.Points.Last.XValue).Within10Minutes(sgOADateTime) Then
                        plotSeries.Points.AddXY(.Last.XValue, Double.NaN)
                        .Last().Color = Color.Transparent
                        .Last().IsEmpty = True
                    End If
                    .AddXY(sgDouble, bgValue)
                    If bgValue > s_limitHigh Then
                        .Last.Color = Color.Yellow
                    ElseIf bgValue < s_limitLow Then
                        .Last.Color = Color.Red
                    Else
                        .Last.Color = mainLineColor
                    End If

                End If
            End With
        Catch ex As Exception
            Stop
            Throw New Exception($"{ex.DecodeException()} exception in {NameOf(PlotOnePoint)}")
        End Try

    End Sub

    <Extension>
    Friend Sub AdjustXAxisStartTime(ByRef axisX As Axis, lastTimeChangeRecord As TimeChangeRecord)
        Dim latestTime As Date = If(lastTimeChangeRecord.previousDateTime > lastTimeChangeRecord.dateTime, lastTimeChangeRecord.previousDateTime, lastTimeChangeRecord.dateTime)
        Dim timeOffset As Double = (latestTime - s_listOfSGs(0).datetime).TotalMinutes
        axisX.IntervalOffset = timeOffset
        axisX.IntervalOffsetType = DateTimeIntervalType.Minutes
    End Sub

    <Extension>
    Friend Sub DrawBasalMarker(ByRef basalSeries As Series, markerOADate As OADate, amount As Single, bolusRow As Double, insulinRow As Double, lineColor As Color, DrawFromBottom As Boolean, toolTip As String)
        Dim startX As OADate
        Dim startY As Double
        If amount.IsMinBasal() Then
            lineColor = GetGraphLineColor("Min Basal")
        End If
        If DrawFromBottom Then
            startX = markerOADate + s_twoHalfMinuteOADate
            startY = amount.RoundSingle(3)
            basalSeries.AddBasalPoint(startX, 0, lineColor, toolTip)
            basalSeries.AddBasalPoint(startX, startY, lineColor, toolTip)
            basalSeries.AddBasalPoint(startX, 0, lineColor, toolTip)
            basalSeries.AddBasalPoint(startX, Double.NaN, Color.Transparent, toolTip)
        Else
            startX = markerOADate + s_twoHalfMinuteOADate
            startY = bolusRow - ((bolusRow - insulinRow) * (amount / MaxBasalPerDose))
            basalSeries.AddBasalPoint(startX, bolusRow, lineColor, toolTip)
            basalSeries.AddBasalPoint(startX, startY, lineColor, toolTip)
            basalSeries.AddBasalPoint(startX, bolusRow, lineColor, toolTip)
            basalSeries.AddBasalPoint(startX, Double.NaN, Color.Transparent, toolTip)
        End If

    End Sub

    <Extension>
    Friend Sub PlotHighLowLimits(chart As Chart)
        If s_listOfLimitRecords.Count = 0 Then Exit Sub
        Dim limitsIndexList() As Integer = GetLimitsList(s_listOfSGs.Count - 1)
        For Each sgListIndex As IndexClass(Of SgRecord) In s_listOfSGs.WithIndex()
            Dim sgOADateTime As OADate = sgListIndex.Value.OaDateTime()
            Try
                Dim limitsLowValue As Single = s_listOfLimitRecords(limitsIndexList(sgListIndex.Index)).lowLimit
                Dim limitsHighValue As Single = s_listOfLimitRecords(limitsIndexList(sgListIndex.Index)).highLimit
                If limitsHighValue <> 0 Then
                    chart.Series(HighLimitSeries).Points.AddXY(sgOADateTime, limitsHighValue)
                End If
                If limitsLowValue <> 0 Then
                    chart.Series(LowLimitSeries).Points.AddXY(sgOADateTime, limitsLowValue)
                End If
            Catch ex As Exception
                Stop
                Throw New Exception($"{ex.DecodeException()} exception while plotting Limits in {NameOf(PlotHighLowLimits)}")
            End Try
        Next
    End Sub

    <Extension>
    Friend Sub PlotMarkers(pageChart As Chart, timeChangeSeries As Series, chartRelativePosition As RectangleF, markerInsulinDictionary As Dictionary(Of OADate, Single), markerMealDictionary As Dictionary(Of OADate, Single), <CallerMemberName> Optional memberName As String = Nothing, <CallerLineNumber()> Optional sourceLineNumber As Integer = 0)
        Dim lastTimeChangeRecord As TimeChangeRecord = Nothing
        markerInsulinDictionary.Clear()
        markerMealDictionary?.Clear()

        For Each markerWithIndex As IndexClass(Of Dictionary(Of String, String)) In s_markers.WithIndex()
            Try
                Dim markerDateTime As Date = markerWithIndex.Value.GetMarkerDateTime
                Dim markerOADateTime As New OADate(markerDateTime)
                Dim bgValueString As String = ""
                Dim bgValue As Single
                Dim entry As Dictionary(Of String, String) = markerWithIndex.Value

                If entry.TryGetValue("value", bgValueString) Then
                    bgValueString.TryParseSingle(bgValue)
                End If
                Dim markerSeriesPoints As DataPointCollection = pageChart.Series(MarkerSeries).Points
                Select Case entry("type")
                    Case "BG_READING"
                        If Not String.IsNullOrWhiteSpace(bgValueString) Then
                            markerSeriesPoints.AddBgReadingPoint(markerOADateTime, bgValueString, bgValue)
                        End If
                    Case "CALIBRATION"
                        markerSeriesPoints.AddCalibrationPoint(markerOADateTime, bgValue, entry)
                    Case "AUTO_BASAL_DELIVERY", "MANUAL_BASAL_DELIVERY"
                        Dim amount As Single = entry(NameOf(AutoBasalDeliveryRecord.bolusAmount)).ParseSingle(3)
                        With pageChart.Series(BasalSeries)
                            .DrawBasalMarker(markerOADateTime,
                                             amount,
                                             HomePageBasalRow,
                                             HomePageInsulinRow,
                                             GetGraphLineColor("Basal Series"),
                                             False,
                                             GetToolTip(entry("type"), amount))
                        End With
                    Case "INSULIN"
                        Select Case entry(NameOf(InsulinRecord.activationType))
                            Case "AUTOCORRECTION"
                                Dim autoCorrection As String = entry(NameOf(InsulinRecord.deliveredFastAmount))
                                With pageChart.Series(BasalSeries)
                                    .DrawBasalMarker(markerOADateTime,
                                                     autoCorrection.ParseSingle,
                                                     HomePageBasalRow,
                                                     HomePageInsulinRow,
                                                     GetGraphLineColor("Auto Correction"),
                                                     False,
                                                     $"Auto Correction: {autoCorrection.TruncateSingleString(3)} U")
                                End With
                            Case "MANUAL", "RECOMMENDED", "UNDETERMINED"
                                If markerInsulinDictionary.TryAdd(markerOADateTime, CInt(HomePageInsulinRow)) Then
                                    markerSeriesPoints.AddXY(markerOADateTime, HomePageInsulinRow - 10)
                                    markerSeriesPoints.Last.MarkerBorderWidth = 2
                                    markerSeriesPoints.Last.MarkerBorderColor = Color.FromArgb(10, Color.Black)
                                    markerSeriesPoints.Last.MarkerSize = 20
                                    markerSeriesPoints.Last.MarkerStyle = MarkerStyle.Square
                                    If Double.IsNaN(HomePageInsulinRow) Then
                                        markerSeriesPoints.Last.Color = Color.Transparent
                                        markerSeriesPoints.Last.MarkerSize = 0
                                    Else
                                        markerSeriesPoints.Last.Color = Color.FromArgb(30, Color.LightBlue)
                                        markerSeriesPoints.Last.ToolTip = $"Bolus: {entry(NameOf(InsulinRecord.deliveredFastAmount))} U"
                                    End If
                                Else
                                    Stop
                                End If
                            Case Else
                                Stop
                        End Select
                    Case "MEAL"
                        If markerMealDictionary Is Nothing Then Continue For
                        If markerMealDictionary.TryAdd(markerOADateTime, HomePageMealRow) Then
                            markerSeriesPoints.AddXY(markerOADateTime, HomePageMealRow + (s_mealImage.Height / 2))
                            markerSeriesPoints.Last.Color = Color.FromArgb(10, Color.Yellow)
                            markerSeriesPoints.Last.MarkerBorderWidth = 2
                            markerSeriesPoints.Last.MarkerBorderColor = Color.FromArgb(10, Color.Yellow)
                            markerSeriesPoints.Last.MarkerSize = 20
                            markerSeriesPoints.Last.MarkerStyle = MarkerStyle.Square
                            markerSeriesPoints.Last.ToolTip = $"Meal: {entry("amount")} grams"
                        End If
                    Case "TIME_CHANGE"
                        With pageChart.Series(ChartSupport.TimeChangeSeries).Points
                            lastTimeChangeRecord = New TimeChangeRecord(entry)
                            markerOADateTime = New OADate(lastTimeChangeRecord.GetLatestTime)
                            Call .AddXY(markerOADateTime, 0)
                            Call .AddXY(markerOADateTime, HomePageBasalRow)
                            Call .AddXY(markerOADateTime, Double.NaN)
                        End With
                    Case Else
                        Stop
                End Select
            Catch ex As Exception
                Stop
                '      Throw New Exception($"{ex.DecodeException()} exception in {memberName} at {sourceLineNumber}")
            End Try
        Next
        If s_listOfTimeChangeMarkers.Any Then
            timeChangeSeries.IsVisibleInLegend = True
            pageChart.ChartAreas(NameOf(ChartArea)).AxisX.AdjustXAxisStartTime(lastTimeChangeRecord)
            pageChart.Legends(0).CustomItems.Last.Enabled = True
        Else
            timeChangeSeries.IsVisibleInLegend = False
            pageChart.Legends(0).CustomItems.Last.Enabled = False
        End If
    End Sub

    <Extension>
    Friend Sub PlotSgSeries(chart As Chart, HomePageMealRow As Double)
        For Each sgListIndex As IndexClass(Of SgRecord) In s_listOfSGs.WithIndex()
            chart.Series(BgSeries).PlotOnePoint(
                                    sgListIndex.Value.OaDateTime(),
                                    sgListIndex.Value.sg,
                                    Color.White,
                                    HomePageMealRow)
        Next
    End Sub

    <Extension>
    Friend Sub PlotTreatmentMarkers(treatmentChart As Chart, treatmentMarkerTimeChangeSeries As Series)
        Dim lastTimeChangeRecord As TimeChangeRecord = Nothing
        s_treatmentMarkerInsulinDictionary.Clear()
        s_treatmentMarkerMealDictionary.Clear()
        For Each markerWithIndex As IndexClass(Of Dictionary(Of String, String)) In s_markers.WithIndex()
            Try
                Dim markerDateTime As Date = markerWithIndex.Value.GetMarkerDateTime
                Dim markerOADateTime As New OADate(markerDateTime)
                Dim bgValueString As String = ""
                Dim bgValue As Single
                Dim entry As Dictionary(Of String, String) = markerWithIndex.Value

                If entry.TryGetValue("value", bgValueString) Then
                    bgValueString.TryParseSingle(bgValue)
                End If
                Dim markerSeriesPoints As DataPointCollection = treatmentChart.Series(MarkerSeries).Points
                Select Case entry("type")
                    Case "AUTO_BASAL_DELIVERY", "MANUAL_BASAL_DELIVERY"
                        Dim amount As Single = entry(NameOf(AutoBasalDeliveryRecord.bolusAmount)).ParseSingle(3)
                        With treatmentChart.Series(BasalSeries)
                            Call .DrawBasalMarker(markerOADateTime,
                                                  amount,
                                                  MaxBasalPerDose,
                                                  TreatmentInsulinRow,
                                                  GetGraphLineColor("Basal Series"),
                                                  True,
                                                  GetToolTip(entry("type"), amount))

                        End With
                    Case "INSULIN"
                        Select Case entry(NameOf(InsulinRecord.activationType))
                            Case "AUTOCORRECTION"
                                Dim autoCorrection As String = entry(NameOf(InsulinRecord.deliveredFastAmount))
                                With treatmentChart.Series(BasalSeries)
                                    .DrawBasalMarker(markerOADateTime, autoCorrection.ParseSingle(3), MaxBasalPerDose, TreatmentInsulinRow, GetGraphLineColor("Auto Correction"), True, $"Auto Correction: {autoCorrection.TruncateSingleString(3)} U")
                                End With
                            Case "MANUAL", "RECOMMENDED", "UNDETERMINED"
                                If s_treatmentMarkerInsulinDictionary.TryAdd(markerOADateTime, TreatmentInsulinRow) Then
                                    markerSeriesPoints.AddXY(markerOADateTime, TreatmentInsulinRow)
                                    markerSeriesPoints.Last.MarkerBorderWidth = 2
                                    markerSeriesPoints.Last.MarkerBorderColor = Color.FromArgb(10, Color.Black)
                                    markerSeriesPoints.Last.MarkerSize = 20
                                    markerSeriesPoints.Last.MarkerStyle = MarkerStyle.Square
                                    If Double.IsNaN(HomePageInsulinRow) Then
                                        markerSeriesPoints.Last.Color = Color.Transparent
                                        markerSeriesPoints.Last.MarkerSize = 0
                                    Else
                                        markerSeriesPoints.Last.Color = Color.FromArgb(30, Color.LightBlue)
                                        markerSeriesPoints.Last.ToolTip = $"Bolus: {entry(NameOf(InsulinRecord.deliveredFastAmount))} U"
                                    End If
                                Else
                                    Stop
                                End If
                            Case Else
                                Stop
                        End Select
                    Case "MEAL"
                        Dim mealRow As Single = CSng(TreatmentInsulinRow * 0.95).RoundSingle(3)
                        If s_treatmentMarkerMealDictionary.TryAdd(markerOADateTime, mealRow) Then
                            markerSeriesPoints.AddXY(markerOADateTime, mealRow)
                            markerSeriesPoints.Last.Color = Color.FromArgb(10, Color.Yellow)
                            markerSeriesPoints.Last.MarkerBorderWidth = 2
                            markerSeriesPoints.Last.MarkerBorderColor = Color.FromArgb(10, Color.Yellow)
                            markerSeriesPoints.Last.MarkerSize = 20
                            markerSeriesPoints.Last.MarkerStyle = MarkerStyle.Square
                            markerSeriesPoints.Last.ToolTip = $"Meal: {entry("amount")} grams"
                        End If
                    Case "BG_READING",
                         "CALIBRATION"
                    Case "TIME_CHANGE"
                        With treatmentChart.Series(TimeChangeSeries).Points
                            lastTimeChangeRecord = New TimeChangeRecord(entry)
                            markerOADateTime = New OADate(lastTimeChangeRecord.GetLatestTime)
                            .AddXY(markerOADateTime, 0)
                            .AddXY(markerOADateTime, TreatmentInsulinRow)
                            .AddXY(markerOADateTime, Double.NaN)
                        End With
                    Case Else
                        Stop
                End Select
            Catch ex As Exception
                Stop
                '      Throw New Exception($"{ex.DecodeException()} exception in {memberName} at {sourceLineNumber}")
            End Try
        Next
        If s_listOfTimeChangeMarkers.Any Then
            treatmentMarkerTimeChangeSeries.IsVisibleInLegend = True
            treatmentChart.ChartAreas(NameOf(ChartArea)).AxisX.AdjustXAxisStartTime(lastTimeChangeRecord)
            treatmentChart.Legends(0).CustomItems.Last.Enabled = True
        Else
            treatmentMarkerTimeChangeSeries.IsVisibleInLegend = False
            treatmentChart.Legends(0).CustomItems.Last.Enabled = False
        End If

    End Sub

    Private Function GetToolTip(type As String, amount As Single) As String
        Dim minBasalMsg As String = ""
        If amount.IsMinBasal() Then
            minBasalMsg = "Min "
        End If

        If type = "AUTO_BASAL_DELIVERY" Then
            Return $"{minBasalMsg}Auto Basal: {amount} U"
        Else
            Return $"{minBasalMsg}Manual Basal: {amount} U"
        End If
    End Function

    <Extension>
    Friend Sub PostPaintSupport(e As ChartPaintEventArgs, ByRef chartRelativePosition As RectangleF, insulinDictionary As Dictionary(Of OADate, Single), mealDictionary As Dictionary(Of OADate, Single), offsetInsulinImage As Boolean, paintOnY2 As Boolean)
        If s_listOfSGs.Count = 0 Then Exit Sub

        If chartRelativePosition.IsEmpty Then
            chartRelativePosition.X = CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.X, s_listOfSGs(0).OaDateTime))
            chartRelativePosition.Y = CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.Y2, HomePageBasalRow))
            chartRelativePosition.Height = CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.Y2, CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.Y2, s_limitHigh)))) - chartRelativePosition.Y
            chartRelativePosition.Width = CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.X, s_listOfSGs.Last.OaDateTime)) - chartRelativePosition.X
        End If

        Dim highLimitY As Single = CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.Y2, s_limitHigh))
        Dim lowLimitY As Single = CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.Y2, s_limitLow))
        Dim criticalLowLimitY As Single = CSng(e.ChartGraphics.GetPositionFromAxis(NameOf(ChartArea), AxisName.Y2, s_criticalLow))
        Dim chartAbsoluteHighRectangle As RectangleF = e.ChartGraphics.GetAbsoluteRectangle(New RectangleF(chartRelativePosition.X, chartRelativePosition.Y, chartRelativePosition.Width, highLimitY - chartRelativePosition.Y))
        Dim chartAbsoluteLowRectangle As RectangleF = e.ChartGraphics.GetAbsoluteRectangle(New RectangleF(chartRelativePosition.X, lowLimitY, chartRelativePosition.Width, criticalLowLimitY - lowLimitY))

        Using b As New SolidBrush(Color.FromArgb(5, Color.Black))
            e.ChartGraphics.Graphics.FillRectangle(b, chartAbsoluteHighRectangle)
            e.ChartGraphics.Graphics.FillRectangle(b, chartAbsoluteLowRectangle)
        End Using

        If insulinDictionary IsNot Nothing Then
            e.PaintMarker(s_insulinImage, insulinDictionary, offsetInsulinImage, paintOnY2)
        End If
        If mealDictionary IsNot Nothing Then
            e.PaintMarker(s_mealImage, mealDictionary, False, paintOnY2)
        End If
    End Sub

    <Extension>
    Public Function IsMinBasal(amount As Single) As Boolean
        Return Math.Abs(amount - 0.025) < 0.001
    End Function

    <Extension>
    Public Function IsMinBasal(amount As String) As Boolean
        Return amount = "0.025"
    End Function

End Module
