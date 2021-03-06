﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System.Configuration;

/// <summary>
/// This program shows the ability to create a SCP.NET topology consuming JAVA Spouts
/// For how to use SCP.NET, please refer to: http://go.microsoft.com/fwlink/?LinkID=525500&clcid=0x409
/// For more Storm samples, please refer to our GitHub repository: http://go.microsoft.com/fwlink/?LinkID=525495&clcid=0x409
/// </summary>

namespace EventHubReader
{
    /// <summary>
    /// TopologyBuilder hybrid topology example with Java Spout and CSharp Bolt
    /// This TopologyDescriptor is marked as Active
    /// </summary>
    [Active(true)]
    public class EventHubReader : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            // Start building a new topology
            TopologyBuilder topologyBuilder = new TopologyBuilder(typeof(EventHubReader).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));
            // Get the number of partitions in EventHub
            var eventHubPartitions = int.Parse(ConfigurationManager.AppSettings["EventHubPartitions"]);
            // Add the EvetnHubSpout to the topology using the SetEventHubSpout and EventHubSpoutConfig helper methods.
            // NOTE: These methods set the spout to read data in a String encoding.
            /*
            topologyBuilder.SetEventHubSpout(
                "EventHubSpout",
                new EventHubSpoutConfig(
                    ConfigurationManager.AppSettings["EventHubSharedAccessKeyName"],
                    ConfigurationManager.AppSettings["EventHubSharedAccessKey"],
                    ConfigurationManager.AppSettings["EventHubNamespace"],
                    ConfigurationManager.AppSettings["EventHubEntityPath"],
                    eventHubPartitions),
                eventHubPartitions);
                */
            // The following is an example of how to create the same spout using the JavaComponentConstructor,
            // which allows us to use UTF-8 encoding for reads.
            // NOTE!!!! This only works with the 9.5 version of the Event Hub components, which are located at
            // https://github.com/hdinsight/hdinsight-storm-examples/blob/master/lib/eventhubs/
            // Create the UTF-8 data scheme
            var schemeConstructor = new JavaComponentConstructor("com.microsoft.eventhubs.spout.UnicodeEventDataScheme");
            // Create the EventHubSpoutConfig
            var eventHubSpoutConfig = new JavaComponentConstructor(
                "com.microsoft.eventhubs.spout.EventHubSpoutConfig",
                new List<Tuple<string, object>>()
                {
                    //comment
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, ConfigurationManager.AppSettings["EventHubSharedAccessKeyName"]),
                    //comment
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, ConfigurationManager.AppSettings["EventHubSharedAccessKey"]),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, ConfigurationManager.AppSettings["EventHubNamespace"]),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, ConfigurationManager.AppSettings["EventHubEntityPath"]),
                    Tuple.Create<string, object>("int", eventHubPartitions),
                    Tuple.Create<string, object>("com.microsoft.eventhubs.spout.IEventDataScheme", schemeConstructor)
                }
               );
            // Create the spout
            var eventHubSpout = new JavaComponentConstructor(
                "com.microsoft.eventhubs.spout.EventHubSpout",
                new List<Tuple<string, object>>()
                {
                    Tuple.Create<string, object>("com.microsoft.eventhubs.spout.EventHubSpoutConfig", eventHubSpoutConfig)
                }
               );
            // Set the spout in the topology
            topologyBuilder.SetJavaSpout("EventHubSpout", eventHubSpout, eventHubPartitions);  
            

            // Set a customized JSON Serializer to serialize a Java object (emitted by Java Spout) into JSON string
            // Here, full name of the Java JSON Serializer class is required
            List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };

            // Create a config for the bolt. It's unused here
            var boltConfig = new StormConfig();

            // Add the logbolt to the topology
            // Use a serializer to understand data from the Java component
            topologyBuilder.SetBolt(
                typeof(LogBolt).Name,
                LogBolt.Get,
                new Dictionary<string, List<string>>(),
                eventHubPartitions,
                true
                ).
                DeclareCustomizedJavaSerializer(javaSerializerInfo).
                shuffleGrouping("EventHubSpout");

            // Create a configuration for the topology
            var topologyConfig = new StormConfig();
            // Increase max pending for the spout
            topologyConfig.setMaxSpoutPending(8192);
            // Parallelism hint for the number of workers to match the number of EventHub partitions
            topologyConfig.setNumWorkers(eventHubPartitions);
            // Add the config and return the topology builder
            topologyBuilder.SetTopologyConfig(topologyConfig);
            return topologyBuilder;
        }
    }
}
