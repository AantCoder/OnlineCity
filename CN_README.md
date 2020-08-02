# OnlineCity/ "多人城镇"
基于游戏 "RimWorld" 的运行模组 "OnlineCity"
作者 Vasilii Ivanov 又名 Aant


# OnlineCity/ "多人城镇"

这是基于RimWorld的模组《OnlineCity》
作者 Василий Иванов 又名 Aant

OnlineCity是RimWorld/环世界 的多人游戏模组。它可以让多个玩家在同一个星球上进行在线游戏。注册成功后，你可以创建自己的家，开始与其他参与者并肩发展。你们将能够监视邻居、他们的定居点和商队的进展，但最重要的是，你们将能够互相提供相当的物质援助，传递食物、药品、武器或其他任何东西，包括定居者。要做到这一点，你只需要通过所有的正常的游戏规则(意思就是单人怎么交易就怎么来)来组建商队，并且正常使用商队交易! 

该MOD的目标是在不破坏游戏平衡性的前提下，让玩家可以和朋友们一起玩，也不会降低游戏中的沉浸感。此刻，牛逼的游戏之路才刚刚开始。它有一套最基本的功能，让你和你的朋友们有机会一起玩。你可以在星球的地图上看到其他玩家，与他们聊天，与商队和定居点交换商品。而发展还在继续! 你可以在群里的新闻里关注它的进展。


# Ссылки / 一些参考资料

官方页面(因为是俄语不太准确反正意思就是官方的主页)：https://vk.com/rimworldonline
(也有向作者提供资助的链接)

在Diskord的交流：https://discord.gg/5DzWrnR

电子邮件：emAnt@mail.ru

Trello的开发流程：https://trello.com/b/gXtWtDjy/onlinecity-mod-rimworld。

目前的游戏和测试服务器为194.87.95.90（至2019年10月10日）。 国内测试连接不上，最好自己搭建服务器。

# Игрокам / 玩家

目前版本的RimWorld 1.0+  1.1 正在开发

要开始玩，请照例在Mods文件夹中安装mods。它还需要安装HugsLib mod。这样一来，最小的集子就可以按照这个顺序进行。
Core
HugsLib
OnlineCity
模组必须与其他几乎所有的模组兼容。然而，这套MODs和，也许，一些设置可能会被限制在你想玩的服务器上。也就是说，在同一服务器上，某些（冲突的）MOD必须对所有玩家都是一样的。


与服务器同步，每隔5秒进行一次，完全保存--15分钟后或通过菜单退出游戏时，会进行一次同步。
不建议通过点击X来关闭游戏! 这可能会导致游戏在上次保存后失去进度。


Полезно знать/怎么用,有什么用处: 

*只要网络游戏只注重互助生存的互助，就可以了。

*其他玩家的房子和商队的颜色是蓝色的。您可以通过突出显示您的房车并右键点击它们来与它们互动。

* 转运货物时考虑到商队的载货能力。如果超载，大篷车就不能动了。通过选择房车，你可以看到它还能带多少货物。

* 为了确保从与其他玩家的交易中收到的货物存放在你的结算中的特定地点，请将所需的仓库重命名，使其包含 "trade "或 "trade "符号。

*你可以和所有在线的玩家聊天。你也可以建立一个私人渠道，当面与一个玩家交流。

* 任何点击玩家的昵称都会将该玩家添加到活动聊天中，打开私人聊天频道，或者查看您所选择的玩家的一般信息。

# Помощь в разработке / 模组发展帮助

发展是从游戏文件夹中的文件夹OnlineCity Mods构思的。

项目:
* Chat -  一起登录聊天的工具
* Converter - 一个将保存的世界文件转移到较新版本的实用程序
* RimWorldOnlineCity - 游戏中的"时尚"库
* ServerConsole - 服务器启动文件
* ServerDll - 服务器，打开指定的端口并等待玩家（根据计划）。
* UnionDll - 包含一个模型和一些代码，客户端和服务器都可以使用。

ServerConsole项目被展开到一个特殊的文件夹RimWorldOnlineCityServerOut。
RimWorldOnlineCity mod库本身到Assemblies文件夹中，这样编译后你可以在游戏中测试它。


你可以先了解一下:
* StartPoint.cs - 是我们的启动点。现在有两个，在游戏的底部面板上增加了一个按钮。而这里（在构造函数中）是第一个初始化。主要是在登录游戏后（登录或注册后）进行初始化。
* SessionClientController.cs - 这里实际上是Init（从StartPoint开始）和InitConnected（在服务器上连接和授权后立即开始工作）。还有就是有一般动作的功能。
* Dialog_MainOnlineCity.cs - 游戏的主要形式，在游戏底部面板上的按钮上运行。现在，暂时来说，当你启动表单时，会被要求连接到服务器，并启动与用户的所有对话。


## 客户端服务器通信.



### 开始交流（或者说是如何安排的？)

Клиент.

* 交流开始于Dialog_LoginForm或Dialog_Registration表单。
* 它们分别调用SessionClientController登录和注册函数。
* 他们调用SessionClientController.Connect函数。
* 它还可以从SessionClient类中调用Connect（它是作为一个单子类实现的）。
* 这里会发生以下情况（只要会话的钥匙是公开传输的）：
* 以一个字节0x00为单位向服务器发送第一个数据包。
* 作为回报，我们会收到一个对称的会话密钥，它将对流量进行加密和解密。
* 直接通信提供了ConnectClient和ConnectServer上的类开放端口。

所有进一步的交换都是通过TransObject<>函数进行的。
* 在TransObject<>中，我们将不同的数据打包到一个标准的单一容器ModelContainer中，并使用Trans方法将其传输到服务器。
* 在Trans中，我们将对象以字节为单位序列化并压缩(GZip.ZipObjByte)，加密(CryptoProvider.SymmetricEncrypt)，然后发送至服务器。
* 我们将从服务器接收到的消息解密、压缩和解序列化为同一个标准的单一ModelContainer。
它还可能包含一个错误信息。

的服务器。

当连接建立后，会发生ServerManader.ConnectionAccepted事件，在这个事件中，会产生一个单独的线程，并启动DoClient处理程序，在服务器上生成一个游戏会话作为SessionServer类，并在其中运行Do方法。

在这种方法中，我们无休止地监听数据流，接收数据包的方式和上面描述的客户端相同，之后我们使用Service方法对接收到的数据包进行 "路由"。详情请看下面的服务器。

### 要求 -- -- 答复（或如何使用）

Клиент.

对于所有类型的请求，都会在SessionClient中创建一个函数。

在其中，传输的参数被打包到模型中，TransObject<>开始向服务器传输并接收来自服务器的响应。在TransObject<>中，除了数据被传输到预期的响应和代码的接收和传输的模型（通常是奇数-请求，而答案是+1从它）。

服务器。

* 收到的请求以标准容器ModelContainer的形式被SessionServer.Service函数路由。
* 这里，根据请求代码选择服务类中的处理函数。
我们可以说，Service类相当于客户端的SessionClient（在SessionClient中我们调用函数，服务器在Service类镜像中处理）。

为了在服务器上添加一个新的处理程序，我们分别在SessionClient和Service类中添加函数，并在SessionServer.Service函数中添加一个带有新代码的条目。



## 与游戏的互动


### 连接.

基本上，游戏数据基本上都存储在ClientData数据中的SessionClientController类中。

* 该模块以Dialog_LoginForm或Dialog_Registration表格开始。
* 它们分别调用SessionClientController登录函数和注册函数，在这两个函数中，连接到服务器。
* 一旦连接成功，InitConnected就会启动。

如果服务器上的世界没有创建，并且管理员登录后，会调用Dialog_CreateWorld对话框。完成后，开始创建世界GameStarter.GameGeneration，在创建CreatingWorld后开始创建世界。这里是世界升级（还没有），并将其数据保存到服务器connect.CreateWorld（toServ）后，用户被抛入主菜单。

如果玩家在服务器上没有保存，我们就创建一个星球，在那里加载其他玩家的定居点，然后通过用户控制选择元落，再通过标准的对话框选择定居者等等。在对话结束后，游戏自然开始工作事件GameStarter.AfterStart = CreatePlayerMap;（启动函数CreatePlayerMap）。在这个功能中，游戏的第一次保存和过渡到InitGame（见下文）。

如果服务器上有一个保存，那么就下载它，在事件GameLoades.AfterLoad之后也去InitGame。

* 如果用户刚刚创建了一个游戏，或者下载了一个游戏，那么当准备好后，初始化游戏功能将被启动。
在它开始。

* 在UpdateWorldController.ClearWorld()中对世界进行预处理(到目前为止，只删除了我们所有的对象(网络玩家的定居点和他们的商队从CaravanOnline继承))。
* 准备好UpdateWorldController.InitGame()；这将使行星与服务器同步。
*运行UpdateWorld(true)的第一次更新；第一次更新的不同之处在于，我们只为我们的定居点(服务器上已经存在的)服务器ID获取安装数据，以便进行适当的通信。否则，每次下载地图时，我们都会发送说我们有新的定居点和商队。
* 运行三个周期性的定时器事件（见下文）。
* 设置一个当你以任何方式退出游戏时发生的事件。它将游戏保存到服务器中，并将其意外地打折。
### 在游戏中的定时器上的周期性事件
* 每半秒更新一次UpdateChats()聊天数据。
我们从服务器接收数据 dc = connect.UpdateChat(Data.ChatsTime)。

在这里，我们通过最后一个要求的日期和时间。作为回应，服务器会发出从过去的时间到当前时刻的所有消息。我们将服务器的新的当前时间保存到下一次请求之前。

然后运行该函数，在Data.ApplyChats(dc)中用来自服务器的数据补充当前的聊天数据。

另外，在聊天中保存了最后一个服务器响应的时间。而如果聊天没有响应，那么在属性bool ServerConnected中的ClientData中检查后，我们检查是否有断开连接（超过8秒没有响应）。


* 每隔几秒钟同步一次世界更新世界(false)
从星球上收集数据UpdateWorldController.SendToServer(第一次没有启动)

更新玩家信息、更新时间等。




* 每15分钟保存一次，并将保存的内容传输到BackgroundSaveGame服务器()。
通过运行SaveGame函数保存，并将结果保存到一个变量中，该变量将在下一次世界同步启动时被发送到服务器上。



