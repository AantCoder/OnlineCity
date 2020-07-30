# OnlineCity
OnlineCity mod for RimWorld
Author: Vasilii Ivanov // Aant
English translation by Travis Martin // T3rrabyte // Lakuna

OnlineCity is a mod for RimWorld that allows players to connect to an online server and play on the same planet. After registering, players can create their own faction and develop along with other players. You are able to watch the progress of your neighbors, their settlements, and their caravans, and you can trade food, medicine, weapons, prisoners, and other commodities with them just as you would a normal non-player faction.

The goal of the mod is to make it possible to play RimWorld with friends without ruining the balance or immersion of the game. The mod is currently in early development, and only contains minimal features to enable you to play with your friends.

# OnlineCity
OnlineCity ��� ��� ���� RimWorld
����� ������� ������ aka Aant

OnlineCity � ��� ������ ������� ���� ��� ���������� ��������� RimWorld. �� ��������� ���������� ������� ������ �� ����� ������� � ������ ������. ����� �����������, �� ������� ������� ���� ��������� � ������ ����������� ��� � ��� � ������� �����������. �� ������� ��������� �� ���������� �������, �� ����������� � ����������, �� ����� ������� �� ������� ��������� ���� ����� ������ ������������ ������, ��������� ���, �����������, ������ ��� ����� ������ ����, ������� ����������. ��� ����� ���������� ���� �� ���� �������� ���� ������� ������� � ��������� �� ���! 

���� ���� � ������� ��������� ���� � ��������, �� ������� ������� � �� ������ ������� ���������� � ����. �� ������ ������ ��������� ���� ���� ������ �������. �� �������� ����������� ������� �������, ����� ������������ ��� ����������� ���������� ����. �� ������ ������ ������ ������� �� ����� �������, �������������� � ���� � ���� � ������������ �������� � ���������� � ����������. � ���������� ������������! �� �� ���������� �� ������ ������� � �������� ������. 

# Links
Official mod page: https://vk.com/rimworldonline (includes donation links).

Discord: https://discord.gg/5DzWrnR.

Email: emAnt@mail.ru.

Track development on Trello: https://trello.com/b/gXtWtDjy/onlinecity-mod-rimworld.

Official server IP: 194.87.95.90 (as of October 2019).

# ������
����������� �������� ����: https://vk.com/rimworldonline
(��� �� ���� ������ ��� ���������� ������ ������)

������� � �������: https://discord.gg/5DzWrnR

����� emAnt@mail.ru

������� ���������� �� Trello: https://trello.com/b/gXtWtDjy/onlinecity-mod-rimworld

������� ������ ��� ���� � ������ 194.87.95.90 (��������� �� 10.2019) 

# Information for Players
Requires RimWorld version 1.1 or above and HugsLib.

In order to connect to a server, your mod list much match that of the server. OnlineCity will automatically synchronize your mod list with the server when you try to connect.

Your save will synchronize with the server every 5 seconds, with a full save every 15 minutes or when you disconnect.

Things to note:
* Networked games are focused on mutual assistance and survival.
* Other players' bases and caravans are blue on the world map.
* In order to force transactions with another player to appear in a certain stockpile, rename that stockpile so that its name contains "trade".
* The general chat sends messages to all players who are online. To chat privately with a player, click on their username.

# �������
������� ������ RimWorld 1.1+

����� ������ ������ ���������� ��� ��� ������ � ����� Mods. ��� ������ ����� ��������� ������������� ��� HugsLib. ����� ������� ����������� ����� ����� ���� � ����� ������������������:
Harmony
Core
HugsLib
OnlineCity

��� ������ ���� ��������� ����������� �� ����� ������� ������. ������ ����� ����� �, ��������, �����-�� �������� ����� �������������� ��������, �� ������� �� ������ ������. �.�. �� ����� ������� ����� ������������ (�����������) ����� ������ ���� �������� � ���� �������.

������������� � �������� ���������� ������ 5 ������, ����������� ���������� � ��� � 15 ����� ��� ��� ������ �� ���� ����������� ����. 
�� ������������� ��������� ���� �������� �� �������! ��� ����� �������� � ������ ��������� ���� � ���������� ����������. 

������� �����: 
* ���� ������� ���� ������������� ������ �� ������������ � ���������. 
* ���� ����� � ��������� ������ ������� � �����. � ���� ����� �����������������, ������� ���� ������� � ����� �� ��� ������ ������� ����. 
* ��� �������� ������� ���������� ���������������� ���������. ��� ��������� ������� �� ������ �������������. ������� �������, ����� ������ ������� ����� �� ��� ����� �������. 
* ����� ������, ���������� �� ������ � ������� ��������, �������������� � ������������ ����� ������ ���������, ������������ ������ ����� ���, ����� �� �������� ������� "����" ��� "trade".
* � ����� ���� ����� �������� �� ����� ��������, ������� ��������� ������. ��� �� ����� ������� ��������� ����� ��� ������� � ����� ������� �����. 
* �� ������ ����� ������ ���� �� �������� ������ ���������� ���������� ������ � �������� ���, �������� ���������� ������ ����, ���� �������� ����� ���������� �� ������ �� �����. 

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

# ������ � ����������
���������� ������������ �� ����� OnlineCity � ������� ����� Mods.

�������:
* Chat - ������� ����� ������� ����� ������������ � �������� � ����� ����
* Converter - ������� ��� �������� ����� ������������ ���� �� ����� ����� ������
* RimWorldOnlineCity - ���������� ���� ������������� � ����
* ServerConsole - �������� ������� �������
* ServerDll - ������, ��������� ��������� ���� � ������ ������� (�� �������)
* UnionDll - �������� ������ � ��������� ���, ������� ������������ � �������� � ��������

������ ������� ServerConsole ��������������� � ����������� ����� RimWorldOnlineCityServerOut 
���� ���������� ���� RimWorldOnlineCity � ����� Assemblies ���, ��� ����� ���������� ����� ����� ����������� � ����.

������ ���������� ����� �:
* StartPoint.cs - ����� ����� ������� ������ ����. ������ ��� ��� ������ ������� ��������� ������ �� ������ ������ � ����. � ����� �� (� ������������) ���� ������ �������������. �������� ������������� ���������� ����� ����� � ���� (����� ������ ��� �����������).
* SessionClientController.cs - ��� ���������� Init (������� ����������� �� StartPoint), � InitConnected (������ ������ ��� ����� ����� ����� �������� � ����������� �� �������). � ����� ��� ������� � ������ ����������.
* Dialog_MainOnlineCity.cs - �������� ����� ����, ������� ����������� �� ������ �� ������ ������ � ����. ������, ��������, ��� ������ ����� ���������� ������ ����������� � ������� � ������ ���� �������� � �������������.

## ����� ������ ������ // Client-Server Communication
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

### ������ ������ (��� ��� ��� ��������)
������.
* ������ ������ ���������� �� ����� Dialog_LoginForm, ���� Dialog_Registration.
* ��� �������� ������� SessionClientController Login � Registration ��������������.
* ��� �������� �������� ����������� �������� SessionClientController.Connect.
* ��� �� �������� Connect �� ������ SessionClient (�� ���������� ��� ��������).
* ����� ���������� ��������� (���� ���� �� ������ ��������� �������):
*  �������� ������� ������ ����� � ���� ���� 0x00
*  � ����� ��� �������� ������������ ���� �� ������, ������� �� ����� ��������� � �������������� ������
* ���������������� ����� ������������ ConnectClient � ����� ����������� ���� �� ������� ConnectServer.

���� ���������� ����� ���������� ����� ������� TransObject<>
* � TransObject<> ������������ ��������� ������ � ����������� ������ ��������� ModelContainer � �������� ������� ������� Trans.
* � Trans �� ����������� ������ � ����� � ������� (GZip.ZipObjByte), ������� (CryptoProvider.SymmetricEncrypt) � ���������� �� ������.
* �������� ��������� �� ������� ��������������, ��������� � ������������� � ����� �� ����������� ������ ��������� ModelContainer.
� ���, �����, ����� ���� �������� ��������� �� ������.

������.

��� ������������ ����������� ��������� ������� ServerManager.ConnectionAccepted � ������� ����������� ��������� ���� � ����������� ���������� DoClient, ������� ��������� ������� ������ �� ������� � ���� ������ SessionServer � ��������� � �� ����� Do.

� ���� ������ �� ���������� ������� ����� � ��������� ������ ���������� ���������� � ������� ����, ����� ���� ���������� ����� "��������������" ������� Service. ��������� ������ ���� ��� ������.

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

### ������ - ����� (��� ��� ������������)
������.

��� ���� ����� �������� ��������� ���� ������� � SessionClient.

� ��� ���������� ��������� ������������ ���������� � ������ � ����������� TransObject<> ��� �������� �� ������ � ������ ������ �� ����. � TransObject<> ������ ������ ���������� ����� ������ ���������� ������ � ���� ������-�������� (������ �������� - ������, � ����� ��� +1 �� ����).

������.
* �������� ������ � ���� ������������ ���������� ModelContainer ���������������� �������� SessionServer.Service.
* ����� �� ������ ���� ������� ���������� �������������� ������� � ������ Service.
����� �������, ��� ����� Service ��� ������ SessionClient ��� ������� (� ��� ������, ��� � SessionClient �� �������� �������, � ������ ������������ � � ���������� ��������� � ������ Service)

��� ���������� ������ ����������� �� ������� �������������� ��������� ������� � ������ SessionClient � Service, � ������ � ������ ������ ������ ������� SessionServer.Service.

## �������������� � ����� // Interaction with the Game
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

### �����������
� �������� ������� ������ ��������� � ������ SessionClientController � ClientData Data.

* ������ ������ ���� ���������� �� ����� Dialog_LoginForm, ���� Dialog_Registration.
* ��� �������� ������� SessionClientController Login � Registration ��������������, � ������� ���������� ����������� � �������.
* ����� ��������� ����������� ����������� InitConnected:

���� ��� �� ������� �� ������, � ����� �����, �� ���������� ������ �������� ���� Dialog_CreateWorld. �� ��� ���������� ����������� �������� ���� GameStarter.GameGeneration, ����� ��������� �������� ����������� CreatingWorld. ��� ���� ������������ ���� (���� �����������) � ���������� ��� ������ �� ������ connect.CreateWorld(toServ) ����� ���� ������������ ���������� � ������� ����.

���� � ������ ��� ����� �� �������, �� ������� �������, ��������� ���� ��������� ������ ������� � �������� ��������� ������������ ��� ������ ���� ������� �, �����, ����������� �������� ������ ���������� � ������. �� ��������� ������� ������������ ������� ���� �������� � ������������ ������� GameStarter.AfterStart = CreatePlayerMap; (����������� ������� CreatePlayerMap). � ���� ������� ���������� ������ ���������� ���� � ������� � InitGame (�� ����).

���� ���� �� ������� ����, �� ��������� ��� � �� ������� GameLoades.AfterLoad ��������� ����� � InitGame

* ���� ������������ ������ ��� ������ ����, ��� �������� �, �� � ������ ���������� ����� �������� ������� InitGame.
� ��� �����������:

* ������������� ���� � UpdateWorldController.ClearWorld() (���� ������ �������� ��� ����� �������� (��������� ������� ������� � �� �������� ����������� �� CaravanOnline)) 
* ���������� ��������� UpdateWorldController.InitGame(); ������� ����� ���������������� ������� � ��������
* ������ ������� ���������� ��� UpdateWorld(true); ������ ���������� ���, ��� �� ������ �������� ������, ����� ���������� ��� ����� ��������� (������� ��� ���� �� �������) serverId ��� ���������� �����. ����� ��� �������� ����� �� �� ������ ��� ����������, ��� � ��� ����� ��������� � ��������.
* ������ ��� ����������� ������� �� ������� (�� ����)
* ��������� ������� ������������ ��� ������ �� ���� ����� ��������. � �� ���������� ������������ ���������� ���� �� ������ � ����������.

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

### ����������� ������� �� ����� ���� �� �������
* ������ ��� ���. ���������� ������ ���� UpdateChats()
�������� ������ � ������� dc = connect.UpdateChat(Data.ChatsTime);

����� �������� ���� � ����� �������� �������. � ����� ������ ������ ��� ��������� �� ����������� ������� �� �������� �������. ����� ������� ����� ������� ��������� �� ���������� �������.

����� ��������� ������� ���������� ������� ������ ���� ����, ��� ������ � ������� � Data.ApplyChats(dc);

������ ����� � ���� ����������� ����� ���������� ������� �������. � ���� ������ �� ��� ���, �� �� �������� � ClientData � �������� bool ServerConnected ��������� �� ����������� (�� ���� ������� ����� 8 ������)

* ������ ��������� ������ ������������� ���� UpdateWorld(false)
C������� ������ � ������� UpdateWorldController.SendToServer (�� ����������� ������ ���)

��������� ���������� �� �������, ����� ���������� � ������

��������� ������� UpdateWorldController.LoadFromServer

* ������ 15 ����� ���������� � �������� ����� �� ������ BackgroundSaveGame()
��������� �������� ������� ���������� SaveGame, � ��������� ��������� � ����������, ������� �������� �� ������ ��������� ������ ������������ ����