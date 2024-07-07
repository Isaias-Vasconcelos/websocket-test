using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace server.Controllers
{
  [ApiController]
  public class SocketController : ControllerBase
  {
    private static readonly ConcurrentBag<WebSocket> clientsConnected = [];

    [HttpGet("/")]
    public IActionResult Get()
    {
      return Ok();
    }

    [Route("/ws")]
    public async Task Channel()
    {
      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
      }

      var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
      clientsConnected.Add(websocket);

      Console.WriteLine($"-> New client conected");

      var buffer = new byte[1024 * 4];

      WebSocketReceiveResult receiveResult = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

      while (!receiveResult.CloseStatus.HasValue)
      {
        var json = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

        var msg = Encoding.UTF8.GetBytes(json);

        await SendMessages(receiveResult, msg);

        receiveResult = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
      }

      clientsConnected.TryTake(out websocket);

      await websocket!.CloseAsync(
        receiveResult.CloseStatus.Value,
        receiveResult.CloseStatusDescription,
        CancellationToken.None
      );
    }

    private static async Task SendMessages(WebSocketReceiveResult webSocketReceive, byte[] mensagem)
    {
      foreach (var socket in clientsConnected)
      {
        if (socket.State == WebSocketState.Open)
        {
          await socket.SendAsync(
            new ArraySegment<byte>(mensagem),
            webSocketReceive.MessageType,
            webSocketReceive.EndOfMessage,
            CancellationToken.None
          );
        }
      }
    }
  }
}
