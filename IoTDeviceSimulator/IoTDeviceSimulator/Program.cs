using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace IoTDeviceSimulator
{
    class Program
    {
        private static DeviceClient s_deviceClient;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private readonly static string s_connectionString = "======Insert Your IoT Hub Device Connection String====";

        // Async method to send simulated telemetry
        private static async void SendDeviceToCloudMessagesAsync()
        {
            // Initial telemetry values
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();
            

            while (true)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                // Create JSON message
                var telemetryDataPoint = new
                {
                    SensorID = "HTU21D",
                    ObjectType = "SensorTagEvent",
                    Version = "1.0",
                    TargetAlarmDevice = "=====Insert Your Device ID=====",
                    Temperature = currentTemperature,
                    Humidity = currentHumidity
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                
                // Send the telemetry message
                await s_deviceClient.SendEventAsync(message);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                Console.ForegroundColor = ConsoleColor.White;
                await Task.Delay(1000);
            }
        }
        public static void Main()
        {
            Console.WriteLine("IoT Hub Quickstarts #1 - Simulated device. Ctrl-C to exit.\n");
            
            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);
            SendDeviceToCloudMessagesAsync();
            ReceiveDataFromAzure();
            Console.ReadLine();
        }
        public static async void ReceiveDataFromAzure()
        {
            Message receivedMessage;
            string messageData;
            
            while (true)
            {
                receivedMessage = await s_deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(messageData);
                    Console.ForegroundColor = ConsoleColor.White;
                    await s_deviceClient.CompleteAsync(receivedMessage);
                }
            }
        }
    }
}
