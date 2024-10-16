using BlobHandles;
using BuildSoft.OscCore;
using System.Text;
using VRC.OSCQuery;

class Program
{
    private static string NowVersion = "V0.0.1";

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        OSCQueryService _oscQuery;
        OscServer _receiver;

        string serverName = "YozoLab VRChat to SlimeVR OSC";
        var port = Extensions.GetAvailableTcpPort();
        var udpPort = Extensions.GetAvailableUdpPort();
        _receiver = OscServer.GetOrCreate(udpPort);

        _receiver.AddMonitorCallback(OnMessageReceived);

        _oscQuery = new OSCQueryServiceBuilder()
            .WithServiceName(serverName)
            .WithTcpPort(port)
            .WithUdpPort(udpPort)
            .StartHttpServer()
            .AdvertiseOSC()
            .AdvertiseOSCQuery()
            .Build();

        _oscQuery.RefreshServices();

        Console.WriteLine($"{serverName} : {NowVersion}");
        Console.WriteLine($"{serverName} running at {_oscQuery.HostIP} tcp:{port} osc: {udpPort}");

        _oscQuery.AddEndpoint<string>("/avatar/change", Attributes.AccessValues.WriteOnly);

        Console.WriteLine("await for incoming params...");

        await Task.Delay(-1);

        _receiver?.Dispose();
        _oscQuery?.Dispose();
    }

    private const string SlimeVRYawResetAddress = "/avatar/parameters/SlimeVRYawReset";
    private const string SlimeVRFullResetAddress = "/avatar/parameters/SlimeVRFullReset";

    private static void OnMessageReceived(BlobString address, OscMessageValues values)
    {
        string addressStr = address.ToString();
        StringBuilder messageBuilder = new StringBuilder();

        if (addressStr == SlimeVRYawResetAddress)
        {
            ProcessMessage(values, "SlimeVRYawReset", "^%+U", messageBuilder);
        }
        else if (addressStr == SlimeVRFullResetAddress)
        {
            ProcessMessage(values, "SlimeVRFullReset", "^%+Y", messageBuilder);
        }

        if (messageBuilder.Length > 0)
        {
            Console.WriteLine($"Received {addressStr} : {messageBuilder}");
        }
    }

    private static void ProcessMessage(OscMessageValues values, string resetType, string keyCombination, StringBuilder messageBuilder)
    {
        values.ForEachElement((i, typeTag) =>
        {
            if (typeTag == TypeTag.True || typeTag == TypeTag.False)
            {
                bool boolValue = values.ReadBooleanElement(i);
                if (boolValue)
                {
                    Console.WriteLine($"Received : {resetType}");
                    Console.WriteLine($"------{resetType}------");
                    SendKeys.SendWait(keyCombination);
                }
            }
            else
            {
                messageBuilder.Append($" {GetStringForValue(values, i, typeTag)}");
            }
        });
    }

    private static string GetStringForValue(OscMessageValues values, int i, TypeTag typeTag)
    {
        return typeTag switch
        {
            TypeTag.Int32 => values.ReadIntElement(i).ToString(),
            TypeTag.String => values.ReadStringElement(i),
            TypeTag.True or TypeTag.False => values.ReadBooleanElement(i).ToString(),
            TypeTag.Float32 => values.ReadFloatElement(i).ToString(),
            _ => "",
        };
    }
}
