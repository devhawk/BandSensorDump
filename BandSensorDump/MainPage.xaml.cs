using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Band;
using Microsoft.Band.Sensors;
using Newtonsoft.Json;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.ApplicationModel.Email;
using Windows.Storage.Streams;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace BandSensorDump
{
    struct AccelerometerReading
    {
        public double AccelerationX;
        public double AccelerationY;
        public double AccelerationZ;
        public DateTimeOffset Timestamp;

        public static AccelerometerReading FromBandSensor(IBandAccelerometerReading reading)
        {
            return new AccelerometerReading
            {
                AccelerationX = reading.AccelerationX,
                AccelerationY = reading.AccelerationY,
                AccelerationZ = reading.AccelerationZ,
                Timestamp = reading.Timestamp
            };
        }
    }

    struct CaloriesReading
    {
        public long Calories;
        public DateTimeOffset Timestamp;

        public static CaloriesReading FromBandSensor(IBandCaloriesReading reading)
        {
            return new CaloriesReading
            {
                Calories = reading.Calories,
                Timestamp = reading.Timestamp
            };
        }
    }

    struct DistanceReading
    {
        public MotionType CurrentMotion;
        public double Pace;
        public double Speed;
        public double TotalDistance;
        public DateTimeOffset Timestamp;

        public static DistanceReading FromBandSensor(IBandDistanceReading reading)
        {
            return new DistanceReading
            {
                CurrentMotion = reading.CurrentMotion,
                Pace = reading.Pace,
                Speed = reading.Speed,
                TotalDistance = reading.TotalDistance,
                Timestamp = reading.Timestamp
            };
        }
    }

    struct GyrosocpeReading
    {
        public double AngularVelocityX;
        public double AngularVelocityY;
        public double AngularVelocityZ;
        public DateTimeOffset Timestamp;

        public static GyrosocpeReading FromBandSensor(IBandGyroscopeReading reading)
        {
            return new GyrosocpeReading
            {
                AngularVelocityX = reading.AngularVelocityX,
                AngularVelocityY = reading.AngularVelocityY,
                AngularVelocityZ = reading.AngularVelocityZ,
                Timestamp = reading.Timestamp
            };
        }
    }

    struct HeartRateReading
    {
        public int HeartRate;
        public HeartRateQuality Quality;
        public DateTimeOffset Timestamp;

        public static HeartRateReading FromBandSensor(IBandHeartRateReading reading)
        {
            return new HeartRateReading
            {
                HeartRate = reading.HeartRate,
                Quality = reading.Quality,
                Timestamp = reading.Timestamp
            };
        }
    }

    struct SkinTempReading
    {
        public double Temperature;
        public DateTimeOffset Timestamp;

        public static SkinTempReading FromBandSensor(IBandSkinTemperatureReading reading)
        {
            return new SkinTempReading
            {
                Temperature = reading.Temperature,
                Timestamp = reading.Timestamp
            };
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private async Task RunOnUiThread(Action action)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => action());
        }

        private async Task UpdateStatusAsync(string msg)
        {
            await RunOnUiThread(() => tbStatus.Text = msg);
        }

        IBandInfo _bandInfo;

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            btnClickMe.Content = "Start Collection";
            btnClickMe.IsEnabled = false;
            tbStatus.Text = "Getting band info";
            IBandInfo[] bands = await BandClientManager.Instance.GetBandsAsync();
            if (bands.Length == 0)
            {
                await UpdateStatusAsync("no bands found");
                return;
            }

            _bandInfo = bands.First();
            await RunOnUiThread(() =>
            {
                tbStatus.Text = $"Using Band {_bandInfo.Name}";
                btnClickMe.IsEnabled = true;
            });
        }

        IBandClient _band;

        IList<AccelerometerReading> _accelerometerData;
        IList<CaloriesReading> _calorieData;
        IList<DistanceReading> _distanceData;
        IList<GyrosocpeReading> _gyroscopeData;
        IList<HeartRateReading> _heartRateData;
        IList<SkinTempReading> _skinTempData;

        void InitDataLists()
        {
            _accelerometerData = new List<AccelerometerReading>();
            _calorieData = new List<CaloriesReading>();
            _distanceData = new List<DistanceReading>();
            _gyroscopeData = new List<GyrosocpeReading>();
            _heartRateData = new List<HeartRateReading>();
            _skinTempData = new List<SkinTempReading>();
        }

        async Task StartAllSensorsAsync()
        {
            IBandSensorManager sensorMgr = _band.SensorManager;

            var t1 = sensorMgr.Accelerometer.StartReadingsAsync();
            var t2 = sensorMgr.Calories.StartReadingsAsync();
            var t3 = sensorMgr.Distance.StartReadingsAsync();
            var t4 = sensorMgr.Gyroscope.StartReadingsAsync();
            var t5 = sensorMgr.HeartRate.StartReadingsAsync();
            var t6 = sensorMgr.SkinTemperature.StartReadingsAsync();

            await Task.WhenAll(t1, t2, t3, t4, t5, t6);
        }

        async Task StopAllSensorsAsync()
        {
            IBandSensorManager sensorMgr = _band.SensorManager;

            var t1 = sensorMgr.Accelerometer.StopReadingsAsync();
            var t2 = sensorMgr.Calories.StopReadingsAsync();
            var t3 = sensorMgr.Distance.StopReadingsAsync();
            var t4 = sensorMgr.Gyroscope.StopReadingsAsync();
            var t5 = sensorMgr.HeartRate.StopReadingsAsync();
            var t6 = sensorMgr.SkinTemperature.StopReadingsAsync();

            await Task.WhenAll(t1, t2, t3, t4, t5, t6);
        }

        void AddEventHandlers()
        {
            IBandSensorManager sensorMgr = _band.SensorManager;

            sensorMgr.Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            sensorMgr.Calories.ReadingChanged += Calories_ReadingChanged;
            sensorMgr.Distance.ReadingChanged += Distance_ReadingChanged;
            sensorMgr.Gyroscope.ReadingChanged += Gyroscope_ReadingChanged;
            sensorMgr.HeartRate.ReadingChanged += HeartRate_ReadingChanged;
            sensorMgr.SkinTemperature.ReadingChanged += SkinTemperature_ReadingChanged; 
        }

        void RemoveEventHandlers()
        {
            IBandSensorManager sensorMgr = _band.SensorManager;

            sensorMgr.Accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
            sensorMgr.Calories.ReadingChanged -= Calories_ReadingChanged;
            sensorMgr.Distance.ReadingChanged -= Distance_ReadingChanged;
            sensorMgr.Gyroscope.ReadingChanged -= Gyroscope_ReadingChanged;
            sensorMgr.HeartRate.ReadingChanged -= HeartRate_ReadingChanged;
            sensorMgr.SkinTemperature.ReadingChanged -= SkinTemperature_ReadingChanged;
        }

        #region Event Handlers
        private void SkinTemperature_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandSkinTemperatureReading> e)
        {
            _skinTempData.Add(SkinTempReading.FromBandSensor(e.SensorReading));
        }

        private void HeartRate_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandHeartRateReading> e)
        {
            _heartRateData.Add(HeartRateReading.FromBandSensor(e.SensorReading));
        }

        private void Gyroscope_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandGyroscopeReading> e)
        {
            _gyroscopeData.Add(GyrosocpeReading.FromBandSensor(e.SensorReading));
        }

        private void Distance_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandDistanceReading> e)
        {
            _distanceData.Add(DistanceReading.FromBandSensor(e.SensorReading));
        }

        private void Calories_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandCaloriesReading> e)
        {
            _calorieData.Add(CaloriesReading.FromBandSensor(e.SensorReading));
        }

        private void Accelerometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandAccelerometerReading> e)
        {
            _accelerometerData.Add(AccelerometerReading.FromBandSensor(e.SensorReading));
        }
        #endregion

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_band == null)
            {
                await StartDataCollectionAsync();
            }
            else
            {
                await CompleteDataCollectionAsync();
            }
        }

        async Task StartDataCollectionAsync()
        {
            if (_bandInfo == null)
                throw new Exception("Can't connect w/o _bandInfo set");

            if (_band != null)
                throw new Exception("Expected _band to be null when starting data collection");

            cbExersize.IsEnabled = false;
            btnClickMe.IsEnabled = false;

            await UpdateStatusAsync($"Connecting to Band {_bandInfo.Name}");
            _band = await BandClientManager.Instance.ConnectAsync(_bandInfo);

            await UpdateStatusAsync($"Band {_bandInfo.Name} connected");

            InitDataLists();
            AddEventHandlers();

            await UpdateStatusAsync($"Collecting sensor data for {_bandInfo.Name}");
            await StartAllSensorsAsync();

            btnClickMe.Content = "Stop Collection";
            btnClickMe.IsEnabled = true;
        }

        async Task CompleteDataCollectionAsync()
        {
            await StopAllSensorsAsync();
            await UpdateStatusAsync($"Completed sensor data collection");

            RemoveEventHandlers();
            _band.Dispose();
            _band = null;

            string exercise = null;
            await RunOnUiThread(() => exercise = (cbExersize.SelectedItem as ComboBoxItem)?.Content.ToString());
            await UpdateStatusAsync($"Collected {_accelerometerData.Count} accelerometer readings");

            await SaveDataAsync(exercise);

            cbExersize.IsEnabled = true;
            btnClickMe.Content = "Start Collection";
            btnClickMe.IsEnabled = true;
        }

        List<StorageFile> _files = new List<StorageFile>();

        async Task SaveDataAsync(string exercise)
        {
            exercise = string.IsNullOrEmpty(exercise) ? "Unknown" : exercise;
            var filename = $"{exercise}-{DateTimeOffset.Now.ToString("yyyyMMdd")}.json";

            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);

            using (var stream = await file.OpenStreamForWriteAsync())
            using (var sw = new StreamWriter(stream, System.Text.Encoding.UTF8))
            using (var jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("Exercise");
                jsonWriter.WriteValue(exercise ?? string.Empty);

                jsonWriter.WritePropertyName("Accelerometer");
                jsonWriter.WriteStartArray();
                foreach (var a in _accelerometerData)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("AccelerationX");
                    jsonWriter.WriteValue(a.AccelerationX);
                    jsonWriter.WritePropertyName("AccelerationY");
                    jsonWriter.WriteValue(a.AccelerationY);
                    jsonWriter.WritePropertyName("AccelerationZ");
                    jsonWriter.WriteValue(a.AccelerationZ);
                    jsonWriter.WritePropertyName("Timestamp");
                    jsonWriter.WriteValue(a.Timestamp.Ticks);
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WritePropertyName("Calories");
                jsonWriter.WriteStartArray();
                foreach (var a in _calorieData)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("Calories");
                    jsonWriter.WriteValue(a.Calories);
                    jsonWriter.WritePropertyName("Timestamp");
                    jsonWriter.WriteValue(a.Timestamp.Ticks);
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WritePropertyName("Distance");
                jsonWriter.WriteStartArray();
                foreach (var a in _distanceData)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("CurrentMotion");
                    jsonWriter.WriteValue(a.CurrentMotion);
                    jsonWriter.WritePropertyName("TotalDisatnce");
                    jsonWriter.WriteValue(a.TotalDistance);
                    jsonWriter.WritePropertyName("Speed");
                    jsonWriter.WriteValue(a.Speed);
                    jsonWriter.WritePropertyName("Pace");
                    jsonWriter.WriteValue(a.Pace);
                    jsonWriter.WritePropertyName("Timestamp");
                    jsonWriter.WriteValue(a.Timestamp.Ticks);
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WritePropertyName("Gyroscope");
                jsonWriter.WriteStartArray();
                foreach (var a in _gyroscopeData)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("AngularVelocityX");
                    jsonWriter.WriteValue(a.AngularVelocityX);
                    jsonWriter.WritePropertyName("AngularVelocityY");
                    jsonWriter.WriteValue(a.AngularVelocityY);
                    jsonWriter.WritePropertyName("AngularVelocityZ");
                    jsonWriter.WriteValue(a.AngularVelocityZ);
                    jsonWriter.WritePropertyName("Timestamp");
                    jsonWriter.WriteValue(a.Timestamp.Ticks);
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WritePropertyName("HeartRate");
                jsonWriter.WriteStartArray();
                foreach (var a in _heartRateData)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("HeartRate");
                    jsonWriter.WriteValue(a.HeartRate);
                    jsonWriter.WritePropertyName("Quality");
                    jsonWriter.WriteValue(a.Quality);
                    jsonWriter.WritePropertyName("Timestamp");
                    jsonWriter.WriteValue(a.Timestamp.Ticks);
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WritePropertyName("SkinTemperature");
                jsonWriter.WriteStartArray();
                foreach (var a in _skinTempData)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("Temperature");
                    jsonWriter.WriteValue(a.Temperature);
                    jsonWriter.WritePropertyName("Timestamp");
                    jsonWriter.WriteValue(a.Timestamp.Ticks);
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteEndObject();
            }

            _files.Add(file);
            lbFiles.Items.Add(file.Name);
        }

        private async void SendReport_Click(object sender, RoutedEventArgs e)
        {
            await SendReportAsync();
        }

        async Task SendReportAsync()
        {
            var attachments = _files.Select(f =>
            {
                var rasr = RandomAccessStreamReference.CreateFromFile(f);
                return new EmailAttachment(f.Name, rasr);
            });

            var em = new EmailMessage();
            em.To.Add(new EmailRecipient("harrypierson@outlook.com"));
            em.Subject = $"{DateTimeOffset.Now.ToString("d")} Workout";
            foreach (var a in attachments)
            {
                em.Attachments.Add(a);
            }

            await EmailManager.ShowComposeNewEmailAsync(em);
        }
    }
}

