﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports System.IO
Imports System.Text.Json
Imports System.Windows.Forms.DataVisualization.Charting

Public Class Form1
    Public WithEvents ActiveInsulinTabChart As Chart
    Public WithEvents CurrentBGSeries As Series
    Public WithEvents HighLimitSeries As Series
    Public WithEvents HomePageChart As Chart
    Public WithEvents LowLimitSeries As Series
    Public WithEvents MarkerSeries As Series
    Public WithEvents TimeInRangeChart As Chart
    Private Const InsulinRow As Integer = 50

    Private Const MarkerRow As Integer = 400

    Private Const MilitaryTimeWithMinuteFormat As String = "HH:mm"
    Private Const TwelveHourTimeWithMinuteFormat As String = "h:mm tt"

    Private Shared ReadOnly s_alwaysFilter As New List(Of String) From {
        "kind",
        "relativeOffset",
        "version"
        }

    Private Shared ReadOnly s_lastAlarmFilter As New List(Of String) From {
        "code",
        "GUID",
        "instanceId",
        "kind",
        "referenceGUID",
        "relativeOffset",
        "version"
        }

    Private Shared ReadOnly s_markersFilter As New List(Of String) From {
        "id",
        "index",
        "kind",
        "relativeOffset",
        "version"
        }

    Private Shared ReadOnly s_notificationHistoryFilter As New List(Of String) From {
        "faultId",
        "GUID",
        "id",
        "index",
        "instanceId",
        "kind",
        "referenceGUID",
        "relativeOffset",
        "version"
        }

    Private ReadOnly _bgMiniDisplay As New BGMiniWindow

    Private ReadOnly _calibrationToolTip As New ToolTip()

    Private ReadOnly _careLinkSnapshotDocPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CareLinkSnapshot.json")

    Private ReadOnly _insulinImage As Bitmap = My.Resources.InsulinVial_Tiny

    Private ReadOnly _listOfSingleItems As New List(Of Integer) From {
                        ItemIndexs.lastSG,
                        ItemIndexs.lastAlarm,
                        ItemIndexs.activeInsulin,
                        ItemIndexs.limits,
                        ItemIndexs.markers,
                        ItemIndexs.notificationHistory,
                        ItemIndexs.basal}

    Private ReadOnly _loginDialog As New LoginForm1
    Private ReadOnly _markerInsulinDictionary As New Dictionary(Of Double, Integer)
    Private ReadOnly _markerMealDictionary As New Dictionary(Of Double, Integer)
    Private ReadOnly _mealImage As Bitmap = My.Resources.MealImage
    Private ReadOnly _savedTitle As String = Me.Text
    Private ReadOnly _sensorLifeToolTip As New ToolTip()
    Private _activeInsulinIncrements As Integer

    Private _client As CareLinkClient

    Private _filterJsonData As Boolean = True

    Private _imagePosition As RectangleF = RectangleF.Empty

    Private _initialized As Boolean = False

    Private _limithigh As Single

    Private _limitLow As Single

    Private _recentData As Dictionary(Of String, String)

    Private _recentDatalast As Dictionary(Of String, String)

    Private _recentDataSameCount As Integer
    Private _timeFormat As String

    Private _updating As Boolean = False

    ' do not rename or move up
    Friend Shared ReadOnly s_zFilterList As New Dictionary(Of Integer, List(Of String)) From {
        {ItemIndexs.lastAlarm, s_lastAlarmFilter},
        {ItemIndexs.lastSG, s_alwaysFilter},
        {ItemIndexs.markers, s_markersFilter},
        {ItemIndexs.notificationHistory, s_notificationHistoryFilter}
        }

    Public ReadOnly _FiveMinuteSpan As New TimeSpan(hours:=0, minutes:=5, seconds:=0)
    Public ReadOnly _ThirtySecondInMilliseconds As Integer = CInt(New TimeSpan(0, 0, seconds:=30).TotalMilliseconds)

#Region "Chart Objects"

    Private _activeInsulinTabChartArea As ChartArea
    Private _homePageChartChartArea As ChartArea

#End Region

#Region "Variables to hold Pump Values"

    Public AboveHyperLimit As Integer
    Public ActiveInsulin As Dictionary(Of String, String)
    Public AverageSG As Double
    Public AverageSGFloat As Double
    Public Basal As Dictionary(Of String, String)
    Public BelowHypoLimit As Integer
    Public BgUnits As String
    Public CalibStatus As String
    Public ClientTimeZoneName As String
    Public ConduitBatteryLevel As Integer
    Public ConduitBatteryStatus As String
    Public ConduitInRange As Boolean
    Public ConduitMedicalDeviceInRange As Boolean
    Public ConduitSensorInRange As Boolean
    Public ConduitSerialNumber As String
    Public CurrentServerTime As String
    Public FirstName As String
    Public GstBatteryLevel As Integer
    Public GstCommunicationState As Boolean
    Public Kind As String
    Public LastAlarm As Dictionary(Of String, String)
    Public LastConduitDateTime As String
    Public LastConduitTime As String
    Public LastConduitUpdateServerTime As String
    Public LastMedicalDeviceDataUpdateServerTime As String
    Public LastName As String
    Public LastSensorTime As String
    Public LastSensorTS As String
    Public LastSensorTSAsString As String
    Public LastSG As Dictionary(Of String, String)
    Public LastSGTrend As String
    Public Limits As List(Of Dictionary(Of String, String))
    Public Markers As List(Of Dictionary(Of String, String))
    Public MaxAutoBasalRate As Double
    Public MaxBolusAmount As Double
    Public MedicalDeviceBatteryLevelPercent As Integer
    Public MedicalDeviceFamily As String
    Public MedicalDeviceSerialNumber As String
    Public MedicalDeviceSuspended As Boolean
    Public MedicalDeviceTime As String
    Public MedicalDeviceTimeAsString As String
    Public PumpBannerState As List(Of Dictionary(Of String, String))
    Public PumpCommunicationState As Boolean
    Public PumpModelNumber As String
    Public ReservoirAmount As Double
    Public ReservoirLevelPercent As Integer
    Public ReservoirRemainingUnits As Double
    Public SensorDurationHours As Integer
    Public SensorDurationMinutes As Integer
    Public SensorState As String
    Public SgBelowLimit As Integer
    Public SGs As New List(Of SgRecord)
    Public SLastSensorTime As Date
    Public SMedicalDeviceTime As Date
    Public SystemStatusMessage As String
    Public TherapyAlgorithmState As Dictionary(Of String, String)
    Public TimeFormat As String
    Public TimeInRange As Integer
    Public TimeToNextCalibHours As UShort = UShort.MaxValue
    Public TimeToNextCalibrationMinutes As Integer
    Public Version As String
    Public timeToNextCalibrationRecommendedMinutes As UShort
    Public calFreeSensor As Boolean
    Public finalCalibration As Boolean
    Public Property BgUnitsString As String

#End Region

    ' Do not rename these name are matched used in case sensitive matching
    Public Enum ItemIndexs As Integer
        lastSensorTS = 0
        medicalDeviceTimeAsString = 1
        lastSensorTSAsString = 2
        kind = 3
        version = 4
        pumpModelNumber = 5
        currentServerTime = 6
        lastConduitTime = 7
        lastConduitUpdateServerTime = 8
        lastMedicalDeviceDataUpdateServerTime = 9
        firstName = 10
        lastName = 11
        conduitSerialNumber = 12
        conduitBatteryLevel = 13
        conduitBatteryStatus = 14
        conduitInRange = 15
        conduitMedicalDeviceInRange = 16
        conduitSensorInRange = 17
        medicalDeviceFamily = 18
        sensorState = 19
        medicalDeviceSerialNumber = 20
        medicalDeviceTime = 21
        sMedicalDeviceTime = 22
        reservoirLevelPercent = 23
        reservoirAmount = 24
        reservoirRemainingUnits = 25
        medicalDeviceBatteryLevelPercent = 26
        sensorDurationHours = 27
        timeToNextCalibHours = 28
        calibStatus = 29
        bgUnits = 30
        timeFormat = 31
        lastSensorTime = 32
        sLastSensorTime = 33
        medicalDeviceSuspended = 34
        lastSGTrend = 35
        lastSG = 36
        lastAlarm = 37
        activeInsulin = 38
        sgs = 39
        limits = 40
        markers = 41
        notificationHistory = 42
        therapyAlgorithmState = 43
        pumpBannerState = 44
        basal = 45
        systemStatusMessage = 46
        averageSG = 47
        belowHypoLimit = 48
        aboveHyperLimit = 49
        timeInRange = 50
        pumpCommunicationState = 51
        gstCommunicationState = 52
        gstBatteryLevel = 53
        lastConduitDateTime = 54
        maxAutoBasalRate = 55
        maxBolusAmount = 56
        sensorDurationMinutes = 57
        timeToNextCalibrationMinutes = 58
        clientTimeZoneName = 59
        sgBelowLimit = 60
        averageSGFloat = 61
        timeToNextCalibrationRecommendedMinutes = 62
        calFreeSensor = 63
        finalCalibration = 65
    End Enum

#Region "Events"

    Private Shared Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        End
    End Sub

    Private Shared Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        End
    End Sub

    Private Shared Sub SGsDataGridView_ColumnAdded(sender As Object, e As DataGridViewColumnEventArgs) Handles SGsDataGridView.ColumnAdded
        With e.Column
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            .ReadOnly = True
            .Resizable = DataGridViewTriState.False
            .HeaderText = .Name.ToTitleCase()
            .DefaultCellStyle = SgRecord.GetCellStyle(.Name)
            If .Name <> NameOf(SgRecord.RecordNumber) Then
                .SortMode = DataGridViewColumnSortMode.NotSortable
            End If
        End With
    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        AboutBox1.Show()
    End Sub

    Private Sub AITComboBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AITComboBox.SelectedIndexChanged
        Dim aitTimeSpan As TimeSpan = TimeSpan.Parse(Me.AITComboBox.SelectedItem.ToString())
        My.Settings.AIT = aitTimeSpan
        My.Settings.Save()
        _activeInsulinIncrements = CInt(TimeSpan.Parse(aitTimeSpan.ToString("hh\:mm").Substring(1)) / _FiveMinuteSpan)
        Me.UpdateActiveInsulinChart()
    End Sub

    Private Sub CalibrationDueImage_MouseHover(sender As Object, e As EventArgs) Handles CalibrationDueImage.MouseHover
        If TimeToNextCalibrationMinutes > 0 AndAlso TimeToNextCalibrationMinutes < 1440 Then
            _calibrationToolTip.SetToolTip(Me.CalibrationDueImage, $"Calibration Due {Now.AddMinutes(TimeToNextCalibrationMinutes).ToShortTimeString}")
        End If
    End Sub

    Private Sub CursorTimer_Tick(sender As Object, e As EventArgs) Handles CursorTimer.Tick
        If Not _homePageChartChartArea.AxisX.ScaleView.IsZoomed Then
            Me.CursorTimer.Enabled = False
            _homePageChartChartArea.CursorX.Position = Double.NaN
        End If
    End Sub

    Private Sub FilterRawJSONDataToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FilterRawJSONDataToolStripMenuItem.Click
        _filterJsonData = Me.FilterRawJSONDataToolStripMenuItem.Checked
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Me.NotifyIcon1.Visible = False
        Me.NotifyIcon1.Icon.Dispose()
        Me.NotifyIcon1.Icon = Nothing
        Me.NotifyIcon1.Visible = False
        Me.NotifyIcon1.Dispose()
        Application.DoEvents()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load
        ' Load all settings
        If My.Settings.UpgradeRequired Then
            My.Settings.Upgrade()
            My.Settings.UpgradeRequired = False
            My.Settings.Save()
        End If
        Me.ShieldUnitsLabel.Parent = Me.ShieldPictureBox
        Me.ShieldUnitsLabel.BackColor = Color.Transparent
        Me.SensorDaysLeftLabel.Parent = Me.SensorTimeLefPictureBox
        Me.SensorDaysLeftLabel.BackColor = Color.Transparent
        Me.SensorDaysLeftLabel.Left = (Me.SensorTimeLefPictureBox.Width \ 2) - (Me.SensorDaysLeftLabel.Width \ 2)
        Me.SensorDaysLeftLabel.Top = (Me.SensorTimeLefPictureBox.Height \ 2) - (Me.SensorDaysLeftLabel.Height \ 2)
        Me.AITComboBox.SelectedIndex = Me.AITComboBox.FindStringExact(My.Settings.AIT.ToString("hh\:mm").Substring(1))
        _activeInsulinIncrements = CInt(TimeSpan.Parse(My.Settings.AIT.ToString("hh\:mm").Substring(1)) / _FiveMinuteSpan)
        Me.InitializeHomePageChart()
        Me.InitializeActiveInsulinTabChart()
        Me.InitializeTimeInRangeChart()
        Me.SGsDataGridView.AutoGenerateColumns = True
        Me.SGsDataGridView.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle With {
            .Alignment = DataGridViewContentAlignment.MiddleCenter
            }

        Me.SnapshotLoadToolStripMenuItem.Enabled = File.Exists(_careLinkSnapshotDocPath)

        If My.Settings.UseTestData Then
            Me.UseLastSavedDataToolStripMenuItem.Checked = False
            Me.UseTestDataToolStripMenuItem.Checked = True
        ElseIf My.Settings.UseLastSavedData AndAlso Me.SnapshotLoadToolStripMenuItem.Enabled Then
            Me.UseLastSavedDataToolStripMenuItem.Checked = True
            Me.UseTestDataToolStripMenuItem.Checked = False
        Else
            Me.DoOptionalLoginAndUpdateData()
        End If
    End Sub

    Private Sub HomePageChart_CursorPositionChanging(sender As Object, e As CursorEventArgs) Handles HomePageChart.CursorPositionChanging
        If Not _initialized Then Exit Sub
        Me.CursorTimer.Interval = _ThirtySecondInMilliseconds
        Me.CursorTimer.Start()
    End Sub

    Private Sub HomePageChart_MouseMove(sender As Object, e As MouseEventArgs) Handles HomePageChart.MouseMove

        If Not _initialized Then
            Exit Sub
        End If
        Dim yInPixels As Double = Me.HomePageChart.ChartAreas("Default").AxisY2.ValueToPixelPosition(e.Y)
        If Double.IsNaN(yInPixels) Then
            Exit Sub
        End If
        Dim result As HitTestResult
        Try
            result = Me.HomePageChart.HitTest(e.X, e.Y)
        Catch ex As Exception
            result = Nothing
        End Try
        If result?.PointIndex >= -1 Then
            If result.Series IsNot Nothing Then
                Me.CursorTimeLabel.Left = e.X - (Me.CursorTimeLabel.Width \ 2)
                Select Case result.Series.Name
                    Case NameOf(HighLimitSeries), NameOf(LowLimitSeries)
                        Me.CursorMessage1Label.Visible = False
                        Me.CursorMessage2Label.Visible = False
                        Me.CursorPictureBox.Image = Nothing
                        Me.CursorTimeLabel.Visible = False
                        Me.CursorValueLabel.Visible = False
                    Case NameOf(MarkerSeries)
                        Dim marketToolTip() As String = result.Series.Points(result.PointIndex).ToolTip.Split(","c)
                        Dim xValue As Date = Date.FromOADate(result.Series.Points(result.PointIndex).XValue)
                        Me.CursorTimeLabel.Visible = True
                        Me.CursorTimeLabel.Text = xValue.ToString(_timeFormat)
                        Me.CursorTimeLabel.Tag = xValue
                        marketToolTip(0) = marketToolTip(0).Trim
                        Me.CursorValueLabel.Visible = True
                        Me.CursorPictureBox.SizeMode = PictureBoxSizeMode.StretchImage
                        Me.CursorPictureBox.Visible = True
                        Select Case marketToolTip.Length
                            Case 2
                                Me.CursorMessage1Label.Text = marketToolTip(0)
                                Select Case marketToolTip(0)
                                    Case "Basal"
                                        Me.CursorPictureBox.Image = My.Resources.InsulinVial
                                    Case "Bolus"
                                        Me.CursorPictureBox.Image = My.Resources.InsulinVial
                                    Case "Meal"
                                        Me.CursorPictureBox.Image = My.Resources.MealImageLarge
                                    Case Else
                                        Me.CursorPictureBox.Image = Nothing
                                End Select
                                Me.CursorMessage2Label.Visible = False
                                Me.CursorValueLabel.Top = Me.CursorMessage1Label.PositionBelow
                                Me.CursorValueLabel.Text = marketToolTip(1).Trim
                            Case 3
                                Select Case marketToolTip(1).Trim
                                    Case "Calibration accepted", "Calibration not accepted"
                                        Me.CursorPictureBox.Image = My.Resources.CalibrationDotRed
                                    Case "Not used For calibration"
                                        Me.CursorPictureBox.Image = My.Resources.CalibrationDot
                                    Case Else
                                        Stop
                                End Select
                                Me.CursorMessage1Label.Text = marketToolTip(0)
                                Me.CursorMessage1Label.Top = Me.CursorPictureBox.PositionBelow
                                Me.CursorMessage2Label.Text = marketToolTip(1).Trim
                                Me.CursorMessage2Label.Top = Me.CursorMessage1Label.PositionBelow
                                Me.CursorMessage2Label.Visible = True
                                Me.CursorValueLabel.Text = marketToolTip(2).Trim
                                Me.CursorValueLabel.Top = Me.CursorMessage2Label.PositionBelow
                            Case Else
                                Stop
                        End Select
                    Case "Default"
                        Me.CursorPictureBox.Image = Nothing
                        Me.CursorMessage2Label.Visible = False
                        Me.CursorValueLabel.Visible = False
                        Me.CursorTimeLabel.Text = Date.FromOADate(result.Series.Points(result.PointIndex).XValue).ToString(_timeFormat)
                        Me.CursorTimeLabel.Visible = True
                        Me.CursorMessage1Label.Text = $"{result.Series.Points(result.PointIndex).YValues(0).RoundDouble(3)} {Me.BgUnitsString}"
                        Me.CursorMessage1Label.Visible = True
                End Select
            End If
        Else
            Me.CursorMessage1Label.Visible = False
            Me.CursorMessage2Label.Visible = False
            Me.CursorPictureBox.Image = Nothing
            Me.CursorTimeLabel.Visible = False
            Me.CursorValueLabel.Visible = False
        End If
    End Sub

    Private Sub HomePageChart_PostPaint(sender As Object, e As ChartPaintEventArgs) Handles HomePageChart.PostPaint
        If Not _initialized Then Exit Sub
        If _imagePosition = Rectangle.Empty Then
            _imagePosition.X = CSng(e.ChartGraphics.GetPositionFromAxis("Default", AxisName.X, SGs(0).datetime.ToOADate))
            _imagePosition.Y = CSng(e.ChartGraphics.GetPositionFromAxis("Default", AxisName.Y, 400))
            _imagePosition.Height = CSng(e.ChartGraphics.GetPositionFromAxis("Default", AxisName.Y, CSng(e.ChartGraphics.GetPositionFromAxis("Default", AxisName.Y, _limithigh)))) - _imagePosition.Y
            _imagePosition.Width = CSng(e.ChartGraphics.GetPositionFromAxis("Default", AxisName.X, SGs.Last.datetime.ToOADate)) - _imagePosition.X
            _imagePosition = e.ChartGraphics.GetAbsoluteRectangle(_imagePosition)
        End If

        Dim highAreaRectangle As New Rectangle(New Point(CInt(_imagePosition.X), CInt(_imagePosition.Y)), New Size(CInt(_imagePosition.Width), 292))

        Using b As New SolidBrush(Color.FromArgb(30, Color.Black))
            e.ChartGraphics.Graphics.FillRectangle(b, highAreaRectangle)
        End Using
        Dim lowHeight As Integer = If(_homePageChartChartArea.AxisX.ScrollBar.IsVisible, CInt(25 - _homePageChartChartArea.AxisX.ScrollBar.Size), 25)
        Dim lowAreaRectangle As New Rectangle(New Point(CInt(_imagePosition.X), 504), New Size(CInt(_imagePosition.Width), lowHeight))
        Using b As New SolidBrush(Color.FromArgb(30, Color.Black))
            e.ChartGraphics.Graphics.FillRectangle(b, lowAreaRectangle)
        End Using
        If Me.CursorTimeLabel.Tag IsNot Nothing Then
            Me.CursorTimeLabel.Left = CInt(e.ChartGraphics.GetPositionFromAxis("Default", AxisName.X, CDate(Me.CursorTimeLabel.Tag).ToOADate))
        End If

        e.PaintMarker(_mealImage, _markerMealDictionary, 0)
        e.PaintMarker(_insulinImage, _markerInsulinDictionary, -6)
    End Sub

    Private Sub LoginToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoginToolStripMenuItem.Click
        Me.DoOptionalLoginAndUpdateData()
    End Sub

    Private Sub SensorAgeLeftLabel_MouseHover(sender As Object, e As EventArgs) Handles SensorDaysLeftLabel.MouseHover
        If SensorDurationHours < 24 Then
            _sensorLifeToolTip.SetToolTip(Me.CalibrationDueImage, $"Sensor will expire in {SensorDurationHours} hours")
        End If
    End Sub

    Private Sub ServerUpdateTimer_Tick(sender As Object, e As EventArgs) Handles ServerUpdateTimer.Tick
        Me.ServerUpdateTimer.Stop()
        Me.WatchdogTimer.Stop()
        Me.WatchdogTimer.Interval = CType(New TimeSpan(0, minutes:=6, 0).TotalMilliseconds, Integer)
        Me.WatchdogTimer.Start()
        Debug.Print($"WatchdogTimer Started at {Now}")
        _recentData = _client.GetRecentData()
        If Me.IsRecentDataUpdated Then
            Me.UpdateAllTabPages()
        ElseIf _recentData Is Nothing Then
            _client = New CareLinkClient(Me.LoginStatus, My.Settings.CareLinkUserName, My.Settings.CareLinkPassword, My.Settings.CountryCode)
            _loginDialog.Client = _client
            _recentData = _client.GetRecentData()
            If Me.IsRecentDataUpdated Then
                Me.UpdateAllTabPages()
            End If
        End If
        Application.DoEvents()
        Me.ServerUpdateTimer.Interval = CType(New TimeSpan(0, minutes:=1, 0).TotalMilliseconds, Integer)
        Me.ServerUpdateTimer.Start()
        Debug.Print($"Me.ServerUpdateTimer Started at {Now}")
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub SetupEmailServerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SetupEmailServerToolStripMenuItem.Click
        MailSetupDialog.ShowDialog()
    End Sub

    Private Sub SGsDataGridView_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs) Handles SGsDataGridView.CellFormatting
        ' Set the background to red for negative values in the Balance column.
        If Me.SGsDataGridView.Columns(e.ColumnIndex).Name.Equals(NameOf(SensorState), StringComparison.OrdinalIgnoreCase) Then
            If CStr(e.Value) <> "NO_ERROR_MESSAGE" Then
                e.CellStyle.BackColor = Color.Yellow
            End If
        End If
        If Me.SGsDataGridView.Columns(e.ColumnIndex).Name.Equals(NameOf(DateTime), StringComparison.OrdinalIgnoreCase) Then
            If e.Value IsNot Nothing Then
                Dim dateValue As Date = CDate(e.Value)
                e.Value = $"{dateValue.ToShortDateString()} {dateValue.ToShortTimeString()}"
            End If
        End If
        If Me.SGsDataGridView.Columns(e.ColumnIndex).Name.Equals(NameOf(SgRecord.sg), StringComparison.OrdinalIgnoreCase) Then
            If e.Value IsNot Nothing AndAlso CSng(e.Value) > 0 Then
                If CSng(e.Value) < 70 Then
                    e.CellStyle.BackColor = Color.Red
                ElseIf CSng(e.Value) > 180 Then
                    e.CellStyle.BackColor = Color.Orange
                End If
            End If
        End If

    End Sub

    Private Sub SGsDataGridView_ColumnHeaderMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles SGsDataGridView.ColumnHeaderMouseClick
        Dim currentSortOrder As SortOrder = Me.SGsDataGridView.Columns(e.ColumnIndex).HeaderCell.SortGlyphDirection
        If Me.SGsDataGridView.Columns(e.ColumnIndex).Name = NameOf(SgRecord.RecordNumber) Then
            If currentSortOrder = SortOrder.None OrElse currentSortOrder = SortOrder.Ascending Then
                Me.SGsDataGridView.DataSource = SGs.OrderByDescending(Function(x) x.RecordNumber).ToList
                currentSortOrder = SortOrder.Descending
            Else
                Me.SGsDataGridView.DataSource = SGs.OrderBy(Function(x) x.RecordNumber).ToList
                currentSortOrder = SortOrder.Ascending
            End If
        End If
        Me.SGsDataGridView.Columns(e.ColumnIndex).HeaderCell.SortGlyphDirection = currentSortOrder
    End Sub

    Private Sub ShowMiniDisplayToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ShowMiniDisplayToolStripMenuItem.Click
        Me.Hide()
        _bgMiniDisplay.Show()
    End Sub

    Private Sub SnapshotLoadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SnapshotLoadToolStripMenuItem.Click
        If File.Exists(_careLinkSnapshotDocPath) Then
            Me.ServerUpdateTimer.Stop()
            _recentData = Loads(File.ReadAllText(_careLinkSnapshotDocPath))
            Me.Text &= $"{_savedTitle} Using Last CareLink Snapshot"
            Me.UpdateAllTabPages()
        End If
    End Sub

    Private Sub SnapshotSaveToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SnapshotSaveToolStripMenuItem.Click
        Dim contents As String = JsonSerializer.Serialize(_recentData, New JsonSerializerOptions)
        Dim options As New JsonDocumentOptions
        Using jd As JsonDocument = JsonDocument.Parse(contents, options)
            File.WriteAllText(_careLinkSnapshotDocPath, JsonSerializer.Serialize(jd, CareLinkClient.JsonFormattingOptions))
        End Using
        Me.SnapshotLoadToolStripMenuItem.Enabled = True
    End Sub

    Private Sub UseLastSavedDataToolStripMenuItem_CheckStateChanged(sender As Object, e As EventArgs) Handles UseLastSavedDataToolStripMenuItem.CheckStateChanged
        If Me.UseLastSavedDataToolStripMenuItem.Checked Then
            Me.UseTestDataToolStripMenuItem.Checked = False
            My.Settings.UseLastSavedData = True
            My.Settings.UseTestData = False
            Me.DoOptionalLoginAndUpdateData()
        Else
            My.Settings.UseLastSavedData = False
        End If
        Me.SnapshotSaveToolStripMenuItem.Enabled = Me.UseLastSavedDataToolStripMenuItem.Checked

        My.Settings.Save()
        If _initialized AndAlso Not (Me.UseTestDataToolStripMenuItem.Checked OrElse Me.UseLastSavedDataToolStripMenuItem.Checked) Then
            Me.DoOptionalLoginAndUpdateData()
        End If
    End Sub

    Private Sub UseTestDataToolStripMenuItem_Checkchange(sender As Object, e As EventArgs) Handles UseTestDataToolStripMenuItem.CheckStateChanged
        If Me.UseTestDataToolStripMenuItem.Checked Then
            Me.UseLastSavedDataToolStripMenuItem.Checked = False
            My.Settings.UseLastSavedData = False
            My.Settings.UseTestData = True
            Me.DoOptionalLoginAndUpdateData()
        Else
            My.Settings.UseTestData = False
        End If
        My.Settings.Save()
        If _initialized AndAlso Not (Me.UseTestDataToolStripMenuItem.Checked OrElse Me.UseLastSavedDataToolStripMenuItem.Checked) Then
            Me.DoOptionalLoginAndUpdateData()
        End If
    End Sub

    Private Sub WatchdogTimer_Tick(sender As Object, e As EventArgs) Handles WatchdogTimer.Tick
        Me.WatchdogTimer.Stop()
        MsgBox("Watchdog Timed Out", MsgBoxStyle.Critical, "Critical Error")
    End Sub

#End Region

    Private Sub DoOptionalLoginAndUpdateData()
        Me.ServerUpdateTimer.Stop()
        Debug.Print($"Me.ServerUpdateTimer stopped at {Now}")
        If Me.UseTestDataToolStripMenuItem.Checked Then
            Me.ViewToolStripMenuItem.Visible = False
            Me.Text = $"{_savedTitle} Using Test Data"
            _recentData = Loads(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleUserData.json")))
        ElseIf Me.UseLastSavedDataToolStripMenuItem.Checked Then
            Me.ViewToolStripMenuItem.Visible = False
            Me.Text &= $"{_savedTitle} Using Last Saved Data"
            _recentData = Loads(File.ReadAllText(CareLinkClient.CareLinkLastDownloadDocPath))
        Else
            Me.Text = _savedTitle
            _loginDialog.ShowDialog()
            _client = _loginDialog.Client
            If _client Is Nothing OrElse Not _client.LoggedIn Then
                Exit Sub
            End If
            _recentData = _client.GetRecentData()
            Me.ViewToolStripMenuItem.Visible = True
            Me.WatchdogTimer.Interval = CType(New TimeSpan(0, minutes:=6, 0).TotalMilliseconds, Integer)
            Me.WatchdogTimer.Start()
            Debug.Print($"Me.WatchdogTimer Started at {Now}")
            Me.ServerUpdateTimer.Interval = CType(New TimeSpan(0, minutes:=1, 0).TotalMilliseconds, Integer)
            Me.ServerUpdateTimer.Start()
            Debug.Print($"Me.ServerUpdateTimer Started at {Now}")
            Me.LoginStatus.Text = "OK"
        End If
        Me.UpdateAllTabPages()
    End Sub

    Private Sub FillOneRowOfTableLayoutPannel(layoutPanel As TableLayoutPanel, innerJson As List(Of Dictionary(Of String, String)), rowIndex As ItemIndexs, filterJsonData As Boolean, timeFormat As String)
        For Each jsonEntry As IndexClass(Of Dictionary(Of String, String)) In innerJson.WithIndex()
            Dim tableLevel1Blue As New TableLayoutPanel With {
                    .Anchor = AnchorStyles.Left Or AnchorStyles.Right,
                    .AutoScroll = False,
                    .AutoSize = True,
                    .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    .ColumnCount = 2,
                    .Dock = DockStyle.Fill,
                    .Margin = New Padding(0),
                    .Name = "InnerTable",
                    .Padding = New Padding(0)
                }
            layoutPanel.Controls.Add(tableLevel1Blue, column:=1, row:=jsonEntry.Index)
            GetInnerTable(Me, jsonEntry.Value, tableLevel1Blue, CType(rowIndex, ItemIndexs), filterJsonData, timeFormat)
            Application.DoEvents()
        Next
    End Sub

#Region "Initialize Charts"

    Private Sub InitializeActiveInsulinTabChart()
        Me.ActiveInsulinTabChart = New Chart With {
             .Anchor = AnchorStyles.Left Or AnchorStyles.Right,
             .BackColor = Color.WhiteSmoke,
             .BackGradientStyle = GradientStyle.TopBottom,
             .BackSecondaryColor = Color.White,
             .BorderlineColor = Color.FromArgb(26, 59, 105),
             .BorderlineDashStyle = ChartDashStyle.Solid,
             .BorderlineWidth = 2,
             .Dock = DockStyle.Fill,
             .Name = "chart1",
             .TabIndex = 0
         }

        _activeInsulinTabChartArea = New ChartArea With {
             .BackColor = Color.FromArgb(180, 23, 47, 19),
             .BackGradientStyle = GradientStyle.TopBottom,
             .BackSecondaryColor = Color.FromArgb(180, 29, 56, 26),
             .BorderColor = Color.FromArgb(64, 64, 64, 64),
             .BorderDashStyle = ChartDashStyle.Solid,
             .Name = "Default",
             .ShadowColor = Color.Transparent
         }

        With _activeInsulinTabChartArea
            .AxisX.IsInterlaced = True
            .AxisX.IsMarginVisible = True
            .AxisX.LabelAutoFitStyle = LabelAutoFitStyles.IncreaseFont Or LabelAutoFitStyles.DecreaseFont Or LabelAutoFitStyles.WordWrap
            .AxisX.LabelStyle.Font = New Font("Trebuchet MS", 8.25F, FontStyle.Bold)
            .AxisX.LineColor = Color.FromArgb(64, 64, 64, 64)
            .AxisX.MajorGrid.LineColor = Color.FromArgb(64, 64, 64, 64)
            .AxisX.ScaleView.Zoomable = True
            .AxisX.ScrollBar.BackColor = Color.White
            .AxisX.ScrollBar.ButtonColor = Color.Lime
            .AxisX.ScrollBar.IsPositionedInside = True
            .AxisX.ScrollBar.LineColor = Color.Yellow
            .AxisX.ScrollBar.LineColor = Color.Black
            .AxisX.ScrollBar.Size = 15
            .AxisY.InterlacedColor = Color.FromArgb(120, Color.LightSlateGray)
            .AxisY.Interval = 2
            .AxisY.IntervalAutoMode = IntervalAutoMode.FixedCount
            .AxisY.IsInterlaced = True
            .AxisY.IsLabelAutoFit = False
            .AxisY.IsMarginVisible = False
            .AxisY.IsStartedFromZero = True
            .AxisY.LabelStyle.Font = New Font("Trebuchet MS", 8.25F, FontStyle.Bold)
            .AxisY.LineColor = Color.FromArgb(64, 64, 64, 64)
            .AxisY.MajorGrid.LineColor = Color.FromArgb(64, 64, 64, 64)
            .AxisY.MajorTickMark = New TickMark() With {.Interval = InsulinRow, .Enabled = False}
            .AxisY.Maximum = 25
            .AxisY.Minimum = 0
            .AxisY.ScaleView.Zoomable = False
            .AxisY.Title = "Active Insulin"
            .AxisY.TitleForeColor = Color.HotPink
            .AxisY2.Maximum = 400
            .AxisY2.Minimum = 0
            .AxisY2.Title = "BG Value"
            .CursorX.AutoScroll = True
            .CursorX.AxisType = AxisType.Primary
            .CursorX.Interval = 0
            .CursorX.IsUserEnabled = True
            .CursorX.IsUserSelectionEnabled = True
            .CursorY.AutoScroll = False
            .CursorY.AxisType = AxisType.Secondary
            .CursorY.Interval = 0
            .CursorY.IsUserEnabled = False
            .CursorY.IsUserSelectionEnabled = False
            .CursorY.LineColor = Color.Transparent
        End With

        Me.ActiveInsulinTabChart.ChartAreas.Add(_activeInsulinTabChartArea)

        Me.ActiveInsulinTabChart.Legends.Add(New Legend With {
                                     .BackColor = Color.Transparent,
                                     .Enabled = False,
                                     .Font = New Font("Trebuchet MS", 8.25F, FontStyle.Bold),
                                     .IsTextAutoFit = False,
                                     .Name = "Default"
                                     }
                                  )
        Me.ActiveInsulinTabChart.Series.Add(New Series With {
                                    .BorderColor = Color.FromArgb(180, 26, 59, 105),
                                    .BorderWidth = 4,
                                    .ChartArea = "Default",
                                    .ChartType = SeriesChartType.Line,
                                    .Color = Color.HotPink,
                                    .Legend = "Default",
                                    .Name = "Default",
                                    .ShadowColor = Color.Black,
                                    .XValueType = ChartValueType.DateTime,
                                    .YAxisType = AxisType.Primary
                                    })
        Me.ActiveInsulinTabChart.Series.Add(New Series With {
                                    .BorderColor = Color.FromArgb(180, 26, 59, 105),
                                    .BorderWidth = 4,
                                    .ChartArea = "Default",
                                    .ChartType = SeriesChartType.Line,
                                    .Color = Color.Blue,
                                    .Legend = "Default",
                                    .Name = NameOf(CurrentBGSeries),
                                    .ShadowColor = Color.Black,
                                    .XValueType = ChartValueType.DateTime,
                                    .YAxisType = AxisType.Secondary
                                    })
        Me.ActiveInsulinTabChart.Series.Add(New Series With {
                                                .BorderColor = Color.Transparent,
                                                .BorderWidth = 1,
                                                .ChartArea = "Default",
                                                .ChartType = SeriesChartType.Point,
                                                .Color = Color.HotPink,
                                                .Name = NameOf(MarkerSeries),
                                                .MarkerSize = 8,
                                                .MarkerStyle = MarkerStyle.Circle,
                                                .XValueType = ChartValueType.DateTime,
                                                .YAxisType = AxisType.Primary
                                                })
        Me.ActiveInsulinTabChart.Series("Default").EmptyPointStyle.Color = Color.Transparent
        Me.ActiveInsulinTabChart.Series("Default").EmptyPointStyle.BorderWidth = 4
        Me.ActiveInsulinTabChart.Titles.Add(New Title With {
                                    .Font = New Font("Trebuchet MS", 12.0F, FontStyle.Bold),
                                    .ForeColor = Color.FromArgb(26, 59, 105),
                                    .Name = "Title1",
                                    .ShadowColor = Color.FromArgb(32, 0, 0, 0),
                                    .ShadowOffset = 3
                                    }
                                 )
        Me.TabPage2RunningActiveInsulin.Controls.Add(Me.ActiveInsulinTabChart)
        Application.DoEvents()

    End Sub

    Private Sub InitializeHomePageChart()
        Me.HomePageChart = New Chart With {
             .Anchor = AnchorStyles.Left Or AnchorStyles.Right,
             .BackColor = Color.WhiteSmoke,
             .BackGradientStyle = GradientStyle.TopBottom,
             .BackSecondaryColor = Color.White,
             .BorderlineColor = Color.FromArgb(26, 59, 105),
             .BorderlineDashStyle = ChartDashStyle.Solid,
             .BorderlineWidth = 2,
             .Location = New Point(3, Me.ShieldPictureBox.Height + 23),
             .Name = "chart1",
             .Size = New Size(Me.TabPage1HomePage.ClientSize.Width - 240, Me.TabPage1HomePage.ClientSize.Height - (Me.ShieldPictureBox.Height + Me.ShieldPictureBox.Top + 26)),
             .TabIndex = 0
         }

        _homePageChartChartArea = New ChartArea With {
             .BackColor = Color.FromArgb(180, 23, 47, 19),
             .BackGradientStyle = GradientStyle.TopBottom,
             .BackSecondaryColor = Color.FromArgb(180, 29, 56, 26),
             .BorderColor = Color.FromArgb(64, 64, 64, 64),
             .BorderDashStyle = ChartDashStyle.Solid,
             .Name = "Default",
             .ShadowColor = Color.Transparent
         }
        With _homePageChartChartArea
            .AxisX.IsInterlaced = True
            .AxisX.IsMarginVisible = True
            .AxisX.LabelAutoFitStyle = LabelAutoFitStyles.IncreaseFont Or LabelAutoFitStyles.DecreaseFont Or LabelAutoFitStyles.WordWrap
            .AxisX.LabelStyle.Font = New Font("Trebuchet MS", 8.25F, FontStyle.Bold)
            .AxisX.LineColor = Color.FromArgb(64, 64, 64, 64)
            .AxisX.MajorGrid.LineColor = Color.FromArgb(64, 64, 64, 64)
            .AxisX.ScaleView.Zoomable = True
            .AxisX.ScrollBar.BackColor = Color.White
            .AxisX.ScrollBar.ButtonColor = Color.Lime
            .AxisX.ScrollBar.IsPositionedInside = True
            .AxisX.ScrollBar.LineColor = Color.Yellow
            .AxisX.ScrollBar.LineColor = Color.Black
            .AxisX.ScrollBar.Size = 15
            .AxisY.InterlacedColor = Color.FromArgb(120, Color.LightSlateGray)
            .AxisY.Interval = InsulinRow
            .AxisY.IntervalAutoMode = IntervalAutoMode.FixedCount
            .AxisY.IsInterlaced = True
            .AxisY.IsLabelAutoFit = False
            .AxisY.IsMarginVisible = False
            .AxisY.IsStartedFromZero = False
            .AxisY.LabelStyle.Font = New Font("Trebuchet MS", 8.25F, FontStyle.Bold)
            .AxisY.LineColor = Color.FromArgb(64, 64, 64, 64)
            .AxisY.MajorGrid.LineColor = Color.FromArgb(64, 64, 64, 64)
            .AxisY.MajorTickMark = New TickMark() With {.Interval = InsulinRow, .Enabled = False}
            .AxisY.Maximum = 400
            .AxisY.Minimum = InsulinRow
            .AxisY.ScaleBreakStyle = New AxisScaleBreakStyle() With {
                .Enabled = True,
                .StartFromZero = StartFromZero.No,
                .BreakLineStyle = BreakLineStyle.Straight
                }
            .AxisY.ScaleView.Zoomable = False
            .AxisY2.Interval = InsulinRow
            .AxisY2.IsMarginVisible = False
            .AxisY2.IsStartedFromZero = False
            .AxisY2.LabelStyle.Font = New Font("Trebuchet MS", 8.25F, FontStyle.Bold)
            .AxisY2.LineColor = Color.FromArgb(64, 64, 64, 64)
            .AxisY2.MajorGrid = New Grid With {
                .Interval = InsulinRow,
                .LineColor = Color.FromArgb(64, 64, 64, 64)
            }
            .AxisY2.MajorTickMark = New TickMark() With {.Interval = InsulinRow, .Enabled = True}
            .AxisY2.Maximum = 400
            .AxisY2.Minimum = InsulinRow
            .AxisY2.ScaleView.Zoomable = False
            .CursorX.AutoScroll = True
            .CursorX.AxisType = AxisType.Primary
            .CursorX.Interval = 0
            .CursorX.IsUserEnabled = True
            .CursorX.IsUserSelectionEnabled = True
            .CursorY.AutoScroll = False
            .CursorY.AxisType = AxisType.Secondary
            .CursorY.Interval = 0
            .CursorY.IsUserEnabled = False
            .CursorY.IsUserSelectionEnabled = False
            .CursorY.LineColor = Color.Transparent
        End With

        Me.HomePageChart.ChartAreas.Add(_homePageChartChartArea)
        Me.HomePageChart.Legends.Add(New Legend With {
                                     .BackColor = Color.Transparent,
                                     .Enabled = False,
                                     .Font = New Font("Trebuchet MS", 8.25F, FontStyle.Bold),
                                     .IsTextAutoFit = False,
                                     .Name = "Default"
                                     }
                                  )
        Me.HomePageChart.Series.Add(New Series With {
                                    .BorderColor = Color.FromArgb(180, 26, 59, 105),
                                    .BorderWidth = 4,
                                    .ChartArea = "Default",
                                    .ChartType = SeriesChartType.Line,
                                    .Color = Color.White,
                                    .Legend = "Default",
                                    .Name = "Default",
                                    .ShadowColor = Color.Black,
                                    .XValueType = ChartValueType.DateTime,
                                    .YAxisType = AxisType.Secondary
                                    })
        Me.MarkerSeries = New Series With {
            .BorderColor = Color.Transparent,
            .BorderWidth = 1,
            .ChartArea = "Default",
            .ChartType = SeriesChartType.Point,
            .Color = Color.HotPink,
            .Name = NameOf(MarkerSeries),
            .MarkerSize = 12,
            .MarkerStyle = MarkerStyle.Circle,
            .XValueType = ChartValueType.DateTime,
            .YAxisType = AxisType.Secondary
            }

        Me.HomePageChart.Series.Add(Me.MarkerSeries)

        Me.HighLimitSeries = New Series With {
                                    .BorderColor = Color.FromArgb(180, Color.Orange),
                                    .BorderWidth = 2,
                                    .ChartArea = "Default",
                                    .ChartType = SeriesChartType.StepLine,
                                    .Color = Color.Orange,
                                    .Name = NameOf(HighLimitSeries),
                                    .ShadowColor = Color.Black,
                                    .XValueType = ChartValueType.DateTime,
                                    .YAxisType = AxisType.Secondary
                                    }
        Me.HomePageChart.Series.Add(Me.HighLimitSeries)
        Me.LowLimitSeries = New Series With {
                                    .BorderColor = Color.FromArgb(180, Color.Red),
                                    .BorderWidth = 2,
                                    .ChartArea = "Default",
                                    .ChartType = SeriesChartType.StepLine,
                                    .Color = Color.Red,
                                    .Name = NameOf(LowLimitSeries),
                                    .ShadowColor = Color.Black,
                                    .XValueType = ChartValueType.DateTime,
                                    .YAxisType = AxisType.Secondary
                                    }
        Me.HomePageChart.Series.Add(Me.LowLimitSeries)
        Me.HomePageChart.Series("Default").EmptyPointStyle.Color = Color.Transparent
        Me.HomePageChart.Series("Default").EmptyPointStyle.BorderWidth = 4
        Me.HomePageChart.Titles.Add(New Title With {
                                    .Font = New Font("Trebuchet MS", 12.0F, FontStyle.Bold),
                                    .ForeColor = Color.FromArgb(26, 59, 105),
                                    .Name = "Title1",
                                    .ShadowColor = Color.FromArgb(32, 0, 0, 0),
                                    .ShadowOffset = 3
                                    }
                                 )
        Me.TabPage1HomePage.Controls.Add(Me.HomePageChart)
        Application.DoEvents()

    End Sub

    Private Sub InitializeTimeInRangeChart()
        Me.TimeInRangeChart = New Chart With {
            .Anchor = AnchorStyles.Right,
            .BackColor = Color.Transparent,
            .BackGradientStyle = GradientStyle.None,
            .BackSecondaryColor = Color.Transparent,
            .BorderlineColor = Color.Transparent,
            .BorderlineWidth = 0,
            .Size = New Size(220, 220)
        }

        With Me.TimeInRangeChart
            .BorderSkin.BackSecondaryColor = Color.Transparent
            .BorderSkin.SkinStyle = BorderSkinStyle.None
            .ChartAreas.Add(New ChartArea With {.Name = "TimeInRangeChartChartArea",
                                                  .BackColor = Color.Black})
            .Location = New Point(Me.TimeInRangeSummaryLabel.FindHorizontalMidpoint - (.Width \ 2),
                                                     Me.TimeInRangeSummaryLabel.FindVerticalMidpoint() - (.Height \ 2))
            .Name = "Default"
            .Series.Add(New Series With {
                                              .ChartArea = "TimeInRangeChartChartArea",
                                              .ChartType = SeriesChartType.Doughnut,
                                              .Name = "Default"})
            .Series("Default")("DoughnutRadius") = "20"
        End With

        Me.TimeInRangeChart.Titles.Add(New Title With {
                                        .Name = "TimeInRangeChartTitle",
                                        .Text = "Time In Range Last 24 Hours"}
                                    )
        Me.TabPage1HomePage.Controls.Add(Me.TimeInRangeChart)
        Application.DoEvents()
    End Sub

#End Region

#Region "Update Data/Tables"

    Private Sub GetLimitsList(ByRef limitsIndexList As Integer())

        Dim limitsIndex As Integer = 0
        For i As Integer = 0 To limitsIndexList.GetUpperBound(0)
            If limitsIndex + 1 < Limits.Count AndAlso CInt(Limits(limitsIndex + 1)("index")) < i Then
                limitsIndex += 1
            End If
            limitsIndexList(i) = limitsIndex
        Next
    End Sub

    Private Function IsRecentDataUpdated() As Boolean
        If _recentDatalast Is Nothing OrElse _recentData Is Nothing Then
            Return False
        End If
        If _recentDataSameCount < 5 Then
            _recentDataSameCount += 1
            For i As Integer = 0 To _recentData.Keys.Count - 1
                If _recentDatalast.Keys(i) <> "currentServerTime" AndAlso _recentDatalast.Values(i) <> _recentData.Values(i) Then
                    _recentDataSameCount = 0
                    Return True
                End If
            Next
            Return False
        End If
        _recentDataSameCount = 0
        Return True
    End Function

    Private Sub UpdateActiveInsulinChart()
        If Not _initialized Then
            Exit Sub
        End If
        _initialized = False

        With Me.ActiveInsulinTabChart
            .Titles("Title1").Text = $"Running Active Insulin in Pink"
            .ChartAreas("Default").AxisX.Minimum = SGs(0).datetime.ToOADate()
            .ChartAreas("Default").AxisX.Maximum = SGs.Last.datetime.ToOADate()
            .Series("Default").Points.Clear()
            .Series(NameOf(CurrentBGSeries)).Points.Clear()
            .Series(NameOf(MarkerSeries)).Points.Clear()
            .ChartAreas("Default").AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Hours
            .ChartAreas("Default").AxisX.MajorGrid.IntervalOffsetType = DateTimeIntervalType.Hours
            .ChartAreas("Default").AxisX.MajorGrid.Interval = 1
            .ChartAreas("Default").AxisX.IntervalType = DateTimeIntervalType.Hours
            .ChartAreas("Default").AxisX.Interval = 2
        End With

        ' Order all markers by time
        Dim timeOrderedMarkers As New SortedDictionary(Of Double, Double)
        Dim sgOaDateTime As Double
        For Each sgListIndex As IndexClass(Of Dictionary(Of String, String)) In Markers.WithIndex()
            sgOaDateTime = Markers.SafeGetSgDateTime(sgListIndex.Index).RoundDown(RoundTo.Minute).ToOADate
            Select Case sgListIndex.Value("type")
                Case "INSULIN"
                    Dim bolusAmount As Double = sgListIndex.Value.GetDecimalValue("programmedFastAmount")
                    If timeOrderedMarkers.ContainsKey(sgOaDateTime) Then
                        timeOrderedMarkers(sgOaDateTime) += bolusAmount
                    Else
                        timeOrderedMarkers.Add(sgOaDateTime, bolusAmount)
                    End If
                Case "AUTO_BASAL_DELIVERY"
                    Dim bolusAmount As Double = sgListIndex.Value.GetDecimalValue("bolusAmount")
                    If timeOrderedMarkers.ContainsKey(sgOaDateTime) Then
                        timeOrderedMarkers(sgOaDateTime) += bolusAmount
                    Else
                        timeOrderedMarkers.Add(sgOaDateTime, bolusAmount)
                    End If
            End Select
        Next

        ' set up table that holds active insulin for every 5 minutes
        Dim remainingInsulinList As New List(Of Insulin)
        Dim currentMarker As Integer = 0
        Dim getSgDateTime As Date = SGs(0).datetime

        For i As Integer = 0 To 287
            Dim initialBolus As Double = 0
            Dim oaTime As Double = (getSgDateTime + (_FiveMinuteSpan * i)).RoundDown(RoundTo.Minute).ToOADate()
            While currentMarker < timeOrderedMarkers.Count AndAlso timeOrderedMarkers.Keys(currentMarker) <= oaTime
                initialBolus += timeOrderedMarkers.Values(currentMarker)
                currentMarker += 1
            End While
            remainingInsulinList.Add(New Insulin(oaTime, initialBolus, _activeInsulinIncrements))
        Next

        _activeInsulinTabChartArea.AxisY2.Maximum = 400

        ' walk all markers, adjust active insulin and then add new marker
        Dim maxActiveInsulin As Double = 0
        For i As Integer = 0 To remainingInsulinList.Count - 1
            If i < _activeInsulinIncrements Then
                Me.ActiveInsulinTabChart.Series("Default").Points.AddXY(remainingInsulinList(i).OaTime, Double.NaN)
                Me.ActiveInsulinTabChart.Series("Default").Points.Last.IsEmpty = True
                If i > 0 Then
                    remainingInsulinList.Adjustlist(0, i)
                End If
                Continue For
            End If
            Dim startIndex As Integer = i - _activeInsulinIncrements + 1
            Dim sum As Double = remainingInsulinList.ConditionalSum(startIndex, _activeInsulinIncrements)
            maxActiveInsulin = Math.Max(sum, maxActiveInsulin)
            Me.ActiveInsulinTabChart.Series("Default").Points.AddXY(remainingInsulinList(i).OaTime, sum)
            remainingInsulinList.Adjustlist(startIndex, _activeInsulinIncrements)
        Next
        _activeInsulinTabChartArea.AxisY.Maximum = Math.Ceiling(maxActiveInsulin) + 1
        maxActiveInsulin = _activeInsulinTabChartArea.AxisY.Maximum

        For Each sgListIndex As IndexClass(Of Dictionary(Of String, String)) In Markers.WithIndex()
            sgOaDateTime = Markers.SafeGetSgDateTime(sgListIndex.Index).RoundDown(RoundTo.Minute).ToOADate
            With Me.ActiveInsulinTabChart.Series(NameOf(MarkerSeries))
                Select Case sgListIndex.Value("type")
                    Case "INSULIN"
                        .Points.AddXY(sgOaDateTime, maxActiveInsulin)
                        Select Case sgListIndex.Value("activationType")
                            Case "AUTOCORRECTION"
                                .Points.Last.ToolTip = $"Auto Correction, {sgListIndex.Value("programmedFastAmount")} U"
                                .Points.Last.Color = Color.MediumPurple
                            Case "RECOMMENDED", "UNDETERMINED"
                                .Points.Last.ToolTip = $"Bolus, {sgListIndex.Value("programmedFastAmount")} U"
                                .Points.Last.Color = Color.LightBlue
                            Case Else
                                Stop
                        End Select
                        .Points.Last.MarkerSize = 15
                        .Points.Last.MarkerStyle = MarkerStyle.Square

                    Case "AUTO_BASAL_DELIVERY"
                        Dim bolusAmount As Double = sgListIndex.Value.GetDecimalValue("bolusAmount")
                        .Points.AddXY(sgOaDateTime, maxActiveInsulin)
                        .Points.Last.ToolTip = $"Basal, {bolusAmount.RoundDouble(3)} U"
                        .Points.Last.MarkerSize = 8
                End Select
            End With
        Next
        For Each sgListIndex As IndexClass(Of SgRecord) In SGs.WithIndex()
            Dim bgValue As Single = sgListIndex.Value.sg

            PlotOnePoint(Me.ActiveInsulinTabChart.Series(NameOf(CurrentBGSeries)), sgListIndex.Value.datetime.ToOADate(), sgListIndex.Value.sg, Color.Black, InsulinRow, _limithigh, _limitLow)
        Next
        _initialized = True
        Application.DoEvents()
    End Sub

    Private Sub UpdateAllTabPages()
        If _recentData Is Nothing OrElse _updating Then
            Exit Sub
        End If
        _updating = True
        Me.UpdateDataTables(_recentData)
        If _recentData.Count > ItemIndexs.finalCalibration + 1 Then
            Stop
        End If
        _initialized = True
        Me.UpdateActiveInsulinChart()
        If Not _initialized Then
            Exit Sub
        End If
        _initialized = False
        Me.UpdateActiveInsulin()
        Me.UpdateAutoModeShield()
        Me.UpdateCalibrationTimeRemaining()
        Me.UpdateInsulinLevel()
        Me.UpdatePumpBattery()
        Me.UpdateRemainingInsulin()
        Me.UpdateSensorLife()
        Me.UpdateTimeInRange()
        Me.UpdateTransmitterBatttery()

        Me.UpdateZHomeTabSerieses()

        Application.DoEvents()
        _recentDatalast = _recentData
        _initialized = True
        _updating = False
    End Sub

    Private Sub UpdateDataTables(localRecentData As Dictionary(Of String, String))
        If localRecentData Is Nothing Then
            Exit Sub
        End If
        Me.Cursor = Cursors.WaitCursor
        Application.DoEvents()
        Me.TableLayoutPanelSummaryData.Controls.Clear()
        Dim rowCount As Integer = Me.TableLayoutPanelSummaryData.RowCount
        Dim newRowCount As Integer = localRecentData.Count - 9
        If rowCount < newRowCount Then
            Me.TableLayoutPanelSummaryData.RowCount = newRowCount
            For i As Integer = rowCount To newRowCount
                Me.TableLayoutPanelSummaryData.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22.0!))
            Next
        End If

        Dim currentRowIndex As Integer = 0
        Dim singleItem As Boolean
        Dim layoutPanel1 As TableLayoutPanel
        For Each c As IndexClass(Of KeyValuePair(Of String, String)) In localRecentData.WithIndex()
            layoutPanel1 = Me.TableLayoutPanelSummaryData
            singleItem = False
            Dim row As KeyValuePair(Of String, String) = c.Value
            Dim rowIndex As ItemIndexs = CType([Enum].Parse(GetType(ItemIndexs), c.Value.Key), ItemIndexs)
            Dim singleItemIndex As ItemIndexs

            Select Case rowIndex
                Case ItemIndexs.lastSensorTS
                    LastSensorTS = row.Value
                Case ItemIndexs.medicalDeviceTimeAsString
                    MedicalDeviceTimeAsString = row.Value
                Case ItemIndexs.lastSensorTSAsString
                    LastSensorTSAsString = row.Value
                Case ItemIndexs.kind
                    Kind = row.Value
                Case ItemIndexs.version
                    Version = row.Value
                Case ItemIndexs.pumpModelNumber
                    PumpModelNumber = row.Value
                Case ItemIndexs.currentServerTime
                    CurrentServerTime = row.Value
                Case ItemIndexs.lastConduitTime
                    LastConduitTime = row.Value
                Case ItemIndexs.lastConduitUpdateServerTime
                    LastConduitUpdateServerTime = row.Value
                Case ItemIndexs.lastMedicalDeviceDataUpdateServerTime
                    LastMedicalDeviceDataUpdateServerTime = row.Value
                Case ItemIndexs.firstName
                    FirstName = row.Value
                Case ItemIndexs.lastName
                    LastName = row.Value
                Case ItemIndexs.conduitSerialNumber
                    ConduitSerialNumber = row.Value
                Case ItemIndexs.conduitBatteryLevel
                    ConduitBatteryLevel = CInt(row.Value)
                Case ItemIndexs.conduitBatteryStatus
                    ConduitBatteryStatus = row.Value
                Case ItemIndexs.conduitInRange
                    ConduitInRange = CBool(row.Value)
                Case ItemIndexs.conduitMedicalDeviceInRange
                    ConduitMedicalDeviceInRange = CBool(row.Value)
                Case ItemIndexs.conduitSensorInRange
                    ConduitSensorInRange = CBool(row.Value)
                Case ItemIndexs.medicalDeviceFamily
                    MedicalDeviceFamily = row.Value
                Case ItemIndexs.sensorState
                    SensorState = row.Value
                Case ItemIndexs.medicalDeviceSerialNumber
                    MedicalDeviceSerialNumber = row.Value
                Case ItemIndexs.medicalDeviceTime
                    MedicalDeviceTime = row.Value
                Case ItemIndexs.sMedicalDeviceTime
                    SMedicalDeviceTime = CDate(row.Value)
                Case ItemIndexs.reservoirLevelPercent
                    ReservoirLevelPercent = CInt(row.Value)
                Case ItemIndexs.reservoirAmount
                    ReservoirAmount = CDbl(row.Value)
                Case ItemIndexs.reservoirRemainingUnits
                    ReservoirRemainingUnits = CType(row.Value, Double)
                Case ItemIndexs.medicalDeviceBatteryLevelPercent
                    MedicalDeviceBatteryLevelPercent = CInt(row.Value)
                Case ItemIndexs.sensorDurationHours
                    SensorDurationHours = CInt(row.Value)
                Case ItemIndexs.timeToNextCalibHours
                    TimeToNextCalibHours = CUShort(row.Value)
                Case ItemIndexs.calibStatus
                    CalibStatus = row.Value
                Case ItemIndexs.bgUnits
                    BgUnits = row.Value
                    Me.BgUnitsString = _unitsStrings(BgUnits)
                    If Me.BgUnitsString = "mg/dl" Then
                        _limithigh = 180
                        _limitLow = 70
                        _homePageChartChartArea.AxisX.LabelStyle.Format = "hh tt"
                        _activeInsulinTabChartArea.AxisX.LabelStyle.Format = "hh tt"
                        _messages("BC_MESSAGE_SG_UNDER_50_MG_DL") = $"Sensor Glucose under 50 {Me.BgUnitsString}"
                    Else
                        _limithigh = 10.0
                        _limitLow = (70 / 18).RoundSingle(1)
                        _activeInsulinTabChartArea.AxisX.LabelStyle.Format = "HH"
                        _messages("BC_MESSAGE_SG_UNDER_50_MG_DL") = $"Sensor Glucose under 2.7 {Me.BgUnitsString}"
                    End If
                    Me.AboveHighLimitMessageLabel.Text = $"Above {_limithigh} {Me.BgUnitsString}"
                    Me.AverageSGUnitsLabel.Text = Me.BgUnitsString
                    Me.BelowLowLimitMessageLabel.Text = $"Below {_limitLow} {Me.BgUnitsString}"
                Case ItemIndexs.timeFormat
                    TimeFormat = row.Value
                    If TimeFormat = "HR_12" Then
                        _timeFormat = TwelveHourTimeWithMinuteFormat
                    Else
                        _timeFormat = MilitaryTimeWithMinuteFormat
                    End If
                Case ItemIndexs.lastSensorTime
                    LastSensorTime = row.Value
                Case ItemIndexs.sLastSensorTime
                    SLastSensorTime = CDate(row.Value)
                Case ItemIndexs.medicalDeviceSuspended
                    MedicalDeviceSuspended = CBool(row.Value)
                Case ItemIndexs.lastSGTrend
                    LastSGTrend = row.Value
                Case ItemIndexs.lastSG
                    layoutPanel1 = Me.TableLayoutPanelTop1
                    layoutPanel1.Controls.Clear()
                    singleItemIndex = ItemIndexs.lastSG
                    layoutPanel1.RowCount = 1
                    singleItem = True
                Case ItemIndexs.lastAlarm
                    layoutPanel1 = Me.TableLayoutPanelTop2
                    layoutPanel1.Controls.Clear()
                    singleItemIndex = ItemIndexs.lastAlarm
                    layoutPanel1.RowCount = 1
                    singleItem = True
                Case ItemIndexs.activeInsulin
                    layoutPanel1 = Me.TableLayoutPanelActiveInsulin
                    layoutPanel1.Controls.Clear()
                    singleItemIndex = ItemIndexs.activeInsulin
                    layoutPanel1.RowCount = 1
                    singleItem = True
                Case ItemIndexs.sgs
                    SGs = LoadList(row.Value).ToSgList()
                    Me.SGsDataGridView.DataSource = SGs
                    For Each column As DataGridViewTextBoxColumn In Me.SGsDataGridView.Columns
                        If _filterJsonData AndAlso s_alwaysFilter.Contains(column.Name) Then
                            Me.SGsDataGridView.Columns(column.Name).Visible = False
                        End If
                    Next
                    Continue For
                Case ItemIndexs.limits
                    layoutPanel1 = Me.TableLayoutPanelLimits
                    layoutPanel1.Controls.Clear()
                    layoutPanel1.AutoSize = True
                    singleItemIndex = ItemIndexs.limits
                    layoutPanel1.RowCount = 1
                    singleItem = True
                Case ItemIndexs.markers
                    layoutPanel1 = Me.TableLayoutPanelMarkers
                    layoutPanel1.Controls.Clear()
                    singleItemIndex = ItemIndexs.markers
                    layoutPanel1.RowCount = 1
                    singleItem = True
                Case ItemIndexs.notificationHistory
                    layoutPanel1 = Me.TableLayoutPanelNotificationHistory
                    layoutPanel1.Controls.Clear()
                    singleItemIndex = ItemIndexs.notificationHistory
                    layoutPanel1.RowCount = 1
                    singleItem = True
                Case ItemIndexs.therapyAlgorithmState
                    ' handled elsewhere
                Case ItemIndexs.pumpBannerState
                    ' handled elsewhere
                Case ItemIndexs.basal
                    layoutPanel1 = Me.TableLayoutPanelBasal
                    layoutPanel1.Controls.Clear()
                    singleItemIndex = ItemIndexs.basal
                    layoutPanel1.RowCount = 1
                    singleItem = True
                Case ItemIndexs.systemStatusMessage
                    SystemStatusMessage = row.Value
                Case ItemIndexs.averageSG
                    AverageSG = CInt(row.Value)
                Case ItemIndexs.belowHypoLimit
                    BelowHypoLimit = CInt(row.Value)
                Case ItemIndexs.aboveHyperLimit
                    AboveHyperLimit = CInt(row.Value)
                Case ItemIndexs.timeInRange
                    TimeInRange = CInt(row.Value)
                Case ItemIndexs.pumpCommunicationState
                    PumpCommunicationState = CBool(row.Value)
                Case ItemIndexs.gstCommunicationState
                    GstCommunicationState = CBool(row.Value)
                Case ItemIndexs.gstBatteryLevel
                    GstBatteryLevel = CInt(row.Value)
                Case ItemIndexs.lastConduitDateTime
                    LastConduitDateTime = row.Value
                Case ItemIndexs.maxAutoBasalRate
                    MaxAutoBasalRate = CDbl(row.Value)
                Case ItemIndexs.maxBolusAmount
                    MaxBolusAmount = CDbl(row.Value)
                Case ItemIndexs.sensorDurationMinutes
                    SensorDurationMinutes = CInt(row.Value)
                Case ItemIndexs.timeToNextCalibrationMinutes
                    TimeToNextCalibrationMinutes = CInt(row.Value)
                Case ItemIndexs.clientTimeZoneName
                    ClientTimeZoneName = row.Value
                Case ItemIndexs.sgBelowLimit
                    SgBelowLimit = CInt(row.Value)
                Case ItemIndexs.averageSGFloat
                    AverageSGFloat = CDbl(row.Value)
                Case ItemIndexs.timeToNextCalibrationRecommendedMinutes
                    timeToNextCalibrationRecommendedMinutes = CUShort(row.Value)
                Case ItemIndexs.calFreeSensor
                    calFreeSensor = CBool(row.Value)
                Case ItemIndexs.finalCalibration
                    finalCalibration = CBool(row.Value)
                Case Else
                    Stop
            End Select

            If _listOfSingleItems.Contains(rowIndex) OrElse singleItem Then
                If Not (singleItem AndAlso singleItemIndex = rowIndex) Then
                    Continue For
                End If
            End If
            Dim tableRelitiveRow As Integer
            If singleItem Then
                tableRelitiveRow = 0
            Else
                tableRelitiveRow = currentRowIndex
                currentRowIndex += 1
            End If
            layoutPanel1.RowStyles(tableRelitiveRow).SizeType = SizeType.AutoSize
            If Not singleItem OrElse rowIndex = ItemIndexs.lastSG OrElse rowIndex = ItemIndexs.lastAlarm Then
                layoutPanel1.Controls.Add(New Label With {
                                                  .Text = $"{CInt(rowIndex)} {row.Key}",
                                                  .Anchor = AnchorStyles.Left Or AnchorStyles.Right,
                                                  .AutoSize = True
                                                  }, 0, tableRelitiveRow)
            End If
            If row.Value?.StartsWith("[") Then
                Dim innerJson As List(Of Dictionary(Of String, String)) = LoadList(row.Value)
                Select Case rowIndex
                    Case ItemIndexs.limits
                        Limits = innerJson
                    Case ItemIndexs.markers
                        Markers = innerJson
                    Case ItemIndexs.notificationHistory
                        ' handled elsewhere
                    Case ItemIndexs.pumpBannerState
                        PumpBannerState = innerJson
                    Case Else
                        Stop
                End Select
                If innerJson.Count > 0 Then
                    layoutPanel1.Parent.Parent.UseWaitCursor = True
                    Application.DoEvents()
                    layoutPanel1.Invoke(Sub()
                                            Me.FillOneRowOfTableLayoutPannel(layoutPanel1,
                                                                          innerJson,
                                                                          rowIndex,
                                                                          _filterJsonData,
                                                                          _timeFormat)
                                        End Sub)
                    Application.DoEvents()

                    layoutPanel1.Parent.Parent.UseWaitCursor = False
                    Application.DoEvents()
                Else
                    Dim rowTextBox As New TextBox With {.Anchor = AnchorStyles.Left Or AnchorStyles.Right,
                                                        .AutoSize = True,
                                                        .[ReadOnly] = True,
                                                        .Text = ""
                                                        }
                    layoutPanel1.Controls.Add(rowTextBox,
                                              If(singleItem, 0, 1),
                                              tableRelitiveRow)

                End If
            ElseIf row.Value?.StartsWith("{") Then
                layoutPanel1.RowStyles(tableRelitiveRow).SizeType = SizeType.AutoSize
                Dim innerJson As Dictionary(Of String, String) = Loads(row.Value)
                Select Case rowIndex
                    Case ItemIndexs.lastSG
                        LastSG = innerJson
                    Case ItemIndexs.lastAlarm
                        LastAlarm = innerJson
                    Case ItemIndexs.activeInsulin
                        ActiveInsulin = innerJson
                    Case ItemIndexs.notificationHistory
                        ' handled elsewhere
                    Case ItemIndexs.therapyAlgorithmState
                        TherapyAlgorithmState = innerJson
                    Case ItemIndexs.basal
                        Basal = innerJson
                    Case Else
                        Stop
                End Select
                Dim tableLevel1Blue As New TableLayoutPanel With {
                        .Anchor = AnchorStyles.Left Or AnchorStyles.Right,
                        .AutoScroll = True,
                        .AutoSize = True,
                        .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        .ColumnCount = 2,
                        .Dock = DockStyle.Fill,
                        .Margin = New Padding(0),
                        .Name = "InnerTable",
                        .Padding = New Padding(0)
                        }
                layoutPanel1.Controls.Add(tableLevel1Blue,
                                          If(singleItem AndAlso Not (rowIndex = ItemIndexs.lastSG OrElse rowIndex = ItemIndexs.lastAlarm), 0, 1),
                                          tableRelitiveRow)
                If rowIndex = ItemIndexs.notificationHistory Then
                    tableLevel1Blue.AutoScroll = False
                End If
                GetInnerTable(Me, innerJson, tableLevel1Blue, rowIndex, _filterJsonData, _timeFormat)
            Else
                Dim rowTextBox As New TextBox With {
                                        .Anchor = AnchorStyles.Left Or AnchorStyles.Right,
                                        .AutoSize = True,
                                        .[ReadOnly] = True,
                                        .Text = row.Value}
                layoutPanel1.Controls.Add(rowTextBox,
                                          If(singleItem, 0, 1),
                                          tableRelitiveRow)
            End If
            Application.DoEvents()
        Next
        Me.Cursor = Cursors.Default
    End Sub

#Region "Home Page Update Utilities"

    Private Sub UpdateActiveInsulin()
        Dim activeInsulinStr As String = $"{ActiveInsulin("amount"):N3}"
        Me.ActiveInsulinValue.Text = $"{activeInsulinStr} U"
        _bgMiniDisplay.ActiveInsulinTextBox.Text = $"Active Insulin {activeInsulinStr}U"
    End Sub

    Private Sub UpdateAutoModeShield()
        Me.SensorMessage.Location = New Point(Me.ShieldPictureBox.Left + (Me.ShieldPictureBox.Width \ 2) - (Me.SensorMessage.Width \ 2), Me.SensorMessage.Top)
        If LastSG("sg") <> "0" Then
            Me.CurrentBG.Location = New Point((Me.ShieldPictureBox.Width \ 2) - (Me.CurrentBG.Width \ 2), Me.ShieldPictureBox.Height \ 4)
            Me.CurrentBG.Parent = Me.ShieldPictureBox
            Me.CurrentBG.Text = LastSG("sg")
            Me.NotifyIcon1.Text = $"{LastSG("sg")} {Me.BgUnitsString}"
            _bgMiniDisplay.CurrentBGString = LastSG("sg")
            Me.CurrentBG.Visible = True
            Me.SensorMessage.Visible = False
            Me.ShieldPictureBox.Image = My.Resources.Shield
            Me.ShieldUnitsLabel.Visible = True
            Me.ShieldUnitsLabel.BackColor = Color.Transparent
            Me.ShieldUnitsLabel.Parent = Me.ShieldPictureBox
            Me.ShieldUnitsLabel.Left = (Me.ShieldPictureBox.Width \ 2) - (Me.ShieldUnitsLabel.Width \ 2)
            Me.ShieldUnitsLabel.Text = Me.BgUnitsString
            Me.ShieldUnitsLabel.Visible = True
        Else
            _bgMiniDisplay.CurrentBGString = "---"
            Me.CurrentBG.Visible = False
            Me.ShieldPictureBox.Image = My.Resources.Shield_Disabled
            Me.SensorMessage.Visible = True
            Me.SensorMessage.Parent = Me.ShieldPictureBox
            Me.SensorMessage.Left = 0
            Me.SensorMessage.BackColor = Color.Transparent
            Dim message As String = ""
            If _messages.TryGetValue(SensorState, message) Then
                message = SensorState.Replace("_", " ")
            Else
                MsgBox($"{SensorState} is unknown sensor message", MsgBoxStyle.OkOnly, $"Form 1 line:{New StackFrame(0, True).GetFileLineNumber()}")
            End If
            Me.SensorMessage.Text = message
            Me.ShieldUnitsLabel.Visible = False
            Me.SensorMessage.Visible = True
        End If
        If _bgMiniDisplay.Visible Then
            _bgMiniDisplay.BGTextBox.SelectionLength = 0
        End If
        Application.DoEvents()
    End Sub

    Private Sub UpdateCalibrationTimeRemaining()
        If TimeToNextCalibHours = Byte.MaxValue Then
            Me.CalibrationDueImage.Image = My.Resources.CalibrationUnavailable
        ElseIf TimeToNextCalibHours < 1 Then
            If SystemStatusMessage = "WAIT_TO_CALIBRATE" OrElse SensorState = "WARM_UP" Then
                Me.CalibrationDueImage.Image = My.Resources.CalibrationNotReady
            Else
                Me.CalibrationDueImage.Image = DrawCenteredArc(My.Resources.CalibrationDotRed, TimeToNextCalibHours, TimeToNextCalibHours / 12)
            End If
        Else
            Me.CalibrationDueImage.Image = DrawCenteredArc(My.Resources.CalibrationDot, TimeToNextCalibHours, TimeToNextCalibHours / 12)
        End If

        Application.DoEvents()
    End Sub

    Private Sub UpdateInsulinLevel()
        If ReservoirLevelPercent = 0 Then
            Me.InsulinLevelPictureBox.Image = Me.ImageList1.Images(0)
            Exit Sub
        End If
        Select Case ReservoirLevelPercent
            Case > 85
                Me.InsulinLevelPictureBox.Image = Me.ImageList1.Images(7)
            Case > 71
                Me.InsulinLevelPictureBox.Image = Me.ImageList1.Images(6)
            Case > 57
                Me.InsulinLevelPictureBox.Image = Me.ImageList1.Images(5)
            Case > 43
                Me.InsulinLevelPictureBox.Image = Me.ImageList1.Images(4)
            Case > 29
                Me.InsulinLevelPictureBox.Image = Me.ImageList1.Images(3)
            Case > 15
                Me.InsulinLevelPictureBox.Image = Me.ImageList1.Images(2)
            Case > 1
                Me.InsulinLevelPictureBox.Image = Me.ImageList1.Images(1)
            Case Else
                Me.InsulinLevelPictureBox.Image = Me.ImageList1.Images(0)
        End Select
        Application.DoEvents()
    End Sub

    Private Sub UpdatePumpBattery()
        If Not ConduitSensorInRange Then
            Me.PumpBatteryPictureBox.Image = My.Resources.PumpBatteryUnknown
            Me.PumpBatteryRemainingLabel.Text = $"Unknown"
            Exit Sub
        End If

        Select Case MedicalDeviceBatteryLevelPercent
            Case > 66
                Me.PumpBatteryPictureBox.Image = My.Resources.PumpBatteryFull
                Me.PumpBatteryRemainingLabel.Text = $"High"
            Case >= 45
                Me.PumpBatteryPictureBox.Image = My.Resources.PumpBatteryMedium
                Me.PumpBatteryRemainingLabel.Text = $"Medium"
            Case > 25
                Me.PumpBatteryPictureBox.Image = My.Resources.PumpBatteryLow
                Me.PumpBatteryRemainingLabel.Text = $"Low"
            Case = 0
                Me.PumpBatteryPictureBox.Image = My.Resources.PumpBatteryCritical
                Me.PumpBatteryRemainingLabel.Text = $"Critical"
        End Select
    End Sub

    Private Sub UpdateRemainingInsulin()
        Me.RemainingInsulinUnits.Text = $"{ReservoirRemainingUnits:N1} U"
    End Sub

    Private Sub UpdateSensorLife()
        If SensorDurationHours = 255 Then
            Me.SensorDaysLeftLabel.Text = $"???"
            Me.SensorTimeLefPictureBox.Image = My.Resources.SensorExpirationUnknown
            Me.SensorTimeLeftLabel.Text = ""
        ElseIf SensorDurationHours >= 24 Then
            Me.SensorDaysLeftLabel.Text = CStr(Math.Ceiling(SensorDurationHours / 24))
            Me.SensorTimeLefPictureBox.Image = My.Resources.SensorLifeOK
            Me.SensorTimeLeftLabel.Text = $"{Me.SensorDaysLeftLabel.Text} Days"
        Else
            If SensorDurationHours = 0 Then
                If SensorDurationMinutes = 0 Then
                    Me.SensorDaysLeftLabel.Text = ""
                    Me.SensorTimeLefPictureBox.Image = My.Resources.SensorExpired
                    Me.SensorTimeLeftLabel.Text = $"Expired"
                Else
                    Me.SensorDaysLeftLabel.Text = $"1"
                    Me.SensorTimeLefPictureBox.Image = My.Resources.SensorLifeNotOK
                    Me.SensorTimeLeftLabel.Text = $"{SensorDurationMinutes} Minutes"
                End If
            Else
                Me.SensorDaysLeftLabel.Text = $"1"
                Me.SensorTimeLefPictureBox.Image = My.Resources.SensorLifeNotOK
                Me.SensorTimeLeftLabel.Text = $"{SensorDurationHours + 1} Hours"
            End If
        End If
        Me.SensorDaysLeftLabel.Visible = True
    End Sub

    Private Sub UpdateTimeInRange()
        With Me.TimeInRangeChart
            .Series("Default").Points.Clear()
            .Series("Default").Points.AddXY($"{AboveHyperLimit}% Above {_limithigh} {Me.BgUnitsString}", AboveHyperLimit / 100)
            .Series("Default").Points.Last().Color = Color.Orange
            .Series("Default").Points.Last().BorderColor = Color.Black
            .Series("Default").Points.Last().BorderWidth = 2
            .Series("Default").Points.AddXY($"{BelowHypoLimit}% Below {_limitLow} {Me.BgUnitsString}", BelowHypoLimit / 100)
            .Series("Default").Points.Last().Color = Color.Red
            .Series("Default").Points.Last().BorderColor = Color.Black
            .Series("Default").Points.Last().BorderWidth = 2
            .Series("Default").Points.AddXY($"{TimeInRange}% In Range", TimeInRange / 100)
            .Series("Default").Points.Last().Color = Color.LawnGreen
            .Series("Default").Points.Last().BorderColor = Color.Black
            .Series("Default").Points.Last().BorderWidth = 2
            .Series("Default")("PieLabelStyle") = "Disabled"
            .Series("Default")("PieStartAngle") = "270"
        End With

        Me.AverageSGValueLabel.Text = If(Me.BgUnitsString = "mg/dl", AverageSG.ToString, AverageSG.RoundDouble(1).ToString())
        Me.AboveHighLimitValueLabel.Text = AboveHyperLimit.ToString()
        Me.BelowLowLimitValueLabel.Text = BelowHypoLimit.ToString()
        Me.TimeInRangeSummaryLabel.Left = Me.TimeInRangeSummaryPercentCharLabel.HorizontalCenterOn(Me.TimeInRangeSummaryLabel)
        Me.TimeInRangeSummaryLabel.Text = TimeInRange.ToString
        Me.TimeInRangeSummaryPercentCharLabel.Left = Me.TimeInRangeChart.HorizontalCenterOn(Me.TimeInRangeSummaryPercentCharLabel)
        Me.TimeInRangeValueLabel.Text = TimeInRange.ToString

    End Sub

    Private Sub UpdateTransmitterBatttery()
        Me.TransmatterBatterPercentLabel.Text = $"{GstBatteryLevel}%"
        If ConduitSensorInRange Then
            Select Case GstBatteryLevel
                Case 100
                    Me.TransmitterBatteryPictureBox.Image = My.Resources.TransmitterBatteryFull
                Case > 50
                    Me.TransmitterBatteryPictureBox.Image = My.Resources.TransmitterBatteryOK
                Case > 20
                    Me.TransmitterBatteryPictureBox.Image = My.Resources.TransmitterBatteryMedium
                Case > 0
                    Me.TransmitterBatteryPictureBox.Image = My.Resources.TransmitterBatteryLow
            End Select
        Else
            Me.TransmitterBatteryPictureBox.Image = My.Resources.TransmitterBatteryUnknown
            Me.TransmatterBatterPercentLabel.Text = $"???"
        End If

    End Sub

    Private Sub UpdateZHomeTabSerieses()
        Me.HomePageChart.Series("Default").Points.Clear()
        Me.HomePageChart.Series(NameOf(MarkerSeries)).Points.Clear()
        Me.HomePageChart.Series(NameOf(HighLimitSeries)).Points.Clear()
        Me.HomePageChart.Series(NameOf(LowLimitSeries)).Points.Clear()
        _markerInsulinDictionary.Clear()
        _markerMealDictionary.Clear()
        For Each sgListIndex As IndexClass(Of Dictionary(Of String, String)) In Markers.WithIndex()
            Dim sgDateTime As Date = Markers.SafeGetSgDateTime(sgListIndex.Index)
            Dim sgOaDateTime As Double = sgDateTime.ToOADate()
            Dim bgValueString As String = ""
            Dim bgValue As Single
            If sgListIndex.Value.TryGetValue("value", bgValueString) Then
                bgValue = CInt(bgValueString)
                If bgValue < InsulinRow Then Stop
            End If
            With Me.HomePageChart.Series(NameOf(MarkerSeries))
                Select Case sgListIndex.Value("type")
                    Case "BG_READING"
                        bgValue = CInt(sgListIndex.Value("value"))
                        .Points.AddXY(sgOaDateTime, bgValue)
                        .Points.Last.BorderColor = Color.Gainsboro
                        .Points.Last.Color = Color.Transparent
                        .Points.Last.MarkerBorderWidth = 2
                        .Points.Last.MarkerSize = 10
                        .Points.Last.ToolTip = $"Blood Glucose, Not used For calibration, {sgListIndex.Value("value")} {Me.BgUnitsString}"
                    Case "CALIBRATION"
                        .Points.AddXY(sgOaDateTime, bgValue)
                        .Points.Last.BorderColor = Color.Red
                        .Points.Last.Color = Color.Transparent
                        .Points.Last.MarkerBorderWidth = 2
                        .Points.Last.MarkerSize = 8
                        .Points.Last.ToolTip = $"Blood Glucose, Calibration {If(CBool(sgListIndex.Value("calibrationSuccess")), "accepted", "not accepted")}, {sgListIndex.Value("value")} {Me.BgUnitsString}"
                    Case "INSULIN"
                        _markerInsulinDictionary.Add(sgOaDateTime, MarkerRow)
                        .Points.AddXY(sgOaDateTime, MarkerRow)
                        Select Case sgListIndex.Value("activationType")
                            Case "AUTOCORRECTION"
                                .Points.Last.Color = Color.FromArgb(60, Color.MediumPurple)
                                .Points.Last.ToolTip = $"Auto Correction, {sgListIndex.Value("programmedFastAmount")} U"
                            Case "RECOMMENDED", "UNDETERMINED"
                                .Points.Last.Color = Color.FromArgb(30, Color.LightBlue)
                                .Points.Last.ToolTip = $"Bolus, {sgListIndex.Value("programmedFastAmount")} U"
                            Case Else
                                Stop
                        End Select
                        .Points.Last.MarkerBorderWidth = 0
                        .Points.Last.MarkerSize = 30
                        .Points.Last.MarkerStyle = MarkerStyle.Square
                    Case "MEAL"
                        _markerMealDictionary.Add(sgOaDateTime, InsulinRow)
                        .Points.AddXY(sgOaDateTime, InsulinRow)
                        .Points.Last.Color = Color.FromArgb(30, Color.Yellow)
                        .Points.Last.MarkerBorderWidth = 0
                        .Points.Last.MarkerSize = 30
                        .Points.Last.MarkerStyle = MarkerStyle.Square
                        .Points.Last.ToolTip = $"Meal, {sgListIndex.Value("amount")} grams"
                    Case "AUTO_BASAL_DELIVERY"
                        .Points.AddXY(sgOaDateTime, MarkerRow)
                        Dim bolusAmount As String = sgListIndex.Value("bolusAmount")
                        .Points.Last.MarkerBorderColor = Color.Black
                        .Points.Last.ToolTip = $"Basal, {bolusAmount.RoundDouble(3)} U"
                    Case "AUTO_MODE_STATUS", "LOW_GLUCOSE_SUSPENDED", "TIME_CHANGE"
                        'Stop
                    Case Else
                        Stop
                End Select
            End With
        Next
        Dim limitsIndexList(SGs.Count - 1) As Integer
        Me.GetLimitsList(limitsIndexList)
        For Each sgListIndex As IndexClass(Of SgRecord) In SGs.WithIndex()
            Dim sgOaDateTime As Double = sgListIndex.Value.datetime.ToOADate()
            PlotOnePoint(Me.HomePageChart.Series("Default"), sgOaDateTime, sgListIndex.Value.sg, Color.White, InsulinRow, _limithigh, _limitLow)
            Dim limitsLowValue As Integer = CInt(Limits(limitsIndexList(sgListIndex.Index))("lowLimit"))
            Dim limitsHighValue As Integer = CInt(Limits(limitsIndexList(sgListIndex.Index))("highLimit"))
            Me.HomePageChart.Series(NameOf(HighLimitSeries)).Points.AddXY(sgOaDateTime, limitsHighValue)
            Me.HomePageChart.Series(NameOf(LowLimitSeries)).Points.AddXY(sgOaDateTime, limitsLowValue)
        Next
    End Sub

#End Region

#End Region

End Class
