using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;

namespace eventtodevice
{
    [DataContract]
    class SensorEvent
    {
        [DataMember]
        public string timestart { get; set; }
        [DataMember]
        public string sensorid { get; set; }
        [DataMember]
        public string alerttype { get; set; }
        [DataMember]
        public string message { get; set; }
        [DataMember]
        public string targetalarmdevice { get; set; }
        [DataMember]
        public string temperature { get; set; }
    }
    public class eventtodevice
    {
        public static ServiceClient iotHubServiceClient;

        [FunctionName("eventtodevice")]
        public static async Task Run([EventHubTrigger("=====Insert your Event Hub name=====", Connection = "EventHubConnectionAppSetting")] EventData[] events, ILogger log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            iotHubServiceClient = ServiceClient.CreateFromConnectionString(config["IoTHubConnectionAppSetting"]);

            var exceptions = new List<Exception>();
            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");

                    SensorEvent newSensorEvent = JsonConvert.DeserializeObject<SensorEvent>(messageBody);
                    log.LogInformation(string.Format("-->Serialized Data: '{0}', '{1}', '{2}', '{3}', '{4}', '{5}'",
                        newSensorEvent.timestart, newSensorEvent.sensorid, newSensorEvent.alerttype, newSensorEvent.message, newSensorEvent.targetalarmdevice, newSensorEvent.temperature));

                    // Issuing alarm to device.
                    string commandParameterNew = "{\"Name\":\"AlarmThreshold\",\"Parameters\":{\"SensorId\":\"" + newSensorEvent.sensorid + "\"}}";
                    log.LogInformation("Issuing alarm to device: '{0}', from sensor: '{1}'", newSensorEvent.targetalarmdevice, newSensorEvent.sensorid);
                    log.LogInformation("New Command Parameter: '{0}'", commandParameterNew);
                    await iotHubServiceClient.SendAsync(newSensorEvent.targetalarmdevice, new Message(Encoding.UTF8.GetBytes(commandParameterNew)));
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
