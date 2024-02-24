namespace PlayerDatabase
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = new ApplicationBuilder();
            var application = builder.Build();

            application.Run();
        }
    }

    class Player
    {
        public Player(Guid id, string name, bool inBan, int level)
        {
            Id = id;
            Name = name;
            InBanList = inBan;
            Level = level;
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; }

        public bool InBanList { get; private set; }

        public int Level { get; private set; }

        public void Ban()
        {
            InBanList = true;
        }

        public void Unban()
        {
            InBanList = false;
        }
    }

    class PlayerPrinter
    {
        private const string OutputFormat = "{0, 4} | {1, -16} | {2, 5} | {3, 6}";

        public static void Print(IReadOnlyDictionary<int, Player> players)
        {
            PrintHeaders();

            foreach (var pair in players)
            {
                var player = pair.Value;
                Console.WriteLine(OutputFormat, pair.Key, player.Name, player.Level, player.InBanList);
            }
        }

        private static void PrintHeaders()
        {
            Console.WriteLine("Список игроков\n");
            Console.WriteLine(OutputFormat, "No", "Name", "Level", "Banned");
        }
    }

    interface ICommand
    {
        string Description { get; }

        void Execute();
    }

    class Command : ICommand
    {
        private readonly Action _action;

        public Command(string description, Action action)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException($"{nameof(description)} can't be null");
            }

            Description = description;
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public string Description { get; }

        public void Execute()
        {
            _action();
        }
    }

    class Shell
    {
        private readonly Dictionary<int, ICommand> _commands = new Dictionary<int, ICommand>();

        public Shell(Dictionary<int, ICommand> commands)
        {
            if (commands == null || commands.Count == 0)
            {
                throw new ArgumentException(nameof(commands));
            }

            _commands = commands;
        }

        public IReadOnlyDictionary<int, ICommand> Commands => _commands;

        public void ExecuteCommandByIndex(int index)
        {
            if (_commands.TryGetValue(index, out var command))
            {
                command.Execute();
            }
            else
            {
                Console.WriteLine($"Команда под индексом {index} не найдена");
            }
        }

        public void PrintCommandsInfo()
        {
            Console.WriteLine("Список доступных команд");

            foreach (var command in _commands)
            {
                Console.WriteLine($"{command.Key}  {command.Value.Description}");
            }
        }
    }

    class Application
    {
        public Application(IDatabase database, Shell shell)
        {
            Database = database;
            Shell = shell;
        }

        public IDatabase Database { get; }

        public Shell Shell { get; }

        public void Run()
        {
            while(true)
            {
                Shell.PrintCommandsInfo();
                Console.WriteLine();
                int commandNumber = InputCommandNumber();
                Shell.ExecuteCommandByIndex(commandNumber);

                Console.WriteLine();
                Console.WriteLine("Нажмите любую кнопку для продолжения");
                Console.ReadLine();
                Console.Clear();
            }
        }

        private static int InputCommandNumber()
        {
            int commandNumber = ConsoleUtils.ReadInteger("Введите номер команды");
            Console.WriteLine();
            return commandNumber;
        }
    }

    class ApplicationBuilder
    {
        private readonly ShellFactory _shellFactory = new ShellFactory();

        public Application Build()
        {
            var database = CreateDatabase();
            return new Application(
                database,
                CreateShell(database));
        }

        private IDatabase CreateDatabase() 
        {
            return new Database();
        }

        private Shell CreateShell(IDatabase database)
        {
            return _shellFactory.Create(database);
        }
    }

    class ShellFactory
    {
        public Shell Create(IDatabase database)
        {
            Dictionary<int, ICommand> comands = new Dictionary<int, ICommand>()
            {
                [1] = CreateAddCommand(database),
                [2] = CreateRemoveCommand(database),
                [3] = CreateBanCommand(database),
                [4] = CreateUnbanCommand(database),
                [5] = CreateShowCommand(database),
                [0] = CreateExitCommand(database)
            };

            return new Shell(comands);
        }

        #region Add

        private ICommand CreateAddCommand(IDatabase database)
        {
            return new Command(
                "Добавление нового игрока в базу",
                () => database.AddPlayer(InputPlayer()));
        }

        private static Player InputPlayer()
        {
            var id = Guid.NewGuid();
            var name = ConsoleUtils.ReadString("Введите имя игрока:");
            var inBan = ConsoleUtils.ReadBool("Игрок в бане или нет.");
            var level = ConsoleUtils.ReadInteger("Введите уровень игрока:");

            return new Player(id, name, inBan, level);
        }

        #endregion Add

        private ICommand CreateRemoveCommand(IDatabase database)
        {
            return new Command(
                "Удаление игрока из базы",
                () => database.RemovePlayer(
                    InputPlayerId()));
        }

        private ICommand CreateBanCommand(IDatabase database)
        {
            return new Command(
                "Забанить игрока",
                () => database.Ban(
                    InputPlayerId()));
        }

        private ICommand CreateUnbanCommand(IDatabase database)
        {
            return new Command(
                "Разбанить игрока",
                () => database.Unban(
                    InputPlayerId()));
        }

        private ICommand CreateShowCommand(IDatabase database)
        {
            return new Command(
                "Показать список игроков",
                () => PlayerPrinter.Print(database.Players));
        }

        private ICommand CreateExitCommand(IDatabase database)
        {
            return new Command(
                "Выход из приложения",
                () => Environment.Exit(0));
        }

        private static int InputPlayerId()
        {
            return ConsoleUtils.ReadInteger("Введите id игрока:");
        }
    }

    interface IDatabase
    {
        public IReadOnlyDictionary<int, Player> Players { get; }

        public bool AddPlayer(Player player);

        public bool RemovePlayer(int playerId);

        public bool Ban(int playerId);

        public bool Unban(int playerId);
    }

    class Database : IDatabase
    {
        private readonly Dictionary<int, Player> _players = new Dictionary<int, Player>();

        private int _index = 0;

        public Database()
        { 
        }

        public Database(IEnumerable<Player> players)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            _players = players.ToDictionary(key => ++_index);
        }

        public IReadOnlyDictionary<int, Player> Players => _players;

        public bool AddPlayer(Player player)
        {
            if (player is null)
            {
                return false;
            }

            _index++;
            _players.Add(_index, player);

            return true;
        }

        public bool RemovePlayer(int playerId)
        {
            if (!_players.ContainsKey(playerId))
            {
                return false;
            }

            _players.Remove(playerId);
            return true;
        }

        public bool Ban(int playerId)
        {
            if (!_players.ContainsKey(playerId))
            {
                return false;
            }

            _players[playerId].Ban();
            return true;
        }

        public bool Unban(int playerId)
        {
            if (!_players.ContainsKey(playerId))
            {
                return false;
            }

            _players[playerId].Unban();
            return true;
        }
    }

    static class ConsoleUtils
    {
        public static bool ReadBool(string message)
        {
            int[] allowedValues = { 0, 1};
            string allowedValuesString = string.Join(", ", allowedValues);

            bool parsed = false;
            bool value = false;
            string input = string.Empty;

            while (!parsed)
            {
                Console.WriteLine(message);
                Console.WriteLine($"Введите {allowedValuesString}.");
                input = Console.ReadLine();
                var intValue = 0;
                parsed = int.TryParse(input, out intValue);

                if (!parsed || !allowedValues.Contains(intValue))
                {
                    Console.WriteLine($"Неудачный ввод данных. Это должно быть число из {allowedValuesString}.");
                    parsed = false;
                }
            }

            return value;
        }

        public static int ReadInteger(string message)
        {
            bool parsed = false;
            int value = 0;
            string input = string.Empty;

            while (!parsed)
            {
                Console.WriteLine(message);
                input = Console.ReadLine();
                parsed = int.TryParse(input, out value);

                if (!parsed)
                {
                    Console.WriteLine("Неудачный ввод данных. Это должно быть число.");
                }
            }

            return value;
        }

        public static string ReadString(string message)
        {
            bool correctInput = false;
            string input = string.Empty;

            while (!correctInput)
            {
                Console.WriteLine(message);
                input = Console.ReadLine();
                correctInput = ! string.IsNullOrWhiteSpace(input);

                if (!correctInput)
                {
                    Console.WriteLine("Неудачные ввод данных. Строка не должна быть пустой.");
                }
            }

            return input;
        }
    }
}
