using Newtonsoft.Json;
using SimpleGameServerCS;

class Program
{
    private const           int    WAIT_TIME = 3;
    private static readonly Server Server    = new();
    private static readonly string Path      = $"{AppDomain.CurrentDomain.BaseDirectory}/Settings.json";

    static void Main(string[] args)
    {
        bool _settingExist = LoadSetting(out int _port);
        StartServer(_port, _settingExist);
        while (true) { }
    }

    private static async void StartServer(int _port, bool _settingExist)
    {
        if (_settingExist)
        {
            Console.WriteLine($"Server started by Port : {_port} in {WAIT_TIME}s \n" +
                              $"Press any key to change Port");
            bool _changePort = await WaitInput();
            if (_changePort)
            {
                bool _validPort = false;
                while (_validPort is false)
                    _validPort = CheckSetValidPort(_validPort);
            }
        }
        else
        {
            Console.WriteLine("Setting file not found.");
            bool _validPort                        = false;
            while (_validPort is false) 
                _validPort = CheckSetValidPort(_validPort);
        }
        
        Console.WriteLine("Launch server...");
        Server.Start(_port);
    }

    private static bool CheckSetValidPort(bool _validPort)
    {
        Console.WriteLine("Enter the port number to start the server");
        string? _enteredPort = Console.ReadLine();

        if (int.TryParse(_enteredPort, out int _iPort) is false ||
            _iPort is <= 0 or >= 65535)
            Console.WriteLine("Invalid port number.");
        else
        {
            Console.Write("You entered ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{_iPort}");
            Console.ResetColor();
            Console.Write(".\nConfirm? (Y/N)");

            string? _confirm = Console.ReadLine()?.ToUpper();

            if (_confirm is not ("Y" or "YES")) return _validPort;

            _validPort = true;
            SaveSetting(_iPort);
        }

        return _validPort;
    }

    private static async Task<bool> WaitInput()
    {
        int  _waitedMs          = 0;
        bool _entered         = false;
        DateTime  _lastCheckedTime = DateTime.Now;
        while (_waitedMs / 1000 < WAIT_TIME)
        {
            await Task.Delay(100);
            _waitedMs          += (DateTime.Now - _lastCheckedTime).Milliseconds;
            _lastCheckedTime =  DateTime.Now;

            if (Console.KeyAvailable)
            {
                _entered = true;
                while (Console.KeyAvailable)
                    Console.ReadKey(true); //버퍼 비우기
                break;
            }
        }

        if (_entered)
            return true;

        Console.WriteLine("None entered. Continue...");

        return false;
    }

    private static void SaveSetting(int _enteredPort)
    {
        ServerSetting _setting          = new(_enteredPort);
        string        _serializedObject = JsonConvert.SerializeObject(_setting);
        File.WriteAllText(Path, _serializedObject);
        Console.WriteLine($"File saved at {Path}");
    }

    private static bool LoadSetting(out int _port)
    {
        if (File.Exists(Path))
        {
            Console.WriteLine($"File loaded at {Path}");

            string _setting = File.ReadAllText(Path);
            
            ServerSetting _serverSetting = JsonConvert.DeserializeObject<ServerSetting>(_setting);
            _port = _serverSetting.port;
            
            return true;
        }

        _port = 0;
        return false;
    }

    struct ServerSetting
    {
        public int port;

        public ServerSetting(int _port)
        {
            port = _port;
        }
    }
}