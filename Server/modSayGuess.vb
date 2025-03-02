﻿Public Module modSayGuess 'SG

#Region "房间信息"

    ''' <summary>
    ''' 你说我猜的房间信息。
    ''' </summary>
    Public Class SGRoomData
        ''' <summary>
        ''' 答案。
        ''' </summary>
        Public Answer As String
        ''' <summary>
        ''' 答案类别。
        ''' </summary>
        Public Hint As String
        ''' <summary>
        ''' 答案选项。
        ''' </summary>
        Public Selections As ArrayList

        ''' <summary>
        ''' 猜对者的分数库。
        ''' </summary>
        Public ScoreBase As Integer
        ''' <summary>
        ''' 猜对者的人数。
        ''' </summary>
        Public CorrectCount As Integer = 0
        ''' <summary>
        ''' 一轮结束后的等待。
        ''' </summary>
        Public EndWait As Boolean = False
        ''' <summary>
        ''' 正在描述的人。
        ''' </summary>
        Public Turner As formMain.UserData
        ''' <summary>
        ''' 倒计时线程。
        ''' </summary>
        Public TimerThread As Thread
        ''' <summary>
        ''' 倒计时。
        ''' </summary>
        Public Timer As Integer = 99
        ''' <summary>
        ''' 是否已显示提示。
        ''' </summary>
        Public Hinted As Boolean = False
    End Class

#End Region

#Region "玩家信息"

    ''' <summary>
    ''' 你说我猜的玩家信息。
    ''' </summary>
    Public Class SGUserData
        ''' <summary>
        ''' 是否已经轮过了。
        ''' </summary>
        Public Turned As Boolean = False
        ''' <summary>
        ''' 玩家的游戏状态。
        ''' </summary>
        Public State As SGStates = SGStates.Failed
        ''' <summary>
        ''' 玩家的得分。
        ''' </summary>
        Public Score As Integer = 0
    End Class
    ''' <summary>
    ''' 玩家的游戏中状态。
    ''' </summary>
    Public Enum SGStates As Integer
        ''' <summary>
        ''' 没有猜出来。
        ''' </summary>
        Failed
        ''' <summary>
        ''' 已经猜出来了。
        ''' </summary>
        Finished
        ''' <summary>
        ''' 正在描述。
        ''' </summary>
        Turning
        ''' <summary>
        ''' 旁观。
        ''' </summary>
        Observe
    End Enum
    ''' <summary>
    ''' 获取玩家的显示信息。
    ''' </summary>
    Public Function SGList(room As formMain.RoomData)
        SGList = ""
        '将描述者置顶显示
        For Each u As formMain.UserData In room.Users
            If u.SG.State = SGStates.Turning Then SGList += "Blue/" & u.Name & "/" & u.SG.Score & "|"
        Next
        '显示其他玩家
        For Each u As formMain.UserData In room.Users
            Select Case u.SG.State
                Case SGStates.Failed
                    SGList += "Red/" & u.Name & "/" & u.SG.Score & "|"
                Case SGStates.Finished
                    SGList += "Green/" & u.Name & "/" & u.SG.Score & "|"
            End Select
        Next
    End Function
    ''' <summary>
    ''' 中途加入的旁观玩家。
    ''' </summary>
    Public Sub SGObserve(user As formMain.UserData)
        user.Send("Clear¨Game|你说我猜（旁观）¨" &
                           "Timer|" & user.Room.SG.Timer & "|" & If(user.Room.SG.EndWait, "Blue", If(user.Room.SG.CorrectCount = 0, "Orange", "Red")) & "¨" &
                           "Chatable|" & user.Room.SG.EndWait.ToString & "¨" &
                           "Select¨" &
                           "Content|" & If(user.Room.SG.Answer = "",
                                    If(user.Room.SG.EndWait, "描述者未选择题目", "等待描述者选择题目"),
                                    If(user.Room.SG.EndWait, "题目：" & user.Room.SG.Answer, If(user.Room.SG.Timer <= 20, "提示：" & user.Room.SG.Answer.Count & " 个字，" & user.Room.SG.Hint, "提示：" & user.Room.SG.Answer.Count & " 个字"))
                               ) & "¨" &
                           "Chat|游戏规则：" & vbCrLf &
                                "　一位玩家描述题目，其余玩家进行猜测。" & vbCrLf &
                                "　猜对的玩家越多，描述者得分越高；然而，如果所有玩家全部猜对，描述者不得分。|False")
        frmMain.BoardcastInRoom("Chat|系统：" & user.Name & " 以旁观者身份加入了房间。|False", user.Room)
        user.SG = New SGUserData With {.State = SGStates.Observe, .Turned = True}
    End Sub

#End Region

#Region "题目"

    ''' <summary>
    ''' 题库。（类别，题目）
    ''' </summary>
    Private SGData As New Dictionary(Of String, ArrayList)
    ''' <summary>
    ''' 加载题库。
    ''' </summary>
    Public Sub SGLoad()
        SGData.Add("名词", RandomChaos({"白内障", "红楼梦", "听诊器", "世界杯", "红绿灯", "泥石流", "强迫症", "花生油", "沙尘暴", "龙卷风", "芝麻油", "兵马俑", "文件夹", "原子弹", "浏览器", "康师傅", "玫瑰花", "造纸术", "儿童节", "端午节", "教师节", "国庆节", "元旦", "孙子", "排球", "夜宵", "婴儿", "火锅", "雷暴", "孜然", "矿石", "鳞甲", "图腾", "网易", "珊瑚", "篝火", "火药", "玻璃", "矿车", "腾讯", "表格", "魔术", "尾巴", "股票", "晴天", "台风", "矮人", "魔术", "兽人", "暴雨", "天使", "月亮", "夜宵", "太阳", "地震", "银河", "流星", "微信", "恒星", "卫星", "大腿", "心脏", "酱油", "芝麻", "蒜", "姜", "肝", "肾", "胃", "茶", "肉", "葱", "脚", "神", "鬼", "冰", "雾"}))
        SGData.Add("行为", RandomChaos({"系鞋带", "打喷嚏", "刮胡子", "测血压", "查水表", "送快递", "关电脑", "玩游戏", "放风筝", "卖保险", "数星星", "找工作", "抄作业", "写小说", "逛街", "减肥", "跳高", "打滚", "卖萌", "梦游", "挂机", "摸鱼", "注册", "跳跃", "吃鱼", "洗脸", "爬山", "绑架", "植树", "跳楼", "消毒", "考试", "投票", "签字", "领奖", "下载", "报警", "喘气", "逃跑", "学习", "砍树", "洗头", "打字", "作曲", "瞄准", "弯腰", "作弊", "上班", "演讲", "拔河", "穿越", "冷笑", "放屁", "下棋", "出国", "开车", "登录", "洗手", "占卜", "骑马", "灭火", "喝酒", "射击", "喝水", "吃面", "咳嗽", "杀人"}))
        SGData.Add("成语", RandomChaos({"一分为二", "一刀两断", "一石二鸟", "一叶知秋", "不堪一击", "可见一斑", "两肋插刀", "三心二意", "三顾茅庐", "朝三暮四", "入木三分", "雨过天晴", "五花大绑", "九死一生", "万箭穿心", "望子成龙", "蜻蜓点水", "惊弓之鸟", "鹤立鸡群", "守株待兔", "呼风唤雨", "旗鼓相当", "狗急跳墙", "东张西望", "狼吞虎咽", "抱头鼠窜", "龙飞凤舞", "拔刀相助", "坐井观天", "张牙舞爪", "指鹿为马", "虎背熊腰", "草船借箭", "开门见山", "移花接木", "天罗地网", "目无全牛", "兴高采烈", "亡羊补牢", "鸡犬不宁", "望梅止渴", "日积月累", "胆小如鼠", "呆若木鸡", "锦上添花", "叶公好龙", "汗流浃背", "抓耳挠腮", "遍体鳞伤", "立竿见影", "绞尽脑汁", "鸡飞狗跳", "幸灾乐祸", "挥汗如雨", "谈笑风生", "点石成金", "惊弓之鸟", "水滴石穿", "破涕为笑", "手舞足蹈", "回头是岸", "愚公移山", "掩耳盗铃", "眉飞色舞", "哭笑不得", "藕断丝连", "纸上谈兵", "眉开眼笑", "血盆大口", "掌上明珠", "出生入死"}))
        SGData.Add("生物", RandomChaos({"丹顶鹤", "猫头鹰", "长颈鹿", "外星人", "地狱犬", "三文鱼", "美杜莎", "皮卡丘", "穿山甲", "美人鱼", "史莱姆", "独角兽", "萤火虫", "萤火虫", "北极熊", "九尾狐", "骆驼", "巨人", "蝌蚪", "青蛙", "蚂蚁", "蜘蛛", "孔雀", "螃蟹", "海马", "母鸡", "蝎子", "鸽子", "海豚", "精灵", "天鹅", "恐龙", "斑马", "鲤鱼", "麻雀", "狼人", "蚊子", "蝙蝠", "章鱼", "骷髅", "蜜蜂", "绵羊", "僵尸", "河豚", "恐龙", "狐狸", "地鼠", "苍蝇", "鳄鱼", "田螺", "水牛", "燕子", "麒麟", "蜥蜴", "狐狸", "鹦鹉", "鲨鱼", "河马", "蜻蜓", "凤凰", "企鹅", "猴", "驴", "鹰", "鹿", "蛇", "虾", "蟹", "蝉", "蚕", "狼", "兔", "龙", "鲸", "熊"}))
        SGData.Add("食品", RandomChaos({"三明治", "爆米花", "棉花糖", "烤红薯", "水煮鱼", "柠檬茶", "番茄酱", "茶叶蛋", "无花果", "方便面", "蛋炒饭", "矿泉水", "葡萄干", "干拌面", "薄荷糖", "猕猴桃", "葡萄酒", "八宝粥", "千层肚", "火龙果", "青苹果", "冰激凌", "小龙虾", "金针菇", "哈密瓜", "饼干", "香肠", "果醋", "豆浆", "菠萝", "鸭肠", "肥肠", "腰果", "冰糖", "红糖", "南瓜", "辣椒", "酸奶", "菠菜", "海带", "腰果", "牛排", "紫薯", "咸鱼", "豌豆", "玉米", "奶酪", "咖啡", "木耳", "黄瓜", "桑葚", "甘蔗", "茄子", "榴莲", "血旺", "柚子", "椰子", "雪碧", "山楂", "柠檬", "白酒", "蛋糕", "麻花", "糯米", "馒头", "披萨", "烧饼", "瓜子", "油条", "牛奶", "可乐"}))
        SGData.Add("物品", RandomChaos({"指甲油", "手电筒", "飞机票", "绿帽子", "橡皮擦", "红领巾", "电灯泡", "不倒翁", "灭火器", "收音机", "笔记本", "服务器", "电子琴", "游戏机", "剃须刀", "榨汁机", "路由器", "电热毯", "三角板", "荧光笔", "三叉戟", "荧光棒", "温度计", "卫生纸", "挖掘机", "充电器", "机关枪", "梳子", "台灯", "铅球", "水晶", "戒指", "花盆", "卷轴", "存折", "长剑", "冰块", "镰刀", "雨衣", "子弹", "蜡烛", "筷子", "电池", "狼皮", "头盔", "秒表", "菜刀", "羊毛", "毛笔", "香皂", "口罩", "金币", "烟花", "木鱼", "口琴", "空调", "枕头", "窗帘", "古筝", "粉笔", "匕首", "铁剑", "石斧", "灯笼", "牙膏", "快板", "锄头", "水壶", "口红", "圆规", "铁锤", "药水", "字典", "法杖", "香水", "剪刀", "彩票"}))
        SGData.Add("地点", RandomChaos({"黑龙江", "天安门", "攀枝花", "石家庄", "葡萄牙", "俄罗斯", "公交站", "游乐园", "停车场", "教学楼", "小卖部", "立交桥", "加油站", "金字塔", "异世界", "土耳其", "水立方", "针叶林", "足球场", "少林寺", "操场", "宿舍", "长城", "鸟巢", "公园", "仓库", "青岛", "山脉", "宫殿", "食堂", "沙滩", "瀑布", "冰山", "盆地", "寺庙", "北京", "教堂", "铁路", "上海", "中国", "美国", "日本", "银行", "花园", "医院", "法院", "酒吧", "墓地", "故宫", "白宫", "英国", "台湾", "厕所", "山东", "阴间", "四川", "长江", "天堂", "巴西", "地狱", "黄河", "澡堂", "网吧", "长沙", "三峡"}))
        SGData.Add("人物", RandomChaos({"周杰伦", "贾宝玉", "红太狼", "光头强", "派大星", "米老鼠", "钢铁侠", "周立波", "蜘蛛侠", "赵本山", "林黛玉", "特斯拉", "贝多芬", "毕福剑", "崔永元", "灰太狼", "马化腾", "阿童木", "刘德华", "匹诺曹", "周树人", "江泽民", "特朗普", "王尼玛", "葫芦娃", "薛定谔", "武则天", "孙悟空", "蝙蝠侠", "暖羊羊", "孔乙己", "乔布斯", "司马光", "蓝精灵", "陈独秀", "蟹老板", "刘慈欣", "爱迪生", "奥特曼", "丘比特", "诸葛亮", "牛顿", "闰土", "林冲", "李白", "熊大", "路飞", "姚明", "柯南", "龙猫", "成龙", "耶稣", "刘翔", "武松", "韩红", "贞子", "霍金", "马云", "孔子", "关羽", "唐僧", "曹操"}))
    End Sub
    ''' <summary>
    ''' 获取题目。
    ''' </summary>
    Private Function SGGet() As String()
        Dim Type As String = RandomOne({"名词", "行为", "成语", "生物", "食品", "物品", "地点", "人物"})
        '取出该类别第一个题目，再放到靠后的随机位置
        Dim Ques As String = SGData(Type)(0)
        SGData(Type).RemoveAt(0)
        SGData(Type).Insert(RandomInteger(SGData(Type).Count - 20, SGData(Type).Count - 1), Ques)
        Return {Ques, Type}
    End Function

#End Region

#Region "游戏状态改变"

    ''' <summary>
    ''' 开始游戏。
    ''' </summary>
    Public Sub SGStart(room As formMain.RoomData)
        '发布公告
        frmMain.BoardcastInRoom("Clear¨Game|你说我猜¨" &
                                "Chat|游戏规则：" & vbCrLf &
                                "　一位玩家描述题目，其余玩家进行猜测。" & vbCrLf &
                                "　猜对的玩家越多，描述者得分越高；然而，如果所有玩家全部猜对，描述者不得分。|False", room)
        '初始化数据
        room.SG = New SGRoomData
        For Each pc As formMain.UserData In room.Users
            pc.SG = New SGUserData
        Next
        room.SG.TimerThread = New Thread(AddressOf SGTimer)
        room.SG.TimerThread.Start(room)
        '随机一人开始描述
        room.SG.Turner = RandomOne(room.Users)
        SGTurnStart(room)
    End Sub
    ''' <summary>
    ''' 结束游戏。
    ''' </summary>
    Public Sub SGEnd(room As formMain.RoomData)
        Dim chats As New ArrayList
        Dim players As New ArrayList
        Dim rank As Integer = 1 '排名
        players.AddRange(room.GetGameUsers)
        Do Until players.Count = 0
            '查找最佳玩家列表
            Dim bp As Integer = -1
            Dim bps As New ArrayList
            For Each player As formMain.UserData In players
                If player.SG.Score > bp Then
                    bp = player.SG.Score
                    bps.Clear()
                    bps.Add(player.Name)
                ElseIf player.SG.Score = bp Then
                    bps.Add(player.Name)
                End If
            Next
            '显示分数
            chats.Add("第 " & rank & " 名：" & Join(bps.ToArray, "、") & "（" & bp & " 分）")
            rank += bps.Count
            '移除这些玩家
            For Each name As String In bps
                For i = 0 To players.Count - 1
                    If players(i).Name = name Then
                        players.RemoveAt(i)
                        GoTo NextPlayer
                    End If
                Next
NextPlayer:
            Next
        Loop
        chats(0) = "Chat|" & chats(0)
        frmMain.BoardcastInRoom("Chat|系统：游戏结束！|False¨" & Join(chats.ToArray, vbCrLf) & "|True", room)
    End Sub
    ''' <summary>
    ''' 玩家中途离开游戏。
    ''' </summary>
    Public Sub SGLeave(user As formMain.UserData, room As formMain.RoomData)
        If user.SG.State = SGStates.Turning And Not room.SG.EndWait Then
            '正在描述的玩家突然溜走
            SGTurnEnd(room)
        ElseIf room.SG.EndWait Then
            '正在等待，重新选人
            Dim unturned As New ArrayList
            For Each u As formMain.UserData In room.GetGameUsers
                If Not u.SG.Turned Then unturned.Add(u)
            Next
            If unturned.Count = 0 Then
                '结束游戏
                room.SG.Turner = Nothing
            Else
                '进入等待
                room.SG.Turner = RandomOne(unturned)
            End If
            room.SG.EndWait = True
        ElseIf user.SG.State = SGStates.Failed Then
            '没有猜出来的玩家退出，重新进行结束判断
            If room.SG.CorrectCount + 1 = room.GetGameUsers.Count Then SGTurnEnd(room)
        End If
    End Sub

#End Region

    ''' <summary>
    ''' 接管聊天。
    ''' </summary>
    Public Sub SGChat(data As String, u As formMain.UserData)
        If (u.SG.State = SGStates.Failed And Not u.Room.SG.EndWait) And (data = u.Room.SG.Answer And Not u.Room.SG.Answer = "") Then
            SGCorrect(u)
        Else
            frmMain.BoardcastInRoom("Chat|" & u.Name & "：" & data & "|" & If(u.SG.State = SGStates.Turning, "True", "False"), u.Room)
        End If

        ''正在等待中
        'If u.Room.SG.EndWait Then
        '    frmMain.BoardcastInRoom("Chat|" & u.Name & "：" & data & "|False", u.Room)
        '    Exit Sub
        'End If
        ''不在等待中
        'Select Case u.SG.State
        '    Case SGStates.Turning
        '        '该玩家正在描述
        '        'For i As Integer = 0 To u.Room.SG.Answer.Length - 1
        '        '    data = data.Replace(u.Room.SG.Answer.Substring(i, 1), "*")
        '        'Next
        '        'If Not data = "" Then frmMain.BoardcastInRoom("Chat|" & u.Name & "：" & data & "|True", u.Room)
        '        frmMain.BoardcastInRoom("Chat|" & u.Name & "：" & data & "|True", u.Room)
        '    Case SGStates.Failed
        '        '该玩家正在猜测
        '        If data = u.Room.SG.Answer Then
        '            SGCorrect(u)
        '        Else
        '            frmMain.BoardcastInRoom("Chat|" & u.Name & "：" & data & "|False", u.Room)
        '        End If
        '    Case SGStates.Finished
        '        '该玩家正在看戏
        '        'For i As Integer = 0 To u.Room.SG.Answer.Length - 1
        '        '    data = data.Replace(u.Room.SG.Answer.Substring(i, 1), "*")
        '        'Next
        '        'If Not data = "" Then frmMain.BoardcastInRoom("Chat|" & u.Name & "：" & data & "|False", u.Room)
        'End Select
    End Sub

    ''' <summary>
    ''' 某人答对了。
    ''' </summary>
    Private Sub SGCorrect(u As formMain.UserData)
        '改变玩家数据
        u.SG.Score += u.Room.SG.ScoreBase
        u.SG.State = SGStates.Finished
        u.Room.SG.CorrectCount += 1
        frmMain.BoardcastInRoom("Chat|系统：" & u.Name & " 猜出了答案，得 " & u.Room.SG.ScoreBase & " 分。" & "|False¨" &
                                                          frmMain.BoardcastList(u.Room), u.Room)
        u.Room.SG.ScoreBase -= 1
        '快速结束
        If u.Room.SG.CorrectCount = 1 Then
            frmMain.BoardcastInRoom("Chat|系统：有玩家已猜出答案，本轮将在 30 秒后结束！|False¨Timer|30|Red", u.Room)
            u.Room.SG.Timer = 30
        End If
        '本轮结束
        If u.Room.SG.CorrectCount + 1 = u.Room.GetGameUsers.Count Then
            SGTurnEnd(u.Room)
        Else
            u.Send("Chatable|False")
        End If
    End Sub

    ''' <summary>
    ''' 描述开始。
    ''' </summary>
    Private Sub SGTurnStart(room As formMain.RoomData)
        Dim player As formMain.UserData = room.SG.Turner
        '初始化玩家状态
        For Each pc As formMain.UserData In room.GetGameUsers
            pc.SG.State = SGStates.Failed
        Next
        '对描述者进行标记
        player.SG.State = SGStates.Turning
        player.SG.Turned = True
        room.SG.Turner = player
        '更新状态
        frmMain.BoardcastInRoom(frmMain.BoardcastList(room), room)
        room.SG.ScoreBase = room.GetGameUsers.Count - 1
        room.SG.CorrectCount = 0
        room.SG.EndWait = False
        room.SG.Hinted = False
        '获取题目
        room.SG.Selections = New ArrayList From {SGGet(), SGGet()}
        room.SG.Answer = ""
        room.SG.Hint = ""
        '发放状态
        For Each us As formMain.UserData In room.Users
            If room.Mode = "NHWC" Then
                frmMain.BoardcastInRoom("UI|dbGrid|True", room)
                frmMain.BoardcastInRoom("UI|listChat|False", room)
            End If
            us.Send("Clear|NHWC")
            Select Case us.SG.State
                Case SGStates.Turning
                    us.Send("Chat|系统：由你进行描述，请选择一个题目。|True¨" &
                                   "Content|请从下方两个题目中选择一个¨" &
                                   "Select|" & room.SG.Selections(0)(0) & "|" & room.SG.Selections(1)(0) & "¨" &
                                   "Timer|99|Orange¨" &
                                   "Chatable|False")
                    us.Send("Drawable|True")
                Case SGStates.Observe
                    us.Send("Chat|系统：由 " & player.Name & " 进行描述。|True¨" &
                                   "Content|等待描述者选择题目¨" &
                                   "Select¨" &
                                   "Timer|99|Orange¨" &
                                   "Chatable|False")
                    us.Send("Drawable|False")
                Case SGStates.Failed
                    us.Send("Chat|系统：由 " & player.Name & " 进行描述。|True¨" &
                                   "Content|等待描述者选择题目¨" &
                                   "Select¨" &
                                   "Timer|99|Orange¨" &
                                   "Chatable|True")
                    us.Send("Drawable|False")
            End Select
        Next
        '倒计时
        room.SG.Timer = 99
    End Sub
    ''' <summary>
    ''' 描述结束。
    ''' </summary>
    Private Sub SGTurnEnd(room As formMain.RoomData)
        '加分
        If room.SG.Answer = "" Then
            frmMain.BoardcastInRoom("Content|描述者未选择题目¨" &
                                                              "Chat|系统：描述者 " & room.SG.Turner.Name & " 未选择题目，本轮结束。|True¨" &
                                                              "Chatable|True¨" &
                                                              "Select", room)
            room.SG.Timer = 5
        Else
            frmMain.BoardcastInRoom("Content|题目：" & room.SG.Answer, room)
            Dim chat As String = "Chat|系统：本轮题目为【" & room.SG.Answer & "】，"
            If Not room.Users.Contains(room.SG.Turner) Then
                chat += "由于描述者 " & room.SG.Turner.Name & " 离线，本轮直接结束。"
                room.SG.Timer = 5
            ElseIf room.SG.CorrectCount + 1 = room.GetGameUsers.Count Then
                chat += "所有玩家均答对，描述者 " & room.SG.Turner.Name & " 不得分。"
                room.SG.Timer = 5
            ElseIf room.SG.CorrectCount = 0 Then
                chat += "无人答对，描述者 " & room.SG.Turner.Name & " 不得分。"
                room.SG.Timer = 10
            Else
                Dim sc As Integer = room.SG.CorrectCount * 2
                chat += "有 " & room.SG.CorrectCount & " 人答对，描述者 " & room.SG.Turner.Name & " 得 " & sc & " 分。"
                room.SG.Timer = 10
                room.SG.Turner.SG.Score += sc
                frmMain.BoardcastInRoom(frmMain.BoardcastList(room), room)
            End If
            frmMain.BoardcastInRoom(chat & "|True¨Chatable|True¨Select", room)
        End If
        '设置尚未描述的玩家
        Dim unturned As New ArrayList
        For Each u As formMain.UserData In room.GetGameUsers
            If Not u.SG.Turned Then unturned.Add(u)
        Next
        If unturned.Count = 0 Then
            '在倒计时后结束游戏
            room.SG.Turner = Nothing
            room.SG.Timer = 5
        Else
            '进入等待
            room.SG.Turner = RandomOne(unturned)
            If room.Mode = "NHWC" Then
                frmMain.BoardcastInRoom("UI|dbGrid|False", room)
                frmMain.BoardcastInRoom("UI|listChat|True", room)
            End If
        End If
            room.SG.EndWait = True
        frmMain.BoardcastInRoom("Timer|" & room.SG.Timer & "|Blue", room)
    End Sub

    Private Sub SGTimer(room As formMain.RoomData)
        Do While room.Gaming And frmMain.RoomList.Contains(room)
            room.SG.Timer -= 1
            If room.SG.Timer = 0 Then
                If room.SG.EndWait Then
                    If IsNothing(room.SG.Turner) Then
                        room.EndGame()
                        Exit Sub
                    Else
                        SGTurnStart(room)
                    End If
                Else
                    SGTurnEnd(room)
                End If
            End If
            If room.SG.Timer = 20 And Not room.SG.Hinted Then
                '显示第二段提示
                room.SG.Hinted = True
                For Each us As formMain.UserData In room.Users
                    If us.SG.State = SGStates.Failed Or us.SG.State = SGStates.Observe Then us.Send("Chat|提示：题目类别为【" & room.SG.Hint & "】。|True")
                    If Not us.SG.State = SGStates.Turning Then us.Send("Content|提示：" & room.SG.Answer.Count & " 个字，" & room.SG.Hint)
                Next
            ElseIf room.SG.Timer = 69 And room.SG.Answer = "" Then
                '没有选择题目，强制结束
                SGTurnEnd(room)
            End If
            Thread.Sleep(999)
        Loop
    End Sub

End Module
