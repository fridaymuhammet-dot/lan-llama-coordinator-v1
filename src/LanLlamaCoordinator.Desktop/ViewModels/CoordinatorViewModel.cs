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
    private readonly DevicePersistenceService _persistence = new();
    private readonly TaskQueueService _taskQueue = new();
    private readonly TaskLifecycleService _taskLifecycle;
    private readonly string _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LanLlamaCoordinator", "devices.json");
    private DeviceInfo? _selectedDevice;
    private TaskRecord? _selectedTask;
    private string _deviceName = string.Empty;
    private string _selectedDeviceType = "Laptop";
    private string _selectedNetworkType = "Ethernet";
    private string _message = "Uygulama çalışıyor. Cihazları ekleyebilir, iş kuyruğunu izleyebilirsiniz.";

    public CoordinatorViewModel()
    {
        _taskLifecycle = new TaskLifecycleService(_taskQueue);

        DeviceTypes = new ObservableCollection<string>(Enum.GetNames<DeviceType>());
        NetworkTypes = new ObservableCollection<string>(Enum.GetNames<NetworkType>());
        AddDeviceCommand = new RelayCommand(AddDevice);
        RemoveDeviceCommand = new RelayCommand(RemoveSelectedDevice);
        EnqueueSampleTaskCommand = new RelayCommand(EnqueueSampleTask);
        AssignSelectedTaskCommand = new RelayCommand(AssignSelectedTask);
        CompleteSelectedTaskCommand = new RelayCommand(CompleteSelectedTask);
        RetrySelectedTaskCommand = new RelayCommand(RetrySelectedTask);

        var savedDevices = _persistence.Load(_storagePath);
        foreach (var device in savedDevices)
        {
            Devices.Add(device);
            _deviceRegistry.Register(device);
        }

        TaskQueue = new ObservableCollection<TaskRecord>(_taskQueue.GetAll());
    }

    public ObservableCollection<DeviceInfo> Devices { get; } = new();
    public ObservableCollection<string> DeviceTypes { get; }
    public ObservableCollection<string> NetworkTypes { get; }
    public ObservableCollection<TaskRecord> TaskQueue { get; }

    public ICommand AddDeviceCommand { get; }
    public ICommand RemoveDeviceCommand { get; }
    public ICommand EnqueueSampleTaskCommand { get; }
    public ICommand AssignSelectedTaskCommand { get; }
    public ICommand CompleteSelectedTaskCommand { get; }
    public ICommand RetrySelectedTaskCommand { get; }

    public int DeviceCount => Devices.Count;
    public int TaskCount => TaskQueue.Count;

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

    public TaskRecord? SelectedTask
    {
        get => _selectedTask;
        set => SetProperty(ref _selectedTask, value);
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
            LastSeenUtc = DateTimeOffset.UtcNow,
            Capabilities = new DeviceCapability
            {
                SupportsLlamaCpp = true,
                SupportsRpc = true,
                RamGb = 32,
                VramGb = 0,
                MaxConcurrency = 2,
                CpuCores = 8
            }
        };

        _deviceRegistry.Register(device);
        Devices.Add(device);
        _persistence.Save(Devices, _storagePath);

        SelectedDevice = device;
        DeviceName = string.Empty;
        Message = $"{name} eklendi. Kayıt kalıcı hale getirildi.";
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
        _persistence.Save(Devices, _storagePath);

        SelectedDevice = null;
        Message = $"{name} cihaz listeden kaldırıldı.";
        OnPropertyChanged(nameof(DeviceCount));
    }

    private void EnqueueSampleTask()
    {
        var task = new TaskRecord
        {
            TaskId = Guid.NewGuid().ToString("N"),
            ModelName = "qwen2.5-coder-7b",
            Prompt = "Yerel ağ üzerinde uygun worker ile çalışacak örnek istek.",
            Status = TaskStatus.Queued,
            RetryCount = 0,
            MaxRetries = 3
        };

        _taskLifecycle.Enqueue(task);
        TaskQueue.Add(task);

        SelectedTask = task;
        Message = $"Örnek görev kuyruğa eklendi: {task.TaskId}";
        OnPropertyChanged(nameof(TaskCount));
    }

    private void AssignSelectedTask()
    {
        if (SelectedTask is null)
        {
            Message = "Atamak için bir görev seçin.";
            return;
        }

        var deviceId = SelectedDevice?.DeviceId ?? Devices.FirstOrDefault()?.DeviceId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            Message = "Görev için en az bir cihaz seçilmelidir.";
            return;
        }

        _taskLifecycle.Assign(SelectedTask.TaskId, deviceId);
        SelectedTask.Status = TaskStatus.Assigned;
        SelectedTask.AssignedDeviceId = deviceId;
        Message = $"Görev {SelectedTask.TaskId} cihaz {deviceId} için atanmıştır.";
        RefreshTaskQueue();
    }

    private void CompleteSelectedTask()
    {
        if (SelectedTask is null)
        {
            Message = "Tamamlamak için bir görev seçin.";
            return;
        }

        _taskLifecycle.Complete(SelectedTask.TaskId, "Örnek sonuç başarılı şekilde tamamlandı.");
        SelectedTask.Status = TaskStatus.Succeeded;
        Message = $"Görev {SelectedTask.TaskId} tamamlandı.";
        RefreshTaskQueue();
    }

    private void RetrySelectedTask()
    {
        if (SelectedTask is null)
        {
            Message = "Yeniden denemek için bir görev seçin.";
            return;
        }

        var taskId = SelectedTask.TaskId;
        _taskLifecycle.Retry(taskId);
        var updatedTask = _taskQueue.Get(taskId);
        if (updatedTask is null)
        {
            Message = "Görev kuyruğunda bulunamadı.";
            return;
        }

        SelectedTask = updatedTask;
        Message = updatedTask.Status == TaskStatus.Failed
            ? $"Görev {taskId} başarısız oldu: {updatedTask.ResultSummary}"
            : $"Görev {taskId} yeniden deneme için işaretlendi. Deneme {updatedTask.RetryCount}/{updatedTask.MaxRetries}.";
        RefreshTaskQueue();
    }

    private void RefreshTaskQueue()
    {
        TaskQueue.Clear();
        foreach (var task in _taskQueue.GetAll())
        {
            TaskQueue.Add(task);
        }
        OnPropertyChanged(nameof(TaskCount));
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
