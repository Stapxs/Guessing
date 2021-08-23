#Disable Warning IDE1006
Imports System.IO.Compression
Imports System.Net.Sockets
Imports System.Text.RegularExpressions
Imports System.Windows.Threading

Public Class formMain

#Region "Load | 初始化"

    Public Const LoadTitle As String = "        Game Center"
    Public gameMode As String = "NSWC"
    Private Sub LoadForm() Handles Me.Loaded
        frmMain = Me
        Title = LoadTitle
        '读取预存的数据
        If (File.Exists(Directory.GetCurrentDirectory() & "/Info.txt")) Then
            Dim fm As New IO.FileStream(Directory.GetCurrentDirectory() & "/Info.txt", FileMode.Open)
            Dim sr As IO.StreamReader = New IO.StreamReader(fm)
            Dim text As String = sr.ReadLine()
            If Not text = "NULL" Then textLogin.Text = text
            text = sr.ReadLine()
            If Not text = "NULL" Then textIp.Text = text
            text = sr.ReadLine()
            If Not text = "NULL" Then textPoit.Text = text
            fm.Close()
        End If
        '判断首次打开
        If Not (File.Exists(Directory.GetCurrentDirectory() & "/Start.txt")) Then
            Dim sr As New IO.StreamWriter(Directory.GetCurrentDirectory() & "/Start.txt")
            sr.WriteLine("Welcome!这个文件只是用来判断你有没有打开过的……")
            sr.Close()
            If Not isShowUpdate Then
                mainLogin.Visibility = Visibility.Collapsed
                mainUpd.Visibility = Visibility.Visible
                isShowUpdate = True
            End If
        End If
    End Sub

#End Region

#Region "Client | 客户端交流"

    '常量
    Public ClientSocket As Socket
    Public Const ClientLength As Integer = 1024
    Public ClientEncoding As Encoding = Encoding.UTF8
    Public ClientPort As Integer = 233
    Public Const ClientVersion As Integer = 10
    Public isShowUpdate As Boolean = False
    Public isShowCreate As Boolean = False
    Public pingStartTime As Date = Date.Now

    Public ClientIP As String
    Public ClientHeartbeatTimeout As Integer = 5
    Public ClientVerMax = 110

    '发送
    ''' <summary>
    ''' 向服务端发送信息。
    ''' </summary>
    ''' <param name="data">要发送的信息的完整内容。</param>
    ''' <param name="showError">在发送失败时是否抛出异常。</param>
    Public Sub ClientSend(data As String, Optional showError As Boolean = True)
        If Not data.StartsWith("Beat") Then Log("Send: " & data)
        Dim th As New Thread(Sub()
                                 Try
                                     ClientSocket.Send(ClientEncoding.GetBytes(data & "¨"))
                                 Catch ex As Exception
                                     If showError Then Throw ex
                                 End Try
                             End Sub)
        th.Start()
    End Sub

    '心跳包
    Public ClientHeartbeatCount As Integer = 0
    ''' <summary>
    ''' 计量心跳包延迟，并在未收到数据时断开链接。
    ''' </summary>
    Public Sub ClientHeartbeat()
        Dim th As New Thread(Sub()
                                 Do Until UserState = UserStates.Offline
                                     ClientHeartbeatCount += 1
                                     If ClientHeartbeatCount >= ClientHeartbeatTimeout Then ClientExecute("Exit|连接超时")
                                     Thread.Sleep(1000)
                                 Loop
                             End Sub)
        th.Start()
    End Sub

    '执行
    ''' <summary>
    ''' 接受服务端数据。
    ''' </summary>
    Public Sub ClientReceiver()
        Dim th As New Thread(Sub()
                                 Try
                                     Dim bytes(ClientLength) As Byte
                                     Do Until UserState = UserStates.Offline
                                         Dim bytesRec As Integer = ClientSocket.Receive(bytes)
                                         ClientReceive(ClientEncoding.GetString(bytes, 0, bytesRec))
                                     Loop
                                 Catch ex As Exception
                                     Try
                                         ClientReceive("Exit|" & ex.Message)
                                     Catch
                                     End Try
                                 End Try
                             End Sub)
        th.Start()
    End Sub
    ''' <summary>
    ''' 处理含有换行符的服务器原始信息，将分拆后的实际数据移交处理。
    ''' </summary>
    Public Sub ClientReceive(data As String)
        Dim datas() As String = data.Split("¨")
        For Each str As String In datas
            If Not str = "" Then ClientExecute(str)
        Next
    End Sub

#End Region 'Send, Execute, Version

#Region "User | 用户数据"

    ''' <summary>
    ''' 用户名。
    ''' </summary>
    Public UserName As String
    ''' <summary>
    ''' 用户是否为房主。
    ''' </summary>
    Public UserMaster As Boolean = False

    ''' <summary>
    ''' 用户目前的页面。
    ''' </summary>
    Public Property UserState As UserStates
        Get
            Return _UserState
        End Get
        Set(value As UserStates)
            If value = _UserState Then Exit Property
            _UserState = value
            Dispatcher.Invoke(Sub() UserStateChange(value))
        End Set
    End Property
    Private _UserState As UserStates = UserStates.Offline
    Public Enum UserStates As Integer
        ''' <summary>
        ''' 尚未登录。
        ''' </summary>
        Offline
        ''' <summary>
        ''' 正在大厅中。
        ''' </summary>
        Center
        ''' <summary>
        ''' 正在房间中。
        ''' </summary>
        Room
        ''' <summary>
        ''' 正在游戏中。
        ''' </summary>
        Game
    End Enum

    ''' <summary>
    ''' 更新用户页面状态，由 UserState_Set 在 UI 线程中自动触发。
    ''' </summary>
    ''' <param name="newState"></param>
    Private Sub UserStateChange(newState As UserStates)
        Dispatcher.Invoke(Sub()
                              Try
                                  Select Case UserState
                                      Case UserStates.Offline
                                          panLogin.Visibility = Visibility.Visible
                                          panCenter.Visibility = Visibility.Collapsed
                                          panRoom.Visibility = Visibility.Collapsed
                                          panChat.Visibility = Visibility.Collapsed
                                          panList.Visibility = Visibility.Collapsed
                                          panGame.Visibility = Visibility.Collapsed
                                      Case UserStates.Center
                                          panLogin.Visibility = Visibility.Collapsed
                                          panCenter.Visibility = Visibility.Visible
                                          panRoom.Visibility = Visibility.Collapsed
                                          panChat.Visibility = Visibility.Collapsed
                                          panList.Visibility = Visibility.Collapsed
                                          panGame.Visibility = Visibility.Collapsed
                                      Case UserStates.Room
                                          panLogin.Visibility = Visibility.Collapsed
                                          panCenter.Visibility = Visibility.Collapsed
                                          panRoom.Visibility = Visibility.Visible
                                          panChat.Visibility = Visibility.Visible
                                          panList.Visibility = Visibility.Visible
                                          panGame.Visibility = Visibility.Collapsed
                                          'panChat.Margin = New Thickness(279, 46, 25, 25)
                                          panGameSelect.Visibility = Visibility.Hidden
                                      Case UserStates.Game
                                          panLogin.Visibility = Visibility.Collapsed
                                          panCenter.Visibility = Visibility.Collapsed
                                          panRoom.Visibility = Visibility.Collapsed
                                          panChat.Visibility = Visibility.Visible
                                          panList.Visibility = Visibility.Visible
                                          panGame.Visibility = Visibility.Visible
                                  End Select
                              Catch
                              End Try
                          End Sub)
    End Sub

#End Region 'Name, State

#Region "Login | 登录"

    ''' <summary>
    ''' 开始登录。
    ''' </summary>
    Public Sub LoginOn(name As String)
        If btnLogin.IsEnabled = False Then Exit Sub
        If Not UserState = UserStates.Offline Then Exit Sub
        btnLogin.IsEnabled = False
        labLogin.Content = ""
        UserName = name
        Dim th As New Thread(Sub()
                                 Try

#Region "发送登录请求并重新构建 ClientSocket"
                                     '保存登录信息

                                     Dispatcher.Invoke(Sub()
                                                           Dim sr As New IO.StreamWriter(Directory.GetCurrentDirectory() & "/Info.txt")
                                                           If Not textLogin.Text = "" Then
                                                               sr.WriteLine(textLogin.Text)
                                                           Else
                                                               sr.WriteLine("NULL")
                                                           End If
                                                           If Not textIp.Text = "" Then
                                                               sr.WriteLine(textIp.Text)
                                                           Else
                                                               sr.WriteLine("NULL")
                                                           End If
                                                           If Not textPoit.Text = "" Then
                                                               sr.WriteLine(textPoit.Text)
                                                           Else
                                                               sr.WriteLine("NULL")
                                                           End If
                                                           sr.Close()
                                                       End Sub)
                                     '构建 Socket
                                     If Not IsNothing(ClientSocket) Then ClientSocket.Dispose()
                                     Dispatcher.Invoke(Sub()
                                                           Dim text As String = textIp.Text
                                                           '域名解析
                                                           Try
                                                               Dim ip() As IPAddress = Dns.GetHostAddresses(text)
                                                               For Each ipa As IPAddress In ip
                                                                   text = ipa.ToString
                                                               Next
                                                           Catch
                                                           End Try
                                                           ClientIP = text
                                                           ClientPort = CInt(textPoit.Text)
                                                       End Sub)
                                     Dim remoteEP As New IPEndPoint(IPAddress.Parse(ClientIP), ClientPort)
                                     ClientSocket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) With {
                                         .ReceiveTimeout = ClientHeartbeatTimeout * 1000 + 5000,
                                         .SendTimeout = ClientHeartbeatTimeout * 1000 + 5000
                                     }
                                     '连接并发送登录请求
                                     ClientSocket.Connect(remoteEP)
                                     ClientSend("Login|" & ClientVersion & "|" & UserName.Replace("¨", "-").Replace("|", "/") & "|" & NetworkInformation.NetworkInterface.GetAllNetworkInterfaces(0).GetPhysicalAddress.ToString)
                                     '接取信息
                                     Dim bytes(ClientLength) As Byte
                                     Dim bytesRec As Integer = ClientSocket.Receive(bytes)
                                     Dim ret As String = ClientEncoding.GetString(bytes, 0, bytesRec)

#End Region

#Region "登录成功的处理"

                                     If ret.Contains("Center") Then
                                         '转交接受的所有信息
                                         ClientReceive(ret)
                                         '重置心跳包
                                         ClientHeartbeat()
                                         ClientHeartbeatCount = 0
                                         '开始接受数据
                                         ClientReceiver()
                                         '更改标题
                                         Dispatcher.Invoke(Sub() Title = LoadTitle & " - " & UserName)
                                         Exit Sub
                                     End If

#End Region

#Region "登录失败的处理（递交 Exit 请求）"

                                     '由非“Center|”的返回值触发
                                     If ret.StartsWith("Exit|") Then
                                         ClientReceive(ret)
                                     Else
                                         ClientReceive("Exit|未知信息：" & ret.Replace("Error|", ""))
                                     End If
                                 Catch ex As Exception
                                     '由一个 Exception 触发
                                     Try
                                         If ex.GetType.Equals(GetType(SocketException)) Then
                                             If CType(ex, SocketException).ErrorCode = 10061 Then
                                                 ClientReceive("Exit|服务器已关闭")
                                                 Exit Sub
                                             End If
                                         End If
                                         ClientReceive("Exit|" & ex.Message)
                                     Catch
                                     End Try

#End Region

                                 Finally
                                     Try
                                         Dispatcher.Invoke(Sub() btnLogin.IsEnabled = True)
                                     Catch
                                     End Try
                                 End Try
                             End Sub)
        th.Start()
    End Sub
    ''' <summary>
    ''' 退出登录并返回到登录页面。
    ''' </summary>
    Public Sub LoginOff() Handles Me.Closing
        If UserState = UserStates.Offline Then Exit Sub
        Dim th As New Thread(AddressOf LoginOffCode)
        th.Start()
    End Sub
    Private Sub LoginOffCode()
        '由于 Lamuda 语句不支持 OERN，故独立写作一个 Sub
        On Error Resume Next
        '更改标题
        Dispatcher.Invoke(Sub() Title = LoadTitle)
        '向服务器发出退出请求
        ClientSend("Exit", False)
        '更新 UI
        UserState = UserStates.Offline
    End Sub

    '触发登录
    Private Sub btnLogin_Click() Handles btnLogin.Click
        LoginOn(textLogin.Text.Trim)
    End Sub
    Private Sub textLogin_KeyUp(sender As Object, e As KeyEventArgs) Handles textLogin.KeyUp
        If e.Key = Key.Enter And btnLogin.IsEnabled Then btnLogin_Click()
    End Sub

#End Region 'On, Off

#Region "Chat | 聊天框"

    ''' <summary>
    ''' 清空聊天消息。
    ''' </summary>
    Public Sub ChatClear()
        On Error Resume Next
        Dispatcher.Invoke(Sub() listChat.Items.Clear())
    End Sub

    ''' <summary>
    ''' 输出聊天消息。
    ''' </summary>
    Public Sub ChatShow(text As String, isbold As Boolean)
        On Error Resume Next
        Dispatcher.Invoke(Sub()
                              Try
                                  Dim tb As New TextBlock With {
                                    .Text = text,
                                    .FontSize = 14,
                                    .FontWeight = If(isbold, FontWeights.Bold, FontWeights.Normal),
                                    .Tag = GetUUID()
                                  }
                                  tb.Margin = New Thickness With {
                                    .Top = 5,
                                    .Bottom = 5
                                  }
                                  listChat.Items.Add(tb)
                                  If listChat.Items.Count >= 100 Then listChat.Items.RemoveAt(0)
                                  listChat.ScrollIntoView(tb)
                              Catch
                              End Try
                          End Sub)
    End Sub

    ''' <summary>
    ''' 用户主动发送文本框内的聊天消息。
    ''' </summary>
    Private Sub ChatSend() Handles btnChat.Click
        If textChat.Text.Trim = "" Then Exit Sub
        If Strings.Left(textChat.Text.Trim, 1) = "/" Then
            If textChat.Text.Trim = "/me Ping|Start" Or textChat.Text.Trim = ".ping" Or textChat.Text.Trim = ".Ping" Then
                pingStartTime = Date.Now
                Dispatcher.Invoke(Sub()
                                      Try
                                          Dim tb As New TextBlock With {
                                            .Text = "[本地] 发送Ping请求，时间戳：" + Date.Now.ToString,
                                            .FontSize = 14,
                                            .FontWeight = If(True, FontWeights.Bold, FontWeights.Normal),
                                            .Tag = GetUUID()
                                          }
                                          tb.Margin = New Thickness With {
                                            .Top = 5,
                                            .Bottom = 5
                                          }
                                          listChat.Items.Add(tb)
                                          If listChat.Items.Count >= 100 Then listChat.Items.RemoveAt(0)
                                          listChat.ScrollIntoView(tb)
                                      Catch
                                      End Try
                                  End Sub)
            End If
        End If
        ClientSend(New RegularExpressions.Regex("[\u0000-\u001F\u007F-\u00A0]").Replace("Chat|" & textChat.Text.Replace("¨", "-").Replace("|", "/"), ""))
        textChat.Text = ""
    End Sub
    Private Sub textChat_KeyUp(sender As Object, e As KeyEventArgs) Handles textChat.KeyUp
        If e.Key = Key.Enter Then ChatSend()
    End Sub

#End Region 'Show, Clear

#Region "Center | 大厅"

    ''' <summary>
    ''' 选择房间。
    ''' </summary>
    Private Sub listCenter_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles listCenter.SelectionChanged
        btnCenterJoin.IsEnabled = False
        If listCenter.SelectedIndex >= 0 Then btnCenterJoin.IsEnabled = True
    End Sub

    ''' <summary>
    ''' 创建房间。
    ''' </summary>
    Private Sub btnCenterCreate_Click(sender As Object, e As RoutedEventArgs) Handles btnCenterCreate.Click
        isShowCreate = True
        panModeSelect.Visibility = Visibility.Visible
    End Sub

    ''' <summary>
    ''' 加入房间。
    ''' </summary>
    Private Sub btnCenterJoin_Click(sender As Object, e As RoutedEventArgs) Handles btnCenterJoin.Click
        UserMaster = False
        ClientSend("Join|" & listCenter.SelectedIndex)
        btnRoomExit.IsEnabled = True
    End Sub

#End Region

#Region "Room | 房间"

    ''' <summary>
    ''' 退出房间。
    ''' </summary>
    Private Sub btnRoomExit_Click(sender As Object, e As RoutedEventArgs) Handles btnRoomExit.Click, btnGameExit.Click
        If UserMaster And panList.Items.Count > 1 Then
            If MsgBox("你是该房间的房主，退出房间会导致房间被解散，是否确认？", MsgBoxStyle.Exclamation + MsgBoxStyle.OkCancel, "提示") = MsgBoxResult.Cancel Then Exit Sub
        ElseIf UserState = UserStates.Game And Not labGameTitle.Content.ToString.Contains("旁观") Then
            If MsgBox("当前正在游戏中，突然下线是很没人品的行为哦，是否确认？", MsgBoxStyle.Exclamation + MsgBoxStyle.OkCancel, "提示") = MsgBoxResult.Cancel Then Exit Sub
        End If
        btnRoomPrepare.Visibility = Visibility.Visible
        btnRoomPrepare.IsEnabled = False
        ClientSend("Leave")
    End Sub

    ''' <summary>
    ''' 准备 / 取消准备 / 开始游戏。
    ''' </summary>
    Private Sub btnRoomPrepare_Click(sender As Object, e As RoutedEventArgs) Handles btnRoomPrepare.Click
        Select Case btnRoomPrepare.Content
            Case "准备"
                ClientSend("Prepare|True")
                btnRoomPrepare.Content = "取消准备"
                btnRoomExit.IsEnabled = False
                btnRoomPrepare.IsEnabled = False
            Case "取消准备"
                ClientSend("Prepare|False")
                btnRoomPrepare.Content = "准备"
                btnRoomExit.IsEnabled = True
            Case "开始游戏"
                btnRoomPrepare.Visibility = Visibility.Collapsed
                ClientSend("Start")
                Return
        End Select
        btnRoomPrepare.IsEnabled = False
        Dim th As New Thread(Sub()
                                 Thread.Sleep(1000)
                                 Dispatcher.Invoke(Sub() btnRoomPrepare.IsEnabled = True)
                             End Sub)
        th.Start()
    End Sub

#End Region

#Region "Game | 游戏"

    Private Sub timerGame_Tick() Handles timerGame.Tick
        If UserState = UserStates.Game And Val(labGameTimer.Content) > 0 Then labGameTimer.Content = Val(labGameTimer.Content) - 1
    End Sub
    Private Sub btnGameSelect1_Click(sender As Object, e As EventArgs) Handles btnGameSelect1.Click
        Dim th As New Thread(Sub() ClientSend("Select|0"))
        th.Start()
    End Sub
    Private Sub btnGameSelect2_Click(sender As Object, e As EventArgs) Handles btnGameSelect2.Click
        Dim th As New Thread(Sub() ClientSend("Select|1"))
        th.Start()
    End Sub

#End Region

#Region "List | 列表"

    Private Sub panList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles panList.SelectionChanged
        panList.SelectedIndex = -1
    End Sub

#End Region

    ''' <summary>
    ''' 执行客户端指令。
    ''' </summary>
    Public Sub ClientExecute(Command As String)
        If Not Command.StartsWith("Beat") Then Log("Execute: " & Command)
        '预处理信息，获取其类型与参数
        Log(Command)
        If Command = "" Then Exit Sub
        Dim CommandType As String = Command.Split("|")(0)
        Dim commanda
        If CommandType = "Draw" Then
            commanda = Command
        End If
        Dim Parm As String = ""
        If Command.StartsWith(CommandType & "|") Then Parm = Command.Substring(CommandType.Length + 1)
        Dim Parms() As String = Parm.Split("|")
        '根据类型执行
        Select Case CommandType

            Case "Exit"
                'Exit(String 退出原因...)：退出登录并返回登录页面，在登录页面显示退出原因
                Dispatcher.Invoke(Sub() If labLogin.Content = "" Then labLogin.Content = Parm)
                LoginOff()

            Case "Chat"
                'Chat(String 文本, Boolean 是否加粗)：在聊天栏增加一行文本，由于是在 List 显示故可以换行
                ChatShow(Parms(0), If(Parms.Length = 1, False, Parms(1)))

            Case "Chatable"
                'Chatable(Boolean 是否可以聊天)：启用或关闭聊天
                Dispatcher.Invoke(Sub()
                                      textChat.IsEnabled = Parm
                                      btnChat.IsEnabled = Parm
                                      If Parm Then textChat.Focus()
                                  End Sub)

            Case "Clear"
                'Clear()：清空聊天栏      'Clear(String 游戏模式)：为了兼容原版，此参数不必须
                If Parms(0) = "" Or Parms(0) = "NSWC" Then
                    ChatClear()
                ElseIf Parms(0) = "NHWC" Then
                    Dispatcher.Invoke(Sub() DrawBorad.Children.Clear())  '清空画板
                End If

            Case "Beat"
                'Beat(String 辨识码...)：回应心跳包并重置掉线计时
                ClientSend("Beat|" & Parm, False)
                ClientHeartbeatCount = 0

            Case "Version"
                'Version(Int 版本): 对接服务器版本号
                Dispatcher.Invoke(Sub()
                                      If Not Parms(0) = "" Then
                                          If Convert.ToInt32(Parms(0)) = ClientVerMax Then
                                              panSeverMessage.Visibility = Visibility.Collapsed
                                          ElseIf Convert.ToInt32(Parms(0)) < ClientVerMax Then
                                              SeverVerSay.Text = "目前连接的服务器版本低于客户端版本，这意味着服务端不兼容客户端提供的部分游戏，请谨慎创建游戏或咨询运营商。"
                                          ElseIf Convert.ToInt32(Parms(0)) > ClientVerMax Then
                                              SeverVerSay.Text = "目前连接的服务器版本高于客户端版本，这意味着客户端不兼容服务端提供的部分游戏，请谨慎创建游戏或咨询运营商。"
                                          End If
                                      End If
                                  End Sub)

            Case "Center"
                'Center(String[] 名称...)：进入大厅，并给出房间列表
                UserState = UserStates.Center
                Dispatcher.Invoke(Sub()
                                      panChat.Margin = New Thickness(279, 46, 25, 25)
                                      dbGrid.Visibility = Visibility.Collapsed
                                      listChat.Visibility = Visibility.Visible
                                      listCenter.Items.Clear()
                                      listCenter.SelectedIndex = -1
                                      For Each Room As String In Parms
                                          If Room.Trim.Length > 0 Then
                                              listCenter.Items.Add(Room.Trim)
                                          End If
                                      Next
                                  End Sub)

            Case "Room"
                'Room(String 名称)：进入房间，并给出房间名
                UserState = UserStates.Room
                If gameMode = "NSWC" Then
                    Dispatcher.Invoke(Sub()
                                          panChat.Margin = New Thickness(279, 46, 25, 25)
                                          labGameObserve.Visibility = Visibility.Collapsed
                                          btnRoomPrepare.Visibility = Visibility.Visible
                                          btnRoomPrepare.IsEnabled = Not UserMaster
                                          btnRoomPrepare.Content = If(UserMaster, "开始游戏", "准备")
                                          btnRoomExit.IsEnabled = True
                                          labRoomName.Content = Parms(0)
                                      End Sub)
                ElseIf gameMode = "NHWC" Then
                    Dispatcher.Invoke(Sub()
                                          dbGrid.Visibility = Visibility.Collapsed
                                          listChat.Visibility = Visibility.Visible
                                          listChat.Margin = New Thickness(0, 0, 0, 56)
                                          panChat.Margin = New Thickness(279, 46, 25, 25)
                                          labGameObserve.Visibility = Visibility.Collapsed
                                          btnRoomPrepare.Visibility = Visibility.Visible
                                          btnRoomPrepare.IsEnabled = Not UserMaster
                                          btnRoomPrepare.Content = If(UserMaster, "开始游戏", "准备")
                                          btnRoomExit.IsEnabled = True
                                          labRoomName.Content = Parms(0)
                                      End Sub)
                End If

            Case "Game"
                'Game(String 标题)：进入游戏页面，并给出标题
                UserState = UserStates.Game
                If gameMode = "NSWC" Then
                    Dispatcher.Invoke(Sub()
                                          btnRoomPrepare.Visibility = Visibility.Collapsed
                                      End Sub)
                ElseIf gameMode = "NHWC" Then
                    Dispatcher.Invoke(Sub()
                                          dbGrid.Visibility = Visibility.Visible
                                          listChat.Margin = New Thickness(0, 309, 0, 56)
                                      End Sub)
                End If
                Dispatcher.Invoke(Sub()
                                      labGameTitle.Content = Parms(0)
                                      Beep()
                                  End Sub)

            Case "Msgbox"
                'Msgbox(String 内容...)：弹窗
                Dim th As New Thread(Sub() MsgBox(Parm, MsgBoxStyle.Information, "提示"))
                th.Start()

            Case "List"
                'List(String[] 颜色/右/左)：刷新用户列表
                Dispatcher.Invoke(Sub()
                                      panList.Items.Clear()
                                      For Each u As String In Parms
                                          If Not u = "" Then
                                              Dim token As New UserToken(u) With {.Padding = New Thickness(2, 4, 0, 4), .Width = 197}
                                              panList.Items.Add(token)
                                          End If
                                      Next
                                  End Sub)

            Case "Start"
                'Start(Boolean 是否可以开始)：向房主切换是否可以开始游戏
                If Not (UserMaster And UserState = UserStates.Room) Then Exit Sub
                Dispatcher.Invoke(Sub() btnRoomPrepare.IsEnabled = Parms(0))

            Case "Content"
                'Content(String 内容...)：改变游戏显示区内容
                Dispatcher.Invoke(Sub() labGameContent.Content = Parm)

            Case "Timer"
                'Timer(Integer 时间, String 颜色)：设置倒计时
                Dispatcher.Invoke(Sub()
                                      timerGame.Reset()
                                      labGameTimer.Content = Parms(0)
                                      Select Case Parms(1)
                                          Case "Orange"
                                              labGameTimer.Foreground = New MyColor(255, 128, 0)
                                          Case "Red"
                                              labGameTimer.Foreground = New MyColor(255, 0, 0)
                                          Case "Blue"
                                              labGameTimer.Foreground = New MyColor(0, 128, 255)
                                      End Select
                                  End Sub)

            Case "Observe"
                'Observe(Integer 人数)：显示旁观者人数
                Dispatcher.Invoke(Sub()
                                      If Val(Parms(0)) = 0 Then
                                          labGameObserve.Visibility = Visibility.Collapsed
                                      Else
                                          labGameObserve.Visibility = Visibility.Visible
                                          labGameObserve.Content = "旁观者：" & Parms(0) & " 人"
                                      End If
                                  End Sub)

            Case "Select"
                'Select(String? 选择1, String? 选择2)：改变选题区显示，若隐藏选题区则无参数
                If gameMode = "NHWC" Then
                    Dispatcher.Invoke(Sub()
                                          If Parm = "" Then
                                              '隐藏
                                              panGameSelect.Visibility = Visibility.Hidden
                                          Else
                                              '显示
                                              Beep()
                                              panGameSelect.Visibility = Visibility.Visible
                                              btnGameSelect1.Text = Parms(0)
                                              btnGameSelect2.Text = Parms(1)
                                          End If
                                      End Sub)
                ElseIf gameMode = "NSWC" Then
                    Dispatcher.Invoke(Sub()
                                          If Parm = "" Then
                                              '隐藏
                                              panChat.Margin = New Thickness(279, 68, 25, 25)
                                              panGameSelect.Visibility = Visibility.Hidden
                                          Else
                                              '显示
                                              'panChat.Margin = New Thickness(279, 46, 25, 25)
                                              Beep()
                                              panGameSelect.Visibility = Visibility.Visible
                                              btnGameSelect1.Text = Parms(0)
                                              btnGameSelect2.Text = Parms(1)
                                          End If
                                      End Sub)
                End If
            Case "Ping"
                'Ping(String Start): 处理收到的服务器ping消息
                If Not Parms(0) = "" Then
                    Dispatcher.Invoke(Sub()
                                          Dim a1 As TimeSpan = Date.Now - pingStartTime
                                          Dim a2 As Integer = a1.TotalMilliseconds
                                          Try
                                              Dim tb As New TextBlock With {
                                                .Text = "[本地] 服务器回应，Ping延迟：" + a2.ToString + " ms",
                                                .FontSize = 14,
                                                .FontWeight = If(True, FontWeights.Bold, FontWeights.Normal),
                                                .Tag = GetUUID()
                                              }
                                              tb.Margin = New Thickness With {
                                                .Top = 5,
                                                .Bottom = 5
                                              }
                                              listChat.Items.Add(tb)
                                              If listChat.Items.Count >= 100 Then listChat.Items.RemoveAt(0)
                                              listChat.ScrollIntoView(tb)
                                          Catch
                                          End Try
                                      End Sub)
                End If
            Case "Mode"
                'Mode(String 游戏模式): 设置游戏模式，此功能为兼容原版不必须
                If Not Parms(0) = "" Then
                    If Parms(0) = "NHWC" Then '你画我猜
                        gameMode = Parms(0)
                        '更改标题
                        Dispatcher.Invoke(Sub() Title = LoadTitle & " - " & UserName & " - " & "你画我猜")
                    End If
                End If
            Case "UI"
                'UI（String UI名称, boolren 显示状态）:对UI进行隐藏、显示
                If Not Parms(0) = "" Then
                    If Parms(0) = "dbGrid" Then
                        Dispatcher.Invoke(Sub()
                                              dbGrid.Visibility = If(Parms(1) = "True", Visibility.Visible, Visibility.Collapsed)
                                          End Sub)
                    ElseIf Parms(0) = "listChat" Then
                        Dispatcher.Invoke(Sub()
                                              If Parms(1) = "True" Then
                                                  listChat.Margin = New Thickness(0, 0, 0, 56)
                                                  panChat.Margin = New Thickness(279, 68, 25, 25)
                                              Else
                                                  listChat.Margin = New Thickness(0, 309, 0, 56)
                                              End If
                                          End Sub)
                    End If
                End If
            Case "Drawable"
                'Drawable(boolren 是否禁用画板): 禁用画板
                If Parms(0) = "True" Then
                    penColor = Brushes.Black
                    Dispatcher.Invoke(Sub()
                                          ban.Visibility = Visibility.Collapsed
                                          pen.Visibility = Visibility.Visible
                                          rubber.Visibility = Visibility.Visible
                                          clear.Visibility = Visibility.Visible
                                          setpen.Visibility = Visibility.Visible
                                      End Sub)
                Else
                    penColor = Brushes.White
                    Dispatcher.Invoke(Sub()
                                          ban.Visibility = Visibility.Visible
                                          pen.Visibility = Visibility.Collapsed
                                          rubber.Visibility = Visibility.Collapsed
                                          clear.Visibility = Visibility.Collapsed
                                          setpen.Visibility = Visibility.Collapsed
                                      End Sub)
                End If
            Case "Draw"
                'Draw(int 坐标条数，Strings 坐标组): 在画板上绘制
                If Not ban.Visibility = Visibility.Visible Then Exit Sub
                Dim Parmsin = Parms
                Dim color = If(Parmsin(0) = "black", Brushes.Black, Brushes.White)
                Dim thickness = Parmsin(1)
                Dim dstartPoint As Point = New Point With {
                                        .X = Convert.ToInt32(Parmsin(2).Split(",")(0)),
                                        .Y = Convert.ToInt32(Parmsin(2).Split(",")(1))
                                       }
                Dim dlastpoint As Point = dstartPoint
                For i = 3 To Parmsin.Length
                    Dim num = i
                    Dispatcher.Invoke(Sub()
                                          '画到控件上
                                          Dim line As Line = New Line With {
                                                    .Stroke = color,
                                                    .StrokeThickness = thickness,
                                                    .X1 = dlastpoint.X,
                                                    .Y1 = dlastpoint.Y,
                                                    .X2 = Convert.ToInt32(Parmsin(num).Split(",")(0)),
                                                    .Y2 = Convert.ToInt32(Parmsin(num).Split(",")(1))
                                                    }
                                          dlastpoint.X = line.X2
                                          dlastpoint.Y = line.Y2
                                          DrawBorad.Children.Add(line)
                                      End Sub)
                Next i
        End Select
    End Sub

    Private Sub Back_Click(sender As Object, e As RoutedEventArgs)
        If isShowUpdate Then
            mainLogin.Visibility = Visibility.Visible
            mainUpd.Visibility = Visibility.Collapsed
            isShowUpdate = False
        ElseIf isShowCreate Then
            isShowCreate = False
            panModeSelect.Visibility = Visibility.Collapsed
        ElseIf UserState = UserStates.Center Then
            Dispatcher.Invoke(Sub() labLogin.Content = "")
            LoginOff()
        ElseIf UserState = UserStates.Room Then
            ClientSend("Leave")
            btnRoomPrepare.Visibility = Visibility.Visible
            btnRoomPrepare.IsEnabled = False
        End If
    End Sub

    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
        If Not isShowUpdate Then
            mainLogin.Visibility = Visibility.Collapsed
            mainUpd.Visibility = Visibility.Visible
            isShowUpdate = True
        End If
    End Sub

    Private Sub Button_Click_1(sender As Object, e As RoutedEventArgs)
        Process.Start("https://afdian.net/@LTCat")
    End Sub

    Private Sub Button_Click_2(sender As Object, e As RoutedEventArgs)
        Process.Start("https://github.com/Stapxs")
    End Sub

#Region "Game - NHWC | 你画我猜画板处理"

    Private Declare Function GetCursorPos Lib "user32" (ByRef lpPoint As POINTAPI) As Long '全屏坐标声明
    Private Declare Function ScreenToClient Lib "user32.dll" (ByVal hwnd As Int32, ByRef lpPoint As POINTAPI) As Int32 '窗口坐标声明
    Private Structure POINTAPI '声明坐标变量
        Public x As Int32 '声明坐标变量为32位
        Public y As Int32 '声明坐标变量为32位
    End Structure

    Dim th As Thread
    Dim isStopDraw As Boolean
    Dim lastPoint As Point
    Dim isOutOBE As Boolean = False
    Dim penColor = Brushes.Black
    Dim penThickness = 2
    Dim pointlist As String = ""
    Dim pointNum As Integer = 0

    Private Sub DrawBorad_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        isStopDraw = False
        '绘画线程
        th = New Thread(Sub()
                            Dim sendtime = 0
                            While Not isStopDraw
                                Dispatcher.Invoke(Sub()
                                                      '获取鼠标坐标
                                                      Dim X As Double = e.GetPosition(DrawBorad).X
                                                      Dim Y As Double = e.GetPosition(DrawBorad).Y
                                                      '超界判断
                                                      UpdateLayout()
                                                      If （X < 0 Or Y < 0 Or X > dbGrid.Width Or Y > dbGrid.Height) And Not isOutOBE Then
                                                          isOutOBE = True
                                                          If Not pointlist = "" Then
                                                              ClientSend("Draw|" + pointNum.ToString + "|" + pointlist)
                                                          End If
                                                          pointlist = ""
                                                          pointNum = 0
                                                          Return
                                                      End If
                                                      '判断开始
                                                      If （lastPoint = Nothing) Or isOutOBE Then
                                                          isOutOBE = False
                                                          lastPoint.X = X
                                                          lastPoint.Y = Y
                                                          If pointlist = "" Then
                                                              If penColor Is Brushes.White Then
                                                                  pointlist = pointlist + "white|"
                                                                  pointlist = pointlist + "6"
                                                              Else
                                                                  pointlist = pointlist + "black|"
                                                                  pointlist = pointlist + "2"
                                                              End If
                                                              pointlist = pointlist + "|" + Convert.ToInt32(X).ToString + "," + Convert.ToInt32(Y).ToString + ""
                                                              pointNum += 3
                                                          Else
                                                              pointlist = pointlist + "|" + Convert.ToInt32(X).ToString + "," + Convert.ToInt32(Y).ToString + ""
                                                              pointNum += 1
                                                          End If
                                                          Return
                                                      End If
                                                      '画到控件上
                                                      Dim line As Line = New Line With {
                                                      .Stroke = penColor,
                                                      .StrokeThickness = penThickness,
                                                      .X1 = lastPoint.X,
                                                      .Y1 = lastPoint.Y,
                                                      .X2 = X,
                                                      .Y2 = Y
                                                      }
                                                      lastPoint.X = X
                                                      lastPoint.Y = Y
                                                      DrawBorad.Children.Add(line)
                                                      If Math.Sqrt(((line.X1 - line.X2) * (line.X1 - line.X2)) + ((line.Y1 - line.Y2) * (line.Y1 - line.Y2))) >= 2 Then
                                                          pointlist = pointlist + "|" + Convert.ToInt32(X).ToString + "," + Convert.ToInt32(Y).ToString
                                                          pointNum += 1
                                                      End If
                                                  End Sub)
                                Thread.Sleep(20)
                                sendtime += 1
                                If sendtime >= 5 And pointNum > 1 Then
                                    'lastPoint = New Point(0, 0)
                                    If Not pointlist = "" Then
                                        ClientSend("Draw|" + pointlist)
                                    End If
                                    pointlist = ""
                                    pointNum = 3
                                    If penColor Is Brushes.White Then
                                        pointlist = pointlist + "white|"
                                        pointlist = pointlist + "6"
                                    Else
                                        pointlist = pointlist + "black|"
                                        pointlist = pointlist + "2"
                                    End If
                                    pointlist = pointlist + "|" + Convert.ToInt32(lastPoint.X).ToString + "," + Convert.ToInt32(lastPoint.Y).ToString
                                End If
                            End While
                        End Sub)
        th.Start()
    End Sub

    Private Sub DrawBorad_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
        isStopDraw = True
        'lastPoint = New Point(0, 0)
        isOutOBE = True
        If Not pointlist = "" Then
            ClientSend("Draw|" + pointlist)
        End If
        pointlist = ""
        pointNum = 0
    End Sub

    Private Sub Rubber_Click(sender As Object, e As RoutedEventArgs)
        penColor = Brushes.White
        penThickness = 6
    End Sub

    Private Sub Pen_Click(sender As Object, e As RoutedEventArgs)
        penColor = Brushes.Black
        penThickness = 3
    End Sub

    Private Sub Clear_Click(sender As Object, e As RoutedEventArgs)
        DrawBorad.Children.Clear()
        ClientSend("Clear|NHWC")
    End Sub

#End Region

#Region "Center | 创建游戏类型处理"

    Private Sub BtnModeSelect1_Click(sender As Object, e As RoutedEventArgs) Handles btnModeSelect1.Click
        '你说我猜
        panModeSelect.Visibility = Visibility.Collapsed
        UserMaster = True
        ClientSend("Create")
        btnRoomExit.IsEnabled = True
        gameMode = "NSWC"
        Title = LoadTitle & " - " & UserName & " - " & "你说我猜"
    End Sub

    Private Sub BtnModeSelect2_Click(sender As Object, e As RoutedEventArgs)
        '你画我猜
        panModeSelect.Visibility = Visibility.Collapsed
        UserMaster = True
        ClientSend("Create|NHWC")
        btnRoomExit.IsEnabled = True
        gameMode = "NHWC"
        Dispatcher.Invoke(Sub() Title = LoadTitle & " - " & UserName & " - " & "你画我猜")
    End Sub

#End Region

    Private Sub Button_Click_3(sender As Object, e As RoutedEventArgs)
        panSeverMessage.Visibility = Visibility.Collapsed
    End Sub

    Private Sub Set_Click(sender As Object, e As RoutedEventArgs)

    End Sub
End Class
