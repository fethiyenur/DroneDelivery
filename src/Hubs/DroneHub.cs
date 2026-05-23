using Microsoft.AspNetCore.SignalR;

namespace DroneKurye.Hubs;

public class DroneHub : Hub
{
    // Client'lar bağlandığında gruba ekle
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "DroneWatchers");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "DroneWatchers");
        await base.OnDisconnectedAsync(exception);
    }

    // Client simülasyonu başlatabilir (admin)
    public async Task StartSimulation()
    {
        await Clients.Caller.SendAsync("SimulationStatus", "started");
    }

    public async Task StopSimulation()
    {
        await Clients.Caller.SendAsync("SimulationStatus", "stopped");
    }
}
