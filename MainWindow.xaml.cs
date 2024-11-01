using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;


public class Logger
{
    private readonly List<string> logs = new List<string>();
    private readonly string logFilePath = "city_log.txt";

    public void LogEvent(string message)
    {
        string logEntry = $"{DateTime.Now}: {message}";
        logs.Add(logEntry);
        Console.WriteLine(logEntry); // Вывод в консоль для отладки
        File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
    }

    public IReadOnlyList<string> GetLogs() => logs.AsReadOnly();
}
public interface ICityModule
{
    void Initialize(City city);
    Task UpdateAsync();
}
public class EmergencyServiceModule : ICityModule
{
    private Logger logger;

    public void Initialize(City city)
    {
        logger = city.Logger;
    }

    public async Task UpdateAsync()
    {
        // Логика для управления аварийными службами
        logger.LogEvent("Проверка состояния аварийных служб.");
        await Task.CompletedTask;
    }
}


public class Weather
{
    private readonly Logger logger;
    public string Condition { get; private set; }
    public string TimeOfDay { get; private set; }

    public Weather(Logger logger)
    {
        this.logger = logger;
    }

    public async Task UpdateWeatherAsync()
    {
        var currentHour = DateTime.Now.Hour;
        Condition = (currentHour % 2 == 0) ? "Солнечно" : "Дождь"; // Простая логика погоды
        TimeOfDay = currentHour < 6 ? "Ночь" : (currentHour < 18 ? "День" : "Вечер");

        logger.LogEvent($"Погода изменена на: {Condition}. Время суток: {TimeOfDay}");
        await Task.CompletedTask;
    }
}


public class Lighting
{
    private readonly Logger logger;
    public bool IsStreetLightOn { get; private set; }

    public Lighting(Logger logger)
    {
        this.logger = logger;
    }

    public async Task UpdateLightingAsync(string timeOfDay)
    {
        if (timeOfDay == "Ночь")
        {
            IsStreetLightOn = true;
            logger.LogEvent("Уличное освещение включено.");
        }
        else
        {
            IsStreetLightOn = false;
            logger.LogEvent("Уличное освещение выключено.");
        }
        await Task.CompletedTask;
    }
}

public class Position
{
    public double X { get; set; }
    public double Y { get; set; }

    public Position(double x, double y)
    {
        X = x;
        Y = y;
    }
}
public class Bus
{
    public int Id { get; set; }
    public Position Position { get; set; }

    public Bus(int id, Position position)
    {
        Id = id;
        Position = position;
    }

    public void Move()
    {
        // Пример движения автобуса
        Position.X += 1;
    }
}


public class Transport
{
    private readonly Logger logger;
    private List<Bus> buses;

    public Transport(Logger logger)
    {
        this.logger = logger;
        buses = new List<Bus> { new Bus(1, new Position(100, 75)) };
    }

    public async Task UpdateTransportAsync(Canvas cityMap)
    {
        foreach (var bus in buses)
        {
            bus.Move();
            logger.LogEvent($"Автобус {bus.Id} переместился на позицию {bus.Position.X}, {bus.Position.Y}.");
            // Логика для обновления отображения на Canvas
        }
        await Task.CompletedTask;
    }
}


public class Resident
{
    public string Name { get; set; }
    public bool IsActive { get; private set; }

    public Resident(string name)
    {
        Name = name;
    }

    public async Task UpdateActivity(string timeOfDay, string weatherCondition)
    {
        // Логика активности жителей
        IsActive = (timeOfDay == "День" && weatherCondition == "Солнечно") ||
                   (timeOfDay == "Ночь" && weatherCondition == "Дождь");
        await Task.CompletedTask;
    }
}

    

public class City
{
    public readonly Logger logger;
    public readonly Lighting lighting;
    public readonly Transport transport;
    public readonly Weather weather;
    public List<Resident> residents;

    public Logger Logger => logger;

    public City()
    {
        logger = new Logger();
        lighting = new Lighting(logger);
        transport = new Transport(logger);
        weather = new Weather(logger);
        residents = new List<Resident> { new Resident("Житель 1"), new Resident("Житель 2") };
    }

    public async Task StartSimulationAsync(Canvas cityMap)
    {
        await weather.UpdateWeatherAsync();
        await lighting.UpdateLightingAsync(weather.TimeOfDay);
        await UpdateResidentsActivity();
        await transport.UpdateTransportAsync(cityMap);
    }

    public async Task UpdateAsync(Canvas cityMap)
    {
        await weather.UpdateWeatherAsync();
        await lighting.UpdateLightingAsync(weather.TimeOfDay);
        await UpdateResidentsActivity();
        await transport.UpdateTransportAsync(cityMap);
    }

    private async Task UpdateResidentsActivity()
    {
        foreach (var resident in residents)
        {
            await resident.UpdateActivity(weather.TimeOfDay, weather.Condition);
            logger.LogEvent($"{resident.Name} активен: {resident.IsActive}");
        }
    }
}


namespace SmartCity
{
    public partial class MainWindow : Window
    {
        private City city;

        public MainWindow()
        {
            InitializeComponent();
            InitializeCity();
            city = new City();
            StartBusAnimation();
        }

        private async void InitializeCity()
        {
            city = new City();
            await city.StartSimulationAsync(CityMap);
        }

        private async void StartBusAnimation()
        {
            while (true)
            {
                // Двигать автобус от первой остановки к второй
                await MoveBus(350, 280, 600, 280);
                // Двигать автобус обратно к первой остановке
                await MoveBus(600, 280, 350, 280);
            }
        }

        private async Task MoveBus(double fromX, double fromY, double toX, double toY)
        {
            // Установка начальной позиции автобуса
            Bus.SetValue(Canvas.LeftProperty, fromX);
            Bus.SetValue(Canvas.TopProperty, fromY);

            // Создание анимации для перемещения
            var moveX = new DoubleAnimation(toX, TimeSpan.FromSeconds(2));
            var moveY = new DoubleAnimation(toY, TimeSpan.FromSeconds(2));

            // Анимация для перемещения по X
            Bus.BeginAnimation(Canvas.LeftProperty, moveX);
            // Анимация для перемещения по Y
            Bus.BeginAnimation(Canvas.TopProperty, moveY);

            // Ждем завершения анимации
            await Task.Delay(2000);
        }

        private async void UpdateCity(object sender, RoutedEventArgs e)
        {
            await city.UpdateAsync(CityMap);
            LightingStatus.Text = city.lighting.IsStreetLightOn ? "Освещение: Включено" : "Освещение: Выключено";
            WeatherStatus.Text = "Погода: " + city.weather.Condition;

            // Обновим отображение активности жителей
            ResidentsActivity.Text = "Активность жителей:\n";
            foreach (var resident in city.residents)
            {
                ResidentsActivity.Text += $"{resident.Name} активен: {(resident.IsActive ? "Да" : "Нет")}\n";
            }
        }
    }



}




