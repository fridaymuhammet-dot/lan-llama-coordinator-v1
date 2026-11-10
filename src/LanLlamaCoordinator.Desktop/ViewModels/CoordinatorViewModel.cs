using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LanLlamaCoordinator.Core.Models;
using LanLlamaCoordinator.Core.Services;

namespace LanLlamaCoordinator.Desktop.ViewModels;

public sealed class CoordinatorViewModel : INotifyPropertyChanged
{
    private readonly DeviceRegistryService _deviceRegistry = new();
    private DeviceInfo? _selectedDevice;
    private string _deviceName = string.Empty;
    private string _selectedDeviceType = "Laptop";
    private string _selectedNetworkType = "Ethernet";
    private string _message = "Henüz cihaz eklenmedi. Eklenen cihazlar eşleştirme yapılana kadar çevrimdışı ve güvenilmeyen durumdadır.";

    public CoordinatorViewModel()
    {
        DeviceTypes = new ObservableCollection<string>(Enum.GetNames<DeviceType>());
        NetworkTypes = new ObservableCollection<string>(Enum.GetNames<NetworkType>());
        AddDeviceCommand = new RelayCommand(AddDevice);
        RemoveDeviceCommand = new RelayCommand(RemoveSelectedDevice);
    }

    public ObservableCollection<DeviceInfo> Devices { get; } = new();
    public ObservableCollection<string> DeviceTypes { get; }
    public ObservableCollection<string> NetworkTypes { get; }
    public ICommand AddDeviceCommand { get; }
    public ICommand RemoveDeviceCommand { get; }

    public int DeviceCount => Devices.Count;

    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value);
    }

    public string SelectedDeviceType
    {
        get => _selectedDeviceType;
        set => SetProperty(ref _selectedDeviceType, value);
    }

    public string SelectedNetworkType
    {
        get => _selectedNetworkType;
        set => SetProperty(ref _selectedNetworkType, value);
    }

    public DeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }

    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void AddDevice()
    {
        var name = DeviceName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Message = "Cihaz eklemek için bir ad girin.";
            return;
        }

        if (!Enum.TryParse<DeviceType>(SelectedDeviceType, out var type) ||
            !Enum.TryParse<NetworkType>(SelectedNetworkType, out var network))
        {
            Message = "Cihaz türü veya bağlantı türü geçersiz.";
            return;
        }

        var device = new DeviceInfo
        {
            DeviceId = Guid.NewGuid().ToString("N"),
            Name = name,
            Type = type,
            Platform = type == DeviceType.Phone ? "android" : "windows",
            Network = network,
            Status = DeviceStatus.Offline,
            Trusted = false,
            LastSeenUtc = DateTimeOffset.UtcNow
        };

        _deviceRegistry.Register(device);
        Devices.Add(device);
        SelectedDevice = device;
        DeviceName = string.Empty;
        Message = $"{name} eklendi. Worker bağlantısı ve güvenli eşleştirme henüz uygulanmadı.";
        OnPropertyChanged(nameof(DeviceCount));
    }

    private void RemoveSelectedDevice()
    {
        if (SelectedDevice is null)
        {
            Message = "Kaldırmak için listeden bir cihaz seçin.";
            return;
        }

        var name = SelectedDevice.Name;
        _deviceRegistry.Remove(SelectedDevice.DeviceId);
        Devices.Remove(SelectedDevice);
        SelectedDevice = null;
        Message = $"{name} yerel listeden kaldırıldı. Bu prototip bağlı worker'a komut göndermez.";
        OnPropertyChanged(nameof(DeviceCount));
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class RelayCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute();
}
