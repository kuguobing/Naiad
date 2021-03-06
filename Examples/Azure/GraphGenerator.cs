﻿/*
 * Naiad ver. 0.3
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0 
 *
 * THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR
 * CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT
 * LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR
 * A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
 *
 * See the Apache Version 2.0 License for specific language governing
 * permissions and limitations under the License.
 */

using Microsoft.Research.Naiad;
using Microsoft.Research.Naiad.Dataflow;
using Microsoft.Research.Naiad.Frameworks.Lindi;
using Microsoft.Research.Naiad.Frameworks.Azure;
using Microsoft.Research.Naiad.Dataflow.PartitionBy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.Storage;

namespace Examples.Azure
{
    public struct GraphProperties
    {
        public int NodeCount;
        public int EdgeCount;

        public GraphProperties(int nodeCount, int edgeCount)
        {
            this.NodeCount = nodeCount;
            this.EdgeCount = edgeCount;
        }
    }
    
    class GraphGenerator : Example
    {
        public string Usage
        {
            get { return "containername directoryname nodecount edgecount"; }
        }

        public void Execute(string[] args)
        {
            var containerName = args[1];
            var directoryName = args[2];

            var nodeCount = int.Parse(args[3]);
            var edgeCount = int.Parse(args[4]);

            CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

            var container = storageAccount.CreateCloudBlobClient()
                                          .GetContainerReference(containerName);

            container.CreateIfNotExists();

            // allocate a new controller from command line arguments.
            using (var controller = NewController.FromArgs(ref args))
            {
                // allocate a new graph manager for the computation.
                using (var manager = controller.NewComputation())
                {
                    // create a new input from a constant data source 
                    var source = new ConstantDataSource<GraphProperties>(new GraphProperties(nodeCount, edgeCount));
                    var input = manager.NewInput(source);

                    // generate the graph, partition by edge source, and write to Azure.
                    input.SelectMany(x => GenerateGraph(x.NodeCount, x.EdgeCount))
                         .PartitionBy(x => x.v1)
                         .WriteBinaryToAzureBlobs(container, directoryName + "/edges-{0}");

                    // start job and wait.
                    manager.Activate();
                    manager.Join();
                }

                controller.Join();
            }
        }

        private static IEnumerable<Pair<int, int>> GenerateGraph(int nodeCount, int edgeCount)
        {
            var random = new Random(0);
            for (int i = 0; i < edgeCount; i++)
                yield return new Pair<int, int>(random.Next(nodeCount), random.Next(nodeCount));
        }
    }
}
