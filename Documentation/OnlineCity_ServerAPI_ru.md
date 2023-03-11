На сервере существует API по которому можно отправлять запросы по протоколу HTTP POST в JSON формате с кодировкой UTF-8.

Запросы посылаются на тот же порт, куда происходит и игровое подключение. После открытия TCP соединения следует посылать POST с JSON запросом в кодировке UTF-8. Допускается только один запрос на подключение, после отправки ответа соединение закрывается.

Пример запроса:
{Q:"s"}
Ответ:
{"OnlineCount":1, "PlayerCount":674, "Onlines":["TheCrazeMan"]}

Все актуальные команды запроса смотри в файле OnlineCityAPIModel.cs. Этот файл вместе с OnlineCityAPIClient.cs можно использовать для запросов. Пример его использования:

var api = new OnlineCityAPIClient("127.0.0.1", 19019);
string response = null;

api.Request("{Q:\"s\"}", (res) => { response = res; });
Thread.Sleep(5000);

api.RequestStatus((res) => { response = $"Online {res.OnlineCount}/{res.PlayerCount}"; });
Thread.Sleep(5000);

api.RequestPlayer("TheCrazeMan", (res) => { response = res.Players[0].Login; });
Thread.Sleep(5000);
