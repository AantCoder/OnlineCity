# OnlineCity
OnlineCity mod for RimWorld
Author: Vasilii Ivanov // Aant
English translation by Travis Martin // T3rrabyte // Lakuna

OnlineCity is a mod for RimWorld that allows players to connect to an online server and play on the same planet. After registering, players can create their own faction and develop along with other players. You are able to watch the progress of your neighbors, their settlements, and their caravans, and you can trade food, medicine, weapons, prisoners, and other commodities with them just as you would a normal non-player faction.

The goal of the mod is to make it possible to play RimWorld with friends without ruining the balance or immersion of the game. The mod is currently in early development, and only contains minimal features to enable you to play with your friends.

# OnlineCity
OnlineCity мод для игры RimWorld
Автор Василий Иванов aka Aant

OnlineCity – это модуль сетевой игры для симулятора выживания RimWorld. Он позволяет нескольким игрокам играть на одной планете в режиме онлайн. После регистрации, вы сможете создать свое поселение и начать развиваться бок о бок с другими участниками. Вы сможете наблюдать за прогрессом соседей, их поселениями и караванами, но самое главное вы сможете оказывать друг другу вполне материальную помощь, передавая еду, медикаменты, оружие или любые другие вещи, включая поселенцев. Для этого необходимо лишь по всем правилам игры собрать караван и добраться до них! 

Цель мода – сделать возможной игру с друзьями, не нарушая баланса и не снижая уровень погружения в игру. На данный момент жизненный путь мода только начался. Он обладает минимальным набором функций, чтобы предоставить вам возможность совместной игры. Вы можете видеть других игроков на карте планеты, переписываться с ними в чате и обмениваться товарами с караванами и поселениям. А разработка продолжается! За ее прогрессом вы можете следить в новостях группы. 

# Links
Official mod page: https://vk.com/rimworldonline (includes donation links).

Discord: https://discord.gg/5DzWrnR.

Email: emAnt@mail.ru.

Track development on Trello: https://trello.com/b/gXtWtDjy/onlinecity-mod-rimworld.

Official server IP: 194.87.95.90 (as of October 2019).

# Ссылки
Официальная страница мода: https://vk.com/rimworldonline
(там же есть ссылки для финансовой помощи автору)

Общение в Дискорд: https://discord.gg/5DzWrnR

Почта emAnt@mail.ru

Процесс разработки на Trello: https://trello.com/b/gXtWtDjy/onlinecity-mod-rimworld

Текущий сервер для игры и тестов 194.87.95.90 (актуально на 10.2019) 

# Information for Players
Requires RimWorld version 1.1 or above and HugsLib.

In order to connect to a server, your mod list much match that of the server. OnlineCity will automatically synchronize your mod list with the server when you try to connect.

Your save will synchronize with the server every 5 seconds, with a full save every 15 minutes or when you disconnect.

Things to note:
* Networked games are focused on mutual assistance and survival.
* Other players' bases and caravans are blue on the world map.
* In order to force transactions with another player to appear in a certain stockpile, rename that stockpile so that its name contains "trade".
* The general chat sends messages to all players who are online. To chat privately with a player, click on their username.

# Игрокам
Текущая версия RimWorld 1.1+

Чтобы начать играть установите мод как обычно в папку Mods. Для работы также требуется установленный мод HugsLib. Таким образом минимальный набор может быть в такой последовательности:
Harmony
Core
HugsLib
OnlineCity

Мод должен быть совместим практически со всеми другими модами. Однако набор модов и, возможно, каких-то настроек может ограничиваться сервером, на котором вы хотите играть. Т.е. на одном сервере набор определенных (конфликтных) модов должен быть одинаков у всех игроков.

Синхронизация с сервером происходит каждые 5 секунд, полноценное сохранение – раз в 15 минут или при выходе из игры посредством меню. 
Не рекомендуется закрывать игру нажатием на крестик! Это может привести к потере прогресса игры с последнего сохранения. 

Полезно знать: 
* Пока сетевая игра ориентирована только на взаимопомощь в выживании. 
* Цвет домов и караванов других игроков – синий. С ними можно взаимодействовать, выделив свой караван и нажав на них правой кнопкой мыши. 
* При передаче товаров учитывайте грузоподъемность караванов. При перегрузе караван не сможет передвигаться. Выделив караван, можно узнать сколько груза он ещё может принять. 
* Чтобы товары, полученные от сделок с другими игроками, складировались в определенном месте вашего поселения, переименуйте нужный склад так, чтобы он содержал символы "торг" или "trade".
* В общем чате можно общаться со всеми игроками, которые находятся онлайн. Так же можно создать приватный канал для общения с одним игроком лично. 
* По щелчку любой кнопки мыши на никнейме игрока происходит добавление игрока в активный чат, открытие приватного канала чата, либо просмотр общей информации об игроке на выбор. 

# Information for Developers
Development is based in the OnlineCity folder in the RimWorld "Mods" folder.

Projects:
* Chat - a utility to allow players to communicate with each other on the server.
* Converter - a utility that converts a world save to newer versions.
* RimWorldOnlineCity - the modification for RimWorld.
* ServerConsole - the server startup shell.
* ServerDll - a server that opens a port and handles connections.
* UnionDll - contains code that is used by both the client and the server.

The ServerConsole project is assembled into a special folder called "RimWorldOnlineCityServerOut".

Notable files:
* StartPoint.cs is the starting point for the mod. It contais two classes that add buttons to the main menu in-game, and the constructor in which the mod is initialized. Basic initialization occurs when the player logs into a server.
* SessionClientController.cs contains the initializer itself (which is called from StartPoint.cs) and InitConnected (which starts work on the game immediately following the connection to the server).
* Dialog_MainOnlineCity.cs contains the main dialog for the game, which is opened through the button on the main menu.

# Помощь в разработке
Разработка задумывается из папки OnlineCity в игровой папке Mods.

Проекты:
* Chat - утилита через которую можно залогиниться и общаться в общем чате
* Converter - утилита для перевода файла сохраненного мира на более новые версии
* RimWorldOnlineCity - библиотека мода прикрепляемая к игре
* ServerConsole - оболочка запуска сервера
* ServerDll - сервер, открывает указанный порт и радует игроков (по задумке)
* UnionDll - содержит модель и некоторый код, который используется и клиентом и сервером

Проект сервера ServerConsole разворачивается в специальную папку RimWorldOnlineCityServerOut 
Сама библиотека мода RimWorldOnlineCity в папку Assemblies так, что после компиляции можно сразу тестировать в игре.

Начать знакомство можно с:
* StartPoint.cs - здесь точка запуска нашего мода. Сейчас тут два класса которые добавляют кнопку на нижней панели в игре. И здесь же (в конструкторе) идет первая инициализация. Основная инициализация происходит после входа в игру (после логина или регистрации).
* SessionClientController.cs - тут собственно Init (который запускается из StartPoint), и InitConnected (начало работы над игрой сразу после коннекта и авторизации на сервере). А также тут функции с общими действиями.
* Dialog_MainOnlineCity.cs - основная форма игры, которая запускается по кнопке на нижней панели в игре. Сейчас, временно, при старте формы происходит запрос подключения к серверу и начало всех диалогов с пользователем.

## Связь клиент сервер // Client-Server Communication
### How the Connection Works
Client:
* The connection begins on Dialog_LoginForm or Dialog_RegistrationForm
* In SessionClientController, the dialogs call the .Login and .Registration methods, respectively.
* Those methods call the SessionClientController.Connect method to create a connection.
* Also calls the SessionClient.Connect method (which is implemented as a singleton), which sends one packet to the server (one byte // 0x00) and receives a key for the connection as a response (which is then used to encrypt and decrypt information to/from the server).
* Direct communication is provided by ConnectClient and the class that opens the port on the ConnectServer.

All further communication occurs through the TransObject<> method.
* TransObject<> is used to pack data into a single ModelContainer and transfer it to the server using .Trans.
* In .Trans, the object is serialized to bytes, compressed (using GZip.ZipObjByte), and encrypted (using CryptoProvider.SymetricEncrypt).
* The same container is used to decompress and deserialize the message from the server.
* Also used to send error messages.

Server:
* A ServerManager.ConnectionAccepted event is raised
* A separate thread is created.
* The DoClient handler is launched
* A game session is created (SessionServer) and runs the .Do method.
* The .Do method listens to the stream of packets from the client.
* Information is routed from the .Do method using the .Service method.

### Начало обмена (или как оно устроено)
Клиент.
* Начало обмена начинается на форме Dialog_LoginForm, либо Dialog_Registration.
* Они вызывают функции SessionClientController Login и Registration соответственно.
* Они вызывают создание подключения функцией SessionClientController.Connect.
* Она же вызывает Connect из класса SessionClient (он реализован как синглтон).
* Здесь происходит следующее (пока ключ на сессию передаётся открыто):
*  Посылаем серверу первый пакет в один байт 0x00
*  В ответ нам приходит симметричный ключ на сессию, которым мы будем шифровать и расшифровывать трафик
* Непосредственную связь обеспечивает ConnectClient и класс открывающий порт на сервере ConnectServer.

Весь дальнейший обмен происходит через функцию TransObject<>
* В TransObject<> запаковываем различные данные в стандартный единый контейнер ModelContainer и передаем серверу методом Trans.
* В Trans мы сериализуем объект в байты и сжимаем (GZip.ZipObjByte), шифруем (CryptoProvider.SymmetricEncrypt) и отправляем на сервер.
* Принитое сообщение от сервера расшифровываем, разжимаем и десериализуем в такой же стандартный единый контейнер ModelContainer.
В нем, также, может быть передано сообщение об ошибке.

Сервер.

При установлении подключения возникает событие ServerManager.ConnectionAccepted в котором порождается отдельная нить и запускается обработчик DoClient, который порождает игровую сессию на сервере в виде класса SessionServer и запускает в нём метод Do.

В этом методе мы бесконечно слушаем поток и принимаем пакеты аналогично описанному у клиента выше, после чего полученный пакет "маршрутизируем" методом Service. Подробнее смотри ниже про сервер.

### How Requests and Responses Work
Client:
* For each request, a separate function is created in the SessionClient.
* In the SessionClient, transmitted parameters are packed into a .TransObject<>, which is sent to the server.
* .TransObject<> also contains the model of the expected response and the reception-transmission codes.
* The response to an odd request is +1.

Server:
* Requests are received in the form of a ModelContainer and are routed by the SessionServer.Service method.
* In the Service method, the method used to process the data in the Service class is selected (based on the data).
* The Service class is an analogue of the SessionClient.
* To add a new handler to the server, a new method must be added to both the SessionClient and Service classes, and an entry with new codes must be added to the SessionServer.Service method.

### Запрос - ответ (или как пользоваться)
Клиент.

Для всех типов запросов создается своя функция в SessionClient.

В ней происходит запаковка передаваемых параметров в модель и запускается TransObject<> для передачи на сервер и приема ответа от него. В TransObject<> помимо данных передается также модель ожидаемого ответа и коды приема-передачи (обычно нечётное - запрос, а ответ это +1 от него).

Сервер.
* Принятый запрос в виде стандартного контейнера ModelContainer маршрутизируется функцией SessionServer.Service.
* Здесь на основе кода запроса выбирается обрабатывающая функция в классе Service.
Можно сказать, что класс Service это аналог SessionClient для клиента (в том смысле, что в SessionClient мы вызываем функцию, а сервер обрабатывает в её зеркальном отражении в классе Service)

Для добавления нового обработчика на сервере соответственно добавляем функции в классы SessionClient и Service, и запись с новыми кодами внутри функции SessionServer.Service.

## Взаимодействие с игрой // Interaction with the Game
### Connection
Game data is stored in the SessionClientController class in ClientData.

Step-by-step if the server does not have a planet:
* The connection begins on Dialog_LoginForm or Dialog_RegistrationForm
* In SessionClientController, the dialogs call the .Login and .Registration methods, respectively.
* After the methods form a connection, the .InitConnected method is called.
* If the server world is not created, the connected user is registered as the administrator and the Dialog_CreateWorld is opened.
* After the user is finished with the dialog, the world is generated (using GameStarter.GameGeneration).
* After the world is generated, the .CreatingWorld method is launched. This method updates the world and saves its data to the server (using CreateWorld.toServ).
* The user is then returned to the main menu.

Step-by-step if the player is connecting to the server for the first time:
* A planet is created, and the other players' settlements are loaded.
* The user selects a landing site and pawns as normal.
* When the game starts as normal, the GameStarter.AfterStart event occurs and the .CreatePlayerMap method runs.
* This method saves the game at first, then calls the .InitGame method.

Step-by-step for normal connections:
* The player loads the world saved on the server.
* The GameLoades.AfterLoad event occurs, which calls the .InitGame method.

Functionality of the .InitGame method:
* Starts world preprocessing in the UpdateWorldController.ClearWorld method. For now, settlements and caravans of networked players are inherited from CaravanOnline.
* Prepares the updater in the UpdateWorldController.InitGame method, which synchronizes the planet with the server.
* Launches the first world update. The first call is different from others in that it receives data used to create settlements which exist on the server.
* Starts three timer events (information below).
* Sets up the event that occurs when you exit the game.

### Подключение
В основном игровые данные храняться в классе SessionClientController в ClientData Data.

* Начало работы мода начинается на форме Dialog_LoginForm, либо Dialog_Registration.
* Они вызывают функции SessionClientController Login и Registration соответственно, в которых происходит подключение к серверу.
* После успешного подключения запускается InitConnected:

Если мир на сервере не создан, и вошел админ, то вызывается диалог создания мира Dialog_CreateWorld. По его завершении запускается создание мира GameStarter.GameGeneration, после окончании создания запускается CreatingWorld. Тут идет модернизация мира (пока отсутствует) и сохранение его данных на сервер connect.CreateWorld(toServ) После чего пользователя выкидывает в главное меню.

Если у игрока нет сейва на сервере, то создаем планету, загружаем туда поселения других игроков и передаем управение пользователя для выбора мета высадки и, потом, стандартным диалогам выбора поселенцев и прочим. По окончании диалога естественным образом игра стартует и срабоатывает событие GameStarter.AfterStart = CreatePlayerMap; (запускается функция CreatePlayerMap). В этой функции происходит первой сохранение игры и переход к InitGame (см ниже).

Если сейв на сервере есть, то загружаем его и по событию GameLoades.AfterLoad переходим также к InitGame

* Если пользователь только что создал игру, или загрузил её, то в момент готовности будет запущена вункция InitGame.
В ней запускается:

* Предобработка мира в UpdateWorldController.ClearWorld() (пока только удаление все наших объектов (поселения сетевых игроков и их караваны наследуются от CaravanOnline)) 
* Подготовка апдейтера UpdateWorldController.InitGame(); который будет синхронизировать планету с сервером
* Запуск первого обновления мир UpdateWorld(true); Первое отличается тем, что мы только получаем данные, чтобы установить для наших поселений (которые уже есть на сервере) serverId для правильной связи. Иначе при загрузке карты мы бы каждый раз отправляли, что у нас новые поселения и караваны.
* Запуск трёх циклических событий по таймеру (см ниже)
* Установка события возникающего при выходе из игры любым способом. В неё происходит внеочередное сохранение игры на сервер и дисконнект.

### Timer Events
Chat:
* Updated every 0.5 seconds (using .UpdateChats).
* The time of the last request is passed through .UpdateChat, and the server responds with all chat messages sent since the last request.
* Data.ApplyChats adds current chat data to chat data from the server.
* Also checks for player disconnects that didn't happen normally (i.e. through a game crash or force close).

World:
* Every few seconds, the world updates. Data is collected from the planet (using UpdateWorldController.SendToServer).
* Data is loaded to the planet (using UpdateWorldController.LoadFromServer).
* Every 15 minutes, the full world is saved to the BackgroundSaveGame (using the .SaveGame method).

### Циклические события во время игры по таймеру
* Каждые пол сек. обновление данных чата UpdateChats()
Получаем данные с сервера dc = connect.UpdateChat(Data.ChatsTime);

Здесь передаем дату и время прошлого запроса. В ответ сервер выдает все сообщения от переданного времени до текущего момента. Новое текущее время сервера сохраняем до следующего запроса.

Далее запускаем функцию дополнения текущих данных чата теми, что пришли с сервера в Data.ApplyChats(dc);

Помимо этого в чате сохраняется время последнего отклика сервера. И если ответа на чат нет, то по проверке в ClientData в свойстве bool ServerConnected проверяем на диссконнект (не было отклика более 8 секунд)

* Каждые несколько секунд синхронизация мира UpdateWorld(false)
Cобираем данные с планеты UpdateWorldController.SendToServer (не запускается первый раз)

Обновляем информацию по игрокам, время обновления и прочее

Обновляем планету UpdateWorldController.LoadFromServer

* Каждые 15 минут сохранение и передача сейва на сервер BackgroundSaveGame()
Сохраняем запуская функцию сохранения SaveGame, а результат сохраняем в переменную, которую отправит на сервер следующий запуск синхронизаци мира