There is an API on the server through which you can send requests via the HTTP POST protocol in JSON format with UTF-8 encoding.

Requests are sent to the same port where the game connection is made. After opening a TCP connection, send a POST with a JSON request in UTF-8 encoding. Only one connection request is allowed, after the response is sent, the connection is closed.

Request example:
```
{Q:"s"}
```
Response:
```
{"OnlineCount":1, "PlayerCount":674, "Onlines":["TheCrazeMan"]}
```

See the OnlineCityAPIModel.cs file for all relevant query commands. This file, along with OnlineCityAPIClient.cs, can be used for queries. An example of its use in C#:
```
var api = new OnlineCityAPIClient("127.0.0.1", 19019);
string response = null;

api.Request("{Q:\"s\"}", (res) => { response = res; });
Thread.Sleep(5000);

api.RequestStatus((res) => { response = $"Online {res.OnlineCount}/{res.PlayerCount}"; });
Thread.Sleep(5000);

api.RequestPlayer("TheCrazeMan", (res) => { response = res.Players[0].Login; });
Thread.Sleep(5000);
```

For a quick check, you can use the telnet console command. To do this, run telnet (make sure you have it, otherwise find on the Internet how to activate it).
Then copy the following text to clipboard
```
o 127.0.0.1 19019
POST / HTTP/1.1

{Q:"s"}
```
Go to the telnet window and paste the command by pressing Ctrl+v. A typical response might be:
```


       HTTP/1.0 200 OK
Content-Type: application/json; charset=utf-8
Connection: close

{"OnlineCount":0,"PlayerCount":2,"Onlines":[]}
```