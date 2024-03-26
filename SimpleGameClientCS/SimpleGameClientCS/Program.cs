using System.Net;
using Newtonsoft.Json;
using SimpleGameClientCS;

class Program
{
    private const           int    WAIT_TIME = 2;
    private static readonly string Path      = $"{AppDomain.CurrentDomain.BaseDirectory}/Settings.json";
    private static readonly Client Client    = new();

    static void Main(string[] args)
    {
        bool _settingExist = LoadSetting(out IPAddress _ip, out int _port);
        StartClient(_ip, _port, _settingExist);
        while (true) { }
    }

    private static async void StartClient(IPAddress _ip, int _port, bool _settingExist)
    {
        if (_settingExist)
        {
            Console.WriteLine($"Attempt to connect to: {_ip}:{_port} in {WAIT_TIME}s \n" +
                              $"Press any key to change destination");
            bool _changeDst = await WaitInput();
            if (_changeDst)
            {
                bool _validData = false;
                while (_validData is false)
                    _validData = CheckSetValidDst(_validData);
            }
        }
        else
        {
            Console.WriteLine("Setting file not found.");
            bool _validData = false;
            while (_validData is false)
                _validData = CheckSetValidDst(_validData);
        }

        Console.WriteLine("Attempt to connect to the server...");
        Client.Start(_ip, _port);
    }

    private static bool CheckSetValidDst(bool _validPort)
    {
        Console.WriteLine("Enter the ip:port to connect");
        string? _enteredDst = Console.ReadLine();
        if (_enteredDst is null)
        {
            Console.WriteLine("Invalid ip:port format.");
            return false;
        }

        int _colonIndex = _enteredDst.IndexOf(':');
        if (_colonIndex is -1)
        {
            Console.WriteLine("Invalid ip:port format.");
            return false;
        }

        string[] _split = _enteredDst.Split(':');
        if (int.TryParse(_split[1], out int _port) is false ||
            _port is <= 0 or >= 65535)
            Console.WriteLine("Invalid port number.");

        IPAddress _ip = IPAddress.None;
        try
        {
            _ip = IPAddress.Parse(_split[0]);
        }
        catch (Exception _e)
        {
            Console.WriteLine(_e);
        }

        {
            Console.Write("You entered ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{_ip}:{_port}");
            Console.ResetColor();
            Console.Write(".\nConfirm? (Y/N)");

            string? _confirm = Console.ReadLine()?.ToUpper();

            if (_confirm is not ("Y" or "YES"))
                return _validPort;

            _validPort = true;
            SaveSetting(_ip, _port);
        }

        return _validPort;
    }

    private static async Task<bool> WaitInput()
    {
        int      _waitedMs        = 0;
        bool     _entered         = false;
        DateTime _lastCheckedTime = DateTime.Now;
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"{WAIT_TIME}s left");
        Console.ForegroundColor = ConsoleColor.Yellow;
        while (_waitedMs / 1000 < WAIT_TIME)
        {
            await Task.Delay(100);
            _waitedMs        += (DateTime.Now - _lastCheckedTime).Milliseconds;
            _lastCheckedTime =  DateTime.Now;
            Console.SetCursorPosition(0, Console.CursorTop);

            float _remain = WAIT_TIME - _waitedMs / 1000;
            Console.Write($"{_remain}s left: ");

            if (Console.KeyAvailable)
            {
                _entered = true;
                while (Console.KeyAvailable)
                    Console.ReadKey(true); //버퍼 비우기
                break;
            }
        }

        Console.ResetColor();
        Console.WriteLine();

        if (_entered)
            return true;

        Console.WriteLine("None entered. Continue...");

        return false;
    }

    private static void SaveSetting(IPAddress _ip, int _enteredPort)
    {
        ClientSetting _setting          = new(_ip, _enteredPort);
        string        _serializedObject = JsonConvert.SerializeObject(_setting);
        File.WriteAllText(Path, _serializedObject);
        Console.WriteLine($"File saved at {Path}");
    }

    private static bool LoadSetting(out IPAddress _ip, out int _port)
    {
        if (File.Exists(Path))
        {
            Console.WriteLine($"File loaded at {Path}");

            string _setting = File.ReadAllText(Path);

            ClientSetting _clientSetting = JsonConvert.DeserializeObject<ClientSetting>(_setting);
            _ip   = IPAddress.Parse(_clientSetting.ip);
            _port = _clientSetting.port;

            return true;
        }

        _ip   = IPAddress.None;
        _port = 0;
        return false;
    }

    private struct ClientSetting
    {
        public string ip;
        public int    port;

        public ClientSetting(string _ip, int _port)
        {
            port = _port;
            ip   = _ip;
        }

        public ClientSetting(IPAddress _ip, int _port)
        {
            port = _port;
            ip   = _ip.ToString();
        }
    }
}